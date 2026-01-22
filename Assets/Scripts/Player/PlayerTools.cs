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

    [Header("Phone Components")]
    [Tooltip("UI or light for the phone (Child of the MainCamera)")]
    public GameObject phoneScreenObject; // Light up display
    public Light phoneFaceLight; //Light up face

    // References
    private PlayerStats playerStats;
    private bool isLighterOn = false;
    private bool isPhoneOn = false;

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
        if (isPhoneOn && playerStats != null)
        {
            playerStats.ReduceFear(5f * Time.deltaTime);
        }

        if (isLighterOn && lighterLight != null)        // Perlin noise for flickering
        {
            float noise = Mathf.PerlinNoise((Time.time * flickerSpeed) + randomOffset, 0f);
            lighterLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
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
        isLighterOn = false;

        if (lighterLight) lighterLight.enabled = false;
        if (lighterParticles) lighterParticles.Stop();
    }

    private void TurnOnPhone()
    {
        isPhoneOn = true;
        if (phoneScreenObject) phoneScreenObject.SetActive(true);
        if (phoneFaceLight) phoneFaceLight.enabled = true;
    }

    private void TurnOffPhone()
    {
        isPhoneOn = false;
        if (phoneScreenObject) phoneScreenObject.SetActive(false);
        if (phoneFaceLight) phoneFaceLight.enabled = false;
    }
}