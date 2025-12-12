using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class FusionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static FusionManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject escPanel;
    [SerializeField] private GameObject settingsPanel;

    [Serializable]
    private struct ProgressData
    {
        public int lastLevelIndex;
    }

    private const string PREFERRED_SAVE_DIR = "Assets/LobbySystem/SaveData";
    private const string SAVE_FILE_NAME = "player_progress.json";

    private string _fallbackDir;
    private bool _leavingToMenu;

    [Header("Runner & Prefabs")]
    [SerializeField] private NetworkRunner _runner;

    [Header("Character Prefabs (0:Warrior, 1:Mage, 2:Archer, 3:Rogue)")]
    [SerializeField] private NetworkPrefabRef[] characterPrefabs = new NetworkPrefabRef[4];

    [Header("Character Selection")]
    [Tooltip("Seçili karakter indexi (0-3). -1 ise PlayerPrefs'ten okunur.")]
    public int selectedCharacterIndex = 0;

    [Header("Scene Index")]
    public int gameSceneBuildIndex = 1;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawned = new();
    private readonly Dictionary<PlayerRef, int> _playerCharacterIndex = new();

    public event Action<List<SessionInfo>> SessionsUpdated;
    public event Action OnRunnerShutdown;

    private NetworkBool _isSprinting;
    private NetworkBool _isJumping;
    private NetworkBool _isFreeze;

    private Task _lobbyJoinTask;
    public bool LobbyReady { get; private set; }
    public NetworkRunner Runner => _runner;

    [Header("Scripts Prefab")]
    public GameObject scriptsPrefab;

    private const string PWD_HASH_KEY = "pwd_hash";
    private const string PWD_LOCKED_KEY = "pwd_locked";
    private const string SALT = "fusion-demo-salt";

    private bool _sceneReady;

    [Header("Feedbacks")]
    [SerializeField] private MMF_Player PasswordCorrect;
    [SerializeField] private MMF_Player PasswordWrong;

    private string GetPreferredSavePath() => Path.Combine(PREFERRED_SAVE_DIR, SAVE_FILE_NAME);

    private string GetFallbackSavePath()
    {
        if (string.IsNullOrEmpty(_fallbackDir))
            _fallbackDir = Path.Combine(Application.persistentDataPath, "LobbySystem", "SaveData");

        Directory.CreateDirectory(_fallbackDir);
        return Path.Combine(_fallbackDir, SAVE_FILE_NAME);
    }

    private string EnsureWritablePath(out bool usedFallback)
    {
        usedFallback = false;
        try
        {
            Directory.CreateDirectory(PREFERRED_SAVE_DIR);
            var testPath = Path.Combine(PREFERRED_SAVE_DIR, ".write_test");
            File.WriteAllText(testPath, "ok");
            File.Delete(testPath);
            return GetPreferredSavePath();
        }
        catch
        {
            usedFallback = true;
            return GetFallbackSavePath();
        }
    }

    private bool TryLoadProgress(out int lastIndex)
    {
        lastIndex = 1; // default: Level 1
        string[] candidates = { GetPreferredSavePath(), GetFallbackSavePath() };

        foreach (var path in candidates)
        {
            try
            {
                if (!File.Exists(path)) continue;

                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<ProgressData>(json);

                // 0 veya negatif sahneleri GEÇERSİZ SAY
                if (data.lastLevelIndex > 0)
                {
                    lastIndex = data.lastLevelIndex;
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    private void SaveProgress(int buildIndex)
    {
        // Menü sahnesini (0) veya negatif değerleri progress olarak kaydetme
        if (buildIndex <= 0)
        {
            // Debug.Log($"[FusionManager] Menü sahnesi ({buildIndex}) progress olarak kaydedilmedi.");
            return;
        }

        var pd = new ProgressData { lastLevelIndex = buildIndex };
        string json = JsonUtility.ToJson(pd, true);

        var path = EnsureWritablePath(out bool usedFallback);
        try
        {
            File.WriteAllText(path, json, Encoding.UTF8);
            if (usedFallback)
                Debug.LogWarning($"[FusionManager] Assets klasörüne yazılamadı. Kayıt şuraya alındı: {path}");
            else
                Debug.Log($"[FusionManager] İlerleme kaydedildi: {path} (level={buildIndex})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FusionManager] JSON kayıt hatası: {e.Message}");
        }
    }


    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PasswordCorrect = GameObject.Find("PasswordCorrect")?.GetComponent<MMF_Player>();
        PasswordWrong = GameObject.Find("PasswordWrong")?.GetComponent<MMF_Player>();

        if (TryLoadProgress(out int lastIdx))
        {
            gameSceneBuildIndex = lastIdx;
           // Debug.Log($"[FusionManager] Son kayıtlı bölüm: {gameSceneBuildIndex}");
        }
        else
        {
            SaveProgress(gameSceneBuildIndex);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_runner) _runner.RemoveCallbacks(this);
    }

    private static string Hash(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(SALT + plain);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    private void EnsureFreshRunner()
    {
        if (_runner)
        {
            _runner.RemoveCallbacks(this);
            _runner = null;
        }

        var go = new GameObject("NetworkRunner");
        DontDestroyOnLoad(go);
        _runner = go.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);
    }

    private async Task EnsureLobbyReadyAsync()
    {
        if (_runner == null) EnsureFreshRunner();

        if (_lobbyJoinTask == null || _lobbyJoinTask.IsCompleted)
        {
            _lobbyJoinTask = _runner.JoinSessionLobby(SessionLobby.ClientServer);
            LobbyReady = false;
        }

        try
        {
            await _lobbyJoinTask;
            LobbyReady = true;
        }
        catch (Exception e)
        {
            LobbyReady = false;
            Debug.LogError($"JoinSessionLobby failed: {e}");
        }
    }

    public Task JoinLobbyIfNeeded() => EnsureLobbyReadyAsync();

    public void ChangeCharacterIndex(int i)
    {
        selectedCharacterIndex += i;
        if (selectedCharacterIndex <= -1) selectedCharacterIndex = 3;
        else if (selectedCharacterIndex >= 4) selectedCharacterIndex = 0;

        PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
        PlayerPrefs.Save();
    }

    private int GetSelectedCharacterIndex()
    {
        if (selectedCharacterIndex >= 0 &&
            selectedCharacterIndex < characterPrefabs.Length &&
            characterPrefabs[selectedCharacterIndex].IsValid)
            return selectedCharacterIndex;

        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            int index = PlayerPrefs.GetInt("SelectedCharacter");
            if (index >= 0 &&
                index < characterPrefabs.Length &&
                characterPrefabs[index].IsValid)
                return index;
        }
        return 0;
    }

    private NetworkPrefabRef GetPlayerPrefab(int characterIndex)
    {
        if (characterIndex >= 0 &&
            characterIndex < characterPrefabs.Length &&
            characterPrefabs[characterIndex].IsValid)
            return characterPrefabs[characterIndex];

        for (int i = 0; i < characterPrefabs.Length; i++)
            if (characterPrefabs[i].IsValid)
                return characterPrefabs[i];

        Debug.LogError("Hiçbir karakter prefabı tanımlanmamış!");
        return default;
    }

    public void SetCharacterIndex(int index)
    {
        if (index >= 0 && index < characterPrefabs.Length)
        {
            selectedCharacterIndex = index;
            PlayerPrefs.SetInt("SelectedCharacter", index);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning($"Geçersiz karakter indexi: {index}. Geçerli aralık: 0-{characterPrefabs.Length - 1}");
        }
    }

    public int GetCurrentCharacterIndex() => GetSelectedCharacterIndex();

    private byte[] EncodeConnectionToken(string password, int characterIndex)
    {
        string data = $"{password}|{characterIndex}";
        return Encoding.UTF8.GetBytes(data);
    }

    private (string password, int characterIndex) DecodeConnectionToken(byte[] token)
    {
        if (token == null || token.Length == 0)
            return (string.Empty, 0);

        string data = Encoding.UTF8.GetString(token);
        var parts = data.Split('|');

        string password = parts.Length > 0 ? parts[0] : string.Empty;
        int characterIndex = 0;

        if (parts.Length > 1 && int.TryParse(parts[1], out int idx))
            characterIndex = idx;

        return (password, characterIndex);
    }

    public async void Create(string sessionName, int maxPlayers, string password)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
            sessionName = "Room_" + UnityEngine.Random.Range(1000, 9999);

        await EnsureLobbyReadyAsync();

        var props = new Dictionary<string, SessionProperty>
        {
            { PWD_LOCKED_KEY, !string.IsNullOrEmpty(password) },
            { PWD_HASH_KEY, Hash(password) }
        };

        int charIndex = GetSelectedCharacterIndex();
        byte[] token = EncodeConnectionToken(password, charIndex);

        await StartGame(GameMode.Host, sessionName, maxPlayers, props, token);
    }

    public async void JoinWithPassword(string sessionName, string password)
    {
        if (string.IsNullOrWhiteSpace(sessionName)) return;

        await EnsureLobbyReadyAsync();

        int charIndex = GetSelectedCharacterIndex();
        byte[] token = EncodeConnectionToken(password, charIndex);

        await StartGame(GameMode.Client, sessionName, null, null, token);
    }

    private async Task StartGame(
        GameMode mode,
        string sessionName,
        int? maxPlayers = null,
        Dictionary<string, SessionProperty> sessionProps = null,
        byte[] connectionToken = null)
    {
        _sceneReady = false;
        EnsureFreshRunner();

        var scene = SceneRef.FromIndex(gameSceneBuildIndex);
        var nsm = _runner.GetComponent<NetworkSceneManagerDefault>() ??
                  _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = nsm,
            SessionProperties = sessionProps
        };

        if (maxPlayers.HasValue && maxPlayers.Value > 0)
            args.PlayerCount = maxPlayers.Value;

        if (connectionToken != null && connectionToken.Length > 0)
            args.ConnectionToken = connectionToken;

        var result = await _runner.StartGame(args);
        if (!result.Ok)
        {
            if (scriptsPrefab) Instantiate(scriptsPrefab);
            PasswordWrong?.PlayFeedbacks();
            Debug.LogError($"StartGame failed: {result.ShutdownReason}");
        }
        else
        {
            PasswordCorrect?.PlayFeedbacks();
        }
    }

    public async Task NextLevel(int buildIndex)
    {
        SaveProgress(buildIndex);

        if (_runner != null && _runner.IsServer)
        {
            try
            {
                var nsm = _runner.GetComponent<NetworkSceneManagerDefault>() ??
                          _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

                var sceneRef = SceneRef.FromIndex(buildIndex);

                await _runner.LoadScene(
                    sceneRef,
                    loadSceneMode: LoadSceneMode.Single,
                    localPhysicsMode: LocalPhysicsMode.None,
                    setActiveOnLoad: true
                );
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FusionManager] NextLevel network load failed -> {e.Message}. Falling back to local load.");
                try { SceneManager.LoadScene(buildIndex, LoadSceneMode.Single); } catch { }
                return;
            }
        }

        try { SceneManager.LoadScene(buildIndex, LoadSceneMode.Single); }
        catch (Exception e) { Debug.LogError($"[FusionManager] NextLevel local load failed -> {e.Message}"); }
    }

    public async Task LeaveToMainMenuAsync()
    {
        if (_leavingToMenu) return;
        _leavingToMenu = true;

        try
        {
            var runner = _runner;
            if (runner != null)
            {
                try { await runner.Shutdown(false); } catch { }
            }

            _spawned.Clear();
            _playerCharacterIndex.Clear();
            LobbyReady = false;
            _lobbyJoinTask = null;
            _sceneReady = false;

            if (_runner) _runner.RemoveCallbacks(this);
            _runner = null;

            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }
        finally
        {
            _leavingToMenu = false;
        }
    }

    private async Task ServerReloadIfMatchRunning(NetworkRunner runner)
    {
        if (!runner || !runner.IsServer) return;

        var grc = FindAnyObjectByType<GameReadyController>();
        if (grc != null && grc.State == MatchState.Playing)
        {
            int build = SceneManager.GetActiveScene().buildIndex;
            try
            {
                var sceneRef = SceneRef.FromIndex(build);
                var nsm = runner.GetComponent<NetworkSceneManagerDefault>() ??
                          runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

                await runner.LoadScene(
                    sceneRef,
                    loadSceneMode: LoadSceneMode.Single,
                    localPhysicsMode: LocalPhysicsMode.None,
                    setActiveOnLoad: true
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[FusionManager] Reload after client leave failed: {e.Message}");
            }
        }
    }

    private void Update()
    {
        _isJumping = Input.GetKey(KeyCode.Space);
        _isSprinting = Input.GetKey(KeyCode.LeftShift);
        _isFreeze = Input.GetKey(KeyCode.Q);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (escPanel != null && settingsPanel != null)
        {
            bool isPanelOpen = escPanel.activeInHierarchy || settingsPanel.activeInHierarchy;
            if (isPanelOpen)
            {
                input.Set(data);
                return;
            }
        }

        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) dir += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) dir += Vector3.back;
        if (Input.GetKey(KeyCode.A)) dir += Vector3.left;
        if (Input.GetKey(KeyCode.D)) dir += Vector3.right;
        if (dir.magnitude > 1f) dir.Normalize();
        data.direction = dir;

        data.mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        data.isJumping = _isJumping;
        data.isSprinting = _isSprinting;
        data.isFreeze = _isFreeze;

        input.Set(data);
    }

    public void OnSessionListUpdated(NetworkRunner _, List<SessionInfo> sessions)
        => SessionsUpdated?.Invoke(sessions);

    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token)
    {
        if (!r.IsServer) return;

        var (password, _) = DecodeConnectionToken(token);

        string expectedHash = null;
        if (r.SessionInfo != null &&
            r.SessionInfo.Properties != null &&
            r.SessionInfo.Properties.TryGetValue(PWD_HASH_KEY, out var sp))
            expectedHash = (string)sp;

        if (!string.IsNullOrEmpty(expectedHash))
        {
            string suppliedHash = Hash(password);
            if (suppliedHash != expectedHash)
            {
                req.Refuse();
                return;
            }
        }
        req.Accept();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer || !_sceneReady) return;
        if (runner.GetPlayerObject(player) != null) return;

        int characterIndex;

        if (player == runner.LocalPlayer)
        {
            characterIndex = GetSelectedCharacterIndex();
        }
        else
        {
            var token = runner.GetPlayerConnectionToken(player);
            if (token != null && token.Length > 0)
            {
                var (_, charIndex) = DecodeConnectionToken(token);
                characterIndex = charIndex;
            }
            else
            {
                characterIndex = 0;
            }
        }

        _playerCharacterIndex[player] = characterIndex;

        var pos = GetSpawnPositionFor(player);
        var prefab = GetPlayerPrefab(characterIndex);
        var obj = runner.Spawn(prefab, pos, Quaternion.identity, player);
        runner.SetPlayerObject(player, obj);
        _spawned[player] = obj;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawned.TryGetValue(player, out var obj))
        {
            runner.Despawn(obj);
            _spawned.Remove(player);
        }

        _playerCharacterIndex.Remove(player);

        if (runner.IsServer && !_leavingToMenu)
            _ = ServerReloadIfMatchRunning(runner);
    }

    public async void OnConnectFailed(NetworkRunner r, NetAddress _, NetConnectFailedReason reason)
    {
        Debug.LogWarning($"[FusionManager] Connect failed: {reason}. Returning to lobby.");
        try { await r.Shutdown(false); } catch { }

        LobbyReady = false;
        _lobbyJoinTask = null;
        _spawned.Clear();
        _playerCharacterIndex.Clear();
        OnRunnerShutdown?.Invoke();

        if (!_leavingToMenu)
        {
            try { SceneManager.LoadScene(0, LoadSceneMode.Single); } catch { }
        }
    }

    public async void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[FusionManager] Disconnected from server: {reason}");

        try { await r.Shutdown(false); } catch { }

        _runner = null;
        LobbyReady = false;
        _lobbyJoinTask = null;
        _spawned.Clear();
        _playerCharacterIndex.Clear();
        OnRunnerShutdown?.Invoke();

        if (!_leavingToMenu)
        {
            try { SceneManager.LoadScene(0, LoadSceneMode.Single); } catch { }
        }
    }

    public void OnShutdown(NetworkRunner r, ShutdownReason _)
    {
        _spawned.Clear();
        _playerCharacterIndex.Clear();
        if (_runner) _runner.RemoveCallbacks(this);
        _runner = null;
        LobbyReady = false;
        _lobbyJoinTask = null;
        OnRunnerShutdown?.Invoke();

        if (!_leavingToMenu)
        {
            try { SceneManager.LoadScene(0, LoadSceneMode.Single); } catch { }
        }
    }

    public void OnSceneLoadStart(NetworkRunner _) => _sceneReady = false;

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        gamePanel = GameObject.Find("GamePanel")?.gameObject;
        if (gamePanel != null)
        {
            var parent = gamePanel.transform.parent;
            if (parent != null)
            {
                escPanel = parent.Find("ESC_Panel")?.gameObject;
                settingsPanel = parent.Find("SettingsPanel")?.gameObject;
            }
        }

        _sceneReady = true;

        int activeBuild = SceneManager.GetActiveScene().buildIndex;

        if (activeBuild > 0)
        {
            SaveProgress(activeBuild);
            gameSceneBuildIndex = activeBuild;
        }

        if (!runner.IsServer) return;

        _spawned.Clear();

        foreach (var player in runner.ActivePlayers)
        {
            var existing = runner.GetPlayerObject(player);
            if (existing == null)
            {
                int characterIndex = 0;

                if (_playerCharacterIndex.TryGetValue(player, out int savedIndex))
                {
                    characterIndex = savedIndex;
                }
                else if (player == runner.LocalPlayer)
                {
                    characterIndex = GetSelectedCharacterIndex();
                    _playerCharacterIndex[player] = characterIndex;
                }
                else
                {
                    var token = runner.GetPlayerConnectionToken(player);
                    if (token != null && token.Length > 0)
                    {
                        var (_, charIndex) = DecodeConnectionToken(token);
                        characterIndex = charIndex;
                        _playerCharacterIndex[player] = characterIndex;
                    }
                }

                var pos = GetSpawnPositionFor(player);
                var prefab = GetPlayerPrefab(characterIndex);
                var obj = runner.Spawn(prefab, pos, Quaternion.identity, player);
                runner.SetPlayerObject(player, obj);
                _spawned[player] = obj;
            }
            else
            {
                existing.transform.SetPositionAndRotation(GetSpawnPositionFor(player), Quaternion.identity);
                _spawned[player] = existing;
            }
        }
    }

    private Vector3 GetSpawnPositionFor(PlayerRef player)
    {
        var points = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (points != null && points.Length > 0)
            return points[player.RawEncoded % points.Length].transform.position;

        return new Vector3((player.RawEncoded % 8) * 3f, 1f, 0f);
    }

    // Boş callback'ler
    public void OnInputMissing(NetworkRunner _, PlayerRef __, NetworkInput ___) { }
    public void OnConnectedToServer(NetworkRunner _) { }
    public void OnUserSimulationMessage(NetworkRunner _, SimulationMessagePtr __) { }
    public void OnCustomAuthenticationResponse(NetworkRunner _, Dictionary<string, object> __) { }
    public void OnHostMigration(NetworkRunner _, HostMigrationToken __) { }
    public void OnObjectExitAOI(NetworkRunner _, NetworkObject __, PlayerRef ___) { }
    public void OnObjectEnterAOI(NetworkRunner _, NetworkObject __, PlayerRef ___) { }
    public void OnReliableDataReceived(NetworkRunner _, PlayerRef __, ReliableKey ___, ArraySegment<byte> ____) { }
    public void OnReliableDataProgress(NetworkRunner _, PlayerRef __, ReliableKey ___, float ____) { }
}
