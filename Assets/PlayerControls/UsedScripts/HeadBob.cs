using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("Walk Bob Settings")]
    public float bobSpeed = 14f;       //Frequency of the bob
    public float bobAmount = 0.05f;    //Amplitude of the bob

    [Header("Sprint Bob Settings")]                 //When sprinting
    public float sprintBobSpeedMultiplier = 1.6f;   //Frequency multiplier of bob
    public float sprintBobAmountMultiplier = 1.8f;  //Ampluitude multiplier of bob

    public PlayerMovement playerMovement;

    private float defaultYPos = 0;
    private float timer = 0;

    void Start()
    {
        defaultYPos = transform.localPosition.y;
    }

    void Update()
    {
        Vector3 localPos = transform.localPosition;

        if (playerMovement.IsMoving())
        {
            bool isSprinting = playerMovement.stamina > 0 &&
                               playerMovement.GetStaminaPercent() < 1f &&
                               playerMovement.IsMoving() &&
                               playerMovement.GetComponent<PlayerMovement>().GetStaminaPercent() > 0 &&
                               playerMovement.GetComponent<PlayerMovement>().IsRunning();

            float speed = bobSpeed;
            float amount = bobAmount;

            if (playerMovement.IsRunning())
            {
                speed *= sprintBobSpeedMultiplier;
                amount *= sprintBobAmountMultiplier;
            }

            timer += Time.deltaTime * speed;
            localPos.y = defaultYPos + Mathf.Sin(timer) * amount;
        }
        else
        {
            timer = 0;
            localPos.y = Mathf.Lerp(localPos.y, defaultYPos, Time.deltaTime * 5f);
        }

        transform.localPosition = localPos;
    }

}
