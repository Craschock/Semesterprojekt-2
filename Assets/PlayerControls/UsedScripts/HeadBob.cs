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
    public float bobAmount = 0.05f;     // base bob amplitude while walking

    [Header("Sprint Bob Settings")]
    public float sprintBobSpeedMultiplier = 1.6f;   // frequency multiplier when sprinting
    public float sprintBobAmountMultiplier = 1.8f;  // amplitude multiplier when sprinting

    [Header("References")]
    public PlayerMovement playerMovement;       // used to check movement & sprint state
    public PlayerInteraction playerInteraction; // needed to disable bob while reading / focusing

    // runtime
    private float defaultYPos;   // starting Y position (local)
    private float timer = 0f;    // bob animation timer

    private void Start()
    {
        // store initial camera height
        defaultYPos = transform.localPosition.y;
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
            // smoothly return to resting height while disabled
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, defaultYPos, Time.deltaTime * 5f);
            transform.localPosition = pos;

            timer = 0f;
            return;
        }

        Vector3 localPos = transform.localPosition;

        // ------------------------------------------------------
        // WALKING / SPRINTING BOB
        // ------------------------------------------------------
        if (playerMovement != null && playerMovement.IsMoving())
        {
            float speed = bobSpeed;
            float amount = bobAmount;

            // apply sprint multipliers
            if (playerMovement.IsRunning())
            {
                speed *= sprintBobSpeedMultiplier;
                amount *= sprintBobAmountMultiplier;
            }

            // advance animation
            timer += Time.deltaTime * speed;

            // apply sine-wave bob motion
            localPos.y = defaultYPos + Mathf.Sin(timer) * amount;
        }
        else
        {
            // --------------------------------------------------
            // NOT MOVING = smoothly return to default height
            // --------------------------------------------------
            timer = 0f;
            localPos.y = Mathf.Lerp(localPos.y, defaultYPos, Time.deltaTime * 5f);
        }

        transform.localPosition = localPos;
    }
}
