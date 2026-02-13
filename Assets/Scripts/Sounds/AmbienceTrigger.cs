using UnityEngine;

public class AmbienceTrigger : MonoBehaviour
{
    [Tooltip("Welches Environment soll hier herrschen? (z.B. Inside oder Forest)")]
    public AmbienceLocation targetLocation;

    [Tooltip("Soll beim Verlassen wieder auf Shrine gewechselt werden?")]
    public bool resetToShrineOnExit = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (AmbienceManager.Instance != null)
            {
                AmbienceManager.Instance.SetLocation(targetLocation);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (resetToShrineOnExit && other.CompareTag("Player"))
        {
            if (AmbienceManager.Instance != null)
            {
                // ZURÜCK ZUM STANDARD (Shrine)
                AmbienceManager.Instance.SetLocation(AmbienceLocation.Shrine);
            }
        }
    }
}