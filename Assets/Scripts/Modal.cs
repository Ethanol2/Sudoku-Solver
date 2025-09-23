using EditorTools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Modal : MonoBehaviour
{
    private static Modal _instance;

    [Header("References")]
    [SerializeField] private GameObject _modalPanel;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;

    [Space]
    [SerializeField] private Button _confirmButton;
    [SerializeField, HideInInspector] private TMP_Text _confirmButtonText;
    [SerializeField] private Button _cancelButton;
    [SerializeField, HideInInspector] private TMP_Text _cancelButtonText;

    public static Modal Instance => _instance;

    public event System.Action OnConfirm;
    private System.Action _passedConfrimAction;
    public event System.Action OnCancel;
    private System.Action _passedCancelAction;
    public event System.Action OnTimeout;
    private System.Action _passedTimeoutAction;

    void OnValidate()
    {
        if (_confirmButton)
            _confirmButtonText = _confirmButton.GetComponentInChildren<TMP_Text>();
        if (_cancelButton)
            _cancelButtonText = _cancelButton.GetComponentInChildren<TMP_Text>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            this.LogError("Instance already exists, destroying object!");
            Destroy(this.gameObject);
            return;
        }

        _instance = this;

        _modalPanel.SetActive(false);
    }
    void OnEnable()
    {
        _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        _cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }
    void OnDisable()
    {
        _confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        _cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
    }

    private void OnConfirmButtonClicked()
    {
        _modalPanel.SetActive(false);

        OnConfirm?.Invoke();
        _passedConfrimAction?.Invoke();
    }
    private void OnCancelButtonClicked()
    {
        _modalPanel.SetActive(false);

        OnCancel?.Invoke();
        _passedCancelAction?.Invoke();
    }
    private void Timeout()
    {
        _modalPanel.SetActive(false);

        _passedTimeoutAction?.Invoke();
        OnTimeout?.Invoke();
    }

    public void Show(ModalData data)
    {
        _titleText.text = data.Title;
        _bodyText.text = data.Body;

        _confirmButton.gameObject.SetActive(data.ShowConfrimButton);
        if (data.ShowConfrimButton)
        {
            _confirmButtonText.text = data.ConfirmButtonText;
            _passedConfrimAction = data.ConfirmButtonEvent;
        }

        _cancelButton.gameObject.SetActive(data.ShowCancelButton);
        if (data.ShowCancelButton)
        {
            _cancelButtonText.text = data.CancelButtonText;
            _passedCancelAction = data.CancelButtonEvent;
        }

        if (data.TimeoutTime <= 0 && !data.ShowCancelButton && !data.ShowConfrimButton)
            data.TimeoutTime = 30f; // Default timeout for modals without buttons

        if (data.TimeoutTime > 0)
        {
            _passedTimeoutAction = data.TimeoutEvent;
            Invoke(nameof(Timeout), data.TimeoutTime);
        }

        _modalPanel.SetActive(true);
    }

    public static bool ShowModal(ModalData data)
    {
        if (_instance)
        {
            Instance.Show(data);
            return true;
        }

        return false;
    }

    public struct ModalData
    {
        public string Title;
        public string Body;
        public bool ShowConfrimButton;
        public string ConfirmButtonText;
        public System.Action ConfirmButtonEvent;
        public bool ShowCancelButton;
        public string CancelButtonText;
        public System.Action CancelButtonEvent;
        public float TimeoutTime;
        public System.Action TimeoutEvent;
    }
}
