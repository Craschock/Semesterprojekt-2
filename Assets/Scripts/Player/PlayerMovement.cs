using UnityEngine;

using UnityEngine.InputSystem;


/// <summary>

/// CharacterController-based movement.

/// Includes Sprint, FOV effects, and Crouching.

/// Integrated with: PlayerStats for stamina management.

/// </summary>

[RequireComponent(typeof(CharacterController))]

[RequireComponent(typeof(PlayerStats))]

public class PlayerMovement : MonoBehaviour

{

    private PlayerControls controls;

    private CharacterController controller;

    private PlayerStats playerStats;


    private Vector2 moveInput;

    private bool isSprinting;

    private bool isCrouching;


    [Header("Camera Effects")]

    public Camera playerCamera;

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


    // Internal physics

    private float gravity = -9.81f;

    private Vector3 velocity;


    // The direction we are NOT allowed to move towards

    private Vector3? restrictedMoveDirection = null;


    private void Awake()

    {

        controls = new PlayerControls();

        controller = GetComponent<CharacterController>();

        playerStats = GetComponent<PlayerStats>();


        controls.Player.Sprint.performed += ctx => isSprinting = true;

        controls.Player.Sprint.canceled += ctx => isSprinting = false;


        controls.Player.Crouch.started += ctx => isCrouching = true;

        controls.Player.Crouch.canceled += ctx => isCrouching = false;

    }


    private void OnEnable() => controls.Enable();

    private void OnDisable() => controls.Disable();


    private void Update()

    {

        HandleCrouchPhysics();

        MovePlayer();

        HandleFOV();

    }


    public void SetMovementRestriction(Vector3 directionFromPlayerToObject)

    {

        restrictedMoveDirection = directionFromPlayerToObject.normalized;

    }


    public void ClearMovementRestriction()

    {

        restrictedMoveDirection = null;

    }


    void HandleCrouchPhysics()

    {

        float targetHeight = isCrouching ? crouchHeight : standHeight;

        if (!isCrouching && Physics.Raycast(transform.position, Vector3.up, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))

        {

            targetHeight = crouchHeight;

        }


        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

        Vector3 center = controller.center;

        center.y = controller.height / 2f;

        controller.center = center;

    }


    void MovePlayer()

    {

        moveInput = controls.Player.Move.ReadValue<Vector2>();

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;


        // apply cursed object restriction

        if (restrictedMoveDirection.HasValue && move.magnitude > 0.1f)

        {

            // Dot Product checks alignment.

            // > 0 means we are trying to move closer to the cursed object.

            float dot = Vector3.Dot(move.normalized, restrictedMoveDirection.Value);


            if (dot > 0)

            {

                // Remove the component of the movement that goes towards the object

                // This allows strafing and moving backward, but stops forward movement.

                move -= restrictedMoveDirection.Value * dot * move.magnitude;

            }

        }


        float currentSpeed = walkSpeed;


        if (isCrouching)

        {

            currentSpeed = crouchSpeed;

            playerStats.StartStaminaRegen();

        }

        else if (isSprinting && moveInput.magnitude > 0)

        {

            if (playerStats.HasStamina(0.1f))

            {

                currentSpeed = sprintSpeed;

                playerStats.UseStamina(playerStats.staminaDrainRate * Time.deltaTime);

            }

            else

            {

                playerStats.StartStaminaRegen();

            }

        }

        else

        {

            playerStats.StartStaminaRegen();

        }


        controller.Move(move * currentSpeed * Time.deltaTime);


        if (controller.isGrounded && velocity.y < 0)

            velocity.y = -2f;

        else

            velocity.y += gravity * Time.deltaTime;


        controller.Move(velocity * Time.deltaTime);

    }


    void HandleFOV()

    {

        if (playerCamera == null) return;


        bool canSprint = !isCrouching && isSprinting && playerStats.HasStamina(0.1f) && moveInput.magnitude > 0;

        float targetFOV = canSprint ? sprintFOV : normalFOV;


        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);

    }


    public bool IsRunning()

    {

        if (isCrouching) return false;

        return isSprinting && moveInput.magnitude > 0 && playerStats.HasStamina(0.1f);

    }


    public bool IsCrouching() => isCrouching;

    public bool IsMoving() => moveInput.magnitude > 0.1f;


    public float GetStaminaPercent()

    {

        PlayerStats.PlayerData data = playerStats.GetStatsData();

        return data.stamina / playerStats.maxStamina;

    }

}