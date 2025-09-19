using System.Collections;
using EditorTools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, TextArea] private string _tooltip = "This is an empty tooltip. Should probably fill this with something";
    [SerializeField] private float _timeToTooltip = 2f;

    [Space]
    [SerializeField] private RectTransform _tooltipCanvas;
    [SerializeField] private RectTransform _tooltipPanel;
    [SerializeField] private TMP_Text _tooltipTextObj;

    private bool _pointerEntered = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerEntered = true;
        Invoke(nameof(DisplayTooltip), _timeToTooltip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerEntered = false;
        CancelInvoke();
        _tooltipPanel.gameObject.SetActive(false);
    }

    public void DisplayTooltip()
    {
        if (!_pointerEntered) return;


        Vector2 panelPosition;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_tooltipCanvas, Input.mousePosition, Camera.main, out panelPosition))
            this.LogError("Something went wrong with the tooltip panel");

        Vector2 panelSize = new Vector2(_tooltipPanel.GetWidth(), _tooltipPanel.GetHeight());

        // Check if the panel is clipping the viewport, and have it snap to the otherside if yes

        _tooltipTextObj.text = _tooltip;
        _tooltipPanel.gameObject.SetActive(true);
    }
}

/// <summary>
/// Source: https://discussions.unity.com/t/solved-getting-position-and-size-of-recttransform-in-screen-coordinates/774050/5
/// </summary>
public static class RectTransformExtension
{
    public static float GetWidth(this RectTransform rt)
    {
        var w = (rt.anchorMax.x - rt.anchorMin.x) * Screen.width + rt.sizeDelta.x;
        return w;
    }

    public static float GetHeight(this RectTransform rt)
    {
        var h = (rt.anchorMax.y - rt.anchorMin.y) * Screen.height + rt.sizeDelta.y;
        return h;
    }
}
