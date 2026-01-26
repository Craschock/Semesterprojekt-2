using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class MainMenu : MonoBehaviour
{
    [Header("FMOD Audio")]
    public EventReference mainMenuMusic;

    [Header("References")]
    public Button continueButton;

    private string savePath;
    private EventInstance musicInstance;

    private void Awake()
    {
        // Define path same as erik
        savePath = Path.Combine(Application.persistentDataPath, "playerSaveData.json");
    }

    private void Start()
    {
        if (!mainMenuMusic.IsNull)
        {
            musicInstance = RuntimeManager.CreateInstance(mainMenuMusic);
            musicInstance.start();
        }

        // Does a savefile exist
        if (continueButton != null)
        {
            if (File.Exists(savePath))
            {
                continueButton.interactable = true; // Button klickbar machen
            }
            else
            {
                continueButton.interactable = false; // Ausgrauen
            }
        }
    }

    public void PlayNewGame()
    {
        // Wir setzen das Signal auf 0 (NICHT laden)
        PlayerPrefs.SetInt("LoadGameOnStart", 0);
        PlayerPrefs.Save();

        LoadGameScene();
    }

    public void ContinueGame()
    {
        // Wir setzen das Signal auf 1 (BITTE laden)
        PlayerPrefs.SetInt("LoadGameOnStart", 1);
        PlayerPrefs.Save();

        LoadGameScene();
    }

    public void ExitGame() 
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }

    private void LoadGameScene()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void OnDestroy()
    {
        // Cleanup
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }
    }
}
