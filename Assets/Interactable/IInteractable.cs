using UnityEngine;

public interface IInteractable
{
    void OnInteract(PlayerInteraction interactor);
    void OnFocus();
    void OnLoseFocus();
}