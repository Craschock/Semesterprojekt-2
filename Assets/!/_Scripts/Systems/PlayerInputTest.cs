using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputTest : MonoBehaviour
{
    private PlayerControls controls;

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
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector2 lookInput = controls.Player.Look.ReadValue<Vector2>();

        // Debug whatÅfs happening
        Debug.Log($"Move: {moveInput}, Look: {lookInput}");
    }
}