using UnityEngine;

/// <summary>
/// A world-space hint (paper note) the player can interact with.
/// When interacted, it opens the Hint UI (PNG + dim background) and freezes player movement + look.
/// Updated to work with the optimized OutlineController on the parent object.
/// </summary>
public class HintInteractable : MonoBehaviour, IInteractable
{
    [Header("Hint Content")]
    public Sprite hintSprite; // PNG (imported as Sprite)

    [TextArea(5, 10)] // Makes a nice big text box in the Inspector
    public string hintContent;

    // Reference to the outline script (assumed to be on the same object or child)
    private OutlineController outline;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    public void OnFocus()
    {
        // Trigger the highlight state
        if (outline != null) outline.SetToHighlight();
    }

    public void OnLoseFocus()
    {
        // Return to proximity glow or default state
        if (outline != null) outline.SetToProximityOrDefault();
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        if (hintSprite == null) return;

        // Show the hint UI and freeze player controls (option B behavior)
        HintUIManager manager = HintUIManager.Instance;
        if (manager != null)
            manager.ShowHint(hintSprite, hintContent, interactor);
    }
}