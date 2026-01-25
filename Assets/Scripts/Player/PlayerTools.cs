using UnityEngine;

/// <summary>
/// Controls the logic and effects for the lighter and the phone
/// </summary>
public class PlayerTools : MonoBehaviour
{
    [Header("Lighter Components")]
    [Tooltip("Light that turns on (Child of the MainCamera)")]
    public Light lighterLight;
    [Tooltip("Particles of the flame (Child of the MainCamera)")]
    public ParticleSystem lighterParticles;

    [Header("Lighter Flicker Settings")]
    public float minIntensity = 13f; // Minimale Helligkeit
    public float maxIntensity = 17f; // Maximale Helligkeit
    public float flickerSpeed = 10f;  // Wie schnell das Licht zappelt

    [Header("Atmosphere Interaction")]
    public FogController fogController;
    public float fogPushStrength = 0.05f;
    public float fogSoftFactor = 2.0f;

    [Header("Phone Components")]
    [Tooltip("UI or light for the phone (Child of the MainCamera)")]
    public GameObject phoneScreenObject; // Light up display
    public Light phoneFaceLight; //Light up face

    // References
    private PlayerStats playerStats;
    private bool isLighterOn = true;
    private bool isPhoneOn = true;

    // Internal Flicker State
    private float randomOffset;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        randomOffset = Random.Range(0f, 100f); 
    }

    private void Start()
    {
        ForceStopAllTools();
    }

    private void Update()
    {
        if (isLighterOn && lighterLight != null)
        {
            float noise = Mathf.PerlinNoise((Time.time * flickerSpeed) + randomOffset, 0f);
            lighterLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            if (fogController != null)
            {
                float baseFog = fogController.CurrentBaseDensity;
                float flickerMod = noise * fogPushStrength;
                float targetFogDensity = Mathf.Max(0f, baseFog - flickerMod);
                float currentFog = RenderSettings.fogDensity;
                float smoothedFog = Mathf.Lerp(currentFog, targetFogDensity, Time.deltaTime * fogSoftFactor);
                RenderSettings.fogDensity = smoothedFog;
            }
        }
    }

    public void SetToolState(bool active)
    {
        // Which mode is active?
        if (playerStats.currentMode == EquipmentMode.Lighter)
        {
            if (active) TurnOnLighter();
            else TurnOffLighter();
        }
        else if (playerStats.currentMode == EquipmentMode.Phone)
        {
            if (active) TurnOnPhone();
            else TurnOffPhone();
        }
    }

    // Force stop if we switch away
    public void ForceStopAllTools()
    {
        TurnOffLighter();
        TurnOffPhone();
    }

    private void TurnOnLighter()
    {
        if (isLighterOn) return;
        isLighterOn = true;

        if (lighterLight) lighterLight.enabled = true;
        if (lighterParticles) lighterParticles.Play();
    }

    private void TurnOffLighter()
    {
        if (!isLighterOn) return;

        isLighterOn = false;
        if (lighterLight) lighterLight.enabled = false;
        if (lighterParticles) lighterParticles.Stop();
        if (fogController != null) RenderSettings.fogDensity = fogController.CurrentBaseDensity;
        
    }

    private void TurnOnPhone()
    {
        isPhoneOn = true;
        if (phoneScreenObject) phoneScreenObject.SetActive(true);
        if (phoneFaceLight) phoneFaceLight.enabled = true;
    }

    private void TurnOffPhone()
    {
        if (!isPhoneOn) return;

        isPhoneOn = false;
        if (phoneScreenObject) phoneScreenObject.SetActive(false);
        if (phoneFaceLight) phoneFaceLight.enabled = false;
    }
}