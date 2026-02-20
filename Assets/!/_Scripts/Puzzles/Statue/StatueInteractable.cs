using UnityEngine;
using FMODUnity;

/// <summary>
/// - A single statue in the puzzle.
/// - Toggles visual feedback.
/// </summary>
[RequireComponent(typeof(OutlineController))]
public class StatueInteractable : MonoBehaviour, IInteractable
{
    [Header("Puzzle References")]
    public StatuePuzzleManager puzzleManager;

    [Header("Visual Feedback")]
    public GameObject activationVisuals;

    [Header("FMOD Audio")]
    public EventReference interactionSound;


    [Header("Debug / Level Design")]
    public bool showGazeDirection = true;
    public float gazeLength = 5f;

    // References
    private OutlineController outline;
    private bool isActivated = false;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();

        if (activationVisuals != null)
            activationVisuals.SetActive(false);
    }

    // --- IInteractable Implementation ---

    public void OnInteract(PlayerInteraction interactor)
    {
        if (isActivated) return;
        if (!interactionSound.IsNull)
        {
            RuntimeManager.PlayOneShot(interactionSound, transform.position);
        }

        if (puzzleManager == null)
        {
            Debug.LogError($"[StatueInteractable] No PuzzleManager assigned on {gameObject.name}!");
            return;
        }

        puzzleManager.OnStatueInteracted(this);
    }

    public void OnFocus()
    {
        if (isActivated) return;

        if (outline != null)
            outline.SetToHighlight();
    }

    public void OnLoseFocus()
    {
        if (isActivated) return;

        if (outline != null)
            outline.SetToProximityOrDefault();
    }

    // --- Public Logic called by Manager ---

    public void SetActivated(bool active)
    {
        isActivated = active;

        if (activationVisuals != null)
            activationVisuals.SetActive(active);

        if (outline != null)
        {
            if (active)
            {
                outline.enabled = false;

                int defaultLayer = LayerMask.NameToLayer("Default");
                SetLayerRecursive(gameObject, defaultLayer);
            }
            else
            {
                // Re-enable if wrong order
                int itemLayer = LayerMask.NameToLayer("Item");
                SetLayerRecursive(gameObject, itemLayer);
                outline.enabled = true;
                outline.SetToDefault();
            }
        }
    }

    public bool IsActivated() => isActivated;

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        // Don't change the "Ignore Raycast" layer (Sensor) if it exists
        if (obj.layer != LayerMask.NameToLayer("Ignore Raycast"))
        {
            obj.layer = layer;
        }

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}