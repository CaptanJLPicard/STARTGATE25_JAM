using UnityEngine;
using Fusion;

[AddComponentMenu("Movement/Fusion PunchMachine PingPong (Ultra Smooth)")]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class PunchMachine : NetworkBehaviour
{
    [Header("Targets (World Space)")]
    public Transform pointA;
    public Transform pointB;

    [Header("Movement")]
    [Min(0.001f)] public float speedAToB = 2f;
    [Min(0.001f)] public float speedBToA = 1f;
    [Min(0f)] public float waitAtEnds = 0f;
    [Min(0)] public int repeatCycles = 0;   // 0 => infinite
    public bool playOnSpawned = true;
    public AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Render Smoothing (Client)")]
    [Tooltip("0 = kapalı. 0.03-0.08 arası çok iyi.")]
    [Range(0f, 0.2f)] public float renderSmoothTime = 0.06f;

    [Tooltip("Çok geride kalırsa (teleport gibi) direkt target'a snapler.")]
    public float snapDistance = 1.5f;

    [Header("Respawn On Trigger (Fusion)")]
    [Tooltip("Boş bırakırsan sahneden LevelManager otomatik bulunur.")]
    [SerializeField] private LevelManager levelManager;

    [Tooltip("İstersen sadece host respawn etsin.")]
    [SerializeField] private bool onlyIfHost = true;

    // --- Networked state (host drives, clients receive) ---
    [Networked] private NetworkBool GoingAToB { get; set; }   // true: A->B, false: B->A
    [Networked] private float T { get; set; }                 // 0..1
    [Networked] private TickTimer WaitTimer { get; set; }
    [Networked] private int DoneCycles { get; set; }
    [Networked] private NetworkBool Playing { get; set; }

    // --- Render smoothing state ---
    private Vector3 _renderPos;
    private Vector3 _renderVel;
    private bool _renderInit;

    private void Awake()
    {
        // LevelManager otomatik bul
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
    }

    public override void Spawned()
    {
        if (!pointA || !pointB)
        {
            Debug.LogWarning("[PunchMachine] pointA / pointB atanmalı.");
            return;
        }

        // render init (herkeste)
        _renderPos = transform.position;
        _renderVel = Vector3.zero;
        _renderInit = true;

        if (HasStateAuthority)
        {
            // En yakın uca snap
            Vector3 a = pointA.position;
            Vector3 b = pointB.position;

            bool startAtA = Vector3.Distance(transform.position, a) <= Vector3.Distance(transform.position, b);

            // A'daysak ilk A->B (T=0), B'deysek ilk B->A (T=1)
            GoingAToB = startAtA;
            T = startAtA ? 0f : 1f;

            DoneCycles = 0;
            Playing = playOnSpawned;
            WaitTimer = TickTimer.None;
        }
    }

    public void StartMove()
    {
        if (HasStateAuthority)
        {
            Playing = true;
            WaitTimer = TickTimer.None;
        }
    }

    public void StopMove()
    {
        if (HasStateAuthority)
        {
            Playing = false;
            WaitTimer = TickTimer.None;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (!Playing) return;
        if (!pointA || !pointB) return;

        // döngü bitti mi?
        if (repeatCycles > 0 && DoneCycles >= repeatCycles)
        {
            Playing = false;
            return;
        }

        // uçta bekleme
        if (WaitTimer.IsRunning && !WaitTimer.Expired(Runner))
            return;

        Vector3 a = pointA.position;
        Vector3 b = pointB.position;

        float dist = Vector3.Distance(a, b);
        if (dist < 0.0001f) return;

        float speed = GoingAToB ? speedAToB : speedBToA;

        // Tick tabanlı t artışı/azalışı
        float step = (speed / dist) * Runner.DeltaTime;

        if (GoingAToB)
        {
            T = Mathf.Min(1f, T + step);

            if (T >= 1f)
            {
                // B'ye vardık
                if (waitAtEnds > 0f)
                    WaitTimer = TickTimer.CreateFromSeconds(Runner, waitAtEnds);

                GoingAToB = false;
            }
        }
        else
        {
            T = Mathf.Max(0f, T - step);

            if (T <= 0f)
            {
                // A'ya vardık => 1 cycle tamam
                if (waitAtEnds > 0f)
                    WaitTimer = TickTimer.CreateFromSeconds(Runner, waitAtEnds);

                GoingAToB = true;
                DoneCycles++;
            }
        }
    }

    public override void Render()
    {
        if (!pointA || !pointB) return;

        float e = ease != null ? ease.Evaluate(Mathf.Clamp01(T)) : T;

        // Network state’den hedef pozisyon
        Vector3 target = Vector3.LerpUnclamped(pointA.position, pointB.position, e);

        if (!_renderInit)
        {
            _renderPos = target;
            _renderVel = Vector3.zero;
            _renderInit = true;
        }

        // Çok uzaksa snaple (packet loss / join in progress / teleport)
        if (renderSmoothTime <= 0f || Vector3.Distance(_renderPos, target) > snapDistance)
        {
            _renderPos = target;
            _renderVel = Vector3.zero;
        }
        else
        {
            // Ekstra ipeksi smoothing
            _renderPos = Vector3.SmoothDamp(
                _renderPos,
                target,
                ref _renderVel,
                renderSmoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );
        }

        transform.position = _renderPos;
    }

    // === TRIGGER: Player girince respawn ===
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);

        if (levelManager == null) return;

        // sadece host
        if (onlyIfHost && !levelManager.HasStateAuthority) return;

        var player = other.GetComponent<Player>();
        if (player != null)
            levelManager.ServerRespawn(player);
    }

    private void OnDrawGizmosSelected()
    {
        if (!pointA || !pointB) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pointA.position, 0.06f);
        Gizmos.DrawSphere(pointB.position, 0.06f);
        Gizmos.DrawLine(pointA.position, pointB.position);
    }
}
