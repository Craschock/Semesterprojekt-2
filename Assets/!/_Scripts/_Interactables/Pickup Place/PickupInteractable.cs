using UnityEngine;
using FMODUnity;

/// <summary>
/// A physical item in the world that the player can pick up, hold, place, and throw.
/// Syncs physics states and layer changes with the OutlineController.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("FMOD Audio")]
    public EventReference pickupSound;

    [Header("Puzzle Settings")]
    public ItemType itemType = ItemType.None;

    // Properties
    public bool IsHeld { get; private set; } = false;
    public bool IsSlotted { get; private set; } = false;

    // References
    private OutlineController outline;
    private Rigidbody rb;

    /// <summary>
    /// Initializes necessary component references.
    /// </summary>
    private void Awake()
    {
        outline = GetComponentInChildren<OutlineController>();
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Triggers the pickup sequence when interacted with.
    /// </summary>
    public void OnInteract(PlayerInteraction interactor)
    {
        if (!pickupSound.IsNull)
        {
            RuntimeManager.PlayOneShot(pickupSound, transform.position);
        }

        interactor.PickUpItem(this);
    }

    /// <summary>
    /// Highlights the object when looked at, as long as it isn't held.
    /// </summary>
    public void OnFocus()
    {
        if (!IsHeld && outline != null)
        {
            outline.SetToHighlight();
        }
    }

    /// <summary>
    /// Removes the highlight when the player looks away.
    /// </summary>
    public void OnLoseFocus()
    {
        if (!IsHeld && outline != null)
        {
            outline.SetToProximityOrDefault();
        }
    }

    /// <summary>
    /// Toggles the physics and outline states based on whether the object is currently held.
    /// </summary>
    /// <param name="held">True if held, false if dropped.</param>
    public void SetHeld(bool held)
    {
        IsHeld = held;

        // Manage Physics
        if (rb != null)
        {
            rb.isKinematic = held;
            rb.useGravity = !held;
        }

        // Manage Visuals
        if (outline != null)
        {
            if (held) outline.SetToHeld();
            else outline.SetToDefault();
        }
        else
        {
            // Fallback
            SetRootLayerByName(held ? "HeldItem" : "Item");
        }
    }

    /// <summary>
    /// Marks the item as slotted in a puzzle and resets its visual state.
    /// </summary>
    /// <param name="slotted">True if placed in a slot.</param>
    public void SetSlotted(bool slotted)
    {
        IsSlotted = slotted;

        if (outline != null)
        {
            outline.SetToDefault();
        }
        else
        {
            SetRootLayerByName("Item");
        }
    }

    /// <summary>
    /// Helper method to set the layer by name if no OutlineController is present.
    /// </summary>
    private void SetRootLayerByName(string layerName)
    {
        int idx = LayerMask.NameToLayer(layerName);
        if (idx < 0) return;

        Transform root = transform.root != null ? transform.root : transform;
        
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.gameObject.layer != LayerMask.NameToLayer("Ignore Raycast"))
            {
                child.gameObject.layer = idx;
            }
        }
    }
}