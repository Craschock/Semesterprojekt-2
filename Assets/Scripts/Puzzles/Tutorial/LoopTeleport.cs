using UnityEngine;

/// <summary>
/// Teleports the player between two landing roots while preserving local offset.
/// Also teleports the held item (if any) by preserving its offset to the player.
/// A short global cooldown prevents immediate re-triggering.
/// </summary>
public class LoopTeleport : MonoBehaviour
{
    [Header("Landing Transforms")]
    public Transform targetLandingRoot;     // landing to teleport TO
    public Transform thisLandingRoot;       // landing this teleporter belongs to
    public CharacterController characterController;

    [Header("Item Teleport")]
    public PlayerInteraction playerInteraction; // reference to PlayerInteraction to query held item

    // shared cooldown across all teleporters to avoid bouncing
    private static float teleportCooldown = 0f;
    private float cooldownDuration = 0.3f;

    private void Update()
    {
        if (teleportCooldown > 0f)
            teleportCooldown -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // only teleport the player (requires player to be tagged "Player")
        if (!other.CompareTag("Player"))
            return;

        // respect global cooldown
        if (teleportCooldown > 0f)
            return;

        Transform player = other.transform;

        // compute player's local offset inside this landing
        Vector3 playerOffset = player.position - thisLandingRoot.position;

        // if player holds an item, compute its offset to the player so we can teleport it too
        Vector3 itemLocalOffset = Vector3.zero;
        bool hasItem = false;

        if (playerInteraction != null && playerInteraction.HeldItemExists())
        {
            Transform item = playerInteraction.GetHeldItemTransform();
            if (item != null)
            {
                hasItem = true;
                itemLocalOffset = item.position - player.position;
            }
        }

        // disable controller before changing position to avoid CharacterController collisions
        characterController.enabled = false;

        // perform teleport
        Vector3 newPlayerPos = targetLandingRoot.position + playerOffset;
        player.position = newPlayerPos;

        // if an item is held, teleport it by preserving its relative offset to the player
        if (hasItem)
        {
            Transform item = playerInteraction.GetHeldItemTransform();
            if (item != null)
            {
                item.position = newPlayerPos + itemLocalOffset;
            }
        }

        // re-enable controller
        characterController.enabled = true;

        // set cooldown so destination trigger doesn't immediately re-fire
        teleportCooldown = cooldownDuration;
    }
}
