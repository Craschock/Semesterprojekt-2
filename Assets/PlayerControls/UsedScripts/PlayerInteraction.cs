using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction:
/// - pick up / drop items
/// - preview placement on PlaceSlot
/// - place items into PlaceSlot or take from it
/// - disables rotation while previewing
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;      // max raycast distance to interact
    public Transform holdPoint;              // world-space target where held item moves to
    public float pickupSmoothing = 1f;       // blend speed used when picking up (transition)
    public float holdSmoothing = 12f;        // smoothing speed for following the hold point
    public float rotationSpeed = 80f;        // base rotation speed for focus mode

    [Header("Preview")]
    public float previewSmoothing = 8f;      // smoothing when previewing to slot (higher = snappier)
    public LayerMask raycastMask;            // layers to raycast against (ensure HeldItem layer is excluded)

    [Header("Focus Mode")]
    public float focusRotationMultiplier = 3f;
    public float maxYaw = 135f;   // horizontal left/right limit (A/D)
    public float maxPitch = 45f;  // vertical up/down limit (W/S)
    public float focusReturnSmoothing = 1f;

    [Header("Item Physics")]
    public float maxHoldDistance = 3f;
    public float wallCheckRadius = 0.3f;
    public LayerMask environmentMask;

    [Header("References")]
    public PlayerMovement playerMovement;
    public PlayerLook playerLook;
    public GameObject uiPrompt;

    // internal smoothing & rotation state
    private Vector2 focusRotationOffset = Vector2.zero; // x = yaw, y = pitch
    private Quaternion focusStartRotation;
    private float focusReturnBlend = 1f;
    private float pickupBlend = 0f;
    private float previewBlend = 0f;

    // runtime
    private Camera cam;
    private PlayerControls input;
    private InputAction interactAction;
    private InputAction focusAction;
    private InputAction rotateXAction;
    private InputAction rotateYAction;

    private IInteractable currentInteractable;     // hit interactable (generic)
    private PickupInteractable heldItem;           // currently held item (null if none)
    private PlaceSlot previewSlot = null;          // slot currently previewing (null if none)
    private bool isPreviewing = false;

    private bool inFocusMode = false;
    private bool interactPressedThisFrame = false;

    // layer name for held items (create this in Unity)
    private readonly string heldItemLayerName = "HeldItem";
    private int heldItemLayerIndex = -1;

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

        // compute held layer index (user must create this layer in editor)
        heldItemLayerIndex = LayerMask.NameToLayer(heldItemLayerName);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (uiPrompt != null)
            uiPrompt.SetActive(false);

        // If raycastMask is not configured, default to everything except HeldItem (if layer exists)
        if (raycastMask == 0)
        {
            raycastMask = Physics.DefaultRaycastLayers;
            if (heldItemLayerIndex >= 0)
                raycastMask &= ~(1 << heldItemLayerIndex); // ignore held item layer for raycasts
        }
    }

    private void Update()
    {
        interactPressedThisFrame = interactAction.WasPerformedThisFrame();

        HandleRaycast();           // handles both items and slots
        HandleHeldObject();        // move held item toward holdPoint or previewPoint
        HandleFocusModeToggle();   // toggle focus and handle rotation
        HandleHeldInteract();      // drop / place / take depending on context
    }

    // --------------------------
    // Raycast and detect hit
    // - prefers PlaceSlot if hit
    // - handles preview state when holding an item and aiming at a slot
    // --------------------------
    private void HandleRaycast()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // Use configured mask — ensures held item doesn't block raycast
        if (Physics.Raycast(ray, out hit, interactDistance, raycastMask))
        {
            // Try place slot first
            PlaceSlot slot = hit.collider.GetComponent<PlaceSlot>();
            if (slot != null)
            {
                // Manage focus transitions for slot
                if (currentInteractable != slot)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = slot;
                    currentInteractable?.OnFocus();
                }

                // If player is holding an item -> preview placement
                if (heldItem != null)
                {
                    // If slot is full -> no preview allowed
                    if (slot.HasItem())
                    {
                        // cannot place here
                        if (isPreviewing) CancelPreview();

                        ShowPrompt("Slot is full");
                        return;
                    }

                    // start preview if new
                    if (previewSlot != slot)
                    {
                        previewSlot = slot;
                        isPreviewing = true;
                        previewBlend = 0f; // reset preview smoothing
                        // disable rotation while previewing
                        // (player can rotate before previewing but not while previewing)
                    }

                    ShowPrompt("Press [E] to Place Item");
                    
                }
                else
                {
                    // not holding: if slot has an item, allow taking it
                    if (slot.HasItem())
                        ShowPrompt("Press [E] to Take Item");
                    else
                        HidePrompt();
                }

                return;
            }

            // Not a slot, check if it's a pickup interactable
            PickupInteractable pickup = hit.collider.GetComponent<PickupInteractable>();
            if (pickup != null)
            {
                if (pickup.IsSlotted)
                    return; // treat it as "not a pickup". prevents double prompt when item is slotted
                // if we were previewing a slot, cancel preview
                if (isPreviewing)
                    CancelPreview();

                if (currentInteractable != pickup)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = pickup;
                    currentInteractable?.OnFocus();
                }

                // If holding an item and looking at a world pickup, show Place prompt? no -> show Drop
                if (heldItem != null)
                {
                    ShowPrompt("Press [E] to Drop");
                }
                else
                {
                    ShowPrompt("Press [E] to Pick Up");
                }

                return;
            }

            // Hit something else -> clear focus/preview
            currentInteractable?.OnLoseFocus();
            currentInteractable = null;
            if (isPreviewing) CancelPreview();
            HidePrompt();
        }
        else
        {
            // nothing hit
            currentInteractable?.OnLoseFocus();
            currentInteractable = null;

            if (isPreviewing)
                CancelPreview();

            // When holding an item, show drop prompt
            if (heldItem != null)
            {
                ShowPrompt("Press [E] to Drop");
            }
            else
            {
                HidePrompt();
            }
        }
    }

    // --------------------------
    // Move held item toward hold or preview or slot preview
    // --------------------------
    private void HandleHeldObject()
    {
        if (heldItem == null) return;

        Transform item = heldItem.transform;

        // ramp pickup blend to 1 for pickup transitions
        pickupBlend = Mathf.Clamp01(pickupBlend + Time.deltaTime * pickupSmoothing);

        // choose target position/rotation depending on preview state
        Vector3 targetPos;
        Quaternion targetRot;

        if (isPreviewing && previewSlot != null)
        {
            // preview mode: lerp toward slot's preview transform
            Transform previewT = previewSlot.GetPreviewTransform();
            targetPos = previewT.position;
            targetRot = previewT.rotation;

            // smooth with a previewBlend (snappier than normal hold)
            previewBlend = Mathf.Clamp01(previewBlend + Time.deltaTime * previewSmoothing);
            item.position = Vector3.Lerp(item.position, targetPos, Time.deltaTime * holdSmoothing * previewBlend);
            item.rotation = Quaternion.Slerp(item.rotation, targetRot, Time.deltaTime * holdSmoothing * previewBlend);
            return;
        }

        // Normal held behavior: follow holdPoint, with wall clipping protection
        targetPos = holdPoint.position;
        targetRot = holdPoint.rotation;

        Vector3 direction = holdPoint.position - cam.transform.position;
        float distance = direction.magnitude;
        if (Physics.SphereCast(cam.transform.position, wallCheckRadius, direction, out RaycastHit hit, distance, environmentMask))
        {
            targetPos = hit.point - direction.normalized * 0.1f;
        }

        item.position = Vector3.Lerp(item.position, targetPos, Time.deltaTime * holdSmoothing * pickupBlend);

        // rotation only reset when NOT in focus mode and NOT previewing
        if (!inFocusMode)
        {
            if (focusReturnBlend < 1f)
                focusReturnBlend = Mathf.Clamp01(focusReturnBlend + Time.deltaTime * focusReturnSmoothing);

            item.rotation = Quaternion.Slerp(item.rotation, targetRot, Time.deltaTime * holdSmoothing * focusReturnBlend);
        }
    }

    // --------------------------
    // Drop / Place / Take logic depending on context
    // - if previewing & interact pressed -> place into slot
    // - if looking at a slot with item & not holding -> take item
    // - if looking at pickup and not holding -> pick up
    // - if holding and looking at nothing -> drop
    // --------------------------
    private void HandleHeldInteract()
    {
        if (!interactPressedThisFrame) return;

        // CASE: placing into preview slot
        if (isPreviewing && previewSlot != null && heldItem != null)
        {
            // SAFETY CHECK: never place into full slot (kinda repetitive)
            if (previewSlot.HasItem())
            {
                CancelPreview();
                return;
            }

            previewSlot.PlaceItem(heldItem);
            // placed -> clear held reference
            heldItem = null;
            isPreviewing = false;
            previewSlot = null;
            HidePrompt();
            interactPressedThisFrame = false;
            return;
        }

        // If not holding and currentInteractable is a PlaceSlot with an item -> take it
        PlaceSlot slot = currentInteractable as PlaceSlot;
        if (slot != null && heldItem == null && slot.HasItem())
        {
            PickupInteractable item = slot.RemoveItem();
            if (item != null)
            {
                // pick the removed item up
                PickUpItem(item);
            }
            interactPressedThisFrame = false;
            return;
        }

        // If current interactable is a PickupInteractable and not holding -> pick it up
        PickupInteractable pickup = currentInteractable as PickupInteractable;
        if (pickup != null && heldItem == null && !pickup.IsSlotted)
        {
            // this will call PickUpItem which sets held state
            pickup.OnInteract(this);
            interactPressedThisFrame = false;
            return;
        }

        // If holding and nothing special -> drop
        if (heldItem != null)
        {
            // drop in world
            DropItem();
            interactPressedThisFrame = false;
            return;
        }
    }

    // --------------------------
    // Focus / Inspect mode toggling
    // - rotation is disabled while previewing (by early return)
    // --------------------------
    private void HandleFocusModeToggle()
    {
        if (heldItem == null)
        {
            if (inFocusMode) ExitFocusMode();
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
        if (playerLook != null) playerLook.lookEnabled = false;
        HidePrompt();

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

        // start smooth return to hold rotation
        focusReturnBlend = 0f;
    }

    // Rotate the held item while in focus mode, but DO NOT rotate while previewing
    private void RotateHeldItem()
    {
        if (isPreviewing) return; // block rotation during preview

        float rotX = rotateXAction.ReadValue<float>();
        float rotY = rotateYAction.ReadValue<float>();

        float deltaYaw = rotX * rotationSpeed * focusRotationMultiplier * Time.deltaTime;
        float deltaPitch = -rotY * rotationSpeed * focusRotationMultiplier * Time.deltaTime;

        focusRotationOffset.x += deltaYaw;
        focusRotationOffset.y += deltaPitch;

        focusRotationOffset.x = Mathf.Clamp(focusRotationOffset.x, -maxYaw, maxYaw);
        focusRotationOffset.y = Mathf.Clamp(focusRotationOffset.y, -maxPitch, maxPitch);

        Quaternion yawRot = Quaternion.AngleAxis(focusRotationOffset.x, Vector3.up);
        Quaternion pitchRot = Quaternion.AngleAxis(focusRotationOffset.y, Vector3.right);

        heldItem.transform.rotation = focusStartRotation * yawRot * pitchRot;
    }

    // --------------------------
    // Pickup & Drop helpers
    // - when picking up we set layer to HeldItem so it doesn't block raycasts
    // - when dropping or placing, the layer is restored so OutlineController can work
    // --------------------------
    public void PickUpItem(PickupInteractable item)
    {
        if (item == null) return;

        heldItem = item;
        heldItem.SetHeld(true);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // unparent; we will lerp toward holdPoint
        item.transform.SetParent(null);
        ShowPrompt("Press [E] to Drop");

        // reset smoothing blends
        pickupBlend = 0f;
        previewBlend = 0f;
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
        ExitFocusMode();
    }

    // Cancel preview and bring held item back to hand
    private void CancelPreview()
    {
        isPreviewing = false;
        previewSlot = null;
        previewBlend = 0f;
        // re-enable rotation if we were blocking it (handled in RotateHeldItem)
    }

    // --------------------------
    // helpers for other systems
    // --------------------------
    public bool HeldItemExists() => heldItem != null;
    public Transform GetHeldItemTransform() => heldItem != null ? heldItem.transform : null;

    // --------------------------
    // UI prompt helpers
    // --------------------------
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
