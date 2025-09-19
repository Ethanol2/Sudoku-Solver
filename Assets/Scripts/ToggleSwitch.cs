using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour
{
    [SerializeField] private bool _active = false;

    [Header("Animation")]
    [SerializeField] private float _toggleTime = 0.5f;
    [SerializeField] private AnimationCurve _animationEase;

    [Space]
    [SerializeField] private float _inactiveXAnchorMin = -2f;
    [SerializeField] private float _activeXAnchorMin = -1f;

    [Header("References")]
    [SerializeField] private Button _button;
    [SerializeField] private RectTransform _movingElement;

    public bool Active
    {
        get => _active;
        set => Toggle(value);
    }

    public event System.Action<bool> OnToggled;
    public UnityEvent<bool> OnToggled_UE;

    void OnValidate()
    {
        if (_movingElement && _movingElement.anchorMin.x != (_active ? _activeXAnchorMin : _inactiveXAnchorMin))
        {
            Vector2 anchorMin = _movingElement.anchorMin;
            Vector2 anchorMax = _movingElement.anchorMax;
            float anchorSeperation = anchorMax.x - anchorMin.x;

            anchorMin.x = _active ? _activeXAnchorMin : _inactiveXAnchorMin;
            anchorMax.x = anchorMin.x + anchorSeperation;
            _movingElement.anchorMin = anchorMin;
            _movingElement.anchorMax = anchorMax;   
        }
    }
    void OnEnable()
    {
        _button.onClick.AddListener(Toggle);
        Toggle(_active);
    }
    void OnDisable()
    {
        _button.onClick.RemoveListener(Toggle);
    }

    public void Toggle() => Toggle(!_active);
    public void Toggle(bool state)
    {
        _active = state;

        StopAllCoroutines();
        StartCoroutine(MoveToggle());

        OnToggled?.Invoke(_active);
        OnToggled_UE.Invoke(_active);
    }

    private IEnumerator MoveToggle()
    {
        float start, end;
        if (_active)
        {
            start = _inactiveXAnchorMin;
            end = _activeXAnchorMin;
        }
        else
        {
            start = _activeXAnchorMin;
            end = _inactiveXAnchorMin;
        }


        Vector2 anchorMin = _movingElement.anchorMin;
        Vector2 anchorMax = _movingElement.anchorMax;

        float anchorSeperation = anchorMax.x - anchorMin.x;

        float t = Mathf.InverseLerp(start, end, anchorMin.x);

        while (t <= 1f)
        {
            t += Time.deltaTime / _toggleTime;

            anchorMin.x = Mathf.Lerp(start, end, _animationEase.Evaluate(t));
            anchorMax.x = anchorMin.x + anchorSeperation;

            _movingElement.anchorMin = anchorMin;
            _movingElement.anchorMax = anchorMax;

            yield return null;
        }

        anchorMin.x = end;
        anchorMax.x = end + anchorSeperation;
        _movingElement.anchorMin = anchorMin;
        _movingElement.anchorMax = anchorMax;        
    }
}
