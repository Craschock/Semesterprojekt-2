using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class GlobalMusicManager : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference battleMusicEvent;
    public string distanceParameterName = "EnemyDistance";

    [Header("Logic")]
    public float maxDistanceCheck = 20f;

    private EventInstance musicInstance;

    private void Start()
    {
        if (battleMusicEvent.IsNull)
        {
            Debug.LogError("[MusicManager] KEIN Event zugewiesen im Inspector!");
            return;
        }

        musicInstance = RuntimeManager.CreateInstance(battleMusicEvent);
        musicInstance.start();
        Debug.Log("[MusicManager] Musik gestartet.");
    }

    private void Update()
    {
        if (!musicInstance.isValid()) return;

        float closestDist = GetDistanceToClosestEnemy();

        // --- DEBUGGING ---
        // Kommentiere das aus, wenn alles geht. 
        // Es zeigt dir: Wie viele Gegner? Wie nah ist der nächste?
        Debug.Log($"Enemies: {EnemyAudio.AllEnemies.Count} | Closest: {closestDist}m");
        // -----------------

        musicInstance.setParameterByName(distanceParameterName, closestDist);
    }

    private float GetDistanceToClosestEnemy()
    {
        if (EnemyAudio.AllEnemies.Count == 0) return maxDistanceCheck;

        float minDistance = maxDistanceCheck;
        Vector3 myPos = transform.position;

        foreach (var enemy in EnemyAudio.AllEnemies)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(myPos, enemy.transform.position);
            if (dist < minDistance) minDistance = dist;
        }
        return minDistance;
    }

    private void OnDestroy()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }
    }
}