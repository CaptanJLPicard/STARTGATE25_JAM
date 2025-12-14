using Fusion;
using UnityEngine;

public enum PowerUpType
{
    JumpBoost,
    SpeedBoost,
    ScaleBoost
}

/// <summary>
/// Rastgele güç veren toplanabilir PowerUp objesi.
/// Network üzerinden senkronize edilir.
/// </summary>
public class RandomPowerUp : NetworkBehaviour
{
    [Header("Boost Duration (seconds)")]
    [SerializeField] private float boostDuration = 10f;

    [Header("Boost Multipliers")]
    [SerializeField] private float jumpBoostMultiplier = 1.5f;    // Zıplama gücü çarpanı
    [SerializeField] private float speedBoostMultiplier = 1.5f;   // Hız çarpanı
    [SerializeField] private float scaleBoostAmount = 0.3f;       // Scale artış miktarı

    [Header("Visual Settings")]
    [SerializeField] private float rotationSpeed = 50f;           // Dönme hızı
    [SerializeField] private float bobSpeed = 2f;                 // Yukarı aşağı hareket hızı
    [SerializeField] private float bobHeight = 0.3f;              // Yukarı aşağı hareket yüksekliği

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 1.5f;           // Toplama yarıçapı
    [SerializeField] private LayerMask playerLayer;               // Player layer'ı

    private Vector3 _startPosition;

    // Networked - tüm client'larda senkronize
    [Networked] private NetworkBool IsCollected { get; set; }
    [Networked] private PowerUpType SelectedBoostType { get; set; }

    public override void Spawned()
    {
        _startPosition = transform.position;

        // Host spawn anında rastgele boost türünü belirlesin
        if (Object.HasStateAuthority)
        {
            SelectedBoostType = (PowerUpType)Random.Range(0, 3);
        }
    }

    public override void Render()
    {
        if (IsCollected) return;

        // Görsel efektler - dönme ve yukarı aşağı hareket
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        float newY = _startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    public override void FixedUpdateNetwork()
    {
        // Sadece Host collision kontrolü yapar
        if (!Object.HasStateAuthority) return;
        if (IsCollected) return;

        // OverlapSphere ile player'ları tespit et
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer);

        foreach (var hit in hits)
        {
            Player player = hit.GetComponentInParent<Player>();
            if (player == null) continue;
            if (player.Object == null || !player.Object.IsValid) continue;

            // İlk bulunan geçerli player'a boost ver
            IsCollected = true;

            Debug.Log($"[RandomPowerUp] {player.Nick} collected {SelectedBoostType} boost!");

            // Boost'u uygula (Host tarafında)
            player.ApplyBoost(SelectedBoostType, boostDuration, GetBoostValue(SelectedBoostType));

            // Despawn
            Runner.Despawn(Object);
            return;
        }
    }

    private float GetBoostValue(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.JumpBoost:
                return jumpBoostMultiplier;
            case PowerUpType.SpeedBoost:
                return speedBoostMultiplier;
            case PowerUpType.ScaleBoost:
                return scaleBoostAmount;
            default:
                return 1f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
#endif
}
