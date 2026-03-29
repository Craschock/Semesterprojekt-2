using UnityEngine;
using FMODUnity;

/// <summary>
/// A specific slot in the world that can accept a PickupInteractable item.
/// Handles positioning, snapping, and checking for correct item types.
/// </summary>
public class PlaceSlot : MonoBehaviour, IInteractable
{
    [Header("FMOD Audio")]
    public EventReference interactSound; // Renamed to fit both pick/place actions

    [Header("Slot Transforms")]
    [Tooltip("Visual preview transform (position + rotation) for held items.")]
    public Transform previewPoint;
    [Tooltip("Final locked transform when the item is officially placed.")]
    public Transform slotPoint;

    [Header("Settings")]
    public bool allowPreviewRotation = true;
    public ItemType requiredType = ItemType.None;

    // Internal state
    private PickupInteractable placedItem;
    private OutlineController outline;

    /// <summary>
    /// Checks if the slot currently holds an item matching the required type.
    /// </summary>
    /// <returns>True if the correct item is slotted.</returns>
    public bool HasCorrectItem()
    {
        return placedItem != null && placedItem.itemType == requiredType;
    }

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    /// <summary>
    /// Triggered when the player interacts with this slot.
    /// Handles placing or taking the item depending on context.
    /// </summary>
    public void OnInteract(PlayerInteraction interactor)
    {
        // Add your interaction logic here (e.g., calling PlaceItem or RemoveItem)
        // This will be triggered by your new PlayerInteraction script.
    }

    public void OnFocus() { if (outline != null) outline.SetToHighlight(); }
    public void OnLoseFocus() { if (outline != null) outline.SetToProximityOrDefault(); }

    /// <summary>
    /// Locks the provided item into this slot, updating its physics and layer state.
    /// </summary>
    /// <param name="item">The item to be placed.</param>
    public void PlaceItem(PickupInteractable item)
    {
        if (item == null) return;

        Transform t = item.transform;
        t.position = slotPoint.position;
        t.rotation = slotPoint.rotation;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        item.SetHeld(false);
        placedItem = item;
        item.SetSlotted(true);

        // Restore default layer 
        int defaultLayer = LayerMask.NameToLayer("Default") >= 0 ? LayerMask.NameToLayer("Default") : 3;
        t.gameObject.layer = defaultLayer;

        if (!interactSound.IsNull) RuntimeManager.PlayOneShot(interactSound, transform.position);
    }

    /// <summary>
    /// Removes the currently placed item from the slot so the player can take it back.
    /// </summary>
    /// <returns>The removed item, or null if the slot was empty.</returns>
    public PickupInteractable RemoveItem()
    {
        if (placedItem == null) return null;

        if (!interactSound.IsNull) RuntimeManager.PlayOneShot(interactSound, transform.position);

        PickupInteractable item = placedItem;
        item.SetSlotted(false);
        placedItem = null;

        return item;
    }
}