using TMPro;
using UnityEngine;

public class LanguageTextHandler : MonoBehaviour
{
    public string EN;
    public string TR;
    public string FR;
    public string ITL;
    public string GRM;
    public string ISP;
    public string BUL;
    public string CZE;

    private void Start()
    {
        if (gameObject.CompareTag("LanguageText"))
        {
            TextDurumunuGuncelle(PlayerPrefs.GetInt("DilTercihi"));
        }
    }

    public void TextDurumunuGuncelle(int tercihNedir)
    {
        switch (tercihNedir)
        {
            case 0:
                GetComponent<TextMeshProUGUI>().text = EN;
                break;
            case 1:
                GetComponent<TextMeshProUGUI>().text = TR;
                break;
            case 2:
                GetComponent<TextMeshProUGUI>().text = FR;
                break;
            case 3:
                GetComponent<TextMeshProUGUI>().text = ITL;
                break;
            case 4:
                GetComponent<TextMeshProUGUI>().text = GRM;
                break;
            case 5:
                GetComponent<TextMeshProUGUI>().text = ISP;
                break;
            case 6:
                GetComponent<TextMeshProUGUI>().text = BUL;
                break;
            case 7:
                GetComponent<TextMeshProUGUI>().text = CZE;
                break;
        }
    }
}