using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BallRespawnTrigger : MonoBehaviour
{
    [Header("Fusion")]
    [Tooltip("Boþsa sahneden LevelManager otomatik bulunur.")]
    [SerializeField] private LevelManager levelManager;

    [Tooltip("Genelde sadece Host respawn etsin.")]
    [SerializeField] private bool onlyIfHost = true;

    [Tooltip("Topun colliderý trigger olsun mu? (Ýstersen çarpýþma yerine trigger)")]
    [SerializeField] private bool forceTrigger = true;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null && forceTrigger) col.isTrigger = true;

        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
            if (levelManager == null) return;
        }

        if (onlyIfHost && !levelManager.HasStateAuthority) return;

        var player = other.GetComponent<Player>();
        if (player != null)
            levelManager.ServerRespawn(player);
    }

    // Trigger kullanmak istemezsen (col.isTrigger=false), bu da çalýþsýn:
    private void OnCollisionEnter(Collision collision)
    {
        if (forceTrigger) return; // trigger aktifse collision'a gerek yok

        var other = collision.collider;
        if (!other.CompareTag("Player")) return;

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
            if (levelManager == null) return;
        }

        if (onlyIfHost && !levelManager.HasStateAuthority) return;

        var player = other.GetComponent<Player>();
        if (player != null)
            levelManager.ServerRespawn(player);
    }
}
