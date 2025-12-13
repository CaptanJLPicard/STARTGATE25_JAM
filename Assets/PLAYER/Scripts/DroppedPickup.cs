using Fusion;
using UnityEngine;

/// <summary>
/// Karakter küçülürken bıraktığı toplanabilir obje.
/// Network üzerinden senkronize edilir.
/// </summary>
public class DroppedPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float activationDelay = 0.5f; // Spawn sonrası aktif olma süresi
    [SerializeField] private Collider pickupCollider;

    [Header("Bonus Values (Inspector'dan veya spawn sırasında ayarlanır)")]
    public float growthAmount = 0.1f;

    // Network synced
    [Networked] private TickTimer LifetimeTimer { get; set; }
    [Networked] private TickTimer ActivationTimer { get; set; }
    [Networked] private NetworkBool IsActive { get; set; }
    [Networked] private float NetworkedScale { get; set; }

    private float lifetime;
    private Vector3 _baseScale; // Prefab'ın orijinal scale değeri

    public override void Spawned()
    {
        Debug.Log($"[DroppedPickup] Spawned! HasStateAuthority: {Object.HasStateAuthority}, HasInputAuthority: {Object.HasInputAuthority}");

        // Prefab'ın orijinal scale değerini kaydet
        _baseScale = transform.localScale;

        // Collider'ı bul
        if (pickupCollider == null)
        {
            pickupCollider = GetComponent<Collider>();
        }

        // Başlangıçta collider devre dışı
        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        // StateAuthority timer'ları başlatır
        if (Object.HasStateAuthority)
        {
            ActivationTimer = TickTimer.CreateFromSeconds(Runner, activationDelay);
            IsActive = false;
            NetworkedScale = 1f;
        }

        // Scale'i uygula
        ApplyScale();
    }

    /// <summary>
    /// Lifetime'ı ayarlar (spawn eden tarafından çağrılır)
    /// </summary>
    public void SetLifetime(float seconds)
    {
        lifetime = seconds;
        if (Object.HasStateAuthority && Runner != null)
        {
            LifetimeTimer = TickTimer.CreateFromSeconds(Runner, seconds);
        }
    }

    /// <summary>
    /// Growth değerini ayarlar (spawn eden tarafından çağrılır)
    /// </summary>
    public void SetGrowthAmount(float growth)
    {
        growthAmount = growth;
    }

    /// <summary>
    /// Player scale'ine göre pickup boyutunu ayarlar
    /// </summary>
    public void SetScaleMultiplier(float playerScale)
    {
        if (Object.HasStateAuthority)
        {
            NetworkedScale = playerScale;
        }
        ApplyScale();
    }

    /// <summary>
    /// Base scale ile multiplier'ı çarparak uygular
    /// </summary>
    private void ApplyScale()
    {
        if (_baseScale == Vector3.zero)
        {
            _baseScale = Vector3.one;
        }
        transform.localScale = _baseScale * NetworkedScale;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Aktivasyon kontrolü
        if (!IsActive && ActivationTimer.Expired(Runner))
        {
            IsActive = true;
        }

        // Lifetime kontrolü - süre dolunca despawn
        if (LifetimeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    public override void Render()
    {
        // Collider durumunu güncelle (tüm clientlar için)
        if (pickupCollider != null)
        {
            pickupCollider.enabled = IsActive;
        }

        // Scale'i güncelle (network sync için)
        ApplyScale();
    }

    /// <summary>
    /// Bu pickup aktif mi? (Toplanabilir mi?)
    /// </summary>
    public bool CanBeCollected()
    {
        return IsActive;
    }
}
