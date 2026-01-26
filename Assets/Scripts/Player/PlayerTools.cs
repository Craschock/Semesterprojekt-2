using UnityEngine;
using System.Collections; // Wichtig für Coroutines
using FMODUnity;
using FMOD.Studio;

public class PlayerTools : MonoBehaviour
{
    [Header("FMOD Audio")]
    public EventReference lighterLoopSound;
    private EventInstance lighterInstance;

    [Header("Lighter Components")]
    public Light lighterLight;
    public ParticleSystem lighterParticles;

    [Header("Lighter Flicker Settings")]
    public float minIntensity = 13f;
    public float maxIntensity = 17f;
    public float flickerSpeed = 10f;

    [Header("Lighter Ignition Timing")]
    [Tooltip("Verzögerung zwischen Sound-Start (Klick) und dem ersten Licht.")]
    public float ignitionDelay = 0.2f;

    [Tooltip("Dauer, die das Licht nach dem ersten Aufleuchten kurz wieder ausgeht (Sputter-Effekt).")]
    public float sputterOffDuration = 0.1f;

    [Header("Atmosphere Interaction")]
    public FogController fogController;
    public float fogPushStrength = 0.05f;
    public float fogSoftFactor = 2.0f;

    [Header("Phone Components")]
    public GameObject phoneScreenObject;
    public Light phoneFaceLight;

    private PlayerStats playerStats;
    private bool isLighterOn = true;
    private bool isPhoneOn = true;
    private float randomOffset;

    private Coroutine ignitionCoroutine;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        randomOffset = Random.Range(0f, 100f);
    }

    private void Start()
    {
        ForceStopAllTools();
    }

    private void OnDisable()
    {
        StopLighterAudio();
    }

    private void Update()
    {
        if (isLighterOn && lighterLight != null && lighterLight.enabled)
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

    public void ForceStopAllTools()
    {
        TurnOffLighter();
        TurnOffPhone();
    }

    private void TurnOnLighter()
    {
        if (isLighterOn) return;
        isLighterOn = true;

        if (!lighterLoopSound.IsNull)
        {
            lighterInstance = RuntimeManager.CreateInstance(lighterLoopSound);
            lighterInstance.start();
        }

        if (ignitionCoroutine != null) StopCoroutine(ignitionCoroutine);
        ignitionCoroutine = StartCoroutine(IgnitionSequence());
    }

    private void TurnOffLighter()
    {
        if (!isLighterOn) return;
        isLighterOn = false;
        if (ignitionCoroutine != null) StopCoroutine(ignitionCoroutine);

        StopLighterAudio();
        SetLighterVisuals(false);

        if (fogController != null) RenderSettings.fogDensity = fogController.CurrentBaseDensity;
    }

    private IEnumerator IgnitionSequence()
    {
        SetLighterVisuals(false);
        yield return new WaitForSeconds(ignitionDelay);
        SetLighterVisuals(true);
        yield return new WaitForSeconds(0.05f);
        SetLighterVisuals(false);
        yield return new WaitForSeconds(sputterOffDuration);
        SetLighterVisuals(true);
    }

    private void SetLighterVisuals(bool state)
    {
        if (lighterLight) lighterLight.enabled = state;

        if (lighterParticles)
        {
            if (state) lighterParticles.Play();
            else lighterParticles.Stop();
        }
    }

    private void StopLighterAudio()
    {
        if (lighterInstance.isValid())
        {
            lighterInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            lighterInstance.release();
        }
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