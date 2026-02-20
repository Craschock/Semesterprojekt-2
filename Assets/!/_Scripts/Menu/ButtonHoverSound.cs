using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;

public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("FMOD Settings")]
    public EventReference hoverSound;

    public void OnPointerEnter(PointerEventData eventData)
    {

        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null && !button.interactable) return;

        if (!hoverSound.IsNull)
        {
            RuntimeManager.PlayOneShot(hoverSound);
        }
    }
}