using UnityEngine;

/// <summary>
/// A world-space Water Hut (Purity) the player can interact with.
/// When interacted, it recharges the players purity.
/// </summary>
[RequireComponent(typeof(OutlineController))]
public class WaterHutInteractable : MonoBehaviour, IInteractable
{
    [Header("Purity Settings")]
    public float amount = 100;

    // Reference to the outline script
    private OutlineController outline;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        interactor.playerStats.RestorePurity(amount);
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
