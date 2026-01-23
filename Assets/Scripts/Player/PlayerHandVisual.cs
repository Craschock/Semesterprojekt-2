using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHandVisuals : MonoBehaviour
{
    [Header("Hand Transforms")]
    public Transform leftHandSlot;
    public Transform rightHandSlot;

    [Header("Highlight Settings")]
    public Vector3 selectionOffset = new Vector3(-0.1f, 0.1f, 0.2f);
    public bool mirrorXForLeftHand = true;  //If Left Hand
    public float movementSmoothing = 10f;

    [Header("Swap Animation")]
    public float dropAmount = 1f;
    public float swapSpeed = 8f;
    public float swapDelay = 0.15f;

    [Header("Phone Watch Settings")]
    [Tooltip("Offset of the default position, when the player is right clicking.")]
    public Vector3 phoneWatchOffset = new Vector3(-0.2f, 0.1f, 0.1f);
    private bool isWatchingPhone = false;

    [Header("Item Mappings")]
    [Tooltip("Map each ConsumableType to a specific Prefab here please UwU.")]
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
    private float currentDropOffset = 0f;
    private float targetDropOffset = 0f;

    private void Awake()
    {
        // Capture the default positions you set in the editor
        if (leftHandSlot != null) leftStartPos = leftHandSlot.localPosition;
        if (rightHandSlot != null) rightStartPos = rightHandSlot.localPosition;

        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.OnInventoryChanged.AddListener(OnInventoryChangedReceived);
            stats.OnSlotSelected.AddListener(OnSlotSelectionChanged);
        }
        else
        {
            Debug.LogError("[PlayerHandVisuals] Could not find PlayerStats script!");
        }
    }

    private void Update()
    {
        // Lerp 'currentDropOffset' always to 'targetDropOffset'
        currentDropOffset = Mathf.Lerp(currentDropOffset, targetDropOffset, Time.deltaTime * swapSpeed);

        // Smoothly animate hands to their target positions
        MoveHand(leftHandSlot, GetTargetPosition(0), Time.deltaTime * movementSmoothing);
        MoveHand(rightHandSlot, GetTargetPosition(1), Time.deltaTime * movementSmoothing);
    }

    public void SetPhoneWatchActive(bool active)
    {
        isWatchingPhone = active;
    }

    // Calculates where the hand SHOULD be based on selection
    private Vector3 GetTargetPosition(int handIndex)
    {
        Vector3 basePos = (handIndex == 0) ? leftStartPos : rightStartPos;
        Vector3 finalPos = basePos;

        // 0 = Left, 1 = Right
        if (handIndex == 0)
        {
            // If Left is selected, add offset. Otherwise, go to start.
            if (currentSelectionIndex == 0)
            {
                Vector3 finalOffset = selectionOffset;
                if (mirrorXForLeftHand) finalOffset.x = -finalOffset.x; // Flip X for symmetry
                finalPos += finalOffset;
            }
        }
        else
        {
            // If Right is selected AND is watching phone, add phoneoffset .
            if (isWatchingPhone)
            {
                finalPos += phoneWatchOffset;
            }
            // If Right is selected, addd normal offset.
            else if (currentSelectionIndex == 1)
            {
                finalPos += selectionOffset;
            }
        }
        finalPos += Vector3.down * (currentDropOffset * dropAmount);

        return finalPos;
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

    public void OnInventoryChangedReceived(int slotIndex, ConsumableType newItemType)
    {
        StartCoroutine(SwapRoutine(slotIndex, newItemType));
    }

    private IEnumerator SwapRoutine(int slotIndex, ConsumableType newItemType)
    {
        targetDropOffset = 1f;
        yield return new WaitForSeconds(swapDelay);
        PerformMeshSwap(slotIndex, newItemType);
        yield return new WaitForSeconds(0.05f);
        targetDropOffset = 0f;
    }

    private void PerformMeshSwap(int slotIndex, ConsumableType newItemType)
    {
        Transform targetHand = (slotIndex == 0) ? leftHandSlot : rightHandSlot;
        if (targetHand == null) return;

        // delete old models
        foreach (Transform child in targetHand) Destroy(child.gameObject);

        if (newItemType == ConsumableType.None) return;

        GameObject prefabToSpawn = GetPrefabByType(newItemType);
        if (prefabToSpawn != null)
        {
            GameObject newObj = Instantiate(prefabToSpawn, targetHand);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;

            int fpLayer = LayerMask.NameToLayer("FirstPerson");
            if (fpLayer == -1) fpLayer = LayerMask.NameToLayer("Ignore Raycast");

            SetLayerRecursive(newObj, fpLayer);
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