using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class EnemyAudio : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference battleMusicEvent;

    private EventInstance musicInstance;

    private void Start()
    {
        StartBattleMusic();
    }

    private void OnEnable()
    {
        if (!musicInstance.isValid())
        {
            StartBattleMusic();
        }
    }

    private void OnDisable()
    {
        StopBattleMusic();
    }

    private void OnDestroy()
    {
        StopBattleMusic();
    }

    private void StartBattleMusic()
    {
        if (battleMusicEvent.IsNull) return;
        musicInstance = RuntimeManager.CreateInstance(battleMusicEvent);
        RuntimeManager.AttachInstanceToGameObject(musicInstance, transform, GetComponent<Rigidbody>());
        musicInstance.start();
    }

    private void StopBattleMusic()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }
    }
}