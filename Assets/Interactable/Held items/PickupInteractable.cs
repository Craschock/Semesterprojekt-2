using UnityEngine;

/// <summary>
/// Pick-up-able item. Works with PlayerInteraction for pickup/place.
/// </summary>
public class PickupInteractable : MonoBehaviour, IInteractable
{
    public bool IsHeld { get; private set; } = false;
    public bool IsSlotted { get; private set; } = false; // true when placed in PlaceSlot

    public OutlineController outlineController;
    public ItemType itemType = ItemType.None;

    private void Awake()
    {
    }

    // called when player looks at this item (in world or in slot)
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

    // mark item as held/unheld by the player
    public void SetHeld(bool held)
    {
        IsHeld = held;

        // when held, outline should be disabled and layer controlled by PlayerInteraction
        if (outlineController != null)
            outlineController.DisableOutline();
    }

    // mark item as slotted/un-slotted
    public void SetSlotted(bool slotted)
    {
        IsSlotted = slotted;

        // when slotted, we keep outlines active (player can pick it up again)
        // Do not disable outline here; OutlineController will handle proximity/highlight
    }
}