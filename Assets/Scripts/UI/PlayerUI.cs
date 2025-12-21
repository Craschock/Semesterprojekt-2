using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI lebenText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI furchtText;
    public TextMeshProUGUI reinheitText;
    public PlayerStats playerStats;

    private int lastStamina = -1;

    private void Update()
    {
        if (playerStats == null) return;

        // Get the data struct from our new system
        PlayerStats.PlayerData data = playerStats.GetStatsData();

        // ------------------------------------------------------
        // Display other stats (casting to int for cleaner text)
        // ------------------------------------------------------
        lebenText.text = "Leben: " + Mathf.RoundToInt(data.health);
        furchtText.text = "Furcht: " + Mathf.RoundToInt(data.fear);
        reinheitText.text = "Reinheit: " + Mathf.RoundToInt(data.purity);

        // ------------------------------------------------------
        // STAMINA
        // ------------------------------------------------------
        int currentStamina = Mathf.RoundToInt(data.stamina);

        if (currentStamina != lastStamina)
        {
            staminaText.text = "Stamina: " + currentStamina.ToString();
            lastStamina = currentStamina;
        }
    }
}