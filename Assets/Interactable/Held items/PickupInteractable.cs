using UnityEngine;

/// <summary>
/// Pick-up-able item. Works with PlayerInteraction for pickup/place.
/// Now supports:
/// - OutlineController located on a child mesh (it will be found automatically)
/// - Proper layer syncing by asking the OutlineController to set layers recursively
/// </summary>
public class PickupInteractable : MonoBehaviour, IInteractable
{
    public bool IsHeld { get; private set; } = false;
    public bool IsSlotted { get; private set; } = false;

    // type used by puzzle system (set in inspector)
    public ItemType itemType = ItemType.None;

    // OutlineController may live on a child mesh. We find it in children.
    private OutlineController outlineController;

    private void Awake()
    {
        // find outline controller in children (the mesh)
        outlineController = GetComponent<OutlineController>();
    }

    // called when player looks at this item (in world or in slot)
    public void OnFocus()
    {
        // only show outline if not held; slotted items can still be highlighted
        if (!IsHeld && outlineController != null)
            outlineController.SetToHighlight();
    }

    // called when player looks away
    public void OnLoseFocus()
    {
        if (!IsHeld && outlineController != null)
            outlineController.SetToProximityOrDefault();
    }

    // called when player presses interact (E)
    public void OnInteract(PlayerInteraction interactor)
    {
        // if the item is slotted, normal pickup is not allowed via OnInteract.
        if (IsSlotted)
            return;

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

        // when held, tell OutlineController to set the Held layer for the whole item
        if (outlineController != null)
        {
            if (held)
                outlineController.SetToHeld();
            else
                outlineController.SetToDefault();
        }
        else
        {
            // fallback: if no outline controller exists, still update root layer
            if (held)
                SetRootLayerByName("HeldItem");
            else
                SetRootLayerByName("Item");
        }
    }

    // mark item as slotted/un-slotted
    public void SetSlotted(bool slotted)
    {
        IsSlotted = slotted;

        // when slotted, do NOT change to Held layer; instead set to default so it can be highlighted/picked later
        if (outlineController != null)
        {
            outlineController.SetToDefault();
        }
        else
        {
            SetRootLayerByName("Item");
        }
    }

    // helper: set root + children layer by layer name (fallback)
    private void SetRootLayerByName(string layerName)
    {
        int idx = LayerMask.NameToLayer(layerName);
        if (idx < 0) return;

        Transform root = transform.root;
        if (root == null) root = transform;

        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            all[i].gameObject.layer = idx;
    }
}
