using UnityEngine;
using System.Collections.Generic;

public class PlayerHandVisuals : MonoBehaviour
{
    [Header("Hand Transforms")]
    [Tooltip("The empty GameObject child of the MainCamera positioned on the left.")]
    public Transform leftHandSlot;
    [Tooltip("The empty GameObject child of the MainCamera positioned on the right.")]
    public Transform rightHandSlot;

    [Header("Item Mappings")]
    [Tooltip("Map each ConsumableType to a specific Prefab here.")]
    public List<ItemVisualMapping> itemMappings;

    // A simple struct to link the Enum to a Prefab in the Inspector
    [System.Serializable]
    public struct ItemVisualMapping
    {
        public ConsumableType type;
        public GameObject modelPrefab;
    }

    private void Start()
    {
        // 1. Find PlayerStats
        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats != null)
        {
            // 2. Listen to the Inventory Changed event
            // Whenever stats runs "OnInventoryChanged.Invoke()", our UpdateHandVisuals method runs automatically.
            stats.OnInventoryChanged.AddListener(UpdateHandVisuals);
        }
        else
        {
            Debug.LogError("[PlayerHandVisuals] Could not find PlayerStats script!");
        }
    }

    // This method is called automatically by the Event system
    public void UpdateHandVisuals(int slotIndex, ConsumableType newItemType)
    {
        // 1. Determine which hand we are updating
        Transform targetHand = (slotIndex == 0) ? leftHandSlot : rightHandSlot;

        if (targetHand == null) return;

        // 2. Destroy the current object in that hand (if any)
        foreach (Transform child in targetHand)
        {
            Destroy(child.gameObject);
        }

        // 3. If the new item is "None", we are done (hand is now empty)
        if (newItemType == ConsumableType.None) return;

        // 4. Find the matching prefab for the new item type
        GameObject prefabToSpawn = GetPrefabByType(newItemType);

        if (prefabToSpawn != null)
        {
            // 5. Instantiate the mesh and parent it to the hand slot
            GameObject newObj = Instantiate(prefabToSpawn, targetHand);

            // 6. Reset position/rotation to align perfectly with the slot
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;

            // Optional: Ensure layer is set to something that doesn't block the camera raycast if needed
        }
    }

    private GameObject GetPrefabByType(ConsumableType type)
    {
        foreach (var mapping in itemMappings)
        {
            if (mapping.type == type)
            {
                return mapping.modelPrefab;
            }
        }
        Debug.LogWarning($"[PlayerHandVisuals] No prefab assigned for item type: {type}");
        return null;
    }
}