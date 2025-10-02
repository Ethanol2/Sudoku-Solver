using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnyClickButton : Button, IPointerClickHandler, IPointerDownHandler
{
    [SerializeField] private bool _enableRightClick = true;
    [SerializeField] private bool _enableMiddleClick = false;

    public UnityEvent OnLeftClick;
    public UnityEvent OnRightClick;
    public UnityEvent OnMiddleClick;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        if (_enableRightClick && eventData.button == PointerEventData.InputButton.Right)
        {
            base.onClick.Invoke();
            OnRightClick.Invoke();
        }

        if (_enableMiddleClick && eventData.button == PointerEventData.InputButton.Middle)
        {
            base.onClick.Invoke();
            OnMiddleClick.Invoke();
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick.Invoke();
        }
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        if (_enableRightClick && eventData.button == PointerEventData.InputButton.Right)
        {
            base.targetGraphic.CrossFadeColor(colors.pressedColor, colors.fadeDuration, true, true);
        }

        if (_enableMiddleClick && eventData.button == PointerEventData.InputButton.Middle)
        {
            base.targetGraphic.CrossFadeColor(colors.pressedColor, colors.fadeDuration, true, true);
        }
    }
}

#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(AnyClickButton))]
public class AnyClickButtonEditor : UnityEditor.UI.ButtonEditor
{
    UnityEditor.SerializedProperty enableRightClick;
    UnityEditor.SerializedProperty enableMiddleClick;
    UnityEditor.SerializedProperty onLeftClick;
    UnityEditor.SerializedProperty onRightClick;
    UnityEditor.SerializedProperty onMiddleClick;

    public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
    {
        enableRightClick = serializedObject.FindProperty("_enableRightClick");
        enableMiddleClick = serializedObject.FindProperty("_enableMiddleClick");
        onLeftClick = serializedObject.FindProperty("OnLeftClick");
        onRightClick = serializedObject.FindProperty("OnRightClick");
        onMiddleClick = serializedObject.FindProperty("OnMiddleClick");

        return base.CreateInspectorGUI();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10f);

        UnityEditor.EditorGUILayout.PropertyField(enableRightClick);
        UnityEditor.EditorGUILayout.PropertyField(enableMiddleClick);

        GUILayout.Space(10f);

        UnityEditor.EditorGUILayout.PropertyField(onLeftClick);
        UnityEditor.EditorGUILayout.PropertyField(onRightClick);
        UnityEditor.EditorGUILayout.PropertyField(onMiddleClick);
    }
}

#endif