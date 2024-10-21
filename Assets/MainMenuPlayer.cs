using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuPlayer : MonoBehaviour
{
    [Header("XP Bar")]
    public Image xpBar;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;

    private void Start()
    {
        UpdateXPDisplay();
    }

    public void UpdateXPDisplay()
    {
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.LevelSystem != null)
        {
            LevelSystem levelSystem = PlayerInventory.Instance.LevelSystem;

            // Update XP bar fill
            xpBar.fillAmount = (float)levelSystem.Experience / levelSystem.ExperienceToNextLevel;

            // Update level text
            levelText.text = $"Level: {levelSystem.Level}";

            // Update XP text
            xpText.text = $"{levelSystem.Experience} / {levelSystem.ExperienceToNextLevel} XP";
        }
        else
        {
            Debug.LogWarning("PlayerInventory or LevelSystem is not initialized.");
        }
    }
}