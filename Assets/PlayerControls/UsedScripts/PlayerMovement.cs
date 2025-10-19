using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls;
    private CharacterController controller;

    private Vector2 moveInput;
    private bool isSprinting;

    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;

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

        controls.Player.Sprint.performed += ctx => isSprinting = true;
        controls.Player.Sprint.canceled += ctx => isSprinting = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        MovePlayer();
        HandleStamina();
    }

    void MovePlayer()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        bool canSprint = isSprinting && stamina > 0 && moveInput.magnitude > 0;
        float speed = canSprint ? sprintSpeed : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleStamina()
    {
        if (isSprinting && moveInput.magnitude > 0)
            stamina -= staminaDrain * Time.deltaTime;
        else
            stamina += staminaRegen * Time.deltaTime;

        stamina = Mathf.Clamp(stamina, 0, maxStamina);
    }

    public bool IsMoving() => moveInput.magnitude > 0.1f;
    public float GetStaminaPercent() => stamina / maxStamina;
}
