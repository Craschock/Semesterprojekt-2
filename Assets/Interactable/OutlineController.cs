using UnityEngine;

/// <summary>
/// Controls which layer the object uses so the "Free Outline" asset can render
/// proximity vs highlight outlines. Layers are switched at runtime.
/// This version avoids changing the layer if the object is currently 'held'.
/// </summary>
public class OutlineController : MonoBehaviour
{
    [Header("Settings")]
    public float proximityDistance = 5f; // distance to player to enable proximity outline

    // layer indices
    private int defaultLayer = 6;
    private int proximityLayer = 7;
    private int highlightLayer = 8;
    private int heldItemLayerIndex = 9;

    private Transform player;
    private bool isHighlighted = false;
    private bool isInProximity = false;

    private void Start()
    {
        player = Camera.main.transform;
        gameObject.layer = defaultLayer;

        //Init layers
        defaultLayer = LayerMask.NameToLayer("Item");
        proximityLayer = LayerMask.NameToLayer("OutlineProximity");
        highlightLayer = LayerMask.NameToLayer("OutlineHighlight");
        heldItemLayerIndex = LayerMask.NameToLayer("HeldItem");
    }

    private void Update()
    {
        // If this object is a pickup and currently held, do NOT change its layer
        var pickup = GetComponent<PickupInteractable>();
        if (pickup != null && pickup.IsHeld)
            return;

        // Also skip if the object is on the HeldItem layer (safety)
        if (heldItemLayerIndex >= 0 && gameObject.layer == heldItemLayerIndex)
            return;

        // compute distance to player and set layer accordingly (unless highlighted)
        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= proximityDistance && !isHighlighted)
        {
            SetLayer(proximityLayer);
            isInProximity = true;
        }
        else if (!isHighlighted)
        {
            SetLayer(defaultLayer);
            isInProximity = false;
        }
    }

    public void SetHighlight()
    {
        isHighlighted = true;
        SetLayer(highlightLayer);
    }

    public void SetProximityOrNone()
    {
        isHighlighted = false;
        SetLayer(isInProximity ? proximityLayer : defaultLayer);
    }

    public void DisableOutline()
    {
        isHighlighted = false;
        isInProximity = false;
        SetLayer(defaultLayer);
    }

    private void SetLayer(int layer)
    {
        gameObject.layer = layer;
    }
}
