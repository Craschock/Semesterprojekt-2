using UnityEngine;

/// <summary>
/// Scans the environment using a raycast from the camera to find IInteractable objects.
/// Automatically handles OnFocus() and OnLoseFocus() events.
/// </summary>
public class InteractionScanner : MonoBehaviour
{
    [Header("Scanner Settings")]
    public Transform cameraTransform;
    public float interactionDistance = 3f;
    public LayerMask interactLayer;

    // The currently looked at interactable object
    public IInteractable CurrentTarget { get; private set; }

    /// <summary>
    /// Fallback to main camera if no transform is assigned.
    /// </summary>
    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    /// <summary>
    /// Fires a raycast every frame to detect interactables.
    /// </summary>
    private void Update()
    {
        PerformScan();
    }

    /// <summary>
    /// Casts a ray and updates the CurrentTarget, triggering focus events.
    /// </summary>
    private void PerformScan()
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // If we look at a NEW object
                if (interactable != CurrentTarget)
                {
                    CurrentTarget?.OnLoseFocus();
                    CurrentTarget = interactable;
                    CurrentTarget.OnFocus();
                }
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    /// <summary>
    /// Clears the current target and calls OnLoseFocus.
    /// </summary>
    private void ClearTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget.OnLoseFocus();
            CurrentTarget = null;
        }
    }
}