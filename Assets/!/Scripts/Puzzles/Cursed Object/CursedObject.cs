using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(OutlineController))]
public class CursedObject : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public Transform visualCenter;
    public LayerMask obstructionMask;
    public Collider curseSensorCollider;

    [Header("FMOD Audio")]
    public EventReference pickupSound;
    public EventReference curseTriggerSound;

    // References
    private OutlineController outline;
    private PlayerMovement playerMovement;
    private Camera playerCam;

    // State
    private bool isPlayerInRange = false;
    private bool isCurseActive = false;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    private void Start()
    {
        playerCam = Camera.main;
        if (visualCenter == null) visualCenter = transform;

        if (curseSensorCollider == null)
        {
            Debug.LogError($"[CursedObject] '{name}' is missing the 'Curse Sensor Collider' reference! Please assign it in the Inspector.");
        }
    }

    // --- INTERFACE IMPLEMENTATION (and outline) ---

    public void OnInteract(PlayerInteraction interactor)
    {
        Debug.Log("(Picked up Object) - Cursed Object Obtained!");

        if (!pickupSound.IsNull) RuntimeManager.PlayOneShot(pickupSound);

        // Check cursed object in playerstats please

        // Debugging
        if (playerMovement != null)
        {
            playerMovement.ClearMovementRestriction();
            playerMovement = null;
        }

        if (CurseVisuals.Instance != null) CurseVisuals.Instance.SetCurseActive(false);

        gameObject.SetActive(false);
    }

    public void OnFocus() { if (outline != null) outline.SetToHighlight(); }

    public void OnLoseFocus() { if (outline != null) outline.SetToProximityOrDefault(); }

    // --- CURSE LOGIC ---
    public void OnCurseZoneEnter(Collider player)
    {
        isPlayerInRange = true;
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    public void OnCurseZoneExit(Collider player)
    {
        LiftCurse();
        isPlayerInRange = false;
        playerMovement = null;
    }

    private void Update()
    {
        // Only run logic if the sensor is active
        if (!isPlayerInRange || playerMovement == null) return;

        if (ShouldCursePlayer())
        {
            ApplyCurse();
        }
        else
        {
            LiftCurse();
        }
    }

    private bool ShouldCursePlayer()
    {
        // Viewport Check (Is it on screen?)
        Vector3 viewportPoint = playerCam.WorldToViewportPoint(visualCenter.position);

        bool isOnScreen = viewportPoint.z > 0 &&
                          viewportPoint.x > 0 && viewportPoint.x < 1 &&
                          viewportPoint.y > 0 && viewportPoint.y < 1;

        if (!isOnScreen) return false;

        // LOS Check
        Vector3 dirToPlayer = (playerCam.transform.position - visualCenter.position);
        float dist = dirToPlayer.magnitude;

        if (Physics.Raycast(visualCenter.position, dirToPlayer.normalized, out RaycastHit hit, dist, obstructionMask))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                return false; // Blocked by wall
            }
        }
        return true;
    }

    private void ApplyCurse()
    {
        // 1. Movement einschränken
        Vector3 dirToObject = visualCenter.position - playerMovement.transform.position;
        dirToObject.y = 0;
        playerMovement.SetMovementRestriction(dirToObject);

        // 2. Visuals an
        if (CurseVisuals.Instance != null)
            CurseVisuals.Instance.SetCurseActive(true);

        // 3. --- AUDIO START (Nur einmalig beim Eintritt) ---
        if (!isCurseActive)
        {
            if (!curseTriggerSound.IsNull)
            {
                RuntimeManager.PlayOneShot(curseTriggerSound, transform.position);
            }
            isCurseActive = true; // Sperre setzen
        }
    }

    private void LiftCurse()
    {
        // Sperre aufheben, damit Sound beim nächsten Mal wieder kommen kann
        isCurseActive = false;

        if (playerMovement != null)
        {
            playerMovement.ClearMovementRestriction();
        }

        if (CurseVisuals.Instance != null)
            CurseVisuals.Instance.SetCurseActive(false);
    }
}