using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI lebenText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI furchtText;
    public TextMeshProUGUI reinheitText;

    public PlayerMovement player; // Link player here

    // Trackers to prevent string garbage creation
    private int lastStamina = -1;

    private void Update()
    {
        // Placeholder text (assumed static for now)
        lebenText.text = "Leben: 100";
        furchtText.text = "Furcht: 0";
        reinheitText.text = "Reinheit: 100";

        // ------------------------------------------------------
        // GARBAGE COLLECTION OPTIMIZATION
        // Only update text if the integer value has changed
        // ------------------------------------------------------
        int currentStamina = Mathf.RoundToInt(player.stamina);

        if (currentStamina != lastStamina)
        {
            staminaText.text = "Stamina: " + currentStamina.ToString();
            lastStamina = currentStamina;
        }
    }
}