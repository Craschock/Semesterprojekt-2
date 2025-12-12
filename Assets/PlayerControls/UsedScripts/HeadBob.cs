using UnityEngine;

/// <summary>
/// Handles camera head-bob animation while walking or sprinting.
/// - Disabled while reading hints
/// - Disabled while in item focus/inspect mode
/// </summary>
public class HeadBob : MonoBehaviour
{
    [Header("Walk Bob Settings")]
    public float bobSpeed = 14f;        // base bob frequency while walking
    public float bobAmountX = 0.05f;    // horizontal sway amount (Figure-8 width)
    public float bobAmountY = 0.05f;    // vertical bob amount (Figure-8 height)

    [Header("Sprint Bob Settings")]
    public float sprintBobSpeedMultiplier = 1.6f;   // frequency multiplier when sprinting
    public float sprintBobAmountMultiplier = 1.8f;  // amplitude multiplier when sprinting

    [Header("References")]
    public PlayerMovement playerMovement;       // used to check movement & sprint state
    public PlayerInteraction playerInteraction; // needed to disable bob while reading / focusing

    // runtime
    private float defaultYPos;   // starting Y position (local)
    private float defaultXPos;   // starting X position (local)
    private float timer = 0f;    // bob animation timer

    private void Start()
    {
        // store initial camera coordinates
        defaultYPos = transform.localPosition.y;
        defaultXPos = transform.localPosition.x;
    }

    private void Update()
    {
        // ------------------------------------------------------
        // BLOCK BOBBING WHEN:
        // - a hint UI is open (reading)
        // - the player is in focus/inspect mode
        // ------------------------------------------------------
        if ((playerInteraction != null && playerInteraction.hintUI.IsHintOpen) ||
            (playerInteraction != null && playerInteraction.IsInFocusMode()))
        {
            // smoothly return to resting position while disabled
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, defaultYPos, Time.deltaTime * 5f);
            pos.x = Mathf.Lerp(pos.x, defaultXPos, Time.deltaTime * 5f);
            transform.localPosition = pos;

            timer = 0f;
            return;
        }

        Vector3 localPos = transform.localPosition;

        // ------------------------------------------------------
        // WALKING / SPRINTING BOB (FIGURE-8)
        // ------------------------------------------------------
        if (playerMovement != null && playerMovement.IsMoving())
        {
            float speed = bobSpeed;
            float amountX = bobAmountX;
            float amountY = bobAmountY;

            // apply sprint multipliers
            if (playerMovement.IsRunning())
            {
                speed *= sprintBobSpeedMultiplier;
                amountX *= sprintBobAmountMultiplier;
                amountY *= sprintBobAmountMultiplier;
            }

            // advance animation
            timer += Time.deltaTime * speed;

            // apply Figure-8 motion
            // Y moves at normal speed (Up/Down)
            // X moves at half speed (Left... Right...)
            localPos.y = defaultYPos + Mathf.Sin(timer) * amountY;
            localPos.x = defaultXPos + Mathf.Cos(timer / 2f) * amountX;
        }
        else
        {
            // --------------------------------------------------
            // NOT MOVING = smoothly return to default position
            // --------------------------------------------------
            timer = 0f;
            localPos.y = Mathf.Lerp(localPos.y, defaultYPos, Time.deltaTime * 5f);
            localPos.x = Mathf.Lerp(localPos.x, defaultXPos, Time.deltaTime * 5f);
        }

        transform.localPosition = localPos;
    }
}