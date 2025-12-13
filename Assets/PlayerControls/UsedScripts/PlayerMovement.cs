using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CharacterController-based movement.
/// Includes Sprint, Stamina, FOV effects, and now CROUCHING.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls;
    private CharacterController controller;

    private Vector2 moveInput;
    private bool isSprinting;
    private bool isCrouching; // Track crouch state

    [Header("Camera Effects")]
    public Camera playerCamera;
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovChangeSpeed = 8f;

    [Header("Movement Speed")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 2.5f; // Slower speed when crouching

    [Header("Crouch Settings")]
    public float standHeight = 2f;   // Standard height of the character
    public float crouchHeight = 1f;  // Height when crouching
    public float crouchTransitionSpeed = 10f; // How fast the physical collider resizes

    [Header("Stamina")]
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float staminaDrain = 20f;
    public float staminaRegen = 10f;

    private float gravity = -9.81f;
    private Vector3 velocity;

    private void Awake()
    {
        controls = new PlayerControls();
        controller = GetComponent<CharacterController>();

        // toggle sprint state
        controls.Player.Sprint.performed += ctx => isSprinting = true;
        controls.Player.Sprint.canceled += ctx => isSprinting = false;

        // toggle crouch state (Hold to crouch, or Toggle - logic below is Hold)
        // If you want Toggle, use .performed with a bool flip. 
        // Here we use started/canceled for "Hold C to Crouch".
        controls.Player.Crouch.started += ctx => isCrouching = true;
        controls.Player.Crouch.canceled += ctx => isCrouching = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        HandleCrouchPhysics(); // Resize collider
        MovePlayer();
        HandleStamina();
        HandleFOV();
    }

    // Resize the CharacterController smoothly
    void HandleCrouchPhysics()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;

        // Check if trying to stand up but blocked by ceiling
        // (Simple raycast up)
        if (!isCrouching && Physics.Raycast(transform.position, Vector3.up, 2f))
        {
            // force crouch if head is blocked
            targetHeight = crouchHeight;
        }

        // Smoothly lerp the height
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

        // Adjust center so we shrink from the top down (feet stay on ground)
        // Center Y is always half of Height
        Vector3 center = controller.center;
        center.y = controller.height / 2f;
        controller.center = center;
    }

    void MovePlayer()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Determine speed
        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting && stamina > 0 && moveInput.magnitude > 0)
        {
            currentSpeed = sprintSpeed;
        }

        controller.Move(move * currentSpeed * Time.deltaTime);

        // simple gravity handling
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    // returns true if the player is running (used by HeadBob)
    public bool IsRunning()
    {
        // Cannot run while crouching
        if (isCrouching) return false;
        return isSprinting && stamina > 0 && moveInput.magnitude > 0;
    }

    public bool IsCrouching() => isCrouching;

    // stamina drain & regen
    void HandleStamina()
    {
        // Don't drain stamina if crouching
        if (!isCrouching && isSprinting && moveInput.magnitude > 0)
            stamina -= staminaDrain * Time.deltaTime;
        else
            stamina += staminaRegen * Time.deltaTime;

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    // smooth change of camera FOV when sprinting
    void HandleFOV()
    {
        if (playerCamera == null) return;

        // No sprint FOV if crouching
        bool canSprint = !isCrouching && isSprinting && stamina > 0 && moveInput.magnitude > 0;
        float targetFOV = canSprint ? sprintFOV : normalFOV;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }

    public bool IsMoving() => moveInput.magnitude > 0.1f;
    public float GetStaminaPercent() => stamina / maxStamina;
}