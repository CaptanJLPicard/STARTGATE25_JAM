using Fusion;
using UnityEngine;

public class CheckpointTrigger : NetworkBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Transform[] spawnPoint; // ör: Spawn2Pos

    private void Awake()
    {
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Host kaydetsin
        if (!HasStateAuthority) return;

        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<Player>();
        if (player == null) return;

        levelManager.ServerSetCheckpoint(player, spawnPoint[Random.Range(0, spawnPoint.Length)]);
    }
}