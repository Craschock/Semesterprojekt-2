using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction: raycast picking, holding, focus/inspect mode and rotation limits.
/// Comments explain sections and purpose of public fields.
/// Minor cleanup applied but logic preserved.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;      // max raycast distance to interact
    public Transform holdPoint;              // world-space target where held item moves to
    public float pickupSmoothing = 1f;       // blend speed used when picking up (transition)
    public float holdSmoothing = 12f;        // smoothing speed for following the hold point
    public float rotationSpeed = 80f;        // base rotation speed for focus mode

    [Header("Focus Mode")]
    public float focusRotationMultiplier = 3f; // extra sensitivity multiplier for focus rotation
    public float maxYaw = 135f;               // horizontal limit (A/D)
    public float maxPitch = 45f;              // vertical limit (W/S)
    public float focusReturnSmoothing = 1f;   // blending speed when returning from focus rotation

    [Header("Item Physics")]
    public float maxHoldDistance = 3f;        // max allowed distance to hold item (safety)
    public float wallCheckRadius = 0.3f;      // spherecast radius to avoid clipping into walls
    public LayerMask environmentMask;         // layers considered "environment" for clipping checks

    [Header("References")]
    public PlayerMovement playerMovement;     // disable movement while inspecting
    public PlayerLook playerLook;             // toggle look input while inspecting
    public GameObject uiPrompt;               // small prompt UI object (TextMeshProUGUI expected)

    // --- internal state -------------------------------------------------
    private Vector2 focusRotationOffset = Vector2.zero; // x = yaw (left/right), y = pitch (up/down)
    private Quaternion focusStartRotation;              // rotation snapshot at EnterFocus
    private float focusReturnBlend = 1f;                // blend used when returning from focus
    private float pickupBlend = 0f;                     // blend used when picking up (0 -> 1)

    private Camera cam;
    private PlayerControls input;

    // input actions
    private InputAction interactAction;
    private InputAction focusAction;
    private InputAction rotateXAction;
    private InputAction rotateYAction;

    // interactable state
    private IInteractable currentInteractable;
    private PickupInteractable heldItem;

    // runtime flags
    private bool inFocusMode = false;
    private bool interactPressedThisFrame = false;

    // ------------------------------------------------------------------
    private void Awake()
    {
        // cache main camera and create input wrapper
        cam = Camera.main;
        input = new PlayerControls();

        // bind actions (these must exist in your input actions asset)
        interactAction = input.Player.Interact;
        focusAction = input.Player.Focus;
        rotateXAction = input.Player.RotateX;
        rotateYAction = input.Player.RotateY;

        // enable used actions
        interactAction.Enable();
        focusAction.Enable();
        rotateXAction.Enable();
        rotateYAction.Enable();
    }

    private void Start()
    {
        // lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (uiPrompt != null)
            uiPrompt.SetActive(false);
    }

    private void Update()
    {
        // read single-frame interact press, then run handlers
        interactPressedThisFrame = interactAction.WasPerformedThisFrame();
        HandleRaycast();           // find interactables under reticle
        HandleHeldObject();        // move held item toward hold point (or clipping fallback)
        HandleFocusModeToggle();   // toggle focus and handle rotation
        HandleHeldInteract();      // drop when holding and pressing interact
    }

    // ------------------------------------------------------------------
    // 1) Raycast-based detection & pickup
    // - shows/hides prompt
    // - calls OnInteract(this) on the IInteractable when E pressed
    // ------------------------------------------------------------------
    private void HandleRaycast()
    {
        // if holding item, we don't perform raycast pickup checks
        if (heldItem != null)
        {
            ShowPrompt("Leck meine Eier");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            // focus state transition
            if (interactable != currentInteractable)
            {
                currentInteractable?.OnLoseFocus();
                currentInteractable = interactable;
                currentInteractable?.OnFocus();
            }

            // prompt UI
            if (currentInteractable != null)
                ShowPrompt("Press [E] to Pick Up");
            else
                HidePrompt();

            // pick up only if not holding anything and interact pressed
            if (interactPressedThisFrame && heldItem == null)
            {
                currentInteractable?.OnInteract(this);
                interactPressedThisFrame = false; // consume press so it doesn't double-fire
            }
        }
        else
        {
            // no hit -> clear focus and UI
            currentInteractable?.OnLoseFocus();
            currentInteractable = null;
            HidePrompt();
        }
    }

    // ------------------------------------------------------------------
    // 2) Move / rotate the held item toward the hold point.
    //    Handles wall clipping by spherecasting toward hold point.
    // ------------------------------------------------------------------
    private void HandleHeldObject()
    {
        if (heldItem == null)
            return;

        Transform item = heldItem.transform;

        // gradually ramp pickup blend to 1 for smooth transition
        pickupBlend = Mathf.Clamp01(pickupBlend + Time.deltaTime * pickupSmoothing);

        // target position is normally the holdPoint, but spherecast to avoid clipping into walls
        Vector3 targetPos = holdPoint.position;
        Vector3 direction = holdPoint.position - cam.transform.position;
        float distance = direction.magnitude;

        if (Physics.SphereCast(cam.transform.position, wallCheckRadius, direction, out RaycastHit hit, distance, environmentMask))
        {
            // move item to a safe point before the geometry
            targetPos = hit.point - direction.normalized * 0.1f;
        }

        // position smoothing (uses both holdSmoothing and pickupBlend)
        item.position = Vector3.Lerp(item.position, targetPos, Time.deltaTime * holdSmoothing * pickupBlend);

        // rotation handling:
        // - when in focus mode, rotation is handled in RotateHeldItem()
        // - when not in focus, smoothly slerp back to the holdPoint's rotation (including returning from focus)
        if (!inFocusMode)
        {
            if (focusReturnBlend < 1f)
                focusReturnBlend = Mathf.Clamp01(focusReturnBlend + Time.deltaTime * focusReturnSmoothing);

            Quaternion targetRot = holdPoint.rotation;
            item.rotation = Quaternion.Slerp(item.rotation, targetRot, Time.deltaTime * holdSmoothing * focusReturnBlend);
        }
    }

    // ------------------------------------------------------------------
    // 3) Drop while holding - only when interact pressed
    // ------------------------------------------------------------------
    private void HandleHeldInteract()
    {
        if (heldItem != null && interactPressedThisFrame)
        {
            DropItem();
            interactPressedThisFrame = false; // consume
        }
    }

    // ------------------------------------------------------------------
    // 4) Focus / Inspect Mode toggling
    // - Enter focus: freeze player movement & disable mouse look
    // - Exit focus: re-enable player and start returning object rotation smoothly
    // ------------------------------------------------------------------
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
        playerMovement.enabled = false;      // stop player moving
        if (playerLook != null) playerLook.lookEnabled = false; // stop mouse look
        HidePrompt();

        // snapshot start rotation and reset offsets
        focusStartRotation = heldItem.transform.rotation;
        focusRotationOffset = Vector2.zero;
        focusReturnBlend = 1f;
    }

    private void ExitFocusMode()
    {
        inFocusMode = false;
        playerMovement.enabled = true;
        if (playerLook != null) playerLook.lookEnabled = true;
        ShowPrompt("Press [E] to Drop");

        // trigger smooth return of rotation (focusReturnBlend is used in HandleHeldObject)
        focusReturnBlend = 0f;
    }

    // ------------------------------------------------------------------
    // 5) Rotate the held item while in focus mode with separate yaw/pitch limits.
    //    Uses angle-axis quaternions relative to the focusStartRotation.
    // ------------------------------------------------------------------
    private void RotateHeldItem()
    {
        float rotX = rotateXAction.ReadValue<float>(); // A/D
        float rotY = rotateYAction.ReadValue<float>(); // W/S

        float deltaYaw = rotX * rotationSpeed * focusRotationMultiplier * Time.deltaTime;
        float deltaPitch = -rotY * rotationSpeed * focusRotationMultiplier * Time.deltaTime;

        // increment offsets
        focusRotationOffset.x += deltaYaw;   // yaw (horizontal)
        focusRotationOffset.y += deltaPitch; // pitch (vertical)

        // clamp yaw and pitch separately
        focusRotationOffset.x = Mathf.Clamp(focusRotationOffset.x, -maxYaw, maxYaw);
        focusRotationOffset.y = Mathf.Clamp(focusRotationOffset.y, -maxPitch, maxPitch);

        // construct rotation: start * yaw * pitch
        Quaternion yawRot = Quaternion.AngleAxis(focusRotationOffset.x, Vector3.up);
        Quaternion pitchRot = Quaternion.AngleAxis(focusRotationOffset.y, Vector3.right);

        heldItem.transform.rotation = focusStartRotation * yawRot * pitchRot;
    }

    // ------------------------------------------------------------------
    // 6) Pickup / Drop helpers
    // ------------------------------------------------------------------
    public void PickUpItem(PickupInteractable item)
    {
        heldItem = item;
        heldItem.SetHeld(true);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // keep item unparented (we lerp it toward holdPoint)
        item.transform.SetParent(null);
        ShowPrompt("Press [E] to Drop");

        // reset pickup blend to animate the pickup transition
        pickupBlend = 0f;
    }

    public void DropItem()
    {
        if (heldItem == null) return;

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        heldItem.SetHeld(false);
        heldItem = null;

        // ensure we exit focus mode if dropping while inspecting
        ExitFocusMode();
    }

    // small helpers used by other systems (LoopTeleport)
    public bool HeldItemExists()
    {
        return heldItem != null;
    }

    public Transform GetHeldItemTransform()
    {
        return heldItem != null ? heldItem.transform : null;
    }

    // ------------------------------------------------------------------
    // 7) UI prompt helpers
    // ------------------------------------------------------------------
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
