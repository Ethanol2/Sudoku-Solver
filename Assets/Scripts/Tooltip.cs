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
    [SerializeField] private Vector2 _positionPadding = new Vector2(2f, -2f);
    [SerializeField] private bool _closeOnClick = true;

    [Space]
    [SerializeField] private RectTransform _tooltipCanvas;
    [SerializeField] private RectTransform _tooltipPanel;
    [SerializeField] private TMP_Text _tooltipTextObj;

    private bool _pointerEntered = false;
    private Camera cam;

    void OnValidate()
    {
        if (!_tooltipCanvas && !_tooltipPanel && !_tooltipTextObj)
        {
            _tooltipCanvas = GameObject.FindGameObjectWithTag("Tooltip").transform as RectTransform;
            _tooltipPanel = _tooltipCanvas.GetChild(0) as RectTransform;
            _tooltipTextObj = _tooltipPanel.GetComponentInChildren<TMP_Text>();
        }
    }
    void Awake()
    {
        cam = Camera.main;
    }
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
        StopAllCoroutines();
    }

    public void DisplayTooltip()
    {
        if (!_pointerEntered) return;
        StartCoroutine(TooltipRoutine());
    }
    private IEnumerator TooltipRoutine()
    {
        _tooltipPanel.gameObject.SetActive(true);
        _tooltipTextObj.text = _tooltip;

        Vector2 panelSize = _tooltipPanel.sizeDelta; //new Vector2(_tooltipPanel.GetWidth(), _tooltipPanel.GetHeight());
        panelSize.y = -panelSize.y;

        Vector2 panelPosition;

        while (_pointerEntered)
        {
            if (_closeOnClick && Input.GetMouseButtonDown(0))
                break;

            panelPosition = Input.mousePosition;
            panelPosition = cam.ScreenToViewportPoint(panelPosition);
            this.Log(panelPosition);
            panelPosition = (_tooltipCanvas.sizeDelta * panelPosition) - (_tooltipCanvas.sizeDelta / 2f);
            this.Log(panelPosition);

            panelPosition += panelSize / 2f + _positionPadding;

            if (!_tooltipCanvas.rect.Contains(panelPosition + panelSize))
            {
                if (panelPosition.x + panelSize.x > _tooltipCanvas.sizeDelta.x / 2f)
                    panelPosition.x -= panelSize.x;
                if (panelPosition.y + panelSize.y < _tooltipCanvas.sizeDelta.y / -2f)
                    panelPosition.y += panelSize.y;
            }

            _tooltipPanel.localPosition = panelPosition;

            yield return null;
        }

        _tooltipPanel.gameObject.SetActive(false);
    }
}
