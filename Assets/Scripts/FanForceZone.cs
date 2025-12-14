using Fusion;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class FanForceZone : NetworkBehaviour
{
    [Header("Zone")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector3 boxHalfExtents = new Vector3(2f, 2f, 4f);

    [Header("Force")]
    [SerializeField] private float pushStrength = 6f;
    [SerializeField] private float pushPerSecond = 10f;
    [SerializeField] private float maxDistanceFalloff = 6f;

    [Header("Direction Angle Offset (Degrees)")]
    [SerializeField] private float yawOffset = 0f;
    [SerializeField] private float pitchOffset = 0f;

    [Header("Updraft Settings")]
    [Tooltip("pushDir.y bu deðerden büyükse alttan yukarý rüzgar sayýlýr")]
    [SerializeField] private float updraftThreshold = 0.35f;

    private float _timer;

    private readonly HashSet<Player> _current = new();
    private readonly HashSet<Player> _previous = new();

    public override void Spawned()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    public override void FixedUpdateNetwork()
    {
        // Kuvveti tek otorite uygulasýn (host/state authority)
        if (!HasStateAuthority) return;

        _timer += Runner.DeltaTime;
        float interval = 1f / Mathf.Max(1f, pushPerSecond);
        if (_timer < interval) return;
        _timer = 0f;

        // önceki tick seti
        _previous.Clear();
        foreach (var p in _current) _previous.Add(p);
        _current.Clear();

        Collider[] hits = Physics.OverlapBox(
            transform.position,
            boxHalfExtents,
            transform.rotation,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        Quaternion rot = transform.rotation * Quaternion.Euler(pitchOffset, yawOffset, 0f);
        Vector3 pushDir = (rot * Vector3.forward).normalized;

        bool isUpdraft = pushDir.y > updraftThreshold;

        foreach (var h in hits)
        {
            var player = h.GetComponentInParent<Player>();
            if (!player || !player.Object) continue;

            _current.Add(player);

            float strength = pushStrength;

            if (maxDistanceFalloff > 0f)
            {
                float d = Vector3.Distance(transform.position, player.transform.position);
                strength *= Mathf.Clamp01(1f - (d / maxDistanceFalloff));
            }

            if (strength <= 0.001f) continue;

            Vector3 impulse = pushDir * strength;

            // Player tarafýnda eklediðin RPC'ler
            //player.RPC_AddExternalPush(impulse);
            //player.RPC_SetUpdraft(isUpdraft);
        }

        // alandan çýkanlar -> updraft kapat
        foreach (var p in _previous)
        {
            //if (!_current.Contains(p))
                //p.RPC_SetUpdraft(false);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);

        Quaternion rot = transform.rotation * Quaternion.Euler(pitchOffset, yawOffset, 0f);
        Vector3 dir = rot * Vector3.forward;

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, dir.normalized * 2f);
    }
#endif
}
