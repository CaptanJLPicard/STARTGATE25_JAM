using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.EventSystems;
using VInspector;

public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    // Sounds
    [SerializeField, ReadOnly] private MMF_Player onPointerEnterFeedBack;
    [SerializeField, ReadOnly] private MMF_Player onPointerClickFeedBack;
    [SerializeField, ReadOnly] private MMF_Player onValueChanged;

    [Header("Hover Scale Effect")]
    [SerializeField] private bool enableHoverScale = true;
    [SerializeField] private float hoverScale = 1.08f;      // hover büyüme oraný
    [SerializeField] private float scaleDuration = 0.10f;   // büyüme/küçülme süresi
    [SerializeField] private bool popOnClick = false;
    [SerializeField] private float clickPopScale = 1.12f;
    [SerializeField] private float clickPopDuration = 0.06f;

    RectTransform _rt;
    Vector3 _baseScale;
    Coroutine _scaleRoutine;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (_rt != null) _baseScale = _rt.localScale;

        var enterObj = GameObject.Find("OnPointerEnterFeedBack");
        if (enterObj) onPointerEnterFeedBack = enterObj.GetComponent<MMF_Player>();

        var clickObj = GameObject.Find("OnPointerClickFeedBack");
        if (clickObj) onPointerClickFeedBack = clickObj.GetComponent<MMF_Player>();

        var valueObj = GameObject.Find("OnValueChangedFeedBack");
        if (valueObj) onValueChanged = valueObj.GetComponent<MMF_Player>();
    }

    void OnEnable()
    {
        // obj disable/enable olunca yanlýþ scale'da kalmasýn
        if (_rt != null) _rt.localScale = _baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (onPointerEnterFeedBack) onPointerEnterFeedBack.PlayFeedbacks();

        if (enableHoverScale && _rt != null)
            StartScale(_baseScale * hoverScale, scaleDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (enableHoverScale && _rt != null)
            StartScale(_baseScale, scaleDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (onPointerClickFeedBack) onPointerClickFeedBack.PlayFeedbacks();

        if (popOnClick && _rt != null)
            StartCoroutine(ClickPop());
    }

    public void OnValueChanged()
    {
        if (onValueChanged) onValueChanged.PlayFeedbacks();
    }

    void StartScale(Vector3 target, float duration)
    {
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScaleRoutine(target, duration));
    }

    IEnumerator ScaleRoutine(Vector3 target, float duration)
    {
        var start = _rt.localScale;
        if (duration <= 0f)
        {
            _rt.localScale = target;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // UI için daha iyi
            float a = Mathf.Clamp01(t / duration);
            _rt.localScale = Vector3.Lerp(start, target, a);
            yield return null;
        }
        _rt.localScale = target;
    }

    IEnumerator ClickPop()
    {
        // hover açýksa pop sonrasý hover scale’a geri dönsün
        var hoverTarget = (_rt != null) ? (_baseScale * hoverScale) : Vector3.one;
        StartScale(_baseScale * clickPopScale, clickPopDuration);
        yield return new WaitForSecondsRealtime(clickPopDuration);

        // Eðer mouse hala üstündeyse hoverTarget, deðilse baseScale
        // (Basit yaklaþým: hover açýkken hoverTarget'a dön)
        StartScale(enableHoverScale ? hoverTarget : _baseScale, clickPopDuration);
    }
}