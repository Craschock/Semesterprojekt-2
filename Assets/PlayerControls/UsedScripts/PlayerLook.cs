using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    private PlayerControls controls;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public float sensitivity = 2f;
    public Transform playerBody;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        lookInput = controls.Player.Look.ReadValue<Vector2>() * sensitivity * Time.deltaTime;

        xRotation -= lookInput.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * lookInput.x);
    }
}
