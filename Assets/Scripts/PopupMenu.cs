using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopupMenu : MonoBehaviour, IPointerExitHandler
{
    void Awake()
    {
        this.gameObject.SetActive(false);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        this.gameObject.SetActive(false);
    }
}
