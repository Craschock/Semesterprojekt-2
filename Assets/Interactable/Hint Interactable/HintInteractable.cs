using UnityEngine;

/// <summary>
/// A world-space hint (paper note) the player can interact with.
/// When interacted, it opens the Hint UI (PNG + dim background) and freezes player movement + look.
/// </summary>
public class HintInteractable : MonoBehaviour, IInteractable
{
    [Header("Hint Content")]
    public Sprite hintSprite; // PNG (imported as Sprite)


    private OutlineController outline;

    private void Awake()
    {
        if (outline == null)
            outline = GetComponentInChildren<OutlineController>();
    }

    public void OnFocus()
    {
        if (outline != null)
            outline.SetToHighlight();
    }

    public void OnLoseFocus()
    {
        if (outline != null)
            outline.SetToProximityOrDefault();
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        if (hintSprite == null) return;

        // Show the hint UI and freeze player controls (option B behavior)
        HintUIManager manager = HintUIManager.Instance;
        if (manager != null)
            manager.ShowHint(hintSprite, interactor);
    }
}