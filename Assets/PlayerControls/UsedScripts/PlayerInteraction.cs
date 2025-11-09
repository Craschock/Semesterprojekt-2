using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private PlayerControls controls;
    private Camera cam;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public float proximityRange = 6f;
    private IInteractable currentInteractable;
    private IInteractable nearbyInteractable;

    private void Awake()
    {
        controls = new PlayerControls();
        cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        controls.Player.Interact.performed -= OnInteract;
        controls.Disable();
    }

    private void Update()
    {
        HandleRaycast();
        HandleProximity();
    }

    void HandleRaycast()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = interactable;
                    currentInteractable.OnFocus();
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }
    }

    void HandleProximity()
    {
        // Cast a small sphere around the player to find nearby interactables
        Collider[] hits = Physics.OverlapSphere(cam.transform.position, proximityRange);

        IInteractable closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float dist = Vector3.Distance(cam.transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closest = interactable;
                    closestDist = dist;
                }
            }
        }

        // If something new is near
        if (closest != nearbyInteractable)
        {
            nearbyInteractable?.OnProximityExit();
            nearbyInteractable = closest;
            nearbyInteractable?.OnProximityEnter();
        }

        // If no interactables are near
        if (closest == null && nearbyInteractable != null)
        {
            nearbyInteractable.OnProximityExit();
            nearbyInteractable = null;
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        currentInteractable?.Interact();
    }
}
