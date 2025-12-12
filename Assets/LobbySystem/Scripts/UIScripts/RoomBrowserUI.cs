using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomBrowserUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FusionManager fusion;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private RoomRowUI rowPrefab;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;

    [Header("Nickname")]
    [SerializeField] private TMP_InputField nickNameInput;

    [Header("Passwords")]
    [SerializeField] private TMP_InputField createPasswordInput;
    [SerializeField] private TMP_InputField joinPasswordInput;

    [Header("Search")]
    [SerializeField] private TMP_InputField searchInput;

    [Header("Room Settings")]
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [Tooltip("Şu an seçili maksimum oyuncu sayısı.")]
    [SerializeField] private int currentMaxPlayers = 2;

    readonly Dictionary<string, RoomRowUI> _rows = new();

    [Header("Countdown")]
    [SerializeField] private float buttonCountdown = 0;

    [Header("CharacterNameText")]
    [SerializeField] private Sprite[] characterSprite;
    [SerializeField] private Image characterImage;
    private int characterIndex = 0;

    public void ChangePrefab(int i)
    {
        characterIndex += i;
        if (characterIndex <= -1) characterIndex = 3;
        else if (characterIndex >= 4) characterIndex = 0;

        if (fusion != null)
        {
            fusion.ChangeCharacterIndex(i);
        }

        if (characterImage != null && characterSprite != null && characterIndex < characterSprite.Length)
        {
            characterImage.sprite = characterSprite[characterIndex];
        }
    }

    async void OnEnable()
    {
        if (!fusion) fusion = FindFirstObjectByType<FusionManager>(FindObjectsInactive.Include);

        fusion.SessionsUpdated += HandleSessions;

        if (createButton) createButton.interactable = false;
        if (joinButton) joinButton.interactable = false;

        if (nickNameInput)
        {
            nickNameInput.characterLimit = 16;
            if (PlayerPrefs.HasKey("Nick"))
            {
                nickNameInput.text = PlayerPrefs.GetString("Nick");
            }
        }

        if (searchInput)
        {
            searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        // Dropdown ilk değerini uygula + listener ekle
        if (maxPlayersDropdown)
        {
            OnMaxPlayersDropdownChanged(maxPlayersDropdown.value);
            maxPlayersDropdown.onValueChanged.AddListener(OnMaxPlayersDropdownChanged);
        }

        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            characterIndex = PlayerPrefs.GetInt("SelectedCharacter");
            if (characterIndex < 0) characterIndex = 0;
            if (characterIndex >= 4) characterIndex = 3;

            if (characterImage != null && characterSprite != null && characterIndex < characterSprite.Length)
            {
                characterImage.sprite = characterSprite[characterIndex];
            }

            if (fusion != null)
            {
                fusion.SetCharacterIndex(characterIndex);
            }
        }

        await fusion.JoinLobbyIfNeeded();

        if (createButton) createButton.interactable = true;
        if (joinButton) joinButton.interactable = true;
    }

    private void OnDisable()
    {
        if (fusion)
        {
            fusion.SessionsUpdated -= HandleSessions;
        }

        if (searchInput)
        {
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        }

        if (maxPlayersDropdown)
        {
            maxPlayersDropdown.onValueChanged.RemoveListener(OnMaxPlayersDropdownChanged);
        }
    }

    private void SaveNickIfAny()
    {
        var nick = nickNameInput ? nickNameInput.text : null;
        if (!string.IsNullOrWhiteSpace(nick))
        {
            nick = nick.Trim();
            if (nick.Length > 16) nick = nick.Substring(0, 16);
            PlayerPrefs.SetString("Nick", nick);
        }
        else
        {
            PlayerPrefs.DeleteKey("Nick");
        }
    }

    /// <summary>
    /// Dropdown OnValueChanged(Int32) eventine bağlanacak fonksiyon.
    /// index -> dropdown.options[index].text (örn: "1","2","3") -> currentMaxPlayers
    /// </summary>
    public void OnMaxPlayersDropdownChanged(int index)
    {
        if (!maxPlayersDropdown || maxPlayersDropdown.options == null || maxPlayersDropdown.options.Count == 0)
        {
            currentMaxPlayers = 2;
            return;
        }

        if (index < 0 || index >= maxPlayersDropdown.options.Count)
            index = 0;

        string label = maxPlayersDropdown.options[index].text;
        if (!int.TryParse(label, out currentMaxPlayers))
        {
            currentMaxPlayers = 2; // parse edemezse fallback
        }

        // Debug.Log($"[RoomBrowserUI] Max players changed to: {currentMaxPlayers}");
    }

    private int GetSelectedMaxPlayers()
    {
        return currentMaxPlayers;
    }

    public void OnClickCreate()
    {
        SaveNickIfAny();

        var name = roomNameInput ? roomNameInput.text : null;
        var pwd = createPasswordInput ? createPasswordInput.text : null;

        if (buttonCountdown <= 0)
        {
            if (fusion != null)
            {
                fusion.SetCharacterIndex(characterIndex);
            }

            int maxPlayers = GetSelectedMaxPlayers();

            fusion?.Create(string.IsNullOrWhiteSpace(name) ? null : name, maxPlayers, pwd);
        }
        else
        {
            if (createButton) createButton.interactable = false;
        }

        ButtonCountdown();
    }

    private void ButtonCountdown(int countdown = 5)
    {
        buttonCountdown = countdown;
    }

    private void Update()
    {
        buttonCountdown -= Time.deltaTime;
        if (buttonCountdown <= 0 && createButton) createButton.interactable = true;
    }

    public void OnClickJoin(string roomNameFromRow = null)
    {
        SaveNickIfAny();

        var name = roomNameFromRow ?? (roomNameInput ? roomNameInput.text : null);
        if (string.IsNullOrWhiteSpace(name)) return;

        if (fusion != null)
        {
            fusion.SetCharacterIndex(characterIndex);
        }

        var pwd = joinPasswordInput ? joinPasswordInput.text : null;
        fusion?.JoinWithPassword(name, pwd);
    }

    private void HandleSessions(List<SessionInfo> sessions)
    {
        if (!contentRoot || !rowPrefab) return;

        var seen = new HashSet<string>();
        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            seen.Add(s.Name);

            if (!_rows.TryGetValue(s.Name, out var row) || !row)
            {
                row = Instantiate(rowPrefab, contentRoot);
                _rows[s.Name] = row;
            }

            row.Bind(s, OnClickJoin);
            row.transform.SetSiblingIndex(i);
        }

        foreach (var k in new List<string>(_rows.Keys))
        {
            if (!seen.Contains(k))
            {
                if (_rows[k]) Destroy(_rows[k].gameObject);
                _rows.Remove(k);
            }
        }

        string currentSearch = searchInput ? searchInput.text : null;
        ApplySearchOrdering(currentSearch);
    }

    // ==== SEARCH / ÖNERİ SİSTEMİ ====

    private void OnSearchChanged(string value)
    {
        ApplySearchOrdering(value);
    }

    private void ApplySearchOrdering(string searchText)
    {
        if (!contentRoot) return;
        if (_rows.Count == 0) return;

        var entries = new List<KeyValuePair<string, RoomRowUI>>(_rows);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            entries.Sort((a, b) =>
                string.Compare(a.Key, b.Key, System.StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            string query = searchText.ToLowerInvariant();

            entries.Sort((a, b) =>
            {
                string nameA = a.Key.ToLowerInvariant();
                string nameB = b.Key.ToLowerInvariant();

                int distA = ComputeStringDistance(query, nameA);
                int distB = ComputeStringDistance(query, nameB);

                int cmp = distA.CompareTo(distB);
                if (cmp == 0)
                {
                    return string.Compare(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
                }

                return cmp;
            });
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Value != null)
            {
                entries[i].Value.transform.SetSiblingIndex(i);
            }
        }
    }

    private int ComputeStringDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] dp = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++)
            dp[i, 0] = i;
        for (int j = 0; j <= m; j++)
            dp[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                int del = dp[i - 1, j] + 1;
                int ins = dp[i, j - 1] + 1;
                int sub = dp[i - 1, j - 1] + cost;

                int min = del;
                if (ins < min) min = ins;
                if (sub < min) min = sub;

                dp[i, j] = min;
            }
        }

        return dp[n, m];
    }
}
