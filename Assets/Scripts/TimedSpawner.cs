using Fusion;
using UnityEngine;

public class TimedSpawner : NetworkBehaviour
{
    [Header("Spawn")]
    [SerializeField] private NetworkPrefabRef prefabToSpawn;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Timing")]
    [Min(0.05f)]
    [SerializeField] private float spawnEverySeconds = 3f;

    [Header("Optional")]
    [SerializeField] private int maxSpawns = 0; // 0 => sonsuz

    private float _nextSpawnTime;
    private int _spawnedCount;

    public override void Spawned()
    {
        // Ýlk spawný hemen istiyorsan:
        _nextSpawnTime = Runner.SimulationTime + spawnEverySeconds;
    }

    public override void FixedUpdateNetwork()
    {
        // Sadece StateAuthority (genelde Host) spawn etsin
        if (!Object.HasStateAuthority) return;

        if (maxSpawns > 0 && _spawnedCount >= maxSpawns) return;

        if (Runner.SimulationTime < _nextSpawnTime) return;

        DoSpawn();
        _spawnedCount++;
        _nextSpawnTime = Runner.SimulationTime + spawnEverySeconds;
    }

    private void DoSpawn()
    {
        if (!prefabToSpawn.IsValid)
        {
            Debug.LogError("[TimedSpawner] prefabToSpawn boþ/invalid!");
            return;
        }

        Transform sp = GetSpawnPoint();
        Vector3 pos = sp ? sp.position : transform.position;
        Quaternion rot = sp ? sp.rotation : transform.rotation;

        Runner.Spawn(prefabToSpawn, pos, rot, null);
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
