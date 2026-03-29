using UnityEngine;

/// <summary>
/// Interface for all objects the player can interact with in the world.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when the player presses the interact button while looking at the object.
    /// </summary>
    /// <param name="interactor">The PlayerInteraction component triggering the interaction.</param>
    void OnInteract(PlayerInteraction interactor);

    /// <summary>
    /// Called when the player's crosshair looks directly at the object.
    /// </summary>
    void OnFocus();

    /// <summary>
    /// Called when the player's crosshair leaves the object.
    /// </summary>
    void OnLoseFocus();
}