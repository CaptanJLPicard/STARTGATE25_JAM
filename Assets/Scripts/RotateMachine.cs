using System.Collections;
using UnityEngine;

[AddComponentMenu("Movement/World Spin Around Pivot 360 (Dual Speed)")]
public class RotateMachine : MonoBehaviour
{
    public enum Mode
    {
        Continuous360,   // Sürekli ayný yönde 360° döngüler
        PingPong360      // 360° ileri, 360° geri (fan gibi ters yön)
    }

    [Header("Pivot (World Space)")]
    public Transform pivot;                 // Dönüþ merkezi (belirlediðin yer)
    public Vector3 pivotOffset = Vector3.zero; // Pivot’a ek offset (istersen)

    [Header("Axis (World Space)")]
    public Vector3 worldAxis = Vector3.up;  // Fan ekseni (Y genelde)

    [Header("Rotation")]
    [Min(0.001f)] public float speedForwardDeg = 360f; // ileri dönüþ hýzý (deg/sn)
    [Min(0.001f)] public float speedBackDeg = 360f; // geri dönüþ hýzý (deg/sn)
    [Min(0f)] public float waitAtEnds = 0f;            // 360 bitince bekleme
    [Min(0)] public int repeatCycles = 0;             // 0 => sonsuz (1 cycle = 360 ileri (+ geri varsa))
    public bool playOnEnable = true;
    public Mode mode = Mode.Continuous360;
    public AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Optional")]
    public bool keepRelativeOffset = true; // Objeyi pivot’a göre mevcut mesafesinde döndür

    Coroutine _routine;
    int _doneCycles;

    void OnEnable()
    {
        if (playOnEnable) StartSpin();
    }

    void OnDisable()
    {
        StopSpin();
    }

    public void StartSpin()
    {
        if (!pivot)
        {
            Debug.LogWarning("[Rotator] pivot atanmalý.");
            return;
        }

        StopSpin();
        _doneCycles = 0;
        _routine = StartCoroutine(SpinRoutine());
    }

    public void StopSpin()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    IEnumerator SpinRoutine()
    {
        bool Infinite() => repeatCycles == 0;

        // Dünya eksenini normalize et
        Vector3 axis = worldAxis.sqrMagnitude < 1e-6f ? Vector3.up : worldAxis.normalized;

        // Pivot noktasý
        Vector3 pivotPos = pivot.position + pivotOffset;

        // Objeyi pivot etrafýnda döndürürken, istersen mevcut offsetini koru
        Vector3 initialOffset = transform.position - pivotPos;
        Quaternion initialRot = transform.rotation;

        while (Infinite() || _doneCycles < repeatCycles)
        {
            // 360 ileri
            yield return Rotate360(pivotPos, axis, speedForwardDeg, initialOffset, initialRot);

            if (waitAtEnds > 0f) yield return new WaitForSeconds(waitAtEnds);

            if (mode == Mode.PingPong360)
            {
                // 360 geri
                yield return Rotate360(pivotPos, axis, speedBackDeg, initialOffset, initialRot, reverse: true);

                if (waitAtEnds > 0f) yield return new WaitForSeconds(waitAtEnds);
            }

            _doneCycles++;
        }

        _routine = null;
    }

    IEnumerator Rotate360(
        Vector3 pivotPos,
        Vector3 axis,
        float speedDegPerSec,
        Vector3 initialOffset,
        Quaternion initialRot,
        bool reverse = false
    )
    {
        float duration = 360f / Mathf.Max(0.0001f, speedDegPerSec);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = ease.Evaluate(Mathf.Clamp01(t));

            float angle = Mathf.LerpUnclamped(0f, 360f, e);
            if (reverse) angle = -angle;

            // Pivot hareket ediyorsa canlý oku
            Vector3 livePivot = pivot.position + pivotOffset;

            // Pozisyonu pivot etrafýnda döndür (fan kanadý gibi)
            if (keepRelativeOffset)
            {
                Vector3 rotatedOffset = Quaternion.AngleAxis(angle, axis) * initialOffset;
                transform.position = livePivot + rotatedOffset;
            }

            // Objeyi de kendi ekseni etrafýnda döndür (görsel pervane dönüþü)
            transform.rotation = Quaternion.AngleAxis(angle, axis) * initialRot;

            yield return null;
        }

        // Son snap
        float finalAngle = reverse ? -360f : 360f;
        Vector3 finalPivot = pivot.position + pivotOffset;

        if (keepRelativeOffset)
        {
            Vector3 rotatedOffset = Quaternion.AngleAxis(finalAngle, axis) * initialOffset;
            transform.position = finalPivot + rotatedOffset;
        }

        transform.rotation = Quaternion.AngleAxis(finalAngle, axis) * initialRot;
    }

    void OnDrawGizmosSelected()
    {
        if (!pivot) return;

        Vector3 p = pivot.position + pivotOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(p, 0.06f);

        Vector3 axis = worldAxis.sqrMagnitude < 1e-6f ? Vector3.up : worldAxis.normalized;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(p, p + axis * 0.5f);
    }
}