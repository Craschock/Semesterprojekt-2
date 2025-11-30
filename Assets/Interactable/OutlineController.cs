using UnityEngine;

/// <summary>
/// Controls which layer the object uses so the "Free Outline" asset can render
/// proximity vs highlight outlines. Layers are switched on the whole root object.
/// This version:
/// - applies layer changes recursively to root + all children
/// - checks pickup state via parent (GetComponentInParent)
/// - exposes public setters for Held/Default/Proximity/Highlight states
/// </summary>
public class OutlineController : MonoBehaviour
{
    [Header("Settings")]
    public float proximityDistance = 5f; // distance to player to enable proximity outline

    // layer indices (populated in Start)
    private int defaultLayer = -1;
    private int proximityLayer = -1;
    private int highlightLayer = -1;
    private int heldItemLayerIndex = -1;

    private Transform player;
    private bool isHighlighted = false;
    private bool isInProximity = false;

    private void Start()
    {
        player = Camera.main.transform;

        // initialize layer indices by name
        defaultLayer = LayerMask.NameToLayer("Item");
        proximityLayer = LayerMask.NameToLayer("OutlineProximity");
        highlightLayer = LayerMask.NameToLayer("OutlineHighlight");
        heldItemLayerIndex = LayerMask.NameToLayer("HeldItem");

        // ensure the whole root starts on the default layer
        SetLayerRecursive(defaultLayer);
    }

    private void Update()
    {
        // check if the item is held by looking up the hierarchy
        var pickupParent = GetComponentInParent<PickupInteractable>();
        if (pickupParent != null && pickupParent.IsHeld)
            return; // if held, do nothing here (layer controlled elsewhere)

        // also skip if the root already sits on HeldItem layer
        if (heldItemLayerIndex >= 0)
        {
            Transform root = transform.root;
            if (root != null && root.gameObject.layer == heldItemLayerIndex)
                return;
        }

        // compute distance from player to root position
        float dist = Vector3.Distance(player.position, transform.root.position);

        if (dist <= proximityDistance && !isHighlighted)
        {
            // set proximity layer on the whole object
            SetLayerRecursive(proximityLayer);
            isInProximity = true;
        }
        else if (!isHighlighted)
        {
            SetLayerRecursive(defaultLayer);
            isInProximity = false;
        }
    }

    // Public helpers for other scripts to force specific outline states
    public void SetToHighlight()
    {
        isHighlighted = true;
        SetLayerRecursive(highlightLayer);
    }

    public void SetToProximityOrDefault()
    {
        isHighlighted = false;
        SetLayerRecursive(isInProximity ? proximityLayer : defaultLayer);
    }

    public void SetToDefault()
    {
        isHighlighted = false;
        isInProximity = false;
        SetLayerRecursive(defaultLayer);
    }

    public void SetToHeld()
    {
        isHighlighted = false;
        // When held we don't want outline layers interfering; set the root + children to HeldItem
        SetLayerRecursive(heldItemLayerIndex);
    }

    // Private: apply a layer to the root and all children (recursive)
    private void SetLayerRecursive(int layer)
    {
        if (layer < 0) return;

        // apply to root object
        Transform root = transform.root;
        if (root == null)
            root = transform; // fallback

        // set layer on root and all children (including the child with this component)
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            all[i].gameObject.layer = layer;
        }
    }
}
