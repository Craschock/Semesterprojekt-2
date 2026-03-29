using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the "Focus/Inspect" mode where the player can closely look at and rotate held items.
/// Uses WASD (move input) to rotate the object and disables normal movement/camera look.
/// </summary>
public class PlayerFocusMode : MonoBehaviour
{
    [Header("References")]
    public PlayerInteraction playerInteraction;
    public PlayerMovement playerMovement;
    public PlayerLook playerLook;
    
    [Header("Positions")]
    [Tooltip("The transform in front of the camera where the item is moved to for inspection.")]
    public Transform inspectPoint;
    
    [Header("Settings")]
    public float transitionSpeed = 10f;
    public float rotationSpeed = 150f;
    
    [Header("Rotation Limits")]
    public float maxRotationX = 45f;
    public float maxRotationY = 45f;
    public float rotationSmoothing = 10f;

    // --- State ---
    public bool IsInFocusMode { get; private set; } = false;

    private PlayerControls controls;
    private PickupInteractable inspectedItem;
    
    // Original state before inspecting
    private Quaternion originalLocalRotation;
    
    // Input & calculation
    private Vector2 currentRotation;
    private Vector2 targetRotation;

    /// <summary>
    /// Initializes the input system and binds the Focus action.
    /// </summary>
    private void Awake()
    {
        controls = new PlayerControls();
        
        // Binde die Focus-Taste (z.B. F) an die Toggle-Methode
        controls.Player.Focus.performed += ctx => ToggleFocusMode();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    /// <summary>
    /// Handles the smooth movement and rotation of the inspected item.
    /// </summary>
    private void Update()
    {
        if (IsInFocusMode && inspectedItem != null)
        {
            HandleInspectRotation();
            
            // Override position from PlayerInteraction to smoothly go to the inspect point
            inspectedItem.transform.position = Vector3.Lerp(
                inspectedItem.transform.position, 
                inspectPoint.position, 
                Time.deltaTime * transitionSpeed);
        }
        else if (inspectedItem != null && !IsInFocusMode)
        {
            // Rotate smoothly back to original local rotation
            inspectedItem.transform.localRotation = Quaternion.Lerp(
                inspectedItem.transform.localRotation, 
                originalLocalRotation, 
                Time.deltaTime * rotationSmoothing);
                
            // Sobald die Rotation fast wieder stimmt, lassen wir das Objekt los
            if (Quaternion.Angle(inspectedItem.transform.localRotation, originalLocalRotation) < 1f)
            {
                inspectedItem.transform.localRotation = originalLocalRotation;
                inspectedItem = null;
            }
        }
    }

    /// <summary>
    /// Switches between Focus Mode ON and OFF.
    /// </summary>
    private void ToggleFocusMode()
    {
        PickupInteractable heldItem = playerInteraction.CurrentHeldItem;
        
        // Nur aktivieren, wenn wir auch wirklich etwas in der Hand halten
        if (!IsInFocusMode && heldItem != null)
        {
            StartFocus(heldItem);
        }
        else if (IsInFocusMode)
        {
            EndFocus();
        }
    }

    /// <summary>
    /// Starts the inspection, saves original state, and disables player movement.
    /// </summary>
    private void StartFocus(PickupInteractable item)
    {
        IsInFocusMode = true;
        inspectedItem = item;
        
        // Speichere die Start-Rotation, damit wir später exakt dorthin zurückkönnen
        originalLocalRotation = inspectedItem.transform.localRotation;
        currentRotation = Vector2.zero;
        targetRotation = Vector2.zero;

        // Deaktiviere das Laufen und Umsehen des Spielers
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerLook != null) playerLook.lookEnabled = false;
    }

    /// <summary>
    /// Ends the inspection and restores player control.
    /// </summary>
    public void EndFocus()
    {
        if (!IsInFocusMode) return;
        IsInFocusMode = false;

        // Gebe die Steuerung wieder an den Spieler zurück
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerLook != null) playerLook.lookEnabled = true;
    }

    /// <summary>
    /// Reads WASD input and calculates limited, smoothed rotation.
    /// </summary>
    private void HandleInspectRotation()
    {
        // Lese das WASD Input aus (Move-Action)
        Vector2 rotateInput = controls.Player.Move.ReadValue<Vector2>();

        // Berechne Ziel-Rotation (Wir invertieren Y für ein natürliches Gefühl)
        targetRotation.x += rotateInput.y * rotationSpeed * Time.deltaTime;
        targetRotation.y -= rotateInput.x * rotationSpeed * Time.deltaTime;

        // Limitiere die Rotation, damit das Item sich nicht überschlägt
        targetRotation.x = Mathf.Clamp(targetRotation.x, -maxRotationX, maxRotationX);
        targetRotation.y = Mathf.Clamp(targetRotation.y, -maxRotationY, maxRotationY);

        // Interpoliere sanft
        currentRotation = Vector2.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationSmoothing);

        // Wende die Rotation relativ zur originalen Rotation an
        inspectedItem.transform.localRotation = originalLocalRotation * Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
    }
}