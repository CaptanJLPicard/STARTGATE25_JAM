using System.Collections;
using UnityEngine;

[AddComponentMenu("Movement/World PingPong Between Transforms (Dual Speed)")]
public class PunchMachine : MonoBehaviour
{
    [Header("Targets (World Space)")]
    public Transform pointA;
    public Transform pointB;

    [Header("Movement")]
    [Min(0.001f)] public float speedAToB = 2f; // A -> B hızı (m/sn)
    [Min(0.001f)] public float speedBToA = 1f; // B -> A hızı (m/sn)
    [Min(0f)] public float waitAtEnds = 0f;    // uçlarda bekleme (sn)
    [Min(0)] public int repeatCycles = 0;      // 0 => sonsuz, 1 döngü = A→B→A
    public bool playOnEnable = true;
    public AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    Coroutine _routine;
    int _doneCycles;

    void OnEnable()
    {
        if (playOnEnable) StartMove();
    }

    void OnDisable()
    {
        StopMove();
    }

    public void StartMove()
    {
        if (!pointA || !pointB)
        {
            Debug.LogWarning("[PingPong] pointA ve/veya pointB atanmalı.");
            return;
        }
        StopMove();
        _doneCycles = 0;
        _routine = StartCoroutine(MoveRoutine());
    }

    public void StopMove()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    IEnumerator MoveRoutine()
    {
        // en yakın uca yerleştir
        Vector3 a = pointA.position;
        Vector3 b = pointB.position;
        transform.position = (Vector3.Distance(transform.position, a) <= Vector3.Distance(transform.position, b)) ? a : b;

        bool Infinite() => repeatCycles == 0;

        while (Infinite() || _doneCycles < repeatCycles)
        {
            // A -> B (farklı hız)
            yield return MoveSegment(() => pointA.position, () => pointB.position, speedAToB);
            if (waitAtEnds > 0f) yield return new WaitForSeconds(waitAtEnds);

            // B -> A (farklı hız)
            yield return MoveSegment(() => pointB.position, () => pointA.position, speedBToA);
            if (waitAtEnds > 0f) yield return new WaitForSeconds(waitAtEnds);

            _doneCycles++;
        }

        _routine = null;
    }

    IEnumerator MoveSegment(System.Func<Vector3> from, System.Func<Vector3> to, float speed)
    {
        // başlangıç anındaki konumları al
        Vector3 start = from();
        Vector3 end = to();
        float dist = Vector3.Distance(start, end);
        if (dist < 1e-5f) { yield return null; yield break; }

        float duration = dist / Mathf.Max(0.0001f, speed);
        float t = 0f;

        transform.position = start;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            // hedefler hareket ediyorsa canlı oku
            Vector3 curFrom = from();
            Vector3 curTo = to();

            float e = ease.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.LerpUnclamped(curFrom, curTo, e);

            yield return null;
        }

        transform.position = to();
    }

    void OnDrawGizmosSelected()
    {
        if (!pointA || !pointB) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pointA.position, 0.06f);
        Gizmos.DrawSphere(pointB.position, 0.06f);
        Gizmos.DrawLine(pointA.position, pointB.position);
    }
}