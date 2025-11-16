using UnityEngine;

public class LoopTeleport : MonoBehaviour
{
    public Transform targetLandingRoot;     // Where to teleport TO
    public Transform thisLandingRoot;       // The root of THIS landing
    public CharacterController characterController;

    // *** Shared cooldown between all teleporter instances ***
    private static float teleportCooldown = 0f;
    private float cooldownDuration = 0.3f;

    private void Update()
    {
        // Count down the cooldown
        if (teleportCooldown > 0f)
            teleportCooldown -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // If cooldown active Å® skip teleport
        if (teleportCooldown > 0f)
            return;

        Transform player = other.transform;

        // Get offset relative to landing so position matches on the other end
        Vector3 offset = player.position - thisLandingRoot.position;

        // Disable controller before teleporting
        characterController.enabled = false;

        // Teleport the player
        player.position = targetLandingRoot.position + offset;

        // Re-enable controller
        characterController.enabled = true;

        // Activate teleport cooldown so the destination trigger doesnÅft fire immediately
        teleportCooldown = cooldownDuration;
    }
}
