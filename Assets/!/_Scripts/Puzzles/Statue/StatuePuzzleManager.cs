using UnityEngine;
using System.Collections.Generic;
using FMODUnity;

/// <summary>
/// Manages the order and logic for the Statue Puzzle.
/// - Checks if player interacts in the correct sequence defined in the list.
/// - Handles Success/Fail states.
/// </summary>
public class StatuePuzzleManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Drag the Statues here IN THE CORRECT ORDER (1st to last).")]
    public List<StatueInteractable> correctSequence;

    [Header("FMOD Audio")]
    public EventReference puzzleCompleteSound;
    public EventReference puzzleFailSound;

    // State
    private int currentIndex = 0;
    private bool isPuzzleComplete = false;

    public void OnStatueInteracted(StatueInteractable statue)
    {
        if (isPuzzleComplete)
        {
            Debug.Log("[StatuePuzzleManager] Puzzle already completed. Ignoring interaction.");
            return;
        }

        // Check if this statue is already active (player clicked the same one again)
        if (statue.IsActivated())
        {
            Debug.Log("[StatuePuzzleManager] Statue already active. Doing nothing.");
            return;
        }

        // Check if this statue is the next one in the sequence
        if (IsCorrectStatue(statue))
        {
            HandleCorrectInteraction(statue);
        }
        else
        {
            HandleIncorrectInteraction();
        }
    }

    private bool IsCorrectStatue(StatueInteractable statue)
    {
        // Safety check
        if (currentIndex >= correctSequence.Count) return false;

        return statue == correctSequence[currentIndex];
    }

    private void HandleCorrectInteraction(StatueInteractable statue)
    {
        Debug.Log($"[StatuePuzzleManager] CORRECT! Index {currentIndex} solved.");

        // Activate visual on statue
        statue.SetActivated(true);

        // Advance index
        currentIndex++;

        // Check Win Condition
        if (currentIndex >= correctSequence.Count)
        {
            CompletePuzzle();
        }
    }

    private void HandleIncorrectInteraction()
    {
        Debug.Log("[StatuePuzzleManager] WRONG STATUE! Resetting puzzle.");

        if (!puzzleFailSound.IsNull)
        {
            RuntimeManager.PlayOneShot(puzzleFailSound, transform.position);
        }

        // Reset all statues visuals
        foreach (var s in correctSequence)
        {
            s.SetActivated(false);
        }

        // Reset index
        currentIndex = 0;

        // Maybe some Fear to player here using PlayerStats? idk
    }

    private void CompletePuzzle()
    {
        isPuzzleComplete = true;
        Debug.Log("[StatuePuzzleManager] PUZZLE SOLVED!");

        if (!puzzleCompleteSound.IsNull)
        {
            RuntimeManager.PlayOneShot(puzzleCompleteSound, transform.position);
        }

        //What will happen? o.O
    }
}