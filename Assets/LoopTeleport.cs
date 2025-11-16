using UnityEngine;

public class LoopTeleport : MonoBehaviour
{
    public Transform targetLandingRoot;     // Landing to teleport TO
    public Transform thisLandingRoot;       // Landing this teleporter belongs to
    public CharacterController characterController;

    [Header("Item Teleport")]
    public PlayerInteraction playerInteraction;   // reference to PlayerInteraction
    public Transform holdPoint;                    // same one used during pickup

    // Shared cooldown across teleporters
    private static float teleportCooldown = 0f;
    private float cooldownDuration = 0.3f;

    private void Update()
    {
        if (teleportCooldown > 0f)
            teleportCooldown -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (teleportCooldown > 0f)
            return;

        Transform player = other.transform;

        // PLAYER OFFSET
        Vector3 playerOffset = player.position - thisLandingRoot.position;

        // ITEM OFFSET (if holding one)
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

        // TELEPORT NOW
        characterController.enabled = false;

        Vector3 newPlayerPos = targetLandingRoot.position + playerOffset;
        player.position = newPlayerPos;

        // TELEPORT ITEM IF ONE IS HELD
        if (hasItem)
        {
            Transform item = playerInteraction.GetHeldItemTransform();
            item.position = newPlayerPos + itemLocalOffset;

            holdPoint.position = newPlayerPos + (holdPoint.position - player.position);
        }

        characterController.enabled = true;

        // ACTIVATE COOLDOWN SO WE DO NOT TELEPORT AGAIN IMMEDIATELY FUCKASS
        teleportCooldown = cooldownDuration;
    }
}
