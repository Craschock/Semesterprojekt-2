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
    public float proximityRadius = 5f; // Desired WORLD SPACE radius

    // layer indices
    private int defaultLayer = -1;
    private int proximityLayer = -1;
    private int highlightLayer = -1;
    private int heldItemLayerIndex = -1;
    private int ignoreRaycastLayer = -1;

    // cache
    private PickupInteractable cachedPickup;
    private bool isHighlighted = false;
    private bool isInProximity = false;
    private SphereCollider proximityCollider;

    private void Start()
    {
        // Initialize Layers
        defaultLayer = LayerMask.NameToLayer("Item");
        proximityLayer = LayerMask.NameToLayer("OutlineProximity");
        highlightLayer = LayerMask.NameToLayer("OutlineHighlight");
        heldItemLayerIndex = LayerMask.NameToLayer("HeldItem");
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        // Cache References (Look on THIS object, the Parent)
        cachedPickup = GetComponent<PickupInteractable>();

        // find collider in children for the Sensor (Sphere Trigger)
        var allColliders = GetComponentsInChildren<SphereCollider>(true);
        foreach (var col in allColliders)
        {
            if (col.isTrigger)
            {
                proximityCollider = col;
                break;
            }
        }

        // AUTO-FIX RADIUS
        if (proximityCollider != null)
        {
            Transform t = proximityCollider.transform;
            float maxScale = Mathf.Max(t.lossyScale.x, t.lossyScale.y, t.lossyScale.z);
            if (maxScale > 0) proximityCollider.radius = proximityRadius / maxScale;
        }

        // Set Initial Layers
        SetLayerRecursive(defaultLayer);
    }

    // ------------------------------------------------------
    // TRIGGER LOGIC
    // ------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // If held, ignore proximity logic completely
            if (cachedPickup != null && cachedPickup.IsHeld) return;

            isInProximity = true;
            if (!isHighlighted) SetLayerRecursive(proximityLayer);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (cachedPickup != null && cachedPickup.IsHeld) return;

            isInProximity = false;
            if (!isHighlighted) SetLayerRecursive(defaultLayer);
        }
    }

    // ------------------------------------------------------
    // PUBLIC METHODS
    // ------------------------------------------------------
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

    public void SetToHeld()
    {
        isHighlighted = false;
        SetLayerRecursive(heldItemLayerIndex);
    }

    public void SetToDefault()
    {
        isHighlighted = false;
        SetLayerRecursive(isInProximity ? proximityLayer : defaultLayer);
    }

    // ------------------------------------------------------
    // HELPER
    // ------------------------------------------------------
    private void SetLayerRecursive(int layer)
    {
        if (layer < 0) return;

        // Apply to Parent (unless it's the sensor layer)
        if (gameObject.layer != ignoreRaycastLayer)
            gameObject.layer = layer;

        // Apply to Children
        foreach (Transform child in transform)
        {
            // Safety: Never overwrite the Sensor's layer
            if (child.gameObject.layer == ignoreRaycastLayer) continue;
            child.gameObject.layer = layer;
        }
    }
}