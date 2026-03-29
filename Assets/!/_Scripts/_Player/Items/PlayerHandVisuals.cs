using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the visual representation of items in the player's hands.
/// Smoothly moves hands and swaps meshes based on equipment events.
/// </summary>
public class PlayerHandVisuals : MonoBehaviour
{
    [Header("Hand Transforms")]
    public Transform leftHandSlot;
    public Transform rightHandSlot;

    [Header("Highlight Settings")]
    public Vector3 selectionOffset = new Vector3(-0.1f, 0.1f, 0.2f);
    public bool mirrorXForLeftHand = true; 
    public float movementSmoothing = 10f;

    [Header("Swap Animation")]
    public float dropAmount = 1f;
    public float swapSpeed = 8f;
    public float swapDelay = 0.15f;

    [Header("Phone Watch Settings")]
    [Tooltip("Offset of the default position, when the player is right clicking.")]
    public Vector3 phoneWatchOffset = new Vector3(-0.2f, 0.1f, 0.1f);
    
    // --- State ---
    private bool isWatchingPhone = false;
    private Vector3 leftStartPos;
    private Vector3 rightStartPos;
    private int currentSelectionIndex = -1;
    private float currentDropOffset = 0f;
    private float targetDropOffset = 0f;

    /// <summary>
    /// Initializes start positions and subscribes to equipment events.
    /// </summary>
    private void Awake()
    {
        if (leftHandSlot != null) leftStartPos = leftHandSlot.localPosition;
        if (rightHandSlot != null) rightStartPos = rightHandSlot.localPosition;

        PlayerEquipmentManager equipment = GetComponent<PlayerEquipmentManager>();

        if (equipment != null)
        {
            // Now listening to the new events with ConsumableItemData!
            equipment.OnInventoryChanged.AddListener(OnInventoryChangedReceived);
            equipment.OnSlotSelected.AddListener(OnSlotSelectionChanged);
        }
        else
        {
            Debug.LogError("[PlayerHandVisuals] Could not find PlayerEquipmentManager script!");
        }
    }

    /// <summary>
    /// Updates hand positions smoothly every frame.
    /// </summary>
    private void Update()
    {
        currentDropOffset = Mathf.Lerp(currentDropOffset, targetDropOffset, Time.deltaTime * swapSpeed);

        MoveHand(leftHandSlot, GetTargetPosition(0), Time.deltaTime * movementSmoothing);
        MoveHand(rightHandSlot, GetTargetPosition(1), Time.deltaTime * movementSmoothing);
    }

    /// <summary>
    /// Toggles the viewing state of the phone, applying a special offset.
    /// </summary>
    public void SetPhoneWatchActive(bool active)
    {
        isWatchingPhone = active;
    }

    /// <summary>
    /// Calculates the target local position for a given hand.
    /// </summary>
    private Vector3 GetTargetPosition(int handIndex)
    {
        Vector3 basePos = (handIndex == 0) ? leftStartPos : rightStartPos;
        Vector3 finalPos = basePos;

        if (handIndex == 0)
        {
            if (currentSelectionIndex == 0)
            {
                Vector3 finalOffset = selectionOffset;
                if (mirrorXForLeftHand) finalOffset.x = -finalOffset.x; 
                finalPos += finalOffset;
            }
        }
        else
        {
            if (isWatchingPhone)
            {
                finalPos += phoneWatchOffset;
            }
            else if (currentSelectionIndex == 1)
            {
                finalPos += selectionOffset;
            }
        }
        
        finalPos += Vector3.down * (currentDropOffset * dropAmount);
        return finalPos;
    }

    /// <summary>
    /// Smoothly interpolates the hand transform towards the target position.
    /// </summary>
    private void MoveHand(Transform hand, Vector3 targetPos, float step)
    {
        if (hand == null) return;
        hand.localPosition = Vector3.Lerp(hand.localPosition, targetPos, step);
    }

    /// <summary>
    /// Updates the current selection index when the slot changes.
    /// </summary>
    private void OnSlotSelectionChanged(int newIndex)
    {
        currentSelectionIndex = newIndex;
    }

    /// <summary>
    /// Triggers the swap animation coroutine when inventory data changes.
    /// </summary>
    public void OnInventoryChangedReceived(int slotIndex, ConsumableItemData newItemData)
    {
        StartCoroutine(SwapRoutine(slotIndex, newItemData));
    }

    /// <summary>
    /// Handles the timing of the visual swap (dropping hand, swapping, raising hand).
    /// </summary>
    private IEnumerator SwapRoutine(int slotIndex, ConsumableItemData newItemData)
    {
        targetDropOffset = 1f;
        yield return new WaitForSeconds(swapDelay);
        
        PerformMeshSwap(slotIndex, newItemData);
        
        yield return new WaitForSeconds(0.05f);
        targetDropOffset = 0f;
    }

    /// <summary>
    /// Destroys the old mesh and instantiates the new prefab directly from the ScriptableObject.
    /// </summary>
    private void PerformMeshSwap(int slotIndex, ConsumableItemData newItemData)
    {
        Transform targetHand = (slotIndex == 0) ? leftHandSlot : rightHandSlot;
        if (targetHand == null) return;

        foreach (Transform child in targetHand) 
        {
            Destroy(child.gameObject);
        }

        // If data is null or has no model, we just leave the hand empty
        if (newItemData == null || newItemData.modelPrefab == null) return;

        GameObject newObj = Instantiate(newItemData.modelPrefab, targetHand);
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.identity;

        int fpLayer = LayerMask.NameToLayer("FirstPerson");
        if (fpLayer == -1) fpLayer = LayerMask.NameToLayer("Ignore Raycast");

        SetLayerRecursive(newObj, fpLayer);
    }

    /// <summary>
    /// Recursively sets the layer of a GameObject and all its children.
    /// </summary>
    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform) 
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}