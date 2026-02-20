using UnityEngine;

public class PurityZone : StatZone
{
    protected override void ApplyEffect(PlayerStats stats)
    {
        if (rate > 0)
        {
            stats.RestorePurity(rate * Time.deltaTime);
        }
        else
        {
            // Convert negative rate to positive for reduction
            stats.ReducePurity(-rate * Time.deltaTime);
        }
    }
}