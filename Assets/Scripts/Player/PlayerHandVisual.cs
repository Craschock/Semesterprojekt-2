using UnityEngine;
using System.Collections.Generic;

public class PlayerHandVisuals : MonoBehaviour
{
    [Header("Hand Transforms")]
    [Tooltip("The empty GameObject child of the MainCamera positioned on the left.")]
    public Transform leftHandSlot;
    [Tooltip("The empty GameObject child of the MainCamera positioned on the right.")]
    public Transform rightHandSlot;

    [Header("Highlight Settings")]
    [Tooltip("How far the selected item moves relative to its start position.")]
    public Vector3 selectionOffset = new Vector3(-0.1f, 0.1f, 0.2f);
    [Tooltip("If true, the X offset is inverted for the left hand (symmetry).")]
    public bool mirrorXForLeftHand = true;  //If Left Hand
    public float movementSmoothing = 10f;

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

    // State
    private Vector3 leftStartPos;
    private Vector3 rightStartPos;
    private int currentSelectionIndex = -1;

    private void Awake()
    {
        // Capture the default positions you set in the editor
        if (leftHandSlot != null) leftStartPos = leftHandSlot.localPosition;
        if (rightHandSlot != null) rightStartPos = rightHandSlot.localPosition;

        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats != null)
        {
            // Listen for Inventory changes (Updates meshes)
            stats.OnInventoryChanged.AddListener(UpdateHandVisuals);

            // Listen for Slot Selection (Updates positions)
            stats.OnSlotSelected.AddListener(OnSlotSelectionChanged);
        }
        else
        {
            Debug.LogError("[PlayerHandVisuals] Could not find PlayerStats script!");
        }
    }

    private void Update()
    {
        // Smoothly animate hands to their target positions
        MoveHand(leftHandSlot, GetTargetPosition(0), Time.deltaTime * movementSmoothing);
        MoveHand(rightHandSlot, GetTargetPosition(1), Time.deltaTime * movementSmoothing);
    }

    // Calculates where the hand SHOULD be based on selection
    private Vector3 GetTargetPosition(int handIndex)
    {
        // 0 = Left, 1 = Right
        if (handIndex == 0)
        {
            // If Left is selected, add offset. Otherwise, go to start.
            if (currentSelectionIndex == 0)
            {
                Vector3 finalOffset = selectionOffset;
                if (mirrorXForLeftHand) finalOffset.x = -finalOffset.x; // Flip X for symmetry
                return leftStartPos + finalOffset;
            }
            return leftStartPos;
        }
        else
        {
            // If Right is selected, add offset.
            if (currentSelectionIndex == 1) return rightStartPos + selectionOffset;
            return rightStartPos;
        }
    }

    private void MoveHand(Transform hand, Vector3 targetPos, float step)
    {
        if (hand == null) return;
        hand.localPosition = Vector3.Lerp(hand.localPosition, targetPos, step);
    }

    // Event Listener
    private void OnSlotSelectionChanged(int newIndex)
    {
        currentSelectionIndex = newIndex;
    }

    // --- MESH SPAWNING LOGIC ---
    public void UpdateHandVisuals(int slotIndex, ConsumableType newItemType)
    {
        Transform targetHand = (slotIndex == 0) ? leftHandSlot : rightHandSlot;
        if (targetHand == null) return;

        // Delete old models
        foreach (Transform child in targetHand) Destroy(child.gameObject);

        if (newItemType == ConsumableType.None) return;

        GameObject prefabToSpawn = GetPrefabByType(newItemType);
        if (prefabToSpawn != null)
        {
            GameObject newObj = Instantiate(prefabToSpawn, targetHand);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;

            // Fix layers recursively so they don't block camera raycasts if needed
            // 2 is ignoreRaycast Layer
            SetLayerRecursive(newObj, 2);
        }
    }

    private GameObject GetPrefabByType(ConsumableType type)
    {
        foreach (var mapping in itemMappings)
        {
            if (mapping.type == type) return mapping.modelPrefab;
        }
        return null;
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
    }
}