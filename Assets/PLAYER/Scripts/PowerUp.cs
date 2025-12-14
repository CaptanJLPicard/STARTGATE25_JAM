using Fusion;
using UnityEngine;

/// <summary>
/// PowerUp objesi - Player'a değdiğinde büyüme sağlar.
/// Trigger kontrolü pickup tarafından yapılır (client uyumluluğu için).
/// </summary>
public class PowerUp : NetworkBehaviour
{
    [Header("PowerUp Settings")]
    [SerializeField] private float growthAmount = 0.2f;

    private bool _collected = false;

    private void OnTriggerEnter(Collider other)
    {
        // Zaten toplandıysa işlem yapma
        if (_collected) return;
        if (Runner == null) return;

        // Player'ı bul
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        // Player'ın NetworkObject'i geçerli mi?
        if (player.Object == null || !player.Object.IsValid) return;

        // Bu player LOCAL player mı?
        if (player.Object.InputAuthority != Runner.LocalPlayer) return;

        _collected = true;

        Debug.Log($"[PowerUp] Collected by {player.gameObject.name}, LocalPlayer: {Runner.LocalPlayer}");

        // Büyüme uygula
        player.Grow(growthAmount);

        // Particle effect - HOST'a istek gönder, HOST spawn etsin, HERKES görsün
        if (player.Object.HasStateAuthority)
        {
            // Ben HOST'um - direkt spawn et (herkese gider)
            player.RPC_SpawnPowerUpParticle(transform.position, Vector3.up);
        }
        else
        {
            // Ben CLIENT'ım - HOST'a istek gönder
            player.RPC_RequestSpawnPowerUpParticle(transform.position, Vector3.up);
        }

        // Despawn - Host ise direkt, client ise RPC
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
        else
        {
            player.RPC_RequestDespawnPickup(Object.Id);
        }
    }
}
