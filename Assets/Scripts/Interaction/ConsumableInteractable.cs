using UnityEngine;

public class ConsumableInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    public ConsumableType type;

    // OutlineController may live on a child mesh
    private OutlineController outline;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        // Logic is handled by PlayerInteraction calling PlayerStats, 
        interactor.TryPickUpConsumable(this.type); // returns boolean
        // if true: despawn
        // if false: nothing
    }

    public void OnFocus()
    {
        if (outline != null) outline.SetToHighlight();

    }

    public void OnLoseFocus()
    {
        if (outline != null) outline.SetToProximityOrDefault();
    }
}