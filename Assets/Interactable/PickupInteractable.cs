using UnityEngine;

/// <summary>
/// Example pickup interactable. Implements IInteractable interface used by PlayerInteraction.
/// Keeps a small OutlineController to manage outline visuals.
/// </summary>
public class PickupInteractable : MonoBehaviour, IInteractable
{
    public bool IsHeld { get; private set; } = false;

    private OutlineController outlineController;

    private void Awake()
    {
        outlineController = GetComponent<OutlineController>();
    }

    // called when player looks at this item
    public void OnFocus()
    {
        if (!IsHeld)
            outlineController.SetHighlight();
    }

    // called when player looks away
    public void OnLoseFocus()
    {
        if (!IsHeld)
            outlineController.SetProximityOrNone();
    }

    // called when player presses interact (E)
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

    // mark item as held/unheld
    public void SetHeld(bool held)
    {
        IsHeld = held;
        outlineController.DisableOutline(); // disable outlines while being held
    }
}
