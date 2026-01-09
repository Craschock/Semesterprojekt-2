using UnityEngine;

/// <summary>
/// A world-space consumable (item) the player can interact with.
/// When interacted, it tries to insert itself into the inventory to be consumed from the player.
/// </summary>
public class ConsumableInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    public ConsumableType type;

    // Reference to the outline script
    private OutlineController outline;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        // Logic is handled by PlayerInteraction calling PlayerStats, 
        bool wasPickedUp = interactor.TryPickUpConsumable(this.type);

        if (wasPickedUp)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Inventory full");
        }
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