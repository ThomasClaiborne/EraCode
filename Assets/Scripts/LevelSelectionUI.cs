using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class LevelButton
    {
        public string levelId;      // e.g., "OPZ1-1"
        public Button button;       // Reference to the existing button
        public Image buttonImage;   // Optional: Reference to button's image if you want to change color
    }

    [Header("Level Buttons")]
    [SerializeField] private LevelButton[] levelButtons;

    [Header("Locked State")]
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Start()
    {
        InitializeLevelStates();
    }

    private void InitializeLevelStates()
    {
        foreach (var levelButton in levelButtons)
        {
            bool isUnlocked = levelButton.levelId == "OPZ1-1" || // First level always unlocked
                             PlayerInventory.Instance.unlockedLevels.Contains(levelButton.levelId);

            // Update button interactability
            levelButton.button.interactable = isUnlocked;

            // Optional: Update button color if buttonImage is assigned
            if (levelButton.buttonImage != null && !isUnlocked)
            {
                levelButton.buttonImage.color = lockedColor;
            }
        }
    }
}
