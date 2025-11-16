using UnityEngine;

public class OutlineController : MonoBehaviour
{
    public float proximityDistance = 5f;

    private Transform player;
    private int defaultLayer = 3;
    private int proximityLayer = 6;
    private int highlightLayer = 7;

    private bool isHighlighted = false;
    private bool isInProximity = false;

    private void Start()
    {
        player = Camera.main.transform;
        gameObject.layer = defaultLayer;
    }

    private void Update()
    {
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
