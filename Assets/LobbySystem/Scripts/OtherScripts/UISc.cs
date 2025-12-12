using UnityEngine;

public class UISc : MonoBehaviour
{
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;

    [SerializeField] private GameObject MainMenuCamera;
    [SerializeField] private GameObject JoinRoomCamera;
    [SerializeField] private GameObject CreateRoomCamera;
    [SerializeField] private GameObject SettingsCamera;

    private void Start()
    {
        Time.timeScale = 1;
    }

    public void JoinRoomPanelBtn()
    {
        joinRoomPanel.gameObject.SetActive(true);
        settingsPanel.gameObject.SetActive(false);
        lobbyPanel.gameObject.SetActive(false);
        MainMenuCamera.SetActive(false);
        JoinRoomCamera.SetActive(true);
        CreateRoomCamera.SetActive(false);
        SettingsCamera.SetActive(false);
    }

    public void LobbyPanelBtn()
    {
        lobbyPanel.gameObject.SetActive(true);
        settingsPanel.gameObject.SetActive(false);
        joinRoomPanel.gameObject.SetActive(false);
        MainMenuCamera.SetActive(false);
        JoinRoomCamera.SetActive(false);
        CreateRoomCamera.SetActive(true);
        SettingsCamera.SetActive(false);
    }

    public void BackBtn()
    {
        joinRoomPanel.gameObject.SetActive(false);
        lobbyPanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        MainMenuCamera.SetActive(true);
        JoinRoomCamera.SetActive(false);
        CreateRoomCamera.SetActive(false);
        SettingsCamera.SetActive(false);
    }
    public void SettingsPanelBtn()
    {
        settingsPanel.gameObject.SetActive(true);
        lobbyPanel.gameObject.SetActive(false);
        joinRoomPanel.gameObject.SetActive(false);
        MainMenuCamera.SetActive(false);
        JoinRoomCamera.SetActive(false);
        CreateRoomCamera.SetActive(false);
        SettingsCamera.SetActive(true);
    }

    public void QuitBtn() => Application.Quit();
}