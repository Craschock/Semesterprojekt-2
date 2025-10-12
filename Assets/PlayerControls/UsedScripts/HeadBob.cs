using UnityEngine;

public class HeadBob : MonoBehaviour
{
    public float bobSpeed = 14f;       // Frequency of the bob
    public float bobAmount = 0.05f;    // Amplitude of the bob
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
            timer += Time.deltaTime * bobSpeed;
            localPos.y = defaultYPos + Mathf.Sin(timer) * bobAmount;
        }
        else
        {
            timer = 0;
            localPos.y = Mathf.Lerp(localPos.y, defaultYPos, Time.deltaTime * 5f);
        }

        transform.localPosition = localPos;
    }
}
