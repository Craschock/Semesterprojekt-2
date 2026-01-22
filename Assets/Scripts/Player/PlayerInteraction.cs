using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction:
/// - pick up / drop items with MOMENTUM and COLLISION FIXES
/// - preview placement on PlaceSlot
/// - place items into PlaceSlot or take from it
/// - disables rotation while previewing
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;      // max raycast distance to interact
    public Transform holdPoint;              // world-space target where held item moves to
    public float pickupSmoothing = 10f;      // blend speed used when picking up (transition)
    public float holdSmoothing = 15f;        // smoothing speed for following the hold point
    public float rotationSpeed = 80f;        // base rotation speed for focus mode

    [Header("Preview")]
    public float previewSmoothing = 8f;      // smoothing when previewing to slot (higher = snappier)
    public LayerMask raycastMask;            // layers to raycast against (ensure HeldItem layer is excluded)

    [Header("Focus Mode")]
    public float focusRotationMultiplier = 3f;
    public float maxYaw = 135f;   // horizontal left/right limit (A/D)
    public float maxPitch = 45f;  // vertical up/down limit (W/S)
    public float focusReturnSmoothing = 5f;

    [Header("Item Physics & Collision")]
    public float maxHoldDistance = 3f;
    public float throwForceMultiplier = 1f; // Adjust to make throws stronger/weaker

    // Mask used to stop held item from clipping (Walls + Other Items)
    // The script AUTOMATICALLY removes 'Player' from this mask in Start()
    public LayerMask collisionMask;

    [Header("References")]
    public PlayerMovement playerMovement;
    public PlayerStats playerStats;
    public PlayerLook playerLook;
    public GameObject uiPrompt;
    public HintUIManager hintUI;
    public PlayerTools playerTools;               // perma player Tools 

    // internal smoothing & rotation state
    private Vector2 focusRotationOffset = Vector2.zero; // x = yaw, y = pitch
    private Quaternion focusStartRotation;
    private float focusReturnBlend = 1f;
    private float pickupBlend = 0f;
    private float previewBlend = 0f;
    private float previewExitBlend = 0f;

    // Physics & Momentum tracking
    private Collider playerCollider; // Reference to player's CharacterController/Collider
    private Vector3 lastHeldPosition;
    private Vector3 currentThrowVelocity;

    // runtime
    private Camera cam;
    private PlayerControls input;
    private InputAction focusAction;
    private InputAction rotateXAction;
    private InputAction rotateYAction;
    private InputAction slot1Action;
    private InputAction slot2Action;
    private InputAction consumeAction;
    private InputAction equipLighterAction;
    private InputAction equipPhoneAction;

    private IInteractable currentInteractable;     // hit interactable (generic)
    private PickupInteractable heldItem;           // currently held item (null if none)
    private PlaceSlot previewSlot = null;          // slot currently previewing (null if none)
    private bool isPreviewing = false;

    private bool inFocusMode = false;

    // layer name for held items (create this in Unity)
    private readonly string heldItemLayerName = "HeldItem";
    private int heldItemLayerIndex = -1;

    private void Awake()
    {
        cam = Camera.main;
        input = new PlayerControls();

        // ------------------------------------------------------
        // INPUT HANDLING: EVENT REGISTRATION
        // ------------------------------------------------------
        input.Player.Interact.performed += OnInteractPerformed;

        focusAction = input.Player.Focus;               // "F"
        rotateXAction = input.Player.RotateX;
        rotateYAction = input.Player.RotateY;

        
        slot1Action = input.Player.Slot1;               // "1"
        slot2Action = input.Player.Slot2;               // "2"
        slot1Action.performed += OnSlot1Performed;
        slot2Action.performed += OnSlot2Performed;

        equipLighterAction = input.Player.EquipLighter; // "F"
        equipPhoneAction = input.Player.EquipPhone;     // "Space"

        if (equipLighterAction != null) equipLighterAction.performed += ctx => OnToggleLighter();
        if (equipPhoneAction != null) equipPhoneAction.performed += ctx => OnTogglePhone();

        consumeAction = input.Player.Consume;
        consumeAction.started += OnConsumeStarted;   // Hold
        consumeAction.canceled += OnConsumeCanceled; // Let go (let it goooooo)


        // Enable Input System
        slot1Action.Enable();
        slot2Action.Enable();
        consumeAction.Enable();
        focusAction.Enable();
        rotateXAction.Enable();
        rotateYAction.Enable();
        equipLighterAction.Enable();
        equipPhoneAction.Enable();

        // compute held layer index (user must create this layer in editor)
        heldItemLayerIndex = LayerMask.NameToLayer(heldItemLayerName);

        // Find the player collider to ignore collisions later
        // Assuming PlayerInteraction is on Camera, go up to PlayerMesh or Player Root
        playerCollider = transform.root.GetComponentInChildren<CharacterController>();
        if (playerCollider == null)
            playerCollider = transform.root.GetComponentInChildren<Collider>();
    }

    // Don't forget to Enable/Disable
    private void OnEnable() => input.Player.Enable();
    private void OnDisable() => input.Player.Disable();

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

        // Auto-configure Collision Mask if empty
        if (collisionMask == 0)
        {
            collisionMask = Physics.DefaultRaycastLayers;
        }

        // CRITICAL FIX: Explicitly remove the Player layer from the collision mask.
        // This stops the SphereCast from hitting the player's own body and flinging the item.
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            collisionMask &= ~(1 << playerLayer);
        }
    }

    private void Update()
    {
        // If a hint is currently open, only allow closing it with E.
        // We handle the actual closing in OnInteractPerformed, but we show the prompt here.
        if (hintUI != null && hintUI.IsHintOpen)
        {
            ShowPrompt("Press [E] to Stop Reading");
            return;
        }

        HandleRaycast();           // handles both items and slots (and now hints too)
        HandleHeldObject();        // move held item toward holdPoint or previewPoint
        HandleFocusModeToggle();   // toggle focus and handle rotation
    }

    // ------------------------------------------------------
    // Run when an action has been performed
    // ------------------------------------------------------
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        // Priority: Close Hint
        if (hintUI != null && hintUI.IsHintOpen)
        {
            hintUI.HideHint();
            return;
        }

        // CASE: Place into slot (while previewing)
        if (isPreviewing && previewSlot != null && heldItem != null)
        {
            if (!previewSlot.HasItem())
            {
                previewSlot.PlaceItem(heldItem);

                // restore collision when placing in slot!
                Collider itemCol = heldItem.GetComponent<Collider>();
                if (playerCollider != null && itemCol != null)
                {
                    Physics.IgnoreCollision(playerCollider, itemCol, false);
                }

                heldItem = null;
                isPreviewing = false;
                previewSlot = null;
                HidePrompt();

                // Activate hands
                if (playerStats != null) playerStats.SetHandsActive(true);
            }
            return;
        }

        // CASE: Take item from slot
        PlaceSlot slot = currentInteractable as PlaceSlot;
        if (slot != null && heldItem == null && slot.HasItem())
        {
            PickupInteractable item = slot.RemoveItem();
            if (item != null)
                PickUpItem(item);
            return;
        }

        // CASE: Pickup world item
        PickupInteractable pickup = currentInteractable as PickupInteractable;
        if (pickup != null && heldItem == null && !pickup.IsSlotted)
        {
            pickup.OnInteract(this);
            return;
        }

        // CASE: Read Hint 
        HintInteractable hint = currentInteractable as HintInteractable;
        if (hint != null && heldItem == null)
        {
            hint.OnInteract(this);
            return;
        }

        // CASE: Take Consumable
        ConsumableInteractable consumable = currentInteractable as ConsumableInteractable;
        if (consumable != null && heldItem == null)
        {
            consumable.OnInteract(this);
            return;
        }

        // CASE: Use WaterHut
        WaterHutInteractable waterHut = currentInteractable as WaterHutInteractable;
        if (waterHut != null && heldItem == null)
        {
            waterHut.OnInteract(this);
            return;
        }

        // CASE: TakeCursedObject
        CursedObject cursedObject = currentInteractable as CursedObject;
        if (cursedObject != null && heldItem == null)
        {
            cursedObject.OnInteract(this);
            return;
        }

        // CASE: Statue Puzzle Interaction
        StatueInteractable statue = currentInteractable as StatueInteractable;
        if (statue != null && heldItem == null)
        {
            statue.OnInteract(this);
            return;
        }

        // CASE: Drop held item
        if (heldItem != null)
        {
            DropItem();
            return;
        }
    }

    private void OnSlot1Performed(InputAction.CallbackContext ctx)
    {
        if (playerStats != null)
        {
            if (playerTools != null) playerTools.ForceStopAllTools();
            playerStats.SelectSlot(0); // Select Slot 1
        }
    }

    private void OnSlot2Performed(InputAction.CallbackContext ctx)
    {
        if (playerStats != null)
        {
            if (playerTools != null) playerTools.ForceStopAllTools();

            playerStats.SelectSlot(1); // Select Slot 2
        }
    }

    private void OnConsumePerformed(InputAction.CallbackContext ctx)
    {
        if (playerStats != null)
        {
            playerStats.ConsumeSelectedSlot(); // Consume Consumable that is currently selected
        }
    }

    private void OnToggleLighter()
    {
        if (playerStats != null)
        {
            if (playerTools != null) playerTools.ForceStopAllTools();
            playerStats.ToggleLighterMode();
        }
    }

    private void OnTogglePhone()
    {
        if (playerStats != null)
        {
            if (playerTools != null) playerTools.ForceStopAllTools();
            playerStats.TogglePhoneMode();
        }
    }

    private void OnConsumeStarted(InputAction.CallbackContext ctx)
    {
        if (playerStats == null) return;

        // Debugging
        Debug.Log($"[Interaction] Consume Started. Mode: {playerStats.currentMode}");

        // IF Inventory Mode: Instant Consume
        if (playerStats.currentMode == EquipmentMode.Inventory)
        {
            playerStats.ConsumeSelectedSlot();
        }
        // If Tool Mode: Use Tool
        else
        {
            if (playerTools != null)
            {
                playerTools.SetToolState(true);
            }
            else
            {
                Debug.LogError("PlayerTools Referenz fehlt im PlayerInteraction Script!");
            }
        }
    }

    private void OnConsumeCanceled(InputAction.CallbackContext ctx)
    {
        // Only relevant in ToolMode (Lass endlich los)
        if (playerStats != null && playerStats.currentMode != EquipmentMode.Inventory)
        {
            Debug.Log("[Interaction] Consume Released (Tool OFF)");
            if (playerTools != null) playerTools.SetToolState(false);
        }
    }

    // --------------------------
    // Raycast and detect hit
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

                // If holding an item and looking at a world pickup -> show Drop
                if (heldItem != null)
                    ShowPrompt("Press [E] to Drop");
                else
                    ShowPrompt("Press [E] to Pick Up");

                return;
            }

            // Check for HintInteractable
            HintInteractable hint = hit.collider.GetComponent<HintInteractable>();
            if (hint != null)
            {
                // no slot preview needed here
                if (isPreviewing)
                    CancelPreview();

                // manage focus transitions
                if (currentInteractable != hint)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = hint;
                    currentInteractable?.OnFocus();
                }

                // show hint prompt (opening hint freezes movement in HintUIManager)
                ShowPrompt("Press [E] to Read");

                return;
            }

            // Check for ConsumableInteractable
            ConsumableInteractable consumable = hit.collider.GetComponent<ConsumableInteractable>();
            if (consumable != null)
            {
                // no slot preview needed here
                if (isPreviewing)
                    CancelPreview();

                // manage focus transitions
                if (currentInteractable != consumable)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = consumable;
                    currentInteractable?.OnFocus();
                }

                // show consumable prompt
                ShowPrompt("Press [E] to Take Item");

                return;
            }

            // Check for WaterHutInteractable
            WaterHutInteractable waterHut = hit.collider.GetComponent<WaterHutInteractable>();
            if (waterHut != null)
            {
                // no slot preview needed here
                if (isPreviewing)
                    CancelPreview();

                // manage focus transitions
                if (currentInteractable != waterHut)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = waterHut;
                    currentInteractable?.OnFocus();
                }

                // show consumable prompt
                ShowPrompt("Press [E] to Clean Yourself");

                return;
            }

            // Check for CursedObject
            CursedObject cursedObject = hit.collider.GetComponent<CursedObject>();
            if (cursedObject != null)
            {
                // no slot preview needed here
                if (isPreviewing)
                    CancelPreview();

                // manage focus transitions
                if (currentInteractable != cursedObject)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = cursedObject;
                    currentInteractable?.OnFocus();
                }

                // show consumable prompt
                ShowPrompt("Press [E] to Burn");

                return;
            }

            // Check for StatueInteractable
            StatueInteractable statueHit = hit.collider.GetComponent<StatueInteractable>();
            if (statueHit != null)
            {
                // no slot preview needed here
                if (isPreviewing) CancelPreview();

                // manage focus transitions (Outline an/aus)
                if (currentInteractable != statueHit)
                {
                    currentInteractable?.OnLoseFocus();
                    currentInteractable = statueHit;
                    currentInteractable?.OnFocus();
                }

                // Only show prompt when statue is intertactable
                if (!statueHit.IsActivated())
                {
                    ShowPrompt("Press [E] to Inspect");
                }
                else
                {
                    HidePrompt();
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
                ShowPrompt("Press [E] to Drop");
            else
                HidePrompt();
        }
    }

    // --------------------------
    // Move held item toward hold or preview or slot preview
    // --------------------------
    private void HandleHeldObject()
    {
        if (heldItem == null) return;
        Transform item = heldItem.transform;

        // 1. Momentum Calculation
        Vector3 displacement = item.position - lastHeldPosition;
        if (Time.deltaTime > 0)
            currentThrowVelocity = displacement / Time.deltaTime;
        currentThrowVelocity = Vector3.ClampMagnitude(currentThrowVelocity, 20f);
        lastHeldPosition = item.position;

        pickupBlend = Mathf.Clamp01(pickupBlend + Time.deltaTime * pickupSmoothing);

        // 2. Preview Mode
        if (isPreviewing && previewSlot != null)
        {
            Transform previewT = previewSlot.GetPreviewTransform();
            previewBlend = Mathf.Clamp01(previewBlend + Time.deltaTime * previewSmoothing);
            item.position = Vector3.Lerp(item.position, previewT.position, Time.deltaTime * holdSmoothing * previewBlend);
            item.rotation = Quaternion.Slerp(item.rotation, previewT.rotation, Time.deltaTime * holdSmoothing * previewBlend);
            return;
        }

        // 3. Normal Hold - SMART COLLISION CHECK
        Vector3 targetPos = holdPoint.position;
        Quaternion targetRot = holdPoint.rotation;

        Vector3 direction = holdPoint.position - cam.transform.position;
        float distance = direction.magnitude;
        RaycastHit hit;
        bool hasHit = false;

        Collider col = heldItem.GetComponent<Collider>();

        // CHECK COLLIDER TYPE
        if (col is BoxCollider box)
        {
            // Calculate half extents properly scaled
            Vector3 halfExtents = Vector3.Scale(box.size, heldItem.transform.lossyScale) * 0.5f;
            // Slightly shrink (0.95f) to avoid snagging on tiny imperfections
            halfExtents *= 0.95f;

            hasHit = Physics.BoxCast(
                cam.transform.position,
                halfExtents,
                direction,
                out hit,
                heldItem.transform.rotation,
                distance,
                collisionMask
            );
        }
        else if (col is SphereCollider sphere)
        {
            float radius = sphere.radius * Mathf.Max(heldItem.transform.lossyScale.x, heldItem.transform.lossyScale.y, heldItem.transform.lossyScale.z);
            hasHit = Physics.SphereCast(cam.transform.position, radius, direction, out hit, distance, collisionMask);
        }
        else
        {
            // Capsule, Mesh, or other: Fallback to a tiny SphereCast so it fits everywhere
            hasHit = Physics.SphereCast(cam.transform.position, 0.05f, direction, out hit, distance, collisionMask);
        }

        if (hasHit)
        {
            targetPos = cam.transform.position + direction.normalized * hit.distance;
        }

        // 4. Smooth Exit from Preview
        if (previewExitBlend > 0f)
        {
            previewExitBlend = Mathf.Clamp01(previewExitBlend - Time.deltaTime * 4f);
            item.position = Vector3.Lerp(item.position, targetPos, Time.deltaTime * holdSmoothing * (pickupBlend * (1f - previewExitBlend)));
            item.rotation = Quaternion.Slerp(item.rotation, targetRot, Time.deltaTime * holdSmoothing * (1f - previewExitBlend));
            return;
        }

        // 5. Final Move
        item.position = Vector3.Lerp(item.position, targetPos, Time.deltaTime * holdSmoothing * pickupBlend);

        if (!inFocusMode)
        {
            if (focusReturnBlend < 1f) focusReturnBlend = Mathf.Clamp01(focusReturnBlend + Time.deltaTime * focusReturnSmoothing);
            item.rotation = Quaternion.Slerp(item.rotation, targetRot, Time.deltaTime * holdSmoothing * focusReturnBlend);
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

        // Deactivate hands
        if (playerStats != null) playerStats.SetHandsActive(false);

        heldItem = item;
        heldItem.SetHeld(true);
        lastHeldPosition = item.transform.position; // Init momentum

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Collider itemCol = item.GetComponent<Collider>();

        // force ignore collision with HeldItem
        if (playerCollider != null && itemCol != null)
        {
            Physics.IgnoreCollision(playerCollider, itemCol, true);
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
        Collider itemCol = heldItem.GetComponent<Collider>();

        // restore collision with player (HeldItem)
        if (playerCollider != null && itemCol != null)
        {
            Physics.IgnoreCollision(playerCollider, itemCol, false);
        }

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;

            // APPLY MOMENTUM
            rb.linearVelocity = currentThrowVelocity * throwForceMultiplier;
        }

        heldItem.SetHeld(false);
        heldItem = null;
        ExitFocusMode();

        // Activate hands
        if (playerStats != null) playerStats.SetHandsActive(true);
    }

    // Cancel preview and bring held item back to hand
    private void CancelPreview()
    {
        if (!isPreviewing) return;

        isPreviewing = false;

        // Start smoothing the transition back to holdPoint
        previewExitBlend = 1f;

        previewSlot = null;
    }

    // --------------------------
    // helpers for other systems
    // --------------------------
    public bool HeldItemExists() => heldItem != null;
    public bool IsInFocusMode() => inFocusMode;
    public Transform GetHeldItemTransform() => heldItem != null ? heldItem.transform : null;

    public bool TryPickUpConsumable(ConsumableType Type) {
        Debug.Log("Try picking up: " + Type);
        bool success = playerStats.AddConsumable(Type);
        return success;
    }

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