using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.EventSystems;
using VInspector;

public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    //Sounds
    [SerializeField, ReadOnly] private MMF_Player onPointerEnterFeedBack;
    [SerializeField, ReadOnly] private MMF_Player onPointerClickFeedBack;
    [SerializeField, ReadOnly] private MMF_Player onValueChanged;

    private void Awake()
    {
        onPointerEnterFeedBack = GameObject.Find("OnPointerEnterFeedBack").gameObject.GetComponent<MMF_Player>();
        onPointerClickFeedBack = GameObject.Find("OnPointerClickFeedBack").gameObject.GetComponent<MMF_Player>();
        onValueChanged = GameObject.Find("OnValueChangedFeedBack").gameObject.GetComponent<MMF_Player>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnterFeedBack.PlayFeedbacks();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerClickFeedBack.PlayFeedbacks();
    }
    
    public void OnValueChanged()
    {
        onValueChanged.PlayFeedbacks();
    }
}
