using Fusion;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkBehaviour
{
    private FusionManager fm;

    [SerializeField] private int currentScene;
    [SerializeField] private GameObject EscapePanel;
    [SerializeField] private GameObject SettingsPanel;
    [SerializeField] private GameObject restartButton; // <-- Restart butonu referansı

    public bool stop;
    [SerializeField] private Transform spawnPosition;

    public override void Spawned()
    {
        if (fm == null)
            fm = FindAnyObjectByType<FusionManager>();

        // Restart butonu sadece StateAuthority (host) ise görünsün
        if (restartButton != null)
            restartButton.SetActive(HasStateAuthority);
    }

    private void Awake()
    {
        var spawnObj = GameObject.FindWithTag("SpawnPoint");
        if (spawnObj != null)
            spawnPosition = spawnObj.transform;
        else
            Debug.LogWarning("SpawnPoint tag'li obje bulunamadı!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        if (other.gameObject.CompareTag("Player") && currentScene > 0)
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                // İLK SPAWN YERİNE IŞINLA
                player.RPC_TeleportToInitialSpawn();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            stop = !stop;
            EscapePanel.SetActive(stop);
        }
    }

    // === MAIN MENU ===
    public void MainMenuBtn()
    {
        if (HasStateAuthority)
            ExecuteMainMenu();
        else
            RPC_MainMenu();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_MainMenu()
    {
        ExecuteMainMenu();
    }

    private void ExecuteMainMenu()
    {
        if (fm != null)
        {
            _ = fm.LeaveToMainMenuAsync();
        }
    }

    // === RESTART ===
    public void RestartBtn()
    {
        // Güvenlik: Host değilse hiç restart yapma
        if (!HasStateAuthority)
            return;

        ExecuteRestart();
    }

    private void ExecuteRestart()
    {
        if (fm == null) return;

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"<color=yellow>Level yeniden başlatılıyor... (buildIndex={buildIndex})</color>");
        _ = fm.NextLevel(buildIndex);
    }

    // === SETTINGS ===
    public void SettingsBtn()
    {
        SettingsPanel.SetActive(true);
        EscapePanel.SetActive(false);
    }

    public void SettingsBtnClose()
    {
        SettingsPanel.SetActive(false);
        EscapePanel.SetActive(true);
    }
}
