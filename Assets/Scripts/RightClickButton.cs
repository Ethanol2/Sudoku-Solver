using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RightClickButton : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent OnClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            OnClick.Invoke();
    }
}
