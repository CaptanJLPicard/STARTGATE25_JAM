using Fusion;
using System.Linq;
using UnityEngine;
using TMPro;

/// <summary>
/// Maximum oyuncu sayisina ulasinca kendini silen duvar.
/// Tum oyuncular baglanana kadar bekler.
/// MaxPlayers degerini Fusion SessionInfo'dan otomatik alir.
/// </summary>
public class PlayerGateWall : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float checkInterval = 0.5f;

    [Header("Visual (Optional)")]
    [SerializeField] private GameObject wallVisual; // Bos ise kendi gameObject'i kullanilir

    [Header("UI")]
    [SerializeField] private TMP_Text playerCountText; // Oyuncu sayisi gosterimi (orn: 2/4)

    [Networked] private NetworkBool IsOpened { get; set; }
    [Networked] private int CurrentPlayerCount { get; set; }
    [Networked] private int MaxPlayerCount { get; set; }
    private TickTimer _checkTimer;

    public override void Spawned()
    {
        IsOpened = false;
        _checkTimer = TickTimer.CreateFromSeconds(Runner, checkInterval);

        // Visual ayarla
        if (wallVisual == null)
            wallVisual = gameObject;

        wallVisual.SetActive(true);
    }

    public override void FixedUpdateNetwork()
    {
        // Sadece Host kontrol eder
        if (!HasStateAuthority) return;
        if (IsOpened) return;

        // Belirli araliklarla kontrol et
        if (!_checkTimer.Expired(Runner)) return;
        _checkTimer = TickTimer.CreateFromSeconds(Runner, checkInterval);

        // Fusion SessionInfo'dan max player sayisini al
        MaxPlayerCount = Runner.SessionInfo.MaxPlayers;
        CurrentPlayerCount = Runner.ActivePlayers.Count();

        Debug.Log($"[PlayerGateWall] Oyuncu sayisi: {CurrentPlayerCount}/{MaxPlayerCount}");

        // Maximum oyuncu sayisina ulasildi mi?
        if (CurrentPlayerCount >= MaxPlayerCount)
        {
            IsOpened = true;
            RPC_OpenGate();
        }
    }

    public override void Render()
    {
        // Client tarafinda da senkronize et
        if (wallVisual != null)
        {
            wallVisual.SetActive(!IsOpened);
        }

        // Oyuncu sayisini goster (orn: 2/4)
        if (playerCountText != null)
        {
            playerCountText.text = $"{CurrentPlayerCount}/{MaxPlayerCount}";
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OpenGate()
    {
        Debug.Log("[PlayerGateWall] Kapi acildi! Tum oyuncular baglandi.");

        // Önce text'i kapat
        if (playerCountText != null)
        {
            playerCountText.gameObject.SetActive(false);
        }

        // Sonra duvari kapat
        if (wallVisual != null)
        {
            wallVisual.SetActive(false);
        }
    }
}
