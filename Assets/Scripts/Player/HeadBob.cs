using UnityEngine;

/// <summary>
/// Handles camera head-bob animation while walking or sprinting.
/// - Disabled while reading hints or in focus mode.
/// - Handles Figure-8 movement.
/// - NOW HANDLES CAMERA HEIGHT FOR CROUCHING.
/// </summary>
public class HeadBob : MonoBehaviour
{
    [Header("Base Settings")]
    public float standCameraY = 1.6f;   // The normal Y height of camera
    public float crouchCameraY = 0.9f;  // The Y height when crouching
    public float heightChangeSpeed = 8f;// How fast camera moves between stand/crouch

    [Header("Walk Bob Settings")]
    public float bobSpeed = 14f;        // base bob frequency while walking
    public float bobAmountX = 0.05f;    // horizontal sway amount (Figure-8 width)
    public float bobAmountY = 0.05f;    // vertical bob amount (Figure-8 height)

    [Header("Sprint Bob Settings")]
    public float sprintBobSpeedMultiplier = 1.6f;   // frequency multiplier when sprinting
    public float sprintBobAmountMultiplier = 1.8f;  // amplitude multiplier when sprinting

    [Header("Crouch Bob Settings")]
    public float crouchBobSpeedMultiplier = 0.6f;   // slower bob when crouching
    public float crouchBobAmountMultiplier = 0.5f;  // smaller bob when crouching

    [Header("References")]
    public PlayerMovement playerMovement;       // used to check movement & sprint state
    public PlayerInteraction playerInteraction; // needed to disable bob while reading / focusing

    // runtime
    private float defaultXPos;   // starting X position (local)
    private float currentBaseY;  // The current resting Y height (animates between stand/crouch)
    private float timer = 0f;    // bob animation timer

    private void Start()
    {
        // store initial camera X coordinates
        defaultXPos = transform.localPosition.x;
        currentBaseY = standCameraY;
    }

    private void Update()
    {
        // 1. Determine Target Base Height (Stand vs Crouch)
        float targetY = (playerMovement != null && playerMovement.IsCrouching()) ? crouchCameraY : standCameraY;

        // Smoothly move the "Base" Y position to target
        currentBaseY = Mathf.Lerp(currentBaseY, targetY, Time.deltaTime * heightChangeSpeed);

        // ------------------------------------------------------
        // BLOCK BOBBING WHEN:
        // - a hint UI is open (reading)
        // - the player is in focus/inspect mode
        // ------------------------------------------------------
        if ((playerInteraction != null && playerInteraction.hintUI.IsHintOpen) ||
            (playerInteraction != null && playerInteraction.IsInFocusMode()))
        {
            // smoothly return to resting position (currentBaseY) while disabled
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, currentBaseY, Time.deltaTime * 5f);
            pos.x = Mathf.Lerp(pos.x, defaultXPos, Time.deltaTime * 5f);
            transform.localPosition = pos;

            timer = 0f;
            return;
        }

        Vector3 localPos = transform.localPosition;

        // ------------------------------------------------------
        // WALKING / SPRINTING / CROUCHING BOB (FIGURE-8)
        // ------------------------------------------------------
        if (playerMovement != null && playerMovement.IsMoving())
        {
            float speed = bobSpeed;
            float amountX = bobAmountX;
            float amountY = bobAmountY;

            // Apply Multipliers
            if (playerMovement.IsCrouching())
            {
                speed *= crouchBobSpeedMultiplier;
                amountX *= crouchBobAmountMultiplier;
                amountY *= crouchBobAmountMultiplier;
            }
            else if (playerMovement.IsRunning())
            {
                speed *= sprintBobSpeedMultiplier;
                amountX *= sprintBobAmountMultiplier;
                amountY *= sprintBobAmountMultiplier;
            }

            // advance animation
            timer += Time.deltaTime * speed;

            // apply Figure-8 motion relative to CURRENT BASE Y
            localPos.y = currentBaseY + Mathf.Sin(timer) * amountY;
            localPos.x = defaultXPos + Mathf.Cos(timer / 2f) * amountX;
        }
        else
        {
            // --------------------------------------------------
            // NOT MOVING = smoothly return to base height
            // --------------------------------------------------
            timer = 0f;
            localPos.y = Mathf.Lerp(localPos.y, currentBaseY, Time.deltaTime * 5f);
            localPos.x = Mathf.Lerp(localPos.x, defaultXPos, Time.deltaTime * 5f);
        }

        transform.localPosition = localPos;
    }
}