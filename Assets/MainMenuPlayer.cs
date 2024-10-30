using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuPlayer : MonoBehaviour
{
    [Header("XP Bar")]
    public Slider xpBar;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI playerNameText;


    private void Start()
    {
        UpdateXPDisplay();
        UpdatePlayerNameDisplay();
    }

    public void UpdateXPDisplay()
    {
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.LevelSystem != null)
        {
            LevelSystem levelSystem = PlayerInventory.Instance.LevelSystem;

            // Update XP bar fill
            xpBar.value = (float)levelSystem.Experience / levelSystem.ExperienceToNextLevel;

            // Update level text
            levelText.text = $"{levelSystem.Level}";

            // Update XP text
            xpText.text = $"{levelSystem.Experience} / {levelSystem.ExperienceToNextLevel} XP";
        }
        else
        {
            Debug.LogWarning("PlayerInventory or LevelSystem is not initialized.");
        }
    }

    public void UpdatePlayerNameDisplay()
    {
        if (playerNameText != null && PlayerInventory.Instance != null)
        {
            string name = PlayerInventory.Instance.GetPlayerName();
            playerNameText.text = string.IsNullOrEmpty(name) ? "New Decoder" : name;
        }
    }
}