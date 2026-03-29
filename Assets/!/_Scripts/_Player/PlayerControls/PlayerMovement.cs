using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CharacterController-based movement using the new Input System.
/// Includes sprinting, crouching, and FOV effects.
/// Integrated with PlayerStatsManager for stamina management.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerStatsManager playerStats; // Updated to new manager
    public Camera playerCamera;
    private CharacterController controller;
    private PlayerControls controls;

    [Header("Camera Effects")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovChangeSpeed = 8f;

    [Header("Movement Speed")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 2.5f;

    [Header("Crouch Settings")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;

    // Internal state
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isSprinting;
    private bool isCrouching;
    private float gravity = -9.81f;
    
    // Curse restriction state
    private bool isRestricted = false;
    private Vector3 restrictedDirection;

    /// <summary>
    /// Initializes controls and component references.
    /// </summary>
    private void Awake()
    {
        controls = new PlayerControls();
        controller = GetComponent<CharacterController>();
        if (playerStats == null) playerStats = GetComponent<PlayerStatsManager>();

        controls.Player.Sprint.started += ctx => isSprinting = true;
        controls.Player.Sprint.canceled += ctx => isSprinting = false;

        controls.Player.Crouch.started += ctx => isCrouching = true;
        controls.Player.Crouch.canceled += ctx => isCrouching = false;
    }

    /// <summary>
    /// Enables input controls.
    /// </summary>
    private void OnEnable() => controls.Enable();

    /// <summary>
    /// Disables input controls.
    /// </summary>
    private void OnDisable() => controls.Disable();

    /// <summary>
    /// Handles crouch height, movement, and FOV updates every frame.
    /// </summary>
    private void Update()
    {
        HandleCrouch();
        HandleMovement();
        HandleFOV();
    }

    /// <summary>
    /// Smoothly adjusts the character controller height and center based on crouch state.
    /// </summary>
    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.center = new Vector3(0, controller.height / 2f, 0);
    }

    /// <summary>
    /// Calculates movement vectors, stamina drain, and applies gravity.
    /// </summary>
    private void HandleMovement()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (isRestricted) move = restrictedDirection;

        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting && moveInput.magnitude > 0 && playerStats != null && playerStats.HasStamina(0.1f))
        {
            currentSpeed = sprintSpeed;
            playerStats.UseStamina(playerStats.staminaDrainRate * Time.deltaTime);
        }

        controller.Move(move * currentSpeed * Time.deltaTime);

        // Simple Gravity handling
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Smoothly interpolates the camera FOV when sprinting.
    /// </summary>
    private void HandleFOV()
    {
        if (playerCamera == null) return;

        bool canSprint = !isCrouching && isSprinting && playerStats != null && playerStats.HasStamina(0.1f) && moveInput.magnitude > 0;
        float targetFOV = canSprint ? sprintFOV : normalFOV;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }

    /// <summary>
    /// Checks if the player is currently running (sprinting, moving, not crouching).
    /// </summary>
    public bool IsRunning()
    {
        if (isCrouching) return false;
        return isSprinting && moveInput.magnitude > 0;
    }

    /// <summary>
    /// Checks if the player is currently crouching.
    /// </summary>
    public bool IsCrouching() => isCrouching;

    /// <summary>
    /// Checks if the player is currently providing movement input.
    /// </summary>
    public bool IsMoving() => moveInput.magnitude > 0;

    /// <summary>
    /// Forces the player to move in a specific direction (used by cursed objects).
    /// </summary>
    public void SetMovementRestriction(Vector3 direction)
    {
        isRestricted = true;
        restrictedDirection = direction.normalized;
    }

    /// <summary>
    /// Lifts the movement restriction.
    /// </summary>
    public void ClearMovementRestriction()
    {
        isRestricted = false;
        restrictedDirection = Vector3.zero;
    }
}