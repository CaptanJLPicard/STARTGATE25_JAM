using Fusion;
using UnityEngine;
using UnityEngine.Video;

public class LastCutsceneTriggger : NetworkBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] VideoPlayer videoPlayer;
    private FusionManager _fm;
    [Networked] private TickTimer life { get; set; }

    private void Awake() => _fm = FindAnyObjectByType<FusionManager>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("UFO"))
        {
            RPC_PlayForAll();
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayForAll()
    {
        if (panel)
        {
            panel.SetActive(true);
            videoPlayer.Play();
        }
        life = TickTimer.CreateFromSeconds(Runner, 16.33f);
    }
    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
        {
            RpcSound();
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSound()
    {
        if (!Object || !Object.HasStateAuthority) return; 
        if (_fm != null) _ = _fm.NextLevel(0);
    }
}
