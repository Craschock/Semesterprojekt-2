using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public enum AmbienceLocation
{
    Forest,
    Shrine,
    Inside
}

public class AmbienceManager : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference ambienceEvent;

    private EventInstance ambienceInstance;

    public static AmbienceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartAmbience();
    }

    private void StartAmbience()
    {
        if (ambienceEvent.IsNull) return;

        ambienceInstance = RuntimeManager.CreateInstance(ambienceEvent);
        ambienceInstance.start();

        // --- ÄNDERUNG: Standard ist jetzt Shrine ---
        SetLocation(AmbienceLocation.Shrine);
    }

    public void SetLocation(AmbienceLocation location)
    {
        if (!ambienceInstance.isValid()) return;

        ambienceInstance.setParameterByNameWithLabel("Location", location.ToString());

        // Debug.Log($"[Ambience] Switched to: {location}");
    }

    private void OnDestroy()
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambienceInstance.release();
        }
    }
}