using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class NearestPlayersToSpawns : NetworkBehaviour
{
    [Header("Spawn Positions")]
    [SerializeField] private Transform pos1;
    [SerializeField] private Transform pos2;
    [SerializeField] private Transform pos3;

    [Header("Settings")]
    [SerializeField] private float reorderCooldown = 0.25f;
    [SerializeField] private string sceneName;

    [Header("Win + Next Level")]
    [SerializeField] private float winDuration = 10f;
    [SerializeField] private int nextSceneBuildIndex = 2;

    [Header("Win Text UI")]
    [SerializeField] private GameObject winTextRoot; // Canvas altındaki Text/Panel objesi (SetActive ile aç/kapat)
    [SerializeField] private TMP_Text winTextLabel;  // İstersen yazı set etmek için (opsiyonel)
    [SerializeField] private TMP_Text timeText;  // İstersen yazı set etmek için (opsiyonel)
    [SerializeField] private string winTextMessage = "YOU WIN!";

    private readonly HashSet<Player> _inside = new HashSet<Player>();
    private float _nextAllowedTime;

    [Networked] private NetworkBool RoundLocked { get; set; }
    [Networked] private TickTimer NextLevelTimer { get; set; }

    private List<Player> GetClosestPlayersGlobal(Player winner, int count)
    {
        // Host tarafında sahnedeki tüm Player'ları al
        var allPlayers = FindObjectsOfType<Player>()
            .Where(p => p != null && p.Object != null && p.Object.IsValid)
            .ToList();

        // Winner her zaman 1. olsun
        var result = new List<Player>();
        if (winner != null) result.Add(winner);

        // Winner hariç diğerlerini objeye olan mesafeye göre sırala
        var others = allPlayers
            .Where(p => p != null && p != winner)
            .OrderBy(p => Vector3.SqrMagnitude(p.transform.position - transform.position))
            .ToList();

        foreach (var p in others)
        {
            if (result.Count >= count) break;
            result.Add(p);
        }

        // count kadar döndür (1-2-3)
        return result.Take(count).ToList();
    }


    public override void Spawned()
    {
        // Herkeste başta kapalı
        if (winTextRoot != null)
            winTextRoot.SetActive(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (RoundLocked && NextLevelTimer.Expired(Runner))
        {
            Runner.LoadScene(sceneName);
        }
    }

    public override void Render()
    {
        // Geri sayım göster
        if (RoundLocked && timeText != null)
        {
            float? remaining = NextLevelTimer.RemainingTime(Runner);
            if (remaining.HasValue && remaining.Value > 0f)
            {
                int seconds = Mathf.CeilToInt(remaining.Value);
                timeText.text = seconds.ToString();
            }
            else
            {
                timeText.text = "0";
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;
        if (RoundLocked) return;
        if (Time.time < _nextAllowedTime) return;
        if (!other.CompareTag("Player")) return;

        var pl = other.GetComponentInParent<Player>();
        if (pl == null) return;

        _inside.Add(pl);
        ReorderAndTeleport();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasStateAuthority) return;
        if (RoundLocked) return;
        if (!other.CompareTag("Player")) return;

        var pl = other.GetComponentInParent<Player>();
        if (pl == null) return;

        _inside.Remove(pl);
        ReorderAndTeleport();
    }

    private void ReorderAndTeleport()
    {
        _nextAllowedTime = Time.time + reorderCooldown;

        _inside.RemoveWhere(p => p == null);

        // Trigger içindeki oyunculardan winner'ı bul (en yakın)
        var orderedInside = _inside
            .OrderBy(p => Vector3.SqrMagnitude(p.transform.position - transform.position))
            .ToList();

        // Winner yoksa çık
        if (orderedInside.Count < 1) return;

        // Winner belirlendiği an round kilitle
        if (!RoundLocked)
        {
            RoundLocked = true;

            var winner = orderedInside[0];

            // ✅ Global en yakın 2 ve 3'ü bul (trigger içinde olmasalar bile)
            var top3 = GetClosestPlayersGlobal(winner, 3);

            // ✅ 1-2-3 ışınla (Teleport + Lock + Dance sistemin)
            if (top3.Count >= 1 && pos1) top3[0].RPC_TeleportLockAndDance(pos1.position, pos1.rotation);
            if (top3.Count >= 2 && pos2) top3[1].RPC_TeleportLockAndDance(pos2.position, pos2.rotation);
            if (top3.Count >= 3 && pos3) top3[2].RPC_TeleportLockAndDance(pos3.position, pos3.rotation);

            // ✅ UI yazısı
            RPC_SetWinText(true, winTextMessage);

            // ✅ 10 sn sonra next level
            NextLevelTimer = TickTimer.CreateFromSeconds(Runner, winDuration);

        }
    }

    private void TeleportPlayer(Player p, Transform target)
    {
        // ✅ Teleport anında hareket kilit + dans + rotasyon hedefe göre
        p.RPC_TeleportLockAndDance(target.position, target.rotation);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetWinText(bool value, string message)
    {
        if (winTextLabel != null)
            winTextLabel.text = message;

        if (winTextRoot != null)
            winTextRoot.SetActive(value);
    }
}