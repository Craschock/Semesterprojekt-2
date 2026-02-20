using UnityEngine;

public class HealthZone : StatZone
{
    protected override void ApplyEffect(PlayerStats stats)
    {
        if (rate > 0)
        {
            stats.Heal(rate * Time.deltaTime);
        }
        else
        {
            // Convert negative rate to positive for reduction
            stats.TakeDamage(-rate * Time.deltaTime);
        }
    }
}