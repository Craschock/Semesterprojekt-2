using UnityEngine;
using System.Collections;

public class FogController : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("The density of visibility when the game starts.")]
    public float initialFogDensity = 0.1f;

    [Tooltip("The density of visibility after solving the puzzle.")]
    public float clearedFogDensity = 0.05f;

    [Tooltip("How many seconds it takes for the fog to clear.")]
    public float clearingDuration = 10f;

    private void Start()
    {
        // force the fog settings on game start
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = initialFogDensity;
    }

    // Call this method when the puzzle is solved!
    public void ClearFog()
    {
        StartCoroutine(ClearFogRoutine());
        Debug.Log("Clearing Fog");
    }

    private IEnumerator ClearFogRoutine()
    {
        float startDensity = RenderSettings.fogDensity;
        float timeElapsed = 0f;

        while (timeElapsed < clearingDuration)
        {
            // Lerp moves the density smoothly from Initial (High) to Cleared (Low)
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, clearedFogDensity, timeElapsed / clearingDuration);

            timeElapsed += Time.deltaTime;
            yield return null; // Wait for next frame
        }

        // Ensure we end exactly on the target density
        RenderSettings.fogDensity = clearedFogDensity;
    }
}