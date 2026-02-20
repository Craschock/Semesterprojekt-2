using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// A world-space Water Hut (Purity) the player can interact with.
/// When interacted, it recharges the players purity.
/// </summary>
[RequireComponent(typeof(OutlineController))]
public class WaterHutInteractable : MonoBehaviour, IInteractable
{
    [Header("Purity Settings")]
    public float amount = 100;

    [Header("FMOD Audio")]
    public EventReference ambienceSound;
    public EventReference interactSound;


    private OutlineController outline;
    private EventInstance ambienceInstance;

    private void Awake()
    {
        outline = GetComponent<OutlineController>();
    }

    private void Start()
    {
        if (!ambienceSound.IsNull)
        {
            ambienceInstance = RuntimeManager.CreateInstance(ambienceSound);
            RuntimeManager.AttachInstanceToGameObject(ambienceInstance, transform, GetComponent<Rigidbody>());
            ambienceInstance.start();
        }
    }

    private void OnDisable()
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambienceInstance.release();
        }
    }

    public void OnInteract(PlayerInteraction interactor)
    {
        if (!interactSound.IsNull)
        {
            RuntimeManager.PlayOneShot(interactSound, transform.position);
        }

        interactor.playerStats.RestorePurity(amount);
    }

    public void OnFocus()
    {
        if (outline != null) outline.SetToHighlight();
    }

    public void OnLoseFocus()
    {
        if (outline != null) outline.SetToProximityOrDefault();
    }
}