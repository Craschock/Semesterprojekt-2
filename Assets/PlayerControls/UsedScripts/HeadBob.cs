using UnityEngine;

/// <summary>
/// Simple head-bob for camera while walking and sprinting.
/// Small, focused comments — adjustable via inspector.
/// </summary>
public class HeadBob : MonoBehaviour
{
    [Header("Walk Bob Settings")]
    public float bobSpeed = 14f;       // base frequency of bob when walking
    public float bobAmount = 0.05f;    // base amplitude of bob when walking

    [Header("Sprint Bob Settings")]
    public float sprintBobSpeedMultiplier = 1.6f;   // multiplier to bobSpeed when sprinting
    public float sprintBobAmountMultiplier = 1.8f;  // multiplier to bobAmount when sprinting

    [Header("References")]
    public PlayerMovement playerMovement; // reference to movement (reads moving/running state)

    private float defaultYPos; // starting local Y
    private float timer = 0f;  // progression of bob

    private void Start()
    {
        // cache default local Y position on start
        defaultYPos = transform.localPosition.y;
    }

    private void Update()
    {
        Vector3 localPos = transform.localPosition;

        // if player is moving, bob; otherwise lerp back to default
        if (playerMovement != null && playerMovement.IsMoving())
        {
            // choose bob speed + amount based on sprint state
            float speed = bobSpeed;
            float amount = bobAmount;

            if (playerMovement.IsRunning())
            {
                speed *= sprintBobSpeedMultiplier;
                amount *= sprintBobAmountMultiplier;
            }

            // progress timer and compute sinusoidal offset
            timer += Time.deltaTime * speed;
            localPos.y = defaultYPos + Mathf.Sin(timer) * amount;
        }
        else
        {
            // not moving: reset timer and smoothly return to default position
            timer = 0f;
            localPos.y = Mathf.Lerp(localPos.y, defaultYPos, Time.deltaTime * 5f);
        }

        transform.localPosition = localPos;
    }
}
