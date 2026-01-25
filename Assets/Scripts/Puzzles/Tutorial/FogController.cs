using UnityEngine;
using System.Collections;

public class FogController : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("The density of visibility when the game starts.")]
    public float initialFogDensity = 0.4f;

    [Tooltip("The density of visibility after solving the puzzle.")]
    public float clearedFogDensity = 0.15f;

    [Tooltip("How many seconds it takes for the fog to clear.")]
    public float clearingDuration = 6.7f;

    [Header("References")]
    public PlayerStats playerStats;

    public float CurrentBaseDensity { get; private set; }

    private void Start()
    {
        // force the fog settings on game start
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        CurrentBaseDensity = initialFogDensity;
        RenderSettings.fogDensity = CurrentBaseDensity;
    }

    [ContextMenu("ClearFog")]
    // Call this method when the puzzle is solved!
    public void ClearFog()
    {
        StartCoroutine(ClearFogRoutine());
        Debug.Log("Clearing Fog");
    }

    private IEnumerator ClearFogRoutine()
    {
        float startDensity = CurrentBaseDensity;
        float timeElapsed = 0f;

        while (timeElapsed < clearingDuration)
        {
            CurrentBaseDensity = Mathf.Lerp(startDensity, clearedFogDensity, timeElapsed / clearingDuration);
            RenderSettings.fogDensity = CurrentBaseDensity;
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        CurrentBaseDensity = clearedFogDensity;
        RenderSettings.fogDensity = CurrentBaseDensity;
    }
}