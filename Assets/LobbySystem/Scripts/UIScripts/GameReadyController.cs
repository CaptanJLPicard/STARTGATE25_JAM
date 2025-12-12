using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public enum MatchState { Waiting, Playing }

public class GameReadyController : NetworkBehaviour
{
    [Header("Events")]
    public UnityEvent onGameStarted;
    public UnityEvent onGameWaiting;

    [Networked] public MatchState State { get; set; }
    [Networked] public int TargetPlayers { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            State = MatchState.Waiting;

            int max = Runner?.SessionInfo?.MaxPlayers ?? 0;
            if (max <= 0)
                TargetPlayers = Runner.ActivePlayers.Count();
            else
                TargetPlayers = max;
        }

        onGameWaiting?.Invoke();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        int total = Runner.SessionInfo.MaxPlayers;
        TargetPlayers = total;

        int ready = ReadyCount;

        if (State == MatchState.Waiting)
        {
            if (total > 0 && ready >= total)
            {
                State = MatchState.Playing;
                Rpc_OnGameStarted();
            }
            else
            {
                Rpc_OnGameWaiting();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_OnGameStarted()
    {
        onGameStarted?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_OnGameWaiting()
    {
        onGameWaiting?.Invoke();
    }

    public int ReadyCount
    {
        get
        {
            int count = 0;
            foreach (var p in Runner.ActivePlayers)
            {
                var obj = Runner.GetPlayerObject(p);
                var pr = obj ? obj.GetComponent<Player>() : null;
                if (pr && pr.IsReady) count++;
            }
            return count;
        }
    }

    public int TotalPlayers => Runner?.ActivePlayers.Count() ?? 0;
}
