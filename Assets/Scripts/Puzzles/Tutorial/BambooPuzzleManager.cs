using UnityEngine;

public class BambooPuzzleManager : MonoBehaviour
{
    [Header("Slots that belong to this puzzle")]
    public PlaceSlot[] slots;

    [Header("Debug")]
    public bool puzzleCompleted = false;

    private FogController fog;

    private void Awake() 
    {
        fog = GetComponent<FogController>();
    }

    private void Update()
    {
        if (!puzzleCompleted && CheckPuzzleComplete())
        {
            puzzleCompleted = true;
            OnPuzzleComplete();
        }
    }

    private bool CheckPuzzleComplete()
    {
        foreach (PlaceSlot slot in slots)
        {
            if (!slot.HasCorrectItem())
                return false;
        }
        return true;
    }

    private void OnPuzzleComplete()
    {
        Debug.Log("Puzzle Completed!");

        if (fog != null)
        {
            fog.ClearFog();
        }
        // TODO: Trigger animation, unlock door, disable looping stairs, etc.
    }
}
