using UnityEngine;

public class CurseSensorRelay : MonoBehaviour
{
    public CursedObject parentScript;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            parentScript.OnCurseZoneEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            parentScript.OnCurseZoneExit(other);
        }
    }
}