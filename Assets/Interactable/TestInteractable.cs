using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    private int defaultLayer = 0;
    private int proximityLayer = 6;
    private int highlightLayer = 7;

    public void Interact()
    {
        Debug.Log($"{name} was interacted with!");
        Destroy(gameObject);
    }

    public void OnFocus()
    {
        gameObject.layer = highlightLayer;
    }

    public void OnLoseFocus()
    {
        gameObject.layer = proximityLayer;
    }

    public void OnProximityEnter()
    {
        gameObject.layer = proximityLayer;
    }

    public void OnProximityExit()
    {
        gameObject.layer = defaultLayer;
    }
}
