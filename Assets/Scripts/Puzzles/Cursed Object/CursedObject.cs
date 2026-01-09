using UnityEngine;

public class CursedObject : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public Transform visualCenter;
    public LayerMask obstructionMask;

    // References
    private OutlineController outline;
    private PlayerMovement playerMovement;
    private Camera playerCam;
    private Collider myCollider;

    // State
    private bool isPlayerInRange = false;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    private void Start()
    {
        playerCam = Camera.main;
        if (visualCenter == null) visualCenter = transform;

        if (GetComponentInChildren<SphereCollider>() == null)
        {
            Debug.LogWarning("CursedObject is missing its Child SphereCollider Sensor!");
        }
    }

    // --- INTERFACE IMPLEMENTATION (and outline) ---

    public void OnInteract(PlayerInteraction interactor)
    {
        Debug.Log("(Picked up Object) - Cursed Object Obtained!");

        // Check cursed object in playerstats please

        // Debugging
        if (playerMovement != null)
        {
            playerMovement.ClearMovementRestriction();
            playerMovement = null;
        }

        gameObject.SetActive(false);
    }

    public void OnFocus()
    {
        if (outline != null) outline.SetToHighlight();
    }

    public void OnLoseFocus()
    {
        if (outline != null) outline.SetToProximityOrDefault();
    }

    // --- CURSE LOGIC ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerMovement = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LiftCurse();
            isPlayerInRange = false;
            playerMovement = null;
        }
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
        Vector3 dirToObject = visualCenter.position - playerMovement.transform.position;
        dirToObject.y = 0;
        playerMovement.SetMovementRestriction(dirToObject);
    }

    private void LiftCurse()
    {
        if (playerMovement != null) 
        {
            playerMovement.ClearMovementRestriction();
        }
    }
}