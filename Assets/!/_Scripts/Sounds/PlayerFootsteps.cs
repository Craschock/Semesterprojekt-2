using UnityEngine;
using FMODUnity;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference footstepEvent;
    [Tooltip("Der Name des Parameters in FMOD (muss exakt stimmen!)")]
    public string surfaceParameterName = "Surface";

    [Header("Step Settings")]
    public float stepDistance = 1.8f;
    public float crouchMultiplier = 0.8f;

    [Header("Surface Definitions")]
    public SurfaceType[] terrainLayerMapping;

    public enum SurfaceType
    {
        Wood = 0,
        Gravel = 1,
        Grass = 2,
        Dirt = 3,
        Stone = 4
    }

    // References
    private CharacterController controller;
    private PlayerMovement playerMovement;

    // State
    private float distanceTraveled;
    private int playerLayer;
    private Vector3 lastPosition; // <--- NEU: Zum manuellen Messen

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void Start()
    {
        // Startposition merken, damit im ersten Frame kein Riesenschritt passiert
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (playerMovement == null || controller == null) return;

        // 1. Manuelle Geschwindigkeitsberechnung (Horizontal)
        // Wir ignorieren Y, damit Fallen/Springen keine Schritte auslöst
        Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 lastPosFlat = new Vector3(lastPosition.x, 0, lastPosition.z);

        float distanceThisFrame = Vector3.Distance(currentPosFlat, lastPosFlat);

        // Debug.Log($"Dist: {distanceThisFrame} | Grounded: {controller.isGrounded}");

        // Check: Haben wir uns bewegt UND sind am Boden?
        // Wir nutzen einen kleinen Threshold (0.001f), um Zittern zu ignorieren
        if (distanceThisFrame > 0.001f && controller.isGrounded)
        {
            distanceTraveled += distanceThisFrame;

            float currentStepDist = stepDistance;
            // Falls du Crouching später wieder einbaust, funktioniert das hier noch
            if (playerMovement.IsCrouching()) currentStepDist *= crouchMultiplier;

            if (distanceTraveled >= currentStepDist)
            {
                PlayFootstep();
                distanceTraveled = 0f;
            }
        }
        else
        {
            // Wenn wir stehen, Schrittzähler "vorbereiten", damit beim Loslaufen sofort was kommt
            distanceTraveled = stepDistance * 0.9f;
        }

        // Position für nächsten Frame merken
        lastPosition = transform.position;
    }

    private void PlayFootstep()
    {
        if (footstepEvent.IsNull) return;

        SurfaceType currentSurface = DetectSurface();

        FMOD.Studio.EventInstance footstepInstance = RuntimeManager.CreateInstance(footstepEvent);
        RuntimeManager.AttachInstanceToGameObject(footstepInstance, transform, GetComponent<Rigidbody>());
        footstepInstance.setParameterByNameWithLabel(surfaceParameterName, currentSurface.ToString());
        footstepInstance.start();
        footstepInstance.release();

        // Debug.Log($"Schritt auf: {currentSurface}");
    }

    private SurfaceType DetectSurface()
    {
        RaycastHit hit;
        int layerMask = Physics.DefaultRaycastLayers;
        if (playerLayer != -1)
        {
            layerMask &= ~(1 << playerLayer);
        }

        // Raycast von 1.5m Höhe nach unten (Länge 3m)
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out hit, 3.0f, layerMask))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                int layerIndex = GetDominantTerrainTexture(hit.point, terrain);
                if (layerIndex >= 0 && layerIndex < terrainLayerMapping.Length)
                {
                    return terrainLayerMapping[layerIndex];
                }
                return SurfaceType.Dirt;
            }

            if (hit.collider.CompareTag("Wood")) return SurfaceType.Wood;
            if (hit.collider.CompareTag("Stone")) return SurfaceType.Stone;
            //if (hit.collider.CompareTag("Grass")) return SurfaceType.Grass;
            //if (hit.collider.CompareTag("Dirt")) return SurfaceType.Dirt;
            //if (hit.collider.CompareTag("Gravel")) return SurfaceType.Gravel;
        }

        return SurfaceType.Stone;
    }

    private int GetDominantTerrainTexture(Vector3 worldPos, Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        float maxMix = 0;
        int maxIndex = 0;

        for (int i = 0; i < splatmapData.GetLength(2); i++)
        {
            if (splatmapData[0, 0, i] > maxMix)
            {
                maxMix = splatmapData[0, 0, i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }
}