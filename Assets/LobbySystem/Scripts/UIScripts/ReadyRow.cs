using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReadyRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private Image dot;

    private Player bound;

    private bool NetReady =>
        bound && bound.Object != null && bound.Object.IsValid;

    public void Bind(Player p)
    {
        bound = p;
        Refresh();
    }

    public void Refresh()
    {
        if (!bound)
        {
            if (nameText) nameText.text = "";
            if (stateText) stateText.text = "";
            if (dot) dot.color = new Color(0.5f, 0.5f, 0.5f);
            return;
        }

        string nick;
        bool isReady;

        if (NetReady)
        {
            var ni = bound.Nick;
            string fallback = $"P{bound.Object.InputAuthority.PlayerId}";
            nick = ni.Length > 0 ? ni.ToString() : fallback;
            isReady = bound.IsReady;
        }
        else
        {
            nick = "CONNECTING…";
            isReady = false;
        }

        if (nameText) nameText.text = nick;
        if (stateText)
        {
            int whichLanguage = PlayerPrefs.GetInt("DilTercihi", 0);
            switch (whichLanguage)
            {
                case 0:
                    if (isReady) stateText.text = "READY";
                    else stateText.text = "WAITING";
                    break;
                case 1:
                    if (isReady) stateText.text = "HAZIR";
                    else stateText.text = "HAZIRLANIYOR";
                    break;
                case 2:
                    if (isReady) stateText.text = "PRÊT";
                    else stateText.text = "EN ATTENTE";
                    break;
                case 3:
                    if (isReady) stateText.text = "PRONTO";
                    else stateText.text = "IN ATTESA";
                    break;
                case 4:
                    if (isReady) stateText.text = "BEREIT";
                    else stateText.text = "WARTEN";
                    break;
                case 5:
                    if (isReady) stateText.text = "LISTO";
                    else stateText.text = "ESPERANDO";
                    break;
                case 6:
                    if (isReady) stateText.text = "ГОТОВ";
                    else stateText.text = "ЧАКАНЕ";
                    break;
                case 7:
                    if (isReady) stateText.text = "PŘIPRAVEN";
                    else stateText.text = "ČEKÁNÍ";
                    break;
            }
        }

        if (dot)
        {
            var c = isReady ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.25f, 0.25f);
            dot.color = c;
        }
    }

    public void OnClickToggleReady()
    {
        if (NetReady && bound.Object.HasInputAuthority)
        {
            bound.RPC_SetReady(!bound.IsReady);
        }
    }
}