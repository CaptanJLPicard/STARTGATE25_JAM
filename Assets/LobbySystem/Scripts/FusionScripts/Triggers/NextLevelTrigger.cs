using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelTrigger : NetworkBehaviour
{
    private bool _menuTransitionInProgress;
    [SerializeField] private int nextSceneIndex;
    [SerializeField] private Player[] players;

    public override void Spawned()
    {
        players = FindObjectsByType<Player>(FindObjectsSortMode.None); //calismassa players = FindObjectsOfType<Player>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            foreach (var player in players) player.enabled = false;
            if (Object == null || !Object.HasStateAuthority) return;
            Invoke("NextLevelMethod", 1.15f);
        }
    }

    private void NextLevelMethod()
    {
        // Yalnızca StateAuthority olan taraf tetiklesin (host)
        var fm = FusionManager.Instance;
        if (fm != null)
        {
            _ = fm.NextLevel(nextSceneIndex); // oyun sahnesine örnek
        }
        else
        {
            Debug.LogWarning("[NextLevelTrigger] FusionManager.Instance not found, loading locally as fallback.");
            SceneManager.LoadScene(nextSceneIndex, LoadSceneMode.Single);
        }
    }

    // UI Button -> Main Menu
    public async void MainMenuBtn()
    {
        if (_menuTransitionInProgress) return;
        _menuTransitionInProgress = true;

        var fm = FusionManager.Instance;
        if (fm != null)
        {
            await fm.LeaveToMainMenuAsync();
        }
        else
        {
            // Fallback: runner yoksa direkt lokal menü yükle
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        _menuTransitionInProgress = false;
    }
}