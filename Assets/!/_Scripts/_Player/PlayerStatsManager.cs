using UnityEngine;
using UnityEngine.Events;
using FMODUnity;

/// <summary>
/// Manages the core physical and mental attributes of the player (Health, Stamina, Fear, Purity).
/// Acts as the central hub for stat modifications.
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    [Header("Configuration")]
    public float maxHealth = 100f;
    public float maxStamina = 100f;
    public float maxFear = 100f;
    public float maxPurity = 100f;

    [Header("Stamina Regeneration")]
    public bool regenerateStamina = true;
    public float staminaRegenRate = 15f;
    public float staminaDrainRate = 20f;

    [Header("Audio")]
    public EventReference staminaDepletedSound;
    public EventReference fearUpSound;

    // --- Current Values ---
    public float CurrentHealth { get; private set; }
    public float CurrentStamina { get; private set; }
    public float CurrentFear { get; private set; }
    public float CurrentPurity { get; private set; }

    // --- Events ---
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent<float> OnFearChanged;
    public UnityEvent<float> OnPurityChanged;

    private bool hasPlayedStaminaEmptySound = false;

    /// <summary>
    /// Initializes all stats to their maximum/default values on startup.
    /// </summary>
    private void Start()
    {
        CurrentHealth = maxHealth;
        CurrentStamina = maxStamina;
        CurrentFear = 0f;
        CurrentPurity = 100f;
        
        UpdateAllEvents();
    }

    /// <summary>
    /// Handles continuous logic like stamina regeneration.
    /// </summary>
    private void Update()
    {
        HandleStaminaRegen();
    }

    /// <summary>
    /// Modifies the player's health by the given amount and clamps it.
    /// </summary>
    /// <param name="amount">Positive to heal, negative to damage.</param>
    public void ModifyHealth(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth / maxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Modifies the player's fear level and checks for threshold events.
    /// </summary>
    /// <param name="amount">Positive to increase fear, negative to reduce.</param>
    public void ModifyFear(float amount)
    {
        float oldFear = CurrentFear;
        CurrentFear = Mathf.Clamp(CurrentFear + amount, 0, maxFear);
        OnFearChanged?.Invoke(CurrentFear / maxFear);

        // Check for 15-step threshold to play sound
        if (amount > 0)
        {
            int stepSize = 15;
            int oldStep = (int)(oldFear / stepSize);
            int newStep = (int)(CurrentFear / stepSize);

            if (newStep > oldStep && !fearUpSound.IsNull)
            {
                RuntimeManager.PlayOneShot(fearUpSound);
            }
        }
    }

    /// <summary>
    /// Modifies the player's purity level.
    /// </summary>
    /// <param name="amount">Positive to restore, negative to reduce.</param>
    public void ModifyPurity(float amount)
    {
        CurrentPurity = Mathf.Clamp(CurrentPurity + amount, 0, maxPurity);
        OnPurityChanged?.Invoke(CurrentPurity / maxPurity);
    }

    /// <summary>
    /// Checks if the player has enough stamina for an action.
    /// </summary>
    /// <param name="amount">The required stamina.</param>
    /// <returns>True if stamina is sufficient, false otherwise.</returns>
    public bool HasStamina(float amount) => CurrentStamina >= amount;

    /// <summary>
    /// Drains stamina by the specified amount and handles depletion audio.
    /// </summary>
    /// <param name="amount">The amount of stamina to drain.</param>
    public void UseStamina(float amount)
    {
        CurrentStamina -= amount;
        if (CurrentStamina <= 0)
        {
            CurrentStamina = 0;
            if (!hasPlayedStaminaEmptySound && !staminaDepletedSound.IsNull)
            {
                RuntimeManager.PlayOneShot(staminaDepletedSound);
                hasPlayedStaminaEmptySound = true;
            }
        }
    }

    /// <summary>
    /// Regenerates stamina over time if allowed.
    /// </summary>
    private void HandleStaminaRegen()
    {
        if (regenerateStamina && CurrentStamina < maxStamina)
        {
            CurrentStamina = Mathf.Clamp(CurrentStamina + (staminaRegenRate * Time.deltaTime), 0, maxStamina);
        }

        if (CurrentStamina > 5f)
        {
            hasPlayedStaminaEmptySound = false;
        }
    }

    /// <summary>
    /// Triggers the death sequence for the player.
    /// </summary>
    private void Die()
    {
        Debug.Log("[PlayerStatsManager] Player Died.");
        // TODO: Implement actual death logic or signal a GameManager
    }

    /// <summary>
    /// Forces UI updates for all stats. Useful after loading a save file.
    /// </summary>
    public void UpdateAllEvents()
    {
        OnHealthChanged?.Invoke(CurrentHealth / maxHealth);
        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
        OnFearChanged?.Invoke(CurrentFear / maxFear);
        OnPurityChanged?.Invoke(CurrentPurity / maxPurity);
    }
}