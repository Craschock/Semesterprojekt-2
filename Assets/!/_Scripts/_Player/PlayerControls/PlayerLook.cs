using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles mouse look for the player camera using the new Input System.
/// Can be toggled off (e.g., used in focus mode).
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [Tooltip("If false, camera look is disabled. Useful while inspecting an object.")]
    public bool lookEnabled = true;

    [Header("Settings")]
    public float sensitivity = 2f;
    public Transform playerBody;

    private PlayerControls controls;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float normalizedScale = 200f;

    /// <summary>
    /// Initializes the new Input System controls.
    /// </summary>
    private void Awake()
    {
        controls = new PlayerControls();
    }

    /// <summary>
    /// Enables the input controls.
    /// </summary>
    private void OnEnable()
    {
        controls.Enable();
    }

    /// <summary>
    /// Disables the input controls.
    /// </summary>
    private void OnDisable()
    {
        controls.Disable();
    }
    
    /// <summary>
    /// Reads input and rotates the camera and player body if look is enabled.
    /// </summary>
    private void Update()
    {
        if (!lookEnabled) return;

        lookInput = controls.Player.Look.ReadValue<Vector2>();
        Vector2 normalizedLookInput = (lookInput / normalizedScale) * sensitivity;

        xRotation -= normalizedLookInput.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * normalizedLookInput.x);
        }
    }
}