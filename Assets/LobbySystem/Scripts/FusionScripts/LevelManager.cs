using System.Collections.Generic;
using Fusion;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkBehaviour
{
    private FusionManager fm;

    [SerializeField] private int currentScene;
    [SerializeField] private GameObject EscapePanel;
    [SerializeField] private GameObject SettingsPanel;
    [SerializeField] private GameObject restartButton;

    public bool stop;

    [Header("Default Spawn (Checkpoint yoksa)")]
    [SerializeField] private Transform defaultSpawn;

    // Oyuncu -> en son checkpoint spawn noktası
    private readonly Dictionary<PlayerRef, Transform> _playerCheckpoint = new();

    public override void Spawned()
    {
        if (fm == null)
            fm = FindFirstObjectByType<FusionManager>(FindObjectsInactive.Include);

        if (restartButton != null)
            restartButton.SetActive(HasStateAuthority);

        // Default spawn boşsa tag ile bul
        if (defaultSpawn == null)
        {
            var spawnObj = GameObject.FindWithTag("SpawnPoint");
            if (spawnObj != null) defaultSpawn = spawnObj.transform;
            else Debug.LogWarning("SpawnPoint tag'li obje bulunamadı! defaultSpawn atanmamış.");
        }
    }

    // === CHECKPOINT KAYDI (Host) ===
    public void ServerSetCheckpoint(Player player, Transform spawnPoint)
    {
        if (!HasStateAuthority) return;
        if (player == null || spawnPoint == null) return;
        if (player.Object == null) return;

        var pr = player.Object.InputAuthority;
        _playerCheckpoint[pr] = spawnPoint;

        // Debug.Log($"Checkpoint set: {pr} -> {spawnPoint.name}");
    }

    // === RESPAWN (Host) ===
    public void ServerRespawn(Player player)
    {
        if (!HasStateAuthority) return;
        if (player == null || player.Object == null) return;

        var pr = player.Object.InputAuthority;

        Transform spawn = null;
        if (!_playerCheckpoint.TryGetValue(pr, out spawn) || spawn == null)
            spawn = defaultSpawn;

        if (spawn == null)
        {
            Debug.LogWarning("Respawn yapılamadı: spawn null (defaultSpawn yok?)");
            return;
        }

        // En sağlıklısı: NetworkCharacterController varsa Teleport
        var ncc = player.GetComponent<NetworkCharacterController>();
        if (ncc != null)
        {
            ncc.Teleport(spawn.position);
        }
        else
        {
            // fallback
            player.transform.position = spawn.position;
        }
    }

    // === ÖLÜM / TELEPORT TRIGGER ===
    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player") && currentScene > 0)
        {
            var player = other.GetComponent<Player>();
            if (player != null)
            {
                // Player RPC yok: direkt LevelManager respawn ediyor
                ServerRespawn(player);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            stop = !stop;
            if (EscapePanel != null) EscapePanel.SetActive(stop);
        }
    }

    // === MAIN MENU ===
    public void MainMenuBtn() => ExecuteMainMenu();

    private void ExecuteMainMenu()
    {
        if (fm == null)
            fm = FindFirstObjectByType<FusionManager>(FindObjectsInactive.Include);

        if (fm != null) _ = fm.LeaveToMainMenuAsync();
        else Debug.LogWarning("FusionManager bulunamadı, odadan çıkılamadı!");
    }

    // === RESTART ===
    public void RestartBtn()
    {
        if (!HasStateAuthority) return;
        ExecuteRestart();
    }

    private void ExecuteRestart()
    {
        if (fm == null) return;

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"<color=yellow>Level yeniden başlatılıyor... (buildIndex={buildIndex})</color>");
        _ = fm.NextLevel(buildIndex);
    }

    // === SETTINGS ===
    public void SettingsBtn()
    {
        if (SettingsPanel != null) SettingsPanel.SetActive(true);
        if (EscapePanel != null) EscapePanel.SetActive(false);
    }

    public void SettingsBtnClose()
    {
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
        if (EscapePanel != null) EscapePanel.SetActive(true);
    }
}