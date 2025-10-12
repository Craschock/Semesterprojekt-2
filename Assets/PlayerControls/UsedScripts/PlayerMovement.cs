using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]     //Only formality check, so no null errors happen
public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls;
    private CharacterController controller;

    private Vector2 moveInput;
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    private Vector3 velocity;

    private void Awake()
    {
        controls = new PlayerControls();
        controller = GetComponent<CharacterController>();
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
        // Get movement input
        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // Convert movement to world space
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        controller.Move(move * moveSpeed * Time.deltaTime);

        // Apply gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f; // Keeps grounded
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    public bool IsMoving()
    {
        return moveInput.magnitude > 0.1f;
    }
}
