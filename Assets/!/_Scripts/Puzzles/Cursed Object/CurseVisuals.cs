using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CurseVisuals : MonoBehaviour
{
    public static CurseVisuals Instance { get; private set; }

    [Header("Volume Settings")]
    public Volume volume;

    [Header("Vignette Settings")]
    public float activeIntensity = 0.55f; // Darkening amount (ist das ueberhaupt ein wort?)
    public float smoothSpeed = 2f;

    private Vignette vignette;
    private float targetIntensity = 0f;

    private void Awake()
    {
        Instance = this;

        // Get vignette from camera volume
        if (volume != null && volume.profile.TryGet(out Vignette v))
        {
            vignette = v;
        }
        else
        {
            Debug.LogError("[CurseVisuals] Kein Vignette Override im Volume Profile gefunden!");
        }
    }

    private void Update()
    {
        if (vignette != null)
        {
            float current = (float)vignette.intensity;
            float newValue = Mathf.Lerp(current, targetIntensity, Time.deltaTime * smoothSpeed);
            vignette.intensity.Override(newValue);
        }
    }

    public void SetCurseActive(bool isActive)
    {
        targetIntensity = isActive ? activeIntensity : 0f;
    }
}