using UnityEngine;

/// <summary>
/// Controls which layer the object uses so the "Free Outline" asset can render
/// proximity vs highlight outlines. Layers are switched at runtime.
/// </summary>
public class OutlineController : MonoBehaviour
{
    [Header("Settings")]
    public float proximityDistance = 5f; // distance to player to enable proximity outline

    // layer indices (set as numbers to avoid hard-coded names)
    private int defaultLayer = 3;
    private int proximityLayer = 6;
    private int highlightLayer = 7;

    private Transform player;
    private bool isHighlighted = false;
    private bool isInProximity = false;

    private void Start()
    {
        player = Camera.main.transform;
        gameObject.layer = defaultLayer;
    }

    private void Update()
    {
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

    // externally invoked to set highlight state
    public void SetHighlight()
    {
        isHighlighted = true;
        SetLayer(highlightLayer);
    }

    // revert highlight but keep proximity if applicable
    public void SetProximityOrNone()
    {
        isHighlighted = false;
        SetLayer(isInProximity ? proximityLayer : defaultLayer);
    }

    // clear any outline (e.g. while held)
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
