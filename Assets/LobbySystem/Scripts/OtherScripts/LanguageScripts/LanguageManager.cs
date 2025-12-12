using TMPro;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public TMP_Dropdown languageChoice;
    private LanguageTextHandler[] objeler;

    private void Start()
    {
        objeler = FindObjectsByType<LanguageTextHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (!PlayerPrefs.HasKey("DilTercihi"))
        {
            if (Application.systemLanguage == SystemLanguage.English) PlayerPrefs.SetInt("DilTercihi", 0);
            else if (Application.systemLanguage == SystemLanguage.Turkish) PlayerPrefs.SetInt("DilTercihi", 1);
            else if (Application.systemLanguage == SystemLanguage.French) PlayerPrefs.SetInt("DilTercihi", 2);
            else if (Application.systemLanguage == SystemLanguage.Italian) PlayerPrefs.SetInt("DilTercihi", 3);
            else if (Application.systemLanguage == SystemLanguage.German) PlayerPrefs.SetInt("DilTercihi", 4);
            else if (Application.systemLanguage == SystemLanguage.Spanish) PlayerPrefs.SetInt("DilTercihi", 5);
            else if (Application.systemLanguage == SystemLanguage.Bulgarian) PlayerPrefs.SetInt("DilTercihi", 6);
            else if (Application.systemLanguage == SystemLanguage.Czech) PlayerPrefs.SetInt("DilTercihi", 7);
        }

        languageChoice.value = PlayerPrefs.GetInt("DilTercihi", 1);
        DilKontrol(PlayerPrefs.GetInt("DilTercihi", 1), false);
    }

    private void TextlerinKontrolu(int dilIndex)
    {
        foreach (var item in objeler)
        {
            item.TextDurumunuGuncelle(dilIndex);
        }
    }

    public void SecilenNedir(int gelenDeger)
    {
        DilKontrol(gelenDeger, true);
    }

    private void DilKontrol(int dilIndex, bool dropmu)
    {
        UpdateTexts(dilIndex, dropmu);
    }

    private void UpdateTexts(int dilIndex, bool dropmu)
    {
        TextlerinKontrolu(dilIndex);
        if (dropmu)
            PlayerPrefs.SetInt("DilTercihi", dilIndex);
    }
}