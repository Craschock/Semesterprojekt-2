using UnityEngine;

public class ConsumableOnConsume : MonoBehaviour
{
    private PlayerStats stats;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void ApplyEffect(ConsumableType type)
    {
        Debug.Log($"[Consumable] Consuming item: {type}");

        switch (type)
        {
            case ConsumableType.None:
                return;
            case ConsumableType.DamageSelf:
                stats.TakeDamage(50);
                return;
            case ConsumableType.SmallHealthBoostItem:
                stats.Heal(25f);
                break;

            case ConsumableType.BigHealthBoostItem:
                stats.Heal(50f);
                break;

            case ConsumableType.SmallFearReductionItem:
                stats.ReduceFear(25f);
                break;

            case ConsumableType.BigFearReductionItem:
                stats.ReduceFear(100f);
                break;

            case ConsumableType.PurityRechargeItem:
                stats.RestorePurity(50f);
                break;

            // Add other cases here
            case ConsumableType.SpeedBoostItem:
            case ConsumableType.ShieldItem:
            case ConsumableType.BatteryItem:
            case ConsumableType.InvisibilityItem:
            case ConsumableType.GlowItem:
            case ConsumableType.PortablePurityStationItem:
            case ConsumableType.EnemyDistractionItem:
            case ConsumableType.EnemyStunItem:
                break;
        }
    }
}