using UnityEngine;

public class FearZone : StatZone
{
    protected override void ApplyEffect(PlayerStats stats)
    {
        if (rate > 0)
        {
            stats.AddFear(rate * Time.deltaTime);
        }
        else
        {
            // Convert negative rate to positive for reduction
            stats.ReduceFear(-rate * Time.deltaTime);
        }
    }
}