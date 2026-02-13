using UnityEngine;
using System.Collections.Generic;

public class EnemyAudio : MonoBehaviour
{
    // Statische Liste aller aktiven Gegner
    public static List<EnemyAudio> AllEnemies = new List<EnemyAudio>();

    private void OnEnable()
    {
        AllEnemies.Add(this);
    }

    private void OnDisable()
    {
        AllEnemies.Remove(this);
    }

    private void OnDestroy()
    {
        AllEnemies.Remove(this);
    }
}