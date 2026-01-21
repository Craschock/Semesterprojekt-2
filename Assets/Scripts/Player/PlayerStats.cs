using UnityEngine;
using UnityEngine.Events;
using System.IO;

public class PlayerStats : MonoBehaviour
{
    [Header("Configuration")]
    public float maxHealth = 100f;
    public float maxStamina = 100f;
    public float maxFear = 100f;
    public float maxPurity = 100f;

    [Header("Regeneration")]
    public bool regenerateStamina = true;
    public float staminaRegenRate = 15f;
    public float staminaDrainRate = 20f;

    private bool areHandsActive = true;

    // Other references
    private ConsumableOnConsume effectHandler;

    // This struct holds the raw data we will want to save to a file later
    [System.Serializable]
    public struct PlayerData
    {
        public float health;
        public float stamina;
        public float fear;
        public float purity;

        // --- Inventory Data ---
        public ConsumableType[] inventory; // Array of size 2
    }

    // The actual instance of our data
    [SerializeField]
    private PlayerData currentStats;

    // --- Selected Slot Tracker ---
    // -1 = None, 0 = Left Hand (Slot 1), 1 = Right Hand (Slot 2)
    private int selectedSlotIndex = 0;

    // Events 
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent<float> OnFearChanged;
    public UnityEvent<float> OnPurityChanged;

    // Event for UI to update inventory slots (optional but recommended)
    public UnityEvent<int, ConsumableType> OnInventoryChanged; // int = slotIndex, type = item
    public UnityEvent<int> OnSlotSelected; // int = newly selected index

    private void Awake()
    {
        // Get consumableOnConsume reference
        effectHandler = GetComponent<ConsumableOnConsume>();
    }

    private void Start()
    {
        ResetStats();
    }

    private void Update()
    {
        HandleStaminaRegen();
    }

    private void ResetStats()
    {
        currentStats.health = maxHealth;
        currentStats.stamina = maxStamina;
        currentStats.fear = 0f;
        currentStats.purity = 100f;

        // Initialize Inventory (Size 2)
        currentStats.inventory = new ConsumableType[2];
        currentStats.inventory[0] = ConsumableType.None;
        currentStats.inventory[1] = ConsumableType.None;

        selectedSlotIndex = -1;

        UpdateAllEvents();
    }

    // --- LOGIC: INVENTORY & CONSUMABLES ---

    public void SelectSlot(int index)
    {
        if (selectedSlotIndex == index) // If same slot
        {
            selectedSlotIndex = -1; // Deselect
            Debug.Log($"[PlayerStats] Deselected Slot {index + 1}");
        }
        else
        {
            selectedSlotIndex = index; // Select new
            Debug.Log($"[PlayerStats] Selected Slot {index + 1}. Item: {currentStats.inventory[index]}");
        }

        // We pass -1 if nothing is selected
        OnSlotSelected?.Invoke(selectedSlotIndex);
    }

    public int GetSelectedSlotIndex() => selectedSlotIndex;

    public void SetHandsActive(bool active)
    {
        areHandsActive = active;

        if (active)
        {
            // �f active; Restore the visuals based on actual inventory

            if (currentStats.inventory != null)
            {
                OnInventoryChanged?.Invoke(0, currentStats.inventory[0]);
                OnInventoryChanged?.Invoke(1, currentStats.inventory[1]);
            }
            // Stelle sicher, dass die Position (Selection) auch wieder stimmt
            OnSlotSelected?.Invoke(selectedSlotIndex);

            Debug.Log("[PlayerStats] Hands Reactivated.");
        }
        else
        {
            // If not active: Fake empty inventory to visuals
            OnInventoryChanged?.Invoke(0, ConsumableType.None);
            OnInventoryChanged?.Invoke(1, ConsumableType.None);
            OnSlotSelected?.Invoke(-1);

            Debug.Log("[PlayerStats] Hands Deactivated (Hidden).");
        }
    }
    // Tries to add item. Returns true if successful, false if full.
    public bool AddConsumable(ConsumableType item)
    {
        // 1. Try to fill Slot 1 first if empty
        if (currentStats.inventory[0] == ConsumableType.None)
        {
            currentStats.inventory[0] = item;
            Debug.Log($"[PlayerStats] Added {item} to Slot 1");
            OnInventoryChanged?.Invoke(0, item);

            // --- AUTO SELECT SLOT 1 ---
            selectedSlotIndex = 0;
            OnSlotSelected?.Invoke(selectedSlotIndex);
            // --------------------------

            return true;
        }
        // 2. Try to fill Slot 2 if empty
        else if (currentStats.inventory[1] == ConsumableType.None)
        {
            currentStats.inventory[1] = item;
            Debug.Log($"[PlayerStats] Added {item} to Slot 2");
            OnInventoryChanged?.Invoke(1, item);

            // --- AUTO SELECT SLOT 2 ---
            selectedSlotIndex = 1;
            OnSlotSelected?.Invoke(selectedSlotIndex);
            // --------------------------

            return true;
        }

        Debug.Log("[PlayerStats] Inventory Full!");
        return false;
    }

    // Returns the item in the currently selected slot and clears the slot
    public ConsumableType ConsumeSelectedSlot()
    {
        // If hands are deactivated, do nothing
        if (!areHandsActive) return ConsumableType.None;

        // If nothing selected, do nothing.
        if (selectedSlotIndex == -1) return ConsumableType.None;

        ConsumableType item = currentStats.inventory[selectedSlotIndex];

        if (item != ConsumableType.None)
        {
            if (effectHandler != null)
            {
                effectHandler.ApplyEffect(item);
            }
            else
            {
                Debug.LogWarning("ConsumableOnConsume component missing from Player object!");
            }

            // Remove item from inventory
            currentStats.inventory[selectedSlotIndex] = ConsumableType.None;
            OnInventoryChanged?.Invoke(selectedSlotIndex, ConsumableType.None);
            Debug.Log($"[PlayerStats] Consumed {item} from Slot {selectedSlotIndex + 1}");
            return item;
        }

        return ConsumableType.None;
    }

    // --- LOGIC: STAMINA ---
    private void HandleStaminaRegen()
    {
        if (regenerateStamina && currentStats.stamina < maxStamina)
        {
            currentStats.stamina += staminaRegenRate * Time.deltaTime;
            currentStats.stamina = Mathf.Clamp(currentStats.stamina, 0, maxStamina);
        }
    }

    public bool HasStamina(float amount) => currentStats.stamina >= amount;

    public void UseStamina(float amount)
    {
        currentStats.stamina -= amount;
        currentStats.stamina = Mathf.Clamp(currentStats.stamina, 0, maxStamina);
        regenerateStamina = false;
    }

    public void StartStaminaRegen() => regenerateStamina = true;
    public void StopStaminaRegen() => regenerateStamina = false;

    // --- LOGIC: HEALTH ---
    public void TakeDamage(float amount)
    {
        currentStats.health -= amount;
        currentStats.health = Mathf.Clamp(currentStats.health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentStats.health / maxHealth);

        if (currentStats.health <= 0) Die();
    }

    public void Heal(float amount)
    {
        currentStats.health += amount;
        currentStats.health = Mathf.Clamp(currentStats.health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentStats.health / maxHealth);
        Debug.Log($"[PlayerStats] Healed Player by {amount}");
    }

    private void Die() => Debug.Log("Player Died");

    // --- LOGIC: FEAR ---
    public void AddFear(float amount)
    {
        currentStats.fear += amount;
        currentStats.fear = Mathf.Clamp(currentStats.fear, 0, maxFear);
        OnFearChanged?.Invoke(currentStats.fear / maxFear);
    }

    public void ReduceFear(float amount)
    {
        currentStats.fear -= amount;
        currentStats.fear = Mathf.Clamp(currentStats.fear, 0, maxFear);
        OnFearChanged?.Invoke(currentStats.fear / maxFear);
    }

    // --- LOGIC: PURITY ---
    public void ReducePurity(float amount)
    {
        currentStats.purity -= amount;
        currentStats.purity = Mathf.Clamp(currentStats.purity, 0, maxPurity);
        OnPurityChanged?.Invoke(currentStats.purity / maxPurity);
    }

    public void RestorePurity(float amount)
    {
        currentStats.purity += amount;
        currentStats.purity = Mathf.Clamp(currentStats.purity, 0, maxPurity);
        OnPurityChanged?.Invoke(currentStats.purity / maxPurity);
    }

    // --- HELPERS FOR SAVE SYSTEM ---
    public PlayerData GetStatsData() => currentStats;

    public void LoadStatsData(PlayerData loadedData)
    {
        currentStats = loadedData;
        UpdateAllEvents();
    }

    private void UpdateAllEvents()
    {
        OnHealthChanged?.Invoke(currentStats.health / maxHealth);
        OnStaminaChanged?.Invoke(currentStats.stamina / maxStamina);
        OnFearChanged?.Invoke(currentStats.fear / maxFear);
        OnPurityChanged?.Invoke(currentStats.purity / maxPurity);

        // Update Inventory Events on Load
        if (currentStats.inventory != null)
        {
            OnInventoryChanged?.Invoke(0, currentStats.inventory[0]);
            OnInventoryChanged?.Invoke(1, currentStats.inventory[1]);
        }
    }

    // --- LOGIC: SAVE & LOAD (JSON) ---
    //save data
    [ContextMenu("Jetzt Speichern")]
    public void SaveGame()
    {
        //convert struct to json
        string json = JsonUtility.ToJson(currentStats, true);
        
        //define path where data will be stroed
        string path = Path.Combine(Application.persistentDataPath, "playerSaveData.json");

        //write json into file
        File.WriteAllText(path, json);
        Debug.Log($"created save file at {path}");
    }

    //load back data
    [ContextMenu("Jetzt Laden")]
    public void LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, "playerSaveData.json");

        //get text out file
        string json = File.ReadAllText(path);

        //convert json back into struct (idk if this technically creates a struct but whatever, it works)
        PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);

        //apply struct into data
        LoadStatsData(loadedData);
        Debug.Log("game loaded successfully.");
        }
    
    //delete entire save file
    public void DeleteSaveFile()
    {
        //DO NOT CALL THIS FUNCTION IF NO SAVE FILE WAS CREATED, THE GAME WILL CRASH FRFR (and the game progress will be deleted)
        string path = Path.Combine(Application.persistentDataPath, "playerSaveData.json");
        File.Delete(path);
    }
}