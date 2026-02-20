using UnityEngine;
using FMODUnity;

public class BambooPuzzleManager : MonoBehaviour
{
    [Header("FMOD Audio")]
    public EventReference puzzleCompleteSound;

    [Header("Slots that belong to this puzzle")]
    public PlaceSlot[] slots;

    [Header("Debug")]
    public GameObject teleport;
    public bool puzzleCompleted = false;

    [Header("References")]
    public PlayerStats playerStats;

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

    [ContextMenu("Complete Puzzle")]
    private void OnPuzzleComplete()
    {
        Debug.Log("Puzzle Completed!");

        if (!puzzleCompleteSound.IsNull)
        {
            RuntimeManager.PlayOneShot(puzzleCompleteSound);
        }

        if (fog != null)
        {
            //if (playerStats != null) 
            //{
            //    playerStats.bambooPuzzle = true;
            //}
            fog.ClearFog();
        }

        if (teleport != null) 
        {
            teleport.SetActive(false);
        }
    }
}
