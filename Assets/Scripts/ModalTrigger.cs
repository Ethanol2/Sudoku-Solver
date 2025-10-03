using UnityEngine;
using UnityEngine.Events;

public class ModalTrigger : MonoBehaviour
{
    [SerializeField] private Modal.ModalData _modalData;

    public UnityEvent OnModalConfirm;
    public UnityEvent OnModalCancel;
    public UnityEvent OnModalTimeout;

    void Start()
    {
        _modalData.ConfirmButtonEvent = OnConfirm;
        _modalData.CancelButtonEvent = OnCancel;
        _modalData.TimeoutEvent = OnTimeout;
    }
    private void OnConfirm() => OnModalConfirm.Invoke();
    private void OnCancel() => OnModalCancel.Invoke();
    private void OnTimeout() => OnModalTimeout.Invoke();

    public void TriggerModal()
    {
        Modal.ShowModal(_modalData);
    }
}
