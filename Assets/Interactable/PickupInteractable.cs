using UnityEngine;

public class PickupInteractable : MonoBehaviour, IInteractable
{
    public bool IsHeld { get; private set; } = false;

    private OutlineController outlineController;

    private void Awake()
    {
        outlineController = GetComponent<OutlineController>();
    }

    public void OnFocus()
    {
        if (!IsHeld)
            outlineController.SetHighlight();
    }

    public void OnLoseFocus()
    {
        if (!IsHeld)
            outlineController.SetProximityOrNone();
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        if (IsHeld)
        {
            interactor.DropItem();
        }
        else
        {
            interactor.PickUpItem(this);
        }
    }

    public void SetHeld(bool held)
    {
        IsHeld = held;
        outlineController.DisableOutline(); // no outline while held
    }
}
