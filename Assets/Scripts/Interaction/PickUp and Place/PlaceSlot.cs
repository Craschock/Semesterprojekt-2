using UnityEngine;
using FMODUnity;

/// <summary>
/// Slot that can accept a PickupInteractable (placeable item).
/// - previewPoint: where a held item moves to visually while aiming at this slot
/// - slotPoint: final locked transform when item is placed
/// Implements IInteractable so the PlayerInteraction raycast can interact with it.
/// </summary>
public class PlaceSlot : MonoBehaviour, IInteractable
{
    [Header("FMOD Audio")]
    public EventReference pickupSound;

    [Header("Slot Transforms")]
    public Transform previewPoint;  // visual preview transform (position+rotation)
    public Transform slotPoint;     // final locked transform when item is placed

    [Header("Settings")]
    public bool allowPreviewRotation = true; // if false, preview doesn't accept rotation
    public ItemType requiredType = ItemType.None;

    // Helper — easy to check if slot is correct
    public bool HasCorrectItem()
    {
        return placedItem != null && placedItem.itemType == requiredType;
    }


    // current placed item (null = slot empty)
    private PickupInteractable placedItem;

    private OutlineController outline;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    // IInteractable: called when player presses E on this slot
    // PlayerInteraction will call PlaceItem or TakeItem depending on state
    public void OnInteract(PlayerInteraction interactor)
    {
        // PlayerInteraction will handle logic; keep this minimal for safety
        // (We keep this so the slot can be raycast-detected as IInteractable)
    }

    // when player looks at the slot
    public void OnFocus()
    {
        // If the slot has an item, highlight the item too
        if (placedItem != null)
        {
            var itemOutline = placedItem.GetComponentInChildren<OutlineController>();
            if (itemOutline != null)
                itemOutline.SetToHighlight();
        }
    }

    // when player looks away
    public void OnLoseFocus()
    {
        // Reset outline on the placed item
        if (placedItem != null)
        {
            var itemOutline = placedItem.GetComponentInChildren<OutlineController>();
            if (itemOutline != null)
                itemOutline.SetToProximityOrDefault();
        }
    }

    // --- Slot API used by PlayerInteraction ---

    public bool HasItem()
    {
        return placedItem != null;
    }

    // returns the transform where preview should aim (if previewPoint null, use slotPoint)
    public Transform GetPreviewTransform()
    {
        return previewPoint != null ? previewPoint : slotPoint;
    }

    // place the item into the slot (called by PlayerInteraction)
    // the item will be snapped to slotPoint, made kinematic, and registered as placed
    public void PlaceItem(PickupInteractable item)
    {
        {
            RuntimeManager.PlayOneShot(pickupSound, transform.position);
        }

        if (item == null || slotPoint == null) return;

        // detach from player (pickup system does unparenting already)
        Transform t = item.transform;

        // move & rotate to final slot pose
        t.position = slotPoint.position;
        t.rotation = slotPoint.rotation;

        // physics: make kinematic so it stays in slot
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // mark item as not held and register
        item.SetHeld(false);
        placedItem = item;
        item.SetSlotted(true);

        // restore layer so outline works (outline default assumed)
        int defaultLayer = 3;
        if (LayerMask.NameToLayer("Default") >= 0)
            defaultLayer = LayerMask.NameToLayer("Default"); // usually 0
        // But your OutlineController used 3 as default — prefer 3 if present
        if (LayerMask.LayerToName(3) != "")
            defaultLayer = 3;
        t.gameObject.layer = defaultLayer;
    }

    // remove currently placed item and return it (used when player picks it up)
    public PickupInteractable RemoveItem()
    {
        {
            RuntimeManager.PlayOneShot(pickupSound, transform.position);
        }

        if (placedItem == null) return null;

        PickupInteractable item = placedItem;
        item.SetSlotted(false);
        placedItem = null;

        // allow physics/outline to be restored by pickup logic in PlayerInteraction
        return item;
    }
}
