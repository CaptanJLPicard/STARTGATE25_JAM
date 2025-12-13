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

    [Header("Win + Next Level")]
    [SerializeField] private float winDuration = 10f;
    [SerializeField] private int nextSceneBuildIndex = 2;

    [Header("Win Text UI")]
    [SerializeField] private GameObject winTextRoot; // Canvas altýndaki Text/Panel objesi (SetActive ile aç/kapat)
    [SerializeField] private TMP_Text winTextLabel;  // Ýstersen yazý set etmek için (opsiyonel)
    [SerializeField] private string winTextMessage = "KAZANDIN!";

    private readonly HashSet<Player> _inside = new HashSet<Player>();
    private float _nextAllowedTime;

    [Networked] private NetworkBool RoundLocked { get; set; }
    [Networked] private TickTimer NextLevelTimer { get; set; }

    public override void Spawned()
    {
        // Herkeste baþta kapalý
        if (winTextRoot != null)
            winTextRoot.SetActive(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (RoundLocked && NextLevelTimer.Expired(Runner))
        {
            Runner.LoadScene("Level2");
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

        var ordered = _inside
            .OrderBy(p => Vector3.SqrMagnitude(p.transform.position - transform.position))
            .Take(3)
            .ToList();

        if (ordered.Count >= 1 && pos1) TeleportPlayer(ordered[0], pos1);
        if (ordered.Count >= 2 && pos2) TeleportPlayer(ordered[1], pos2);
        if (ordered.Count >= 3 && pos3) TeleportPlayer(ordered[2], pos3);

        // Winner belirlendiði an
        if (ordered.Count >= 1 && !RoundLocked)
        {
            RoundLocked = true;

            // 1-2-3 win anim
            for (int i = 0; i < ordered.Count; i++)
                if (ordered[i] != null)
                    ordered[i].RPC_PlayWin();

            // UI yazýsýný aç (herkeste)
            RPC_SetWinText(true, winTextMessage);

            // 10 sn sonra next level
            NextLevelTimer = TickTimer.CreateFromSeconds(Runner, winDuration);
        }
    }

    private void TeleportPlayer(Player p, Transform target)
    {
        p.TeleportTo(target.position, target.rotation);
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