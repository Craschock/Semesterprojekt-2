using UnityEngine;

public abstract class StatZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("How much to add/remove per second")]
    public float rate = 10f;

    // This logic runs every frame the player is inside the sphere
    private void OnTriggerStay(Collider other)
    {
        // check if the object is the player
        if (other.CompareTag("Player"))
        {
            // Try to find the stats script
            // (We look on the root in case the collider is on a child mesh)
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();

            if (stats != null)
            {
                ApplyEffect(stats);
            }
        }
    }

    // Each specific zone will define what "ApplyEffect" actually does (This one is abstract duhh-)
    protected abstract void ApplyEffect(PlayerStats stats);
}