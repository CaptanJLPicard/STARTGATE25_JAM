using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// PowerUp boost iconlarini gosteren UI sistemi.
/// Her boost icin bir icon gosterir, boost aktifken gorunur, bitmek uzereyken blink yapar.
/// </summary>
public class PowerUpUI : MonoBehaviour
{
    [Header("Icon References")]
    [SerializeField] private Image jumpBoostIcon;
    [SerializeField] private Image speedBoostIcon;
    [SerializeField] private Image scaleBoostIcon;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite jumpBoostSprite;
    [SerializeField] private Sprite speedBoostSprite;
    [SerializeField] private Sprite scaleBoostSprite;

    [Header("Blink Settings")]
    [SerializeField] private float blinkStartTime = 3f;      // Kac saniye kala blink baslasin
    [SerializeField] private float blinkSpeed = 5f;          // Blink hizi
    [SerializeField] private float minAlpha = 0.3f;          // Minimum alpha degeri

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;    // Icon acilma suresi
    [SerializeField] private float fadeOutDuration = 0.3f;   // Icon kapanma suresi

    private Player _localPlayer;
    private CanvasGroup _jumpCanvasGroup;
    private CanvasGroup _speedCanvasGroup;
    private CanvasGroup _scaleCanvasGroup;

    // Fade animasyonu icin
    private float _jumpTargetAlpha;
    private float _speedTargetAlpha;
    private float _scaleTargetAlpha;

    private void Start()
    {
        // CanvasGroup'lari al veya ekle
        _jumpCanvasGroup = GetOrAddCanvasGroup(jumpBoostIcon);
        _speedCanvasGroup = GetOrAddCanvasGroup(speedBoostIcon);
        _scaleCanvasGroup = GetOrAddCanvasGroup(scaleBoostIcon);

        // Sprite'lari ayarla
        if (jumpBoostIcon != null && jumpBoostSprite != null)
            jumpBoostIcon.sprite = jumpBoostSprite;
        if (speedBoostIcon != null && speedBoostSprite != null)
            speedBoostIcon.sprite = speedBoostSprite;
        if (scaleBoostIcon != null && scaleBoostSprite != null)
            scaleBoostIcon.sprite = scaleBoostSprite;

        // Baslangicta tum iconlari gizle
        SetIconAlpha(_jumpCanvasGroup, 0f);
        SetIconAlpha(_speedCanvasGroup, 0f);
        SetIconAlpha(_scaleCanvasGroup, 0f);
    }

    private void Update()
    {
        // Local player'i bul
        if (_localPlayer == null)
        {
            FindLocalPlayer();
            return;
        }

        // Her boost icin UI guncelle
        UpdateBoostIcon(PowerUpType.JumpBoost, _jumpCanvasGroup, ref _jumpTargetAlpha);
        UpdateBoostIcon(PowerUpType.SpeedBoost, _speedCanvasGroup, ref _speedTargetAlpha);
        UpdateBoostIcon(PowerUpType.ScaleBoost, _scaleCanvasGroup, ref _scaleTargetAlpha);
    }

    private void FindLocalPlayer()
    {
        // Tum Player'lari bul ve local olani sec
        Player[] players = FindObjectsOfType<Player>();
        foreach (var player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                _localPlayer = player;
                Debug.Log("[PowerUpUI] Local player bulundu!");
                break;
            }
        }
    }

    private void UpdateBoostIcon(PowerUpType type, CanvasGroup canvasGroup, ref float targetAlpha)
    {
        if (canvasGroup == null || _localPlayer == null) return;

        bool hasBoost = false;
        float remainingTime = 0f;

        // Boost durumunu kontrol et
        switch (type)
        {
            case PowerUpType.JumpBoost:
                hasBoost = _localPlayer.HasJumpBoost;
                remainingTime = _localPlayer.GetBoostRemainingTime(PowerUpType.JumpBoost);
                break;
            case PowerUpType.SpeedBoost:
                hasBoost = _localPlayer.HasSpeedBoost;
                remainingTime = _localPlayer.GetBoostRemainingTime(PowerUpType.SpeedBoost);
                break;
            case PowerUpType.ScaleBoost:
                hasBoost = _localPlayer.HasScaleBoost;
                remainingTime = _localPlayer.GetBoostRemainingTime(PowerUpType.ScaleBoost);
                break;
        }

        if (hasBoost)
        {
            // Boost aktif
            if (remainingTime <= blinkStartTime && remainingTime > 0)
            {
                // Bitmek uzere - blink efekti
                float blinkValue = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f; // 0-1 arasi
                targetAlpha = Mathf.Lerp(minAlpha, 1f, blinkValue);
            }
            else
            {
                // Normal gorunum
                targetAlpha = 1f;
            }
        }
        else
        {
            // Boost yok - gizle
            targetAlpha = 0f;
        }

        // Smooth alpha gecisi
        float currentAlpha = canvasGroup.alpha;
        float speed = targetAlpha > currentAlpha ? (1f / fadeInDuration) : (1f / fadeOutDuration);
        canvasGroup.alpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.deltaTime);

        // Tamamen seffaf ise gameObject'i deaktif et (performans)
        canvasGroup.gameObject.SetActive(canvasGroup.alpha > 0.01f);
    }

    private CanvasGroup GetOrAddCanvasGroup(Image image)
    {
        if (image == null) return null;

        CanvasGroup cg = image.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = image.gameObject.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    private void SetIconAlpha(CanvasGroup cg, float alpha)
    {
        if (cg != null)
        {
            cg.alpha = alpha;
            cg.gameObject.SetActive(alpha > 0.01f);
        }
    }
}
