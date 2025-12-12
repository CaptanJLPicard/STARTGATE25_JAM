using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText, countText;
    [SerializeField] private Button joinButton;
    [SerializeField] private float buttonCountdown = 0;

    public void Bind(SessionInfo info, Action<string> onJoin)
    {
        if (nameText) nameText.text = info.Name;
        if (countText)
            countText.text = info.MaxPlayers > 0
                ? $"{info.PlayerCount}/{info.MaxPlayers}"
                : info.PlayerCount.ToString();

        if (joinButton)
        {
            bool canJoin = info.IsOpen && (info.MaxPlayers <= 0 || info.PlayerCount < info.MaxPlayers);
            joinButton.interactable = canJoin;
            joinButton.onClick.RemoveAllListeners();
            string n = info.Name;
            joinButton.onClick.AddListener(() => ButtonCountdown());
            if (buttonCountdown <= 0) joinButton.onClick.AddListener(() => onJoin?.Invoke(n));
        }
    }

    private void ButtonCountdown(int countdown = 5)
    {
        buttonCountdown = countdown;
    }

    private void Update()
    {
        buttonCountdown -= Time.deltaTime;
        if (buttonCountdown > 0) joinButton.interactable = false;
    }
}