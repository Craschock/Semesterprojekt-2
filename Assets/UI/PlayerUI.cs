using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI lebenText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI furchtText;
    public TextMeshProUGUI reinheitText;

    public PlayerMovement player; // Link player here

    private void Update()
    {
        lebenText.text = "Leben: 100"; // Placeholder
        furchtText.text = "Furcht: 0"; // Placeholder
        reinheitText.text = "Reinheit: 100"; // Placeholder

        staminaText.text = "Stamina: " + Mathf.Round(player.stamina);
    }
}
