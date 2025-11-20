using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Basic CharacterController-based movement with sprint + stamina and smooth FOV change.
/// Minor cleanup and concise comments added.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls;
    private CharacterController controller;

    private Vector2 moveInput;
    private bool isSprinting;

    [Header("Camera Effects")]
    public Camera playerCamera;
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovChangeSpeed = 8f;

    [Header("Movement Speed")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;

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

        // toggle sprint state using Input System bindings
        controls.Player.Sprint.performed += ctx => isSprinting = true;
        controls.Player.Sprint.canceled += ctx => isSprinting = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        MovePlayer();
        HandleStamina();
        HandleFOV();
    }

    // basic movement using CharacterController
    void MovePlayer()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        bool canSprint = isSprinting && stamina > 0 && moveInput.magnitude > 0;
        float speed = canSprint ? sprintSpeed : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

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
        return isSprinting && stamina > 0 && moveInput.magnitude > 0;
    }

    // stamina drain & regen
    void HandleStamina()
    {
        if (isSprinting && moveInput.magnitude > 0)
            stamina -= staminaDrain * Time.deltaTime;
        else
            stamina += staminaRegen * Time.deltaTime;

        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    // smooth change of camera FOV when sprinting
    void HandleFOV()
    {
        if (playerCamera == null) return;

        bool canSprint = isSprinting && stamina > 0 && moveInput.magnitude > 0;
        float targetFOV = canSprint ? sprintFOV : normalFOV;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }

    // helper used by other scripts
    public bool IsMoving() => moveInput.magnitude > 0.1f;
    public float GetStaminaPercent() => stamina / maxStamina;
}
