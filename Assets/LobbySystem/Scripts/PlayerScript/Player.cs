using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
    #region References

    [Header("References")] [SerializeField]
    private NetworkCharacterController _characterController;

    [SerializeField] private Animator playerAnim;

    #endregion

    #region Movement

    [Header("Movement & Jump (2D)")] [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;

    private bool wasGrounded = true;
    private Quaternion _lockedRotation;

    private bool _freezeApplied = false;
    private float _baseGravity = -20f;

    private const float FREEZE_DELAY_SECONDS = 0.1f;
    private const float FREEZE_HOLD_SECONDS = 5f;

    private LevelManager levelManager;

    #endregion


    #region Networked Variables

    [Networked] public NetworkBool NetIsRunning { get; set; }
    [Networked] public NetworkBool NetIsJumping { get; set; }
    [Networked] public NetworkBool NetIsFreeze { get; set; }
    [Networked] public Vector3 NetVelocity { get; set; }
    [Networked] public float NetIsCurrentSpeed { get; set; }

    [Networked] private byte FreezePhaseN { get; set; }
    [Networked] private float FreezeSecondsLeft { get; set; }

    [Networked] public NetworkBool NetIsTouchingWall { get; set; }

    [Networked] public bool IsReady { get; set; }

    [Networked] public NetworkString<_16> Nick { get; set; }

    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject escPanel;
    [SerializeField] private GameObject settingsPanel;
    [Networked] public bool gameReady { get; set; }
    [Networked] private Vector3 InitialSpawnPosition { get; set; }

    #endregion

    #region Visual Helpers

    [HideInInspector] public float isSprinting;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool isFreeze;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public bool isTouchingWall;
    [Range(1, 25), SerializeField] private float runningSmoothness = 5f;

    // Animasyon state tracking
    private bool _lastAnimJumping = false;
    private bool _lastAnimRunning = false;
    private bool _lastAnimWalking = false;
    private bool _lastAnimFrozen = false;

    #endregion

    #region Shared helpers (scan all players)

    private static readonly List<Player> _tempPlayers = new List<Player>();
    private static readonly HashSet<Player> s_registry = new HashSet<Player>();
    private TextMeshProUGUI _freezeText;
    [SerializeField] private TextMeshProUGUI _nickNameText;

    private void OnEnable() => s_registry.Add(this);
    private void OnDisable() => s_registry.Remove(this);

    private void CollectAllPlayers()
    {
        _tempPlayers.Clear();
        if (Runner != null) Runner.GetAllBehaviours<Player>(_tempPlayers);
        else _tempPlayers.AddRange(s_registry);
    }

    private Player GetActiveFreezeOwner()
    {
        CollectAllPlayers();

        Player winner = null;
        int winnerId = int.MaxValue;

        foreach (var p in _tempPlayers)
        {
            if (p == null || p.Object == null) continue;
            if (p.FreezePhaseN == 0) continue;

            int pid = p.Object.InputAuthority.PlayerId;
            if (pid < winnerId)
            {
                winnerId = pid;
                winner = p;
            }
        }

        return winner;
    }

    private void ResolveContentionKeepSmallestId()
    {
        var owner = GetActiveFreezeOwner();
        if (owner == null) return;

        int ownerId = owner.Object.InputAuthority.PlayerId;

        foreach (var p in _tempPlayers)
        {
            if (p == null || p.Object == null) continue;
            if (p == owner) continue;

            if (p.FreezePhaseN != 0)
            {
                int pid = p.Object.InputAuthority.PlayerId;
                if (pid != ownerId)
                {
                    p.FreezePhaseN = 0;
                    p.FreezeSecondsLeft = 0f;
                }
            }
        }
    }

    private Transform TryGetOtherPlayerRoot()
    {
        CollectAllPlayers();
        foreach (var p in _tempPlayers)
        {
            if (p == null || p == this) continue;
            return p.transform;
        }

        return null;
    }

    private void TeleportAbove(Transform other)
    {
        if (other == null) return;

        var targetPos = other.position + new Vector3(0, 2f, 0f);

        NetVelocity = Vector3.zero;

        if (_characterController != null)
        {
            try
            {
                _characterController.Velocity = Vector3.zero;
            }
            catch
            {
            }

            try
            {
                _characterController.Teleport(targetPos);
                return;
            }
            catch
            {
            }
        }

        transform.position = targetPos;
    }

    #endregion

    #region RPC

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool value) => IsReady = value;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNick(NetworkString<_16> nick)
    {
        string s = nick.ToString().Trim();
        if (string.IsNullOrEmpty(s))
        {
            Nick = $"P{Object.InputAuthority.PlayerId}";
        }
        else
        {
            if (s.Length > 16) s = s.Substring(0, 16);
            Nick = s;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TeleportToInitialSpawn()
    {
        RPC_TeleportToPosition(InitialSpawnPosition);

        if (Object.HasInputAuthority)
        {
            Debug.Log($"<color=green>İlk spawn noktasına dönüyorsun!</color>");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TeleportToPosition(Vector3 targetPosition)
    {
        NetVelocity = Vector3.zero;

        if (_characterController != null)
        {
            try
            {
                _characterController.Velocity = Vector3.zero;
                _characterController.Teleport(targetPosition);

                if (Object.HasInputAuthority)
                {
                    Debug.Log($"<color=green>Spawn noktasına ışınlandınız!</color>");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Teleport hatası: {e.Message}");
                transform.position = targetPosition;
            }
        }
        else
        {
            transform.position = targetPosition;
        }

        NetIsTouchingWall = false;
        NetIsJumping = false;
    }

    #endregion
}