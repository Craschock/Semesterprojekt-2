using UnityEngine;
using FMODUnity;

/// <summary>
/// A world-space consumable item the player can interact with.
/// Tries to insert its ConsumableItemData into the player's inventory.
/// </summary>
[RequireComponent(typeof(OutlineController))]
public class ConsumableInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    public ConsumableItemData itemData;

    [Header("FMOD Audio")]
    public EventReference pickupSound;

    private OutlineController outline;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    /// <summary>
    /// Attempts to add this item to the player's inventory on interact.
    /// </summary>
    public void OnInteract(PlayerInteraction interactor)
    {
        if (itemData == null)
        {
            Debug.LogError($"[Consumable] No ItemData assigned to {gameObject.name}!");
            return;
        }

        bool wasPickedUp = interactor.TryPickUpConsumable(this.itemData);

        if (wasPickedUp)
        {
            if (!pickupSound.IsNull)
            {
                RuntimeManager.PlayOneShot(pickupSound, transform.position);
            }
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Inventory full");
        }
    }

    public void OnFocus() { if (outline != null) outline.SetToHighlight(); }
    public void OnLoseFocus() { if (outline != null) outline.SetToProximityOrDefault(); }
}