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
    [SerializeField] private float defaultGrowthAmount = 0.1f;

    // Network synced
    [Networked] private TickTimer LifetimeTimer { get; set; }
    [Networked] private TickTimer ActivationTimer { get; set; }
    [Networked] private NetworkBool IsActive { get; set; }
    [Networked] private float NetworkedScale { get; set; } = 1f; // Default 1 olmalı, 0 olursa görünmez
    [Networked] public float GrowthAmount { get; set; } // Network synced growth value

    private float lifetime;
    private Vector3 _baseScale; // Prefab'ın orijinal scale değeri

    public override void Spawned()
    {
        Debug.Log($"[DroppedPickup] Spawned! HasStateAuthority: {Object.HasStateAuthority}, HasInputAuthority: {Object.HasInputAuthority}, NetworkedScale: {NetworkedScale}");

        // Prefab'ın orijinal scale değerini kaydet (0 olmamalı)
        if (transform.localScale.sqrMagnitude > 0.001f)
        {
            _baseScale = transform.localScale;
        }
        else
        {
            _baseScale = Vector3.one;
        }

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

        // StateAuthority timer'ları ve değerleri başlatır
        if (Object.HasStateAuthority)
        {
            ActivationTimer = TickTimer.CreateFromSeconds(Runner, activationDelay);
            IsActive = false;
            // NetworkedScale zaten SetScaleMultiplier ile ayarlanmış olabilir, kontrol et
            if (NetworkedScale <= 0.01f)
            {
                NetworkedScale = 1f;
            }
            // GrowthAmount default değeri
            if (GrowthAmount <= 0f)
            {
                GrowthAmount = defaultGrowthAmount;
            }
        }

        // Scale'i uygula - tüm clientlar için
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
        if (Object.HasStateAuthority)
        {
            GrowthAmount = growth;
        }
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

        // NetworkedScale sync olmadan önce 0 olabilir, bu durumda 1 kullan
        float scaleToApply = NetworkedScale > 0.01f ? NetworkedScale : 1f;
        transform.localScale = _baseScale * scaleToApply;
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
