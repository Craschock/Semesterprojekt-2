using UnityEngine;

/// <summary>
/// Controls which layer the object uses so the camera can render proximity vs highlight outlines.
/// Layers are switched recursively on the whole root object to ensure all child meshes get outlined.
/// </summary>
public class OutlineController : MonoBehaviour
{
    [Header("Settings")]
    public float proximityRadius = 5f;

    // Layer indices
    private int defaultLayer = -1;
    private int proximityLayer = -1;
    private int highlightLayer = -1;
    private int heldItemLayerIndex = -1;
    private int ignoreRaycastLayer = -1;

    // State cache
    private PickupInteractable cachedPickup;
    private bool isHighlighted = false;
    private bool isInProximity = false;
    private SphereCollider proximityCollider;

    /// <summary>
    /// Initializes layer indices and sets up the proximity trigger collider.
    /// </summary>
    private void Start()
    {
        defaultLayer = LayerMask.NameToLayer("Item");
        proximityLayer = LayerMask.NameToLayer("OutlineProximity");
        highlightLayer = LayerMask.NameToLayer("OutlineHighlight");
        heldItemLayerIndex = LayerMask.NameToLayer("HeldItem");
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        // Cache parent reference
        cachedPickup = GetComponentInParent<PickupInteractable>();

        // Setup proximity trigger
        proximityCollider = gameObject.AddComponent<SphereCollider>();
        proximityCollider.isTrigger = true;
        proximityCollider.radius = proximityRadius;
        proximityCollider.gameObject.layer = ignoreRaycastLayer; 
    }

    /// <summary>
    /// Detects when the player enters proximity and applies the proximity layer.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (cachedPickup != null && cachedPickup.IsHeld) return;
            
            isInProximity = true;
            if (!isHighlighted) SetLayerRecursive(proximityLayer);
        }
    }

    /// <summary>
    /// Detects when the player leaves proximity and restores the default layer.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInProximity = false;
            if (!isHighlighted) SetLayerRecursive(defaultLayer);
        }
    }

    /// <summary>
    /// Activates the highlight state (e.g., when the player aims at the object).
    /// </summary>
    public void SetToHighlight()
    {
        isHighlighted = true;
        SetLayerRecursive(highlightLayer);
    }

    /// <summary>
    /// Reverts the outline to either the proximity state (if near) or default state.
    /// </summary>
    public void SetToProximityOrDefault()
    {
        isHighlighted = false;
        SetLayerRecursive(isInProximity ? proximityLayer : defaultLayer);
    }

    /// <summary>
    /// Sets the layer specifically for when the object is being held by the player.
    /// </summary>
    public void SetToHeld()
    {
        isHighlighted = false;
        SetLayerRecursive(heldItemLayerIndex);
    }

    /// <summary>
    /// Completely resets the outline to the default item state.
    /// </summary>
    public void SetToDefault()
    {
        isHighlighted = false;
        SetLayerRecursive(isInProximity ? proximityLayer : defaultLayer);
    }

    /// <summary>
    /// Recursively applies a layer to this object and all its children.
    /// </summary>
    /// <param name="layer">The layer index to apply.</param>
    private void SetLayerRecursive(int layer)
    {
        if (layer < 0) return;

        // Apply to Parent (unless it's the sensor layer)
        if (gameObject.layer != ignoreRaycastLayer)
        {
            gameObject.layer = layer;
        }

        // Apply to Children
        foreach (Transform child in transform)
        {
            // Safety: Never overwrite the Sensor's layer
            if (child.gameObject.layer == ignoreRaycastLayer) continue;
            child.gameObject.layer = layer;
        }
    }
}