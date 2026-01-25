using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;

    [Header("Script References")]
    public PlayerStats playerStats;
    public PlayerLook playerLook;

    private PlayerControls controls;
    private bool isPaused = false;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Player.Pause.performed += ctx => TogglePause();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerLook != null) playerLook.lookEnabled = true;
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (playerLook != null) playerLook.lookEnabled = false;
    }

    // --- BUTTON FUNCTIONS ---

    public void OpenSettings()
    {
        Debug.Log("Open Settings (TODO: Link to Settings Prefab)");
    }

    public void SaveGame()
    {
        if (playerStats != null)
        {
            playerStats.SaveGame();
            Debug.Log("[PauseMenu] Game Saved.");
        }
    }

    public void LoadSave()
    {
        if (playerStats != null)
        {
            Time.timeScale = 1f;
            playerStats.LoadGame();
            Resume();
            Debug.Log("[PauseMenu] Save Loaded.");
        }
    }

    public void QuitToDesktop()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}