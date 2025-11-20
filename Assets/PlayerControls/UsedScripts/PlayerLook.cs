using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles mouse look for the player camera. Can be toggled off (used in focus mode).
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [Tooltip("If false, camera look is disabled. Useful while inspecting an object.")]
    public bool lookEnabled = true;

    private PlayerControls controls;
    private Vector2 lookInput;
    private float xRotation = 0f;

    [Header("Settings")]
    public float sensitivity = 2f;
    public Transform playerBody; // yaw rotates this transform

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
        // skip camera rotation if disabled (e.g. when inspecting an object)
        if (!lookEnabled)
            return;

        // read look vector from new Input System (mouse/gamepad)
        lookInput = controls.Player.Look.ReadValue<Vector2>() * sensitivity * Time.deltaTime;

        xRotation -= lookInput.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * lookInput.x);
    }
}
