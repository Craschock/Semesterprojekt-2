using UnityEngine;
using FMODUnity;

/// <summary>
/// A world-space consumable (item) the player can interact with.
/// When interacted, it tries to insert itself into the inventory to be consumed from the player.
/// </summary>
[RequireComponent(typeof(OutlineController))]
public class ConsumableInteractable : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    public ConsumableType type;

    [Header("FMOD Audio")]
    public EventReference pickupSound;

    private OutlineController outline;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        bool wasPickedUp = interactor.TryPickUpConsumable(this.type);

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
            // "Error"-Sound maybe
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