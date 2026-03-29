using UnityEngine;
using FMODUnity;

/// <summary>
/// Defines the core data and effects for a consumable item.
/// Created as a ScriptableObject to allow easy creation of new items in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "New Consumable", menuName = "Project/Items/Consumable")]
public class ConsumableItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Item";
    public GameObject modelPrefab;

    [Header("Stat Effects")]
    public float healthChange = 0f;
    public float fearChange = 0f;
    public float purityChange = 0f;

    [Header("Special Flags")]
    public bool isLighter = false;
    public bool isPhone = false;

    [Header("FMOD Audio")]
    public EventReference selectSound;
    public EventReference useSound;

    /// <summary>
    /// Applies the stat changes of this item to the provided PlayerStatsManager.
    /// </summary>
    /// <param name="stats">The PlayerStatsManager instance to apply effects to.</param>
    public void ApplyEffects(PlayerStatsManager stats)
    {
        if (healthChange != 0) stats.ModifyHealth(healthChange);
        if (fearChange != 0) stats.ModifyFear(fearChange);
        if (purityChange != 0) stats.ModifyPurity(purityChange);
    }
}