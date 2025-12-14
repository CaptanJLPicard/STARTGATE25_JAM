using Fusion;
using UnityEngine;

[AddComponentMenu("Movement/Network Rotate Machine (Fusion)")]
public class RotateMachine : NetworkBehaviour
{
    public enum Mode
    {
        Continuous360,   // Sürekli aynı yönde 360° döngüler
        PingPong360      // 360° ileri, 360° geri (fan gibi ters yön)
    }

    [Header("Pivot (World Space)")]
    public Transform pivot;                 // Dönüş merkezi
    public Vector3 pivotOffset = Vector3.zero;

    [Header("Axis (World Space)")]
    public Vector3 worldAxis = Vector3.up;  // Dönüş ekseni

    [Header("Rotation")]
    [Min(0.001f)] public float speedForwardDeg = 360f; // ileri dönüş hızı (deg/sn)
    [Min(0.001f)] public float speedBackDeg = 360f;    // geri dönüş hızı (deg/sn)
    [Min(0f)] public float waitAtEnds = 0f;            // 360 bitince bekleme
    public Mode mode = Mode.Continuous360;
    public AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Player Push Settings")]
    [SerializeField] private float pushForce = 15f;           // İtme kuvveti
    [SerializeField] private float tangentialMultiplier = 1f; // Teğetsel (dönüş yönü) kuvvet çarpanı
    [SerializeField] private float radialMultiplier = 0.5f;   // Radyal (dışa doğru) kuvvet çarpanı
    [SerializeField] private LayerMask playerLayer;           // Player layer

    [Header("Optional")]
    public bool keepRelativeOffset = true;

    // Networked state - tüm client'larda senkronize
    [Networked] private float CurrentAngle { get; set; }
    [Networked] private NetworkBool IsReversing { get; set; }
    [Networked] private float WaitTimer { get; set; }

    // Local
    private Vector3 _initialOffset;
    private Quaternion _initialRot;
    private Vector3 _axis;
    private bool _initialized;
    private float _previousAngle;

    public override void Spawned()
    {
        if (!pivot)
        {
            Debug.LogWarning("[RotateMachine] pivot atanmalı.");
            return;
        }

        _axis = worldAxis.sqrMagnitude < 1e-6f ? Vector3.up : worldAxis.normalized;
        Vector3 pivotPos = pivot.position + pivotOffset;
        _initialOffset = transform.position - pivotPos;
        _initialRot = transform.rotation;
        _initialized = true;
        _previousAngle = CurrentAngle;
    }

    public override void FixedUpdateNetwork()
    {
        if (!_initialized || !pivot) return;

        // Sadece StateAuthority açı hesaplar
        if (Object.HasStateAuthority)
        {
            UpdateRotationState();
        }

        // Tüm client'larda transform güncelle
        ApplyRotation();
    }

    private void UpdateRotationState()
    {
        // Bekleme varsa bekle
        if (WaitTimer > 0)
        {
            WaitTimer -= Runner.DeltaTime;
            return;
        }

        float speed = IsReversing ? speedBackDeg : speedForwardDeg;
        float deltaAngle = speed * Runner.DeltaTime;

        if (IsReversing)
        {
            CurrentAngle -= deltaAngle;

            if (CurrentAngle <= 0f)
            {
                CurrentAngle = 0f;
                IsReversing = false;

                if (waitAtEnds > 0f)
                    WaitTimer = waitAtEnds;
            }
        }
        else
        {
            CurrentAngle += deltaAngle;

            if (CurrentAngle >= 360f)
            {
                if (mode == Mode.PingPong360)
                {
                    CurrentAngle = 360f;
                    IsReversing = true;

                    if (waitAtEnds > 0f)
                        WaitTimer = waitAtEnds;
                }
                else
                {
                    // Continuous - sıfırla ve devam et
                    CurrentAngle -= 360f;
                }
            }
        }
    }

    private void ApplyRotation()
    {
        Vector3 livePivot = pivot.position + pivotOffset;

        float angle = IsReversing ? CurrentAngle : CurrentAngle;
        if (mode == Mode.Continuous360)
        {
            angle = CurrentAngle % 360f;
        }

        // Ease curve uygula (opsiyonel)
        float normalizedAngle = angle / 360f;
        float easedNormalized = ease.Evaluate(normalizedAngle);
        float easedAngle = easedNormalized * 360f;

        // Pozisyonu pivot etrafında döndür
        if (keepRelativeOffset)
        {
            Vector3 rotatedOffset = Quaternion.AngleAxis(easedAngle, _axis) * _initialOffset;
            transform.position = livePivot + rotatedOffset;
        }

        // Objeyi kendi ekseni etrafında döndür
        transform.rotation = Quaternion.AngleAxis(easedAngle, _axis) * _initialRot;

        _previousAngle = CurrentAngle;
    }

    // Physics collision - Player'ı it
    private void OnCollisionStay(Collision collision)
    {
        TryPushPlayer(collision.gameObject, collision.contacts[0].point);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPushPlayer(other.gameObject, other.ClosestPoint(transform.position));
    }

    private void TryPushPlayer(GameObject obj, Vector3 contactPoint)
    {
        // Layer kontrolü
        if (playerLayer != 0 && ((1 << obj.layer) & playerLayer) == 0)
            return;

        Player player = obj.GetComponentInParent<Player>();
        if (player == null) return;

        // Sadece StateAuthority (host) push uygular
        if (!Object.HasStateAuthority) return;

        // Dönüş hızını hesapla
        float currentSpeed = IsReversing ? -speedBackDeg : speedForwardDeg;
        float angularVelocity = currentSpeed * Mathf.Deg2Rad; // rad/s

        // Pivot pozisyonu
        Vector3 pivotPos = pivot.position + pivotOffset;

        // Contact point'ten pivot'a vektör
        Vector3 radialDir = (contactPoint - pivotPos);
        radialDir.y = 0; // Yatay düzlemde
        float radius = radialDir.magnitude;

        if (radius < 0.01f) return;

        radialDir = radialDir.normalized;

        // Teğetsel yön (dönüş yönünde)
        Vector3 tangentDir = Vector3.Cross(_axis, radialDir).normalized;

        // Lineer hız = açısal hız * yarıçap
        float linearSpeed = Mathf.Abs(angularVelocity) * radius;

        // Push vektörü hesapla
        Vector3 pushDir = Vector3.zero;

        // Teğetsel kuvvet (dönüş yönünde)
        pushDir += tangentDir * Mathf.Sign(angularVelocity) * tangentialMultiplier;

        // Radyal kuvvet (dışa doğru)
        pushDir += radialDir * radialMultiplier;

        pushDir = pushDir.normalized;

        // Final push
        Vector3 finalPush = pushDir * linearSpeed * pushForce * Runner.DeltaTime;

        // Player'ın ExternalPush'una ekle (mevcut fan sistemi gibi)
        //player.RPC_AddExternalPush(finalPush);
    }

    // Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!pivot) return;

        Vector3 p = pivot.position + pivotOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(p, 0.06f);

        Vector3 axis = worldAxis.sqrMagnitude < 1e-6f ? Vector3.up : worldAxis.normalized;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(p, p + axis * 0.5f);

        // Dönüş yönünü göster
        Gizmos.color = Color.cyan;
        Vector3 tangent = Vector3.Cross(axis, Vector3.forward).normalized;
        if (tangent.sqrMagnitude < 0.1f)
            tangent = Vector3.Cross(axis, Vector3.right).normalized;
        Gizmos.DrawLine(p, p + tangent * 0.3f);
    }
}
