using UnityEngine;
using UnityEngine.Events;
using System.IO;
using FMODUnity;
using FMOD.Studio;

public enum EquipmentMode
{
    Inventory,
    Lighter,
    Phone
}

public class PlayerStats : MonoBehaviour
{
    [Header("FMOD Audio")]
    public EventReference staminaDepletedSound;
    public EventReference modeSwitchSound;
    public EventReference consumableSelectSound;
    private bool hasPlayedStaminaEmptySound = false;

    private EventInstance currentSelectionSoundInstance;

    [Header("Configuration")]
    public float maxHealth = 100f;
    public float maxStamina = 100f;
    public float maxFear = 100f;
    public float maxPurity = 100f;

    [Header("Regeneration")]
    public bool regenerateStamina = true;
    public float staminaRegenRate = 15f;
    public float staminaDrainRate = 20f;

    [Header("Equipment State")]
    public EquipmentMode currentMode = EquipmentMode.Inventory;

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

        //puzzle progress
        public bool bambooPuzzle;
        public bool lightPuzzle;
        public bool statuePuzzle;
        public bool cursedPuzzle;

        //location
        public Vector3 playerPosition;

        //rotation
        public Quaternion playerRotation;

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

        if (PlayerPrefs.GetInt("LoadGameOnStart", 0) == 1)
        {
            Debug.Log("[PlayerStats] Loading Save Game from Menu selection...");
            LoadGame();
            PlayerPrefs.SetInt("LoadGameOnStart", 0);
            PlayerPrefs.Save();
        }
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
        if (currentMode != EquipmentMode.Inventory)
        {
            SetMode(EquipmentMode.Inventory);
        }

        StopSelectionSound();

        if (selectedSlotIndex == index) // Deselect
        {
            selectedSlotIndex = -1; 
            Debug.Log($"[PlayerStats] Deselected Slot {index + 1}");
        }
        else
        {
            selectedSlotIndex = index; // Select new
            Debug.Log($"[PlayerStats] Selected Slot {index + 1}. Item: {currentStats.inventory[index]}");
            
            ConsumableType itemInSlot = currentStats.inventory[index];
            
            if (itemInSlot != ConsumableType.None && 
                itemInSlot != ConsumableType.GlowItem && 
                itemInSlot != ConsumableType.SmallFearReductionItem)
            {
                PlaySelectionSound(itemInSlot);
            }
        }
        OnSlotSelected?.Invoke(selectedSlotIndex);
    }

    public int GetSelectedSlotIndex() => selectedSlotIndex;

    public void ToggleLighterMode()
    {
        // No swap if hands are deactivated
        if (!areHandsActive) return;

        if (currentMode == EquipmentMode.Lighter)
        {
            SetMode(EquipmentMode.Inventory);
        }
        else
        {
            SetMode(EquipmentMode.Lighter);
        }
    }

    public void TogglePhoneMode()
    {
        // No swap if hands are deactivated
        if (!areHandsActive) return;

        if (currentMode == EquipmentMode.Phone)
        {
            SetMode(EquipmentMode.Inventory);
        }
        else
        {
            SetMode(EquipmentMode.Phone);
        }
    }

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

    public bool AddConsumable(ConsumableType item)
    {
        // Try to fill Slot 1 first if empty
        if (currentStats.inventory[0] == ConsumableType.None)
        {
            currentStats.inventory[0] = item;
            Debug.Log($"[PlayerStats] Added {item} to Slot 1");
            OnInventoryChanged?.Invoke(0, item);

            // Auto select slot 1
            selectedSlotIndex = 0;
            OnSlotSelected?.Invoke(selectedSlotIndex);

            return true;
        }
        // Try to fill Slot 2 if empty
        else if (currentStats.inventory[1] == ConsumableType.None)
        {
            currentStats.inventory[1] = item;
            Debug.Log($"[PlayerStats] Added {item} to Slot 2");
            OnInventoryChanged?.Invoke(1, item);

            // Auto select slot 2
            selectedSlotIndex = 1;
            OnSlotSelected?.Invoke(selectedSlotIndex);

            return true;
        }

        Debug.Log("[PlayerStats] Inventory Full!");
        return false;
    }

    public ConsumableType ConsumeSelectedSlot()
    {
        // If hands are deactivated, do nothing
        if (!areHandsActive) return ConsumableType.None;

        // If nothing selected, do nothing.
        if (selectedSlotIndex == -1) return ConsumableType.None;

        ConsumableType item = currentStats.inventory[selectedSlotIndex];

        if (item != ConsumableType.None)
        {
            StopSelectionSound();

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

    private void PlaySelectionSound(ConsumableType type)
    {
        if (consumableSelectSound.IsNull) return;
        currentSelectionSoundInstance = RuntimeManager.CreateInstance(consumableSelectSound);
        currentSelectionSoundInstance.setParameterByNameWithLabel("ItemType", type.ToString());
        currentSelectionSoundInstance.start();
    }

    private void StopSelectionSound()
    {
        if (currentSelectionSoundInstance.isValid())
        {
            currentSelectionSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentSelectionSoundInstance.release();
        }
    }

    public void UpdateVisualsForCurrentMode()
    {
        // Safety
        if (!areHandsActive)
        {
            OnInventoryChanged?.Invoke(0, ConsumableType.None);
            OnInventoryChanged?.Invoke(1, ConsumableType.None);
            OnSlotSelected?.Invoke(-1);
            return;
        }

        switch (currentMode)
        {
            case EquipmentMode.Inventory:
                // Show Normal Inventory
                if (currentStats.inventory != null)
                {
                    OnInventoryChanged?.Invoke(0, currentStats.inventory[0]);
                    OnInventoryChanged?.Invoke(1, currentStats.inventory[1]);
                }
                OnSlotSelected?.Invoke(selectedSlotIndex);
                break;

            case EquipmentMode.Lighter:
                // Links leer, Rechts Feuerzeug (GlowItem)
                OnInventoryChanged?.Invoke(0, ConsumableType.None);
                OnInventoryChanged?.Invoke(1, ConsumableType.GlowItem);
                OnSlotSelected?.Invoke(-1); // No SLot highlight
                break;

            case EquipmentMode.Phone:
                // Links leer, Rechts Handy (SmallFearREductionItem)
                OnInventoryChanged?.Invoke(0, ConsumableType.None);
                OnInventoryChanged?.Invoke(1, ConsumableType.SmallFearReductionItem);
                OnSlotSelected?.Invoke(-1); // No Slot highlight
                break;
        }
    }

    public void SetHandsActive(bool active)
    {
        areHandsActive = active;

        if (active)
        {
            // Restore visual based on current mode (see method above)
            UpdateVisualsForCurrentMode();
            Debug.Log("[PlayerStats] Hands Reactivated.");
        }
        else
        {
            // Hide everything
            OnInventoryChanged?.Invoke(0, ConsumableType.None);
            OnInventoryChanged?.Invoke(1, ConsumableType.None);
            OnSlotSelected?.Invoke(-1);
            Debug.Log("[PlayerStats] Hands Deactivated (Hidden).");
        }
    }

    // --- LOGIC: STAMINA ---
    private void HandleStaminaRegen()
    {
        if (regenerateStamina && currentStats.stamina < maxStamina)
        {
            currentStats.stamina += staminaRegenRate * Time.deltaTime;
            currentStats.stamina = Mathf.Clamp(currentStats.stamina, 0, maxStamina);
        }
        if (currentStats.stamina > 5f)
        {
            hasPlayedStaminaEmptySound = false;
        }
    }

    public bool HasStamina(float amount) => currentStats.stamina >= amount;

    public void UseStamina(float amount)
    {
        currentStats.stamina -= amount;
        if (currentStats.stamina <= 0)
        {
            currentStats.stamina = 0;
            if (!hasPlayedStaminaEmptySound)
            {
                if (!staminaDepletedSound.IsNull)
                {
                    RuntimeManager.PlayOneShot(staminaDepletedSound);
                }
                hasPlayedStaminaEmptySound = true;
            }
        }
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
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.position = loadedData.playerPosition;
        transform.rotation = loadedData.playerRotation;

        if (cc != null) cc.enabled = true;

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
        //capture position at time of saving (avoids constant saving, causing lag (maybe))
        currentStats.playerPosition = transform.position;
        //same for rotation
        currentStats.playerRotation = transform.rotation;

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