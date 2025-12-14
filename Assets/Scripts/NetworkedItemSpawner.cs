using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Network uyumlu item spawner - INetworkRunnerCallbacks kullanarak garanti calisir.
/// Tek prefab, birden fazla pozisyon.
/// </summary>
public class NetworkedItemSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefab")]
    [Tooltip("Spawn edilecek prefab (NetworkObject icermeli)")]
    [SerializeField] private NetworkPrefabRef prefab;

    [Header("Spawn Pozisyonlari")]
    [Tooltip("Spawn noktalarini buraya ekleyin")]
    [SerializeField] private Transform[] spawnPositions;

    [Header("Ayarlar")]
    [Tooltip("Obje alindiktan sonra tekrar spawn suresi (saniye)")]
    [SerializeField] private float respawnDelay = 5f;

    // Tracking
    private NetworkRunner _runner;
    private NetworkId[] _spawnedIds;
    private float[] _respawnTimers;
    private bool[] _waitingRespawn;
    private bool _spawned = false;

    private void Awake()
    {
        int count = spawnPositions != null ? spawnPositions.Length : 0;
        _spawnedIds = new NetworkId[count];
        _respawnTimers = new float[count];
        _waitingRespawn = new bool[count];
    }

    private void Start()
    {
        // FusionManager uzerinden Runner'a kayit ol
        StartCoroutine(RegisterToRunner());
    }

    private System.Collections.IEnumerator RegisterToRunner()
    {
        // FusionManager hazir olana kadar bekle
        while (FusionManager.Instance == null || FusionManager.Instance.Runner == null)
        {
            yield return null;
        }

        _runner = FusionManager.Instance.Runner;
        _runner.AddCallbacks(this);
        Debug.Log("[Spawner] Registered to Runner");

        // Eger sahne zaten yuklendiyse ve server isek spawn et
        if (_runner.IsServer && !_spawned && _runner.IsRunning)
        {
            _spawned = true;
            SpawnAll();
        }
    }

    private void OnDestroy()
    {
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }

    private void Update()
    {
        if (_runner == null || !_runner.IsRunning || !_runner.IsServer) return;

        // Respawn kontrol
        for (int i = 0; i < _spawnedIds.Length; i++)
        {
            if (_waitingRespawn[i])
            {
                if (Time.time >= _respawnTimers[i])
                {
                    SpawnAt(i);
                }
            }
            else if (_spawnedIds[i].IsValid)
            {
                if (!_runner.TryFindObject(_spawnedIds[i], out _))
                {
                    _waitingRespawn[i] = true;
                    _respawnTimers[i] = Time.time + respawnDelay;
                    _spawnedIds[i] = default;
                    Debug.Log($"[Spawner] Index {i} will respawn in {respawnDelay}s");
                }
            }
        }
    }

    private void SpawnAll()
    {
        if (_runner == null || !_runner.IsServer) return;
        if (!prefab.IsValid)
        {
            Debug.LogError("[Spawner] Prefab invalid!");
            return;
        }

        Debug.Log($"[Spawner] SpawnAll - {spawnPositions.Length} positions");

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            SpawnAt(i);
        }
    }

    private void SpawnAt(int index)
    {
        if (_runner == null || !_runner.IsServer) return;
        if (spawnPositions == null || index >= spawnPositions.Length) return;
        if (spawnPositions[index] == null) return;

        Vector3 pos = spawnPositions[index].position;
        Quaternion rot = spawnPositions[index].rotation;

        // Async spawn kullan
        _runner.SpawnAsync(prefab, pos, rot, null, null, default, (spawnOp) =>
        {
            if (spawnOp.IsSpawned && spawnOp.Object != null)
            {
                _spawnedIds[index] = spawnOp.Object.Id;
                _waitingRespawn[index] = false;
                Debug.Log($"[Spawner] Spawned at {index}: {pos}");
            }
            else
            {
                Debug.LogError($"[Spawner] SpawnAsync FAILED at {index}");
            }
        });
    }

    #region INetworkRunnerCallbacks

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        _runner = runner;

        if (!runner.IsServer) return;
        if (_spawned) return;

        _spawned = true;
        SpawnAll();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _runner = null;
        _spawned = false;
        for (int i = 0; i < _spawnedIds.Length; i++)
        {
            _spawnedIds[i] = default;
            _waitingRespawn[i] = false;
        }
    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (spawnPositions == null) return;
        Gizmos.color = prefab.IsValid ? Color.green : Color.red;
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            if (spawnPositions[i] != null)
            {
                Gizmos.DrawWireSphere(spawnPositions[i].position, 0.5f);
                UnityEditor.Handles.Label(spawnPositions[i].position + Vector3.up, $"[{i}]");
            }
        }
    }
#endif
}
