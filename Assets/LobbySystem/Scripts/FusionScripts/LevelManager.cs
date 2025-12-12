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
    [SerializeField] private GameObject restartButton;

    public bool stop;
    [SerializeField] private Transform spawnPosition;

    public override void Spawned()
    {
        if (fm == null)
            fm = FindFirstObjectByType<FusionManager>(FindObjectsInactive.Include);

        // Restart butonu sadece host
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
                player.RPC_TeleportToInitialSpawn();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            stop = !stop;
            if (EscapePanel != null) EscapePanel.SetActive(stop);
        }
    }

    // === MAIN MENU ===
    public void MainMenuBtn()
    {
        // Herkes (client dahil) kendi tarafında çıkabilsin
        ExecuteMainMenu();
    }

    private void ExecuteMainMenu()
    {
        if (fm == null)
            fm = FindFirstObjectByType<FusionManager>(FindObjectsInactive.Include);

        if (fm != null)
        {
            _ = fm.LeaveToMainMenuAsync();
        }
        else
        {
            Debug.LogWarning("FusionManager bulunamadı, odadan çıkılamadı!");
        }
    }

    // === RESTART ===
    public void RestartBtn()
    {
        if (!HasStateAuthority) return;
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
        if (SettingsPanel != null) SettingsPanel.SetActive(true);
        if (EscapePanel != null) EscapePanel.SetActive(false);
    }

    public void SettingsBtnClose()
    {
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
        if (EscapePanel != null) EscapePanel.SetActive(true);
    }
}