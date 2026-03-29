using UnityEngine;
using UnityEngine.Events;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Defines the current active equipment mode of the player.
/// </summary>
public enum EquipmentMode
{
    Inventory = 0,
    Lighter = 1,
    Phone = 2
}

/// <summary>
/// Manages the player's 2-slot inventory, active equipment mode (Lighter/Phone), and item consumption.
/// Replaces the inventory logic from the old PlayerStats script.
/// </summary>
public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the player's stats manager to apply item effects.")]
    public PlayerStatsManager statsManager;

    [Header("Global Audio")]
    public EventReference modeSwitchSound;

    [Header("State")]
    public EquipmentMode currentMode = EquipmentMode.Inventory;
    
    [Header("Tool Data")]
    public ConsumableItemData lighterItemData;
    public ConsumableItemData phoneItemData;
    
    // The inventory array holding our new ScriptableObjects
    private ConsumableItemData[] inventory = new ConsumableItemData[2];
    
    // -1 = None, 0 = Left Hand (Slot 1), 1 = Right Hand (Slot 2)
    private int selectedSlotIndex = -1;
    private bool areHandsActive = true;

    // FMOD Audio Instance for looping selection sounds (like a radio)
    private EventInstance currentSelectionSoundInstance;

    // --- Events ---
    // Now passes the ScriptableObject instead of the old Enum!
    public UnityEvent<int, ConsumableItemData> OnInventoryChanged; 
    public UnityEvent<int> OnSlotSelected;

    /// <summary>
    /// Initializes the inventory setup on start.
    /// </summary>
    private void Start()
    {
        if (statsManager == null)
        {
            statsManager = GetComponent<PlayerStatsManager>();
        }
        
        // Ensure UI and visuals are updated to empty on start
        UpdateVisualsForCurrentMode();
    }

    /// <summary>
    /// Stops any looping audio when the script is disabled or destroyed.
    /// </summary>
    private void OnDisable()
    {
        StopSelectionSound();
    }

    /// <summary>
    /// Adds a consumable item to the first available slot.
    /// </summary>
    /// <param name="itemData">The ScriptableObject data of the item.</param>
    /// <returns>True if successfully added, false if inventory is full.</returns>
    public bool AddConsumable(ConsumableItemData itemData)
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = itemData;
                Debug.Log($"[EquipmentManager] Added {itemData.itemName} to Slot {i + 1}");
                
                OnInventoryChanged?.Invoke(i, itemData);
                SelectSlot(i); // Auto-select the picked up item
                
                return true;
            }
        }

        Debug.Log("[EquipmentManager] Inventory Full!");
        return false;
    }

    /// <summary>
    /// Selects a specific inventory slot and plays its selection sound.
    /// </summary>
    /// <param name="index">The slot index (0 or 1).</param>
    public void SelectSlot(int index)
    {
        if (currentMode != EquipmentMode.Inventory)
        {
            SetMode(EquipmentMode.Inventory);
        }

        StopSelectionSound();

        // If clicking the same slot, deselect it
        if (selectedSlotIndex == index)
        {
            selectedSlotIndex = -1;
            Debug.Log($"[EquipmentManager] Deselected Slot {index + 1}");
        }
        else
        {
            selectedSlotIndex = index;
            ConsumableItemData itemInSlot = inventory[index];

            if (itemInSlot != null)
            {
                Debug.Log($"[EquipmentManager] Selected Slot {index + 1}. Item: {itemInSlot.itemName}");
                PlaySelectionSound(itemInSlot);
            }
        }

        OnSlotSelected?.Invoke(selectedSlotIndex);
    }

    /// <summary>
    /// Consumes the currently selected item, applies its effects, and clears the slot.
    /// </summary>
    /// <returns>The consumed item data, or null if nothing was consumed.</returns>
    public ConsumableItemData ConsumeSelectedSlot()
    {
        if (!areHandsActive || selectedSlotIndex == -1) return null;

        ConsumableItemData item = inventory[selectedSlotIndex];

        if (item != null)
        {
            StopSelectionSound();

            // Play the item's specific use sound
            if (!item.useSound.IsNull)
            {
                RuntimeManager.PlayOneShot(item.useSound, transform.position);
            }

            // Apply effects via the ScriptableObject
            if (statsManager != null)
            {
                item.ApplyEffects(statsManager);
            }

            // Clear the slot
            inventory[selectedSlotIndex] = null;
            OnInventoryChanged?.Invoke(selectedSlotIndex, null);
            
            Debug.Log($"[EquipmentManager] Consumed {item.itemName}");
            return item;
        }

        return null;
    }

    /// <summary>
    /// Switches the active equipment mode (Inventory, Lighter, Phone).
    /// </summary>
    /// <param name="newMode">The mode to switch to.</param>
    public void SetMode(EquipmentMode newMode)
    {
        if (currentMode == newMode) return;
        
        StopSelectionSound();
        currentMode = newMode;

        if (!modeSwitchSound.IsNull)
        {
            RuntimeManager.PlayOneShot(modeSwitchSound);
        }

        UpdateVisualsForCurrentMode();
    }

    /// <summary>
    /// Toggles the lighter mode on and off.
    /// </summary>
    public void ToggleLighterMode()
    {
        if (!areHandsActive) return;
        SetMode(currentMode == EquipmentMode.Lighter ? EquipmentMode.Inventory : EquipmentMode.Lighter);
    }

    /// <summary>
    /// Toggles the phone mode on and off.
    /// </summary>
    public void TogglePhoneMode()
    {
        if (!areHandsActive) return;
        SetMode(currentMode == EquipmentMode.Phone ? EquipmentMode.Inventory : EquipmentMode.Phone);
    }

    /// <summary>
    /// Fires events to update UI and Hand Visuals based on the current mode.
    /// </summary>
    public void UpdateVisualsForCurrentMode()
    {
        if (!areHandsActive)
        {
            OnInventoryChanged?.Invoke(0, null);
            OnInventoryChanged?.Invoke(1, null);
            OnSlotSelected?.Invoke(-1);
            return;
        }

        switch (currentMode)
        {
            case EquipmentMode.Inventory:
                OnInventoryChanged?.Invoke(0, inventory[0]);
                OnInventoryChanged?.Invoke(1, inventory[1]);
                OnSlotSelected?.Invoke(selectedSlotIndex);
                break;

            case EquipmentMode.Lighter:
                // Spawns the lighter in the right hand
                OnInventoryChanged?.Invoke(0, null);
                OnInventoryChanged?.Invoke(1, lighterItemData);
                OnSlotSelected?.Invoke(-1); 
                break;

            case EquipmentMode.Phone:
                // Spawns the phone in the right hand
                OnInventoryChanged?.Invoke(0, null);
                OnInventoryChanged?.Invoke(1, phoneItemData);
                OnSlotSelected?.Invoke(-1); 
                break;
        }
    }
    
    /// <summary>
    /// Enables or disables the player's hands (e.g., during cutscenes or specific puzzles).
    /// </summary>
    /// <param name="active">True to show hands, false to hide.</param>
    public void SetHandsActive(bool active)
    {
        areHandsActive = active;

        if (active)
        {
            UpdateVisualsForCurrentMode();
        }
        else
        {
            StopSelectionSound();
            OnInventoryChanged?.Invoke(0, null);
            OnInventoryChanged?.Invoke(1, null);
            OnSlotSelected?.Invoke(-1);
        }
    }

    /// <summary>
    /// Plays the specific selection sound defined in the item's ScriptableObject.
    /// </summary>
    /// <param name="item">The selected item data.</param>
    private void PlaySelectionSound(ConsumableItemData item)
    {
        if (item.selectSound.IsNull) return;

        currentSelectionSoundInstance = RuntimeManager.CreateInstance(item.selectSound);
        currentSelectionSoundInstance.start();
        // We do not release here, so we can stop looping sounds (like a radio) later
    }

    /// <summary>
    /// Stops the currently playing selection sound (if any).
    /// </summary>
    private void StopSelectionSound()
    {
        if (currentSelectionSoundInstance.isValid())
        {
            currentSelectionSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentSelectionSoundInstance.release();
        }
    }
}