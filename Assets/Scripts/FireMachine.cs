using Fusion;
using UnityEngine;

[AddComponentMenu("Traps/Flamethrower Trap (Fusion)")]
public class FireMachine : NetworkBehaviour
{
    [Header("Flame Object (enable/disable)")]
    [SerializeField] private GameObject flameObject;     // aç/kapatýlacak GO (VFX/mesh vs)
    [SerializeField] private Collider flameTrigger;       // IsTrigger = true collider (alevin alaný)

    [Header("Timing")]
    [Min(0.05f)] public float onDuration = 1.5f;          // alev açýk kalma süresi
    [Min(0.05f)] public float offDuration = 2.0f;         // alev kapalý kalma süresi
    public bool startOn = false;

    [Networked] private NetworkBool IsOn { get; set; }
    [Networked] private TickTimer NextToggle { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsOn = startOn;
            NextToggle = TickTimer.CreateFromSeconds(Runner, IsOn ? onDuration : offDuration);
        }

        ApplyState(IsOn);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (NextToggle.Expired(Runner))
        {
            IsOn = !IsOn;
            NextToggle = TickTimer.CreateFromSeconds(Runner, IsOn ? onDuration : offDuration);
        }

        // Her tickte deðil, sadece deðiþince Apply yapmak istersen:
        // (Basit tutmak için her tickte güvenli þekilde uyguluyoruz)
        ApplyState(IsOn);
    }

    private void ApplyState(bool on)
    {
        if (flameObject != null && flameObject.activeSelf != on)
            flameObject.SetActive(on);

        if (flameTrigger != null && flameTrigger.enabled != on)
            flameTrigger.enabled = on;
    }
}