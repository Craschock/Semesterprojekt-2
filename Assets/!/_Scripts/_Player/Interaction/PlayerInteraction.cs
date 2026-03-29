using UnityEngine;
using UnityEngine.InputSystem; // WICHTIG: Neues Input System

/// <summary>
/// Handles player input for interactions, tools, and manages held physical items.
/// Routes inputs from PlayerControls to the Scanner and EquipmentManager.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("System References")]
    public InteractionScanner scanner;
    public PlayerEquipmentManager equipmentManager;

    [Header("Physical Holding Settings")]
    public Transform holdPoint;
    public float throwForce = 10f;
    public float holdSmoothSpeed = 10f;

    // The currently held object
    private PickupInteractable currentHeldItem;
    public PickupInteractable CurrentHeldItem => currentHeldItem;
    
    // Input System Reference
    private PlayerControls controls;

    /// <summary>
    /// Initializes the input system and binds the actions.
    /// </summary>
    private void Awake()
    {
        controls = new PlayerControls();
        
        // Interact (E)
        controls.Player.Interact.performed += ctx => HandleInteract();

        // Right Click (Consume)
        controls.Player.Consume.performed += ctx => HandleSecondaryAction();

        // Inventory Slots (1 & 2)
        controls.Player.Slot1.performed += ctx => equipmentManager?.SelectSlot(0);
        controls.Player.Slot2.performed += ctx => equipmentManager?.SelectSlot(1);

        // Tools (F & Space)
        controls.Player.EquipLighter.performed += ctx => equipmentManager?.ToggleLighterMode();
        controls.Player.EquipPhone.performed += ctx => equipmentManager?.TogglePhoneMode();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    /// <summary>
    /// Updates the held item position and checks for unmapped inputs (like Left Click).
    /// </summary>
    private void Update()
    {
        UpdateHeldItemPosition();

        // Fallback for Left Click (Primary Action: Throw / Use Item)
        // Since it's not explicitly in your PlayerControls action map
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandlePrimaryAction();
        }
    }

    /// <summary>
    /// Called when the Interact key (E) is pressed.
    /// </summary>
    private void HandleInteract()
    {
        if (currentHeldItem != null)
        {
            DropItem();
        }
        else if (scanner != null && scanner.CurrentTarget != null)
        {
            scanner.CurrentTarget.OnInteract(this);
        }
    }

    /// <summary>
    /// Called on Left Click. Throws a held object or consumes the selected item.
    /// </summary>
    private void HandlePrimaryAction()
    {
        if (currentHeldItem != null)
        {
            ThrowItem();
        }
        else if (equipmentManager != null)
        {
            // Note: If you want right-click to consume instead, move this to HandleSecondaryAction
            equipmentManager.ConsumeSelectedSlot();
        }
    }

    /// <summary>
    /// Called on Right Click. Drops a held object.
    /// </summary>
    private void HandleSecondaryAction()
    {
        if (currentHeldItem != null)
        {
            DropItem();
        }
    }

    /// <summary>
    /// Smoothly moves the held physics object to the hold point.
    /// </summary>
    private void UpdateHeldItemPosition()
    {
        if (currentHeldItem != null && holdPoint != null)
        {
            currentHeldItem.transform.position = Vector3.Lerp(currentHeldItem.transform.position, holdPoint.position, Time.deltaTime * holdSmoothSpeed);
        }
    }

    /// <summary>
    /// Sets the provided item as the currently held physics object.
    /// </summary>
    public void PickUpItem(PickupInteractable item)
    {
        if (currentHeldItem != null) DropItem();

        currentHeldItem = item;
        currentHeldItem.SetHeld(true);
    }

    /// <summary>
    /// Drops the currently held physics item back into the world.
    /// </summary>
    private void DropItem()
    {
        if (currentHeldItem == null) return;
        
        PlayerFocusMode focusMode = GetComponent<PlayerFocusMode>();
        if (focusMode != null && focusMode.IsInFocusMode)
        {
            focusMode.EndFocus();
        }
        
        currentHeldItem.SetHeld(false);
        currentHeldItem = null;
    }

    /// <summary>
    /// Applies forward force to the currently held physics item.
    /// </summary>
    private void ThrowItem()
    {
        if (currentHeldItem == null) return;

        PlayerFocusMode focusMode = GetComponent<PlayerFocusMode>();
        if (focusMode != null && focusMode.IsInFocusMode)
        {
            focusMode.EndFocus();
        }
        
        Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
        currentHeldItem.SetHeld(false);
        currentHeldItem = null;

        if (rb != null && scanner.cameraTransform != null)
        {
            rb.AddForce(scanner.cameraTransform.forward * throwForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Attempts to store a consumable in the inventory using the EquipmentManager.
    /// </summary>
    public bool TryPickUpConsumable(ConsumableItemData itemData)
    {
        if (equipmentManager != null)
        {
            return equipmentManager.AddConsumable(itemData);
        }
        return false;
    }
}