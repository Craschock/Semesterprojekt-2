using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Global manager for displaying hint images (paper notes).
/// Handles:
/// - dim background
/// - centered hint sprite
/// - freezing/unfreezing player movement + camera look (option B)
/// - closing again with the same interact key (E)
/// </summary>
public class HintUIManager : MonoBehaviour
{
    public static HintUIManager Instance { get; private set; }

    [Header("UI References")]
    public CanvasGroup root;      // container for easy show/hide (optional but recommended)
    public Image dimmer;          // full-screen black/grey image (alpha > 0)
    public Image hintImage;       // centered image that displays the PNG
    public TextMeshProUGUI hintTextDisplay;

    private bool isHintOpen = false;
    private PlayerMovement cachedMovement; // stored while hint is open
    private PlayerLook cachedLook;         // stored while hint is open

    public bool IsHintOpen => isHintOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetVisible(false);
    }

    /// <summary>
    /// Open a hint: show UI and freeze movement + look.
    /// </summary>
    public void ShowHint(Sprite sprite, string text, PlayerInteraction interactor)
    {
        if (sprite == null || interactor == null) return;

        // store references to re-enable later
        cachedMovement = interactor.playerMovement;
        cachedLook = interactor.playerLook;

        // set image
        hintImage.sprite = sprite;
        hintImage.SetNativeSize(); // keeps correct aspect

        // set text
        if (hintTextDisplay != null)
        {
            hintTextDisplay.text = text;
        }

        // show UI
        SetVisible(true);
        isHintOpen = true;

        // freeze controls
        if (cachedMovement != null) cachedMovement.enabled = false;
        if (cachedLook != null) cachedLook.lookEnabled = false;
    }

    /// <summary>
    /// Close the hint and restore player control + cursor state.
    /// </summary>
    public void HideHint()
    {
        if (!isHintOpen) return;

        SetVisible(false);
        isHintOpen = false;

        if (cachedMovement != null) cachedMovement.enabled = true;
        if (cachedLook != null) cachedLook.lookEnabled = true;

        cachedMovement = null;
        cachedLook = null;
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.alpha = visible ? 1f : 0f;
            root.blocksRaycasts = visible;
            root.interactable = visible;
        }

        if (dimmer != null) dimmer.gameObject.SetActive(visible);
        if (hintImage != null) hintImage.gameObject.SetActive(visible);
        if (hintTextDisplay != null) hintTextDisplay.gameObject.SetActive(visible);
    }
}