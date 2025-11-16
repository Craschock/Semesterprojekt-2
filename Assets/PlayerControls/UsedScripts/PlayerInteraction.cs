using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;
    public Transform holdPoint;
    public float pickupSmoothing = 6f;
    public float holdSmoothing = 12f;
    public float rotationSpeed = 80f;

    [Header("Item Physics")]
    public float maxHoldDistance = 3f;
    public float wallCheckRadius = 0.3f;
    public LayerMask environmentMask;

    [Header("References")]
    public PlayerMovement playerMovement;
    public GameObject uiPrompt;

    private float pickupBlend = 0f;

    private Camera cam;
    private PlayerControls input;

    private InputAction interactAction;
    private InputAction focusAction;
    private InputAction rotateXAction;
    private InputAction rotateYAction;

    private IInteractable currentInteractable;
    private PickupInteractable heldItem;

    private bool inFocusMode = false;
    private bool interactPressedThisFrame = false;


    private void Awake()
    {
        cam = Camera.main;
        input = new PlayerControls();

        interactAction = input.Player.Interact;
        focusAction = input.Player.Focus;
        rotateXAction = input.Player.RotateX;
        rotateYAction = input.Player.RotateY;

        interactAction.Enable();
        focusAction.Enable();
        rotateXAction.Enable();
        rotateYAction.Enable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (uiPrompt != null)
            uiPrompt.SetActive(false);
    }

    private void Update()
    {
        interactPressedThisFrame = interactAction.WasPerformedThisFrame();
        HandleRaycast();
        HandleHeldObject();
        HandleFocusModeToggle();
        HandleHeldInteract();
    }

    // ────────────────────────────────────────────────
    // 1. RAYCAST INTERACTION
    // ────────────────────────────────────────────────
    private void HandleRaycast()
    {
        if (heldItem != null)
        {
            ShowPrompt("Press [E] to Drop");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != currentInteractable)
            {
                currentInteractable?.OnLoseFocus();
                currentInteractable = interactable;
                currentInteractable?.OnFocus();
            }

            if (currentInteractable != null)
                ShowPrompt("Press [E] to Pick Up");
            else
                HidePrompt();

            // ✔ Pick up ONLY when not holding anything
            if (interactPressedThisFrame && heldItem == null)
            {
                currentInteractable?.OnInteract(this);
                interactPressedThisFrame = false; // consume press
            }
        }
        else
        {
            currentInteractable?.OnLoseFocus();
            currentInteractable = null;
            HidePrompt();
        }
    }

    // ────────────────────────────────────────────────
    // 2. HANDLE HELD OBJECT
    // ────────────────────────────────────────────────
    private void HandleHeldObject()
    {
        if (heldItem == null)
            return;

        Transform item = heldItem.transform;

        // Increase pickup blend until it reaches 1
        pickupBlend = Mathf.Clamp01(pickupBlend + Time.deltaTime * pickupSmoothing);

        // Determine target position and rotation
        Vector3 targetPos = holdPoint.position;
        Quaternion targetRot = holdPoint.rotation;

        // Wall clipping check
        Vector3 direction = holdPoint.position - cam.transform.position;
        float distance = direction.magnitude;

        if (Physics.SphereCast(cam.transform.position, wallCheckRadius, direction,
            out RaycastHit hit, distance, environmentMask))
        {
            targetPos = hit.point - direction.normalized * 0.1f;
        }

        // Smooth movement
        item.position = Vector3.Lerp(item.position, targetPos, Time.deltaTime * holdSmoothing * pickupBlend);

        // Smooth rotation
        item.rotation = Quaternion.Slerp(item.rotation, targetRot, Time.deltaTime * holdSmoothing * pickupBlend);
    }

    // ────────────────────────────────────────────────
    // 3. DROP ONLY WHEN HOLDING
    // ────────────────────────────────────────────────
    private void HandleHeldInteract()
    {
        if (heldItem != null && interactPressedThisFrame)
        {
            DropItem();
            interactPressedThisFrame = false; // consume press
        }
    }

    // ────────────────────────────────────────────────
    // 4. FOCUS MODE & ROTATION
    // ────────────────────────────────────────────────
    private void HandleFocusModeToggle()
    {
        if (heldItem == null)
        {
            if (inFocusMode)
                ExitFocusMode();
            return;
        }

        if (focusAction.WasPerformedThisFrame())
        {
            if (!inFocusMode) EnterFocusMode();
            else ExitFocusMode();
        }

        if (inFocusMode)
            RotateHeldItem();
    }

    private void EnterFocusMode()
    {
        inFocusMode = true;
        playerMovement.enabled = false;
        HidePrompt();
    }

    private void ExitFocusMode()
    {
        inFocusMode = false;
        playerMovement.enabled = true;
        ShowPrompt("Press [E] to Drop");
    }

    private void RotateHeldItem()
    {
        float rotX = rotateXAction.ReadValue<float>();
        float rotY = rotateYAction.ReadValue<float>();

        heldItem.transform.Rotate(cam.transform.up, rotX * rotationSpeed * Time.deltaTime, Space.World);
        heldItem.transform.Rotate(cam.transform.right, -rotY * rotationSpeed * Time.deltaTime, Space.World);
    }

    // ────────────────────────────────────────────────
    // 5. PICKUP & DROP
    // ────────────────────────────────────────────────
    public void PickUpItem(PickupInteractable item)
    {
        heldItem = item;
        heldItem.SetHeld(true);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        item.transform.SetParent(null);
        ShowPrompt("Press [E] to Drop");

        pickupBlend = 0f; //Just to reset smoothing lmao
    }

    public void DropItem()
    {
        if (heldItem == null) return;

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        heldItem.SetHeld(false);
        heldItem = null;

        ExitFocusMode();
    }

    public bool HeldItemExists()
    {
        return heldItem != null;
    }

    public Transform GetHeldItemTransform()
    {
        return heldItem != null ? heldItem.transform : null;
    }

    // ────────────────────────────────────────────────
    // 6. UI PROMPTS
    // ────────────────────────────────────────────────
    private void ShowPrompt(string text)
    {
        if (uiPrompt == null) return;

        uiPrompt.SetActive(true);
        uiPrompt.GetComponent<TMPro.TextMeshProUGUI>().text = text;
    }

    private void HidePrompt()
    {
        if (uiPrompt == null) return;

        uiPrompt.SetActive(false);
    }
}
