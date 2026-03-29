using UnityEngine;

/// <summary>
/// Handles camera head-bob animation while walking, sprinting, or crouching.
/// Automatically adjusts base height and applies a Figure-8 motion.
/// </summary>
public class HeadBob : MonoBehaviour
{
    [Header("Base Settings")]
    public float standCameraY = 1.6f;
    public float crouchCameraY = 0.9f;
    public float heightChangeSpeed = 8f;

    [Header("Walk Bob Settings")]
    public float bobSpeed = 14f;
    public float bobAmountX = 0.05f;
    public float bobAmountY = 0.05f;

    [Header("Sprint Bob Settings")]
    public float sprintBobSpeedMultiplier = 1.6f;
    public float sprintBobAmountMultiplier = 1.8f;

    [Header("Crouch Bob Settings")]
    public float crouchBobSpeedMultiplier = 0.6f;
    public float crouchBobAmountMultiplier = 0.5f;

    [Header("References")]
    public PlayerMovement playerMovement;

    private float defaultXPos = 0f;
    private float timer = 0f;

    /// <summary>
    /// Stores the initial X position of the camera to center the bobbing.
    /// </summary>
    private void Start()
    {
        defaultXPos = transform.localPosition.x;
    }

    /// <summary>
    /// Calculates and applies the camera position offsets every frame.
    /// </summary>
    private void Update()
    {
        if (playerMovement == null) return;

        Vector3 localPos = transform.localPosition;

        // Base Height Transition (Crouch vs Stand)
        float targetBaseY = playerMovement.IsCrouching() ? crouchCameraY : standCameraY;
        float currentBaseY = Mathf.Lerp(localPos.y, targetBaseY, Time.deltaTime * heightChangeSpeed);

        if (playerMovement.IsMoving())
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

            timer += Time.deltaTime * speed;

            // Apply Figure-8 motion
            localPos.y = currentBaseY + Mathf.Sin(timer) * amountY;
            localPos.x = defaultXPos + Mathf.Cos(timer / 2f) * amountX;
        }
        else
        {
            // Smoothly return to center when stopped
            timer = 0f;
            localPos.y = Mathf.Lerp(localPos.y, currentBaseY, Time.deltaTime * 5f);
            localPos.x = Mathf.Lerp(localPos.x, defaultXPos, Time.deltaTime * 5f);
        }

        transform.localPosition = localPos;
    }
}