using UnityEngine;

public class CheatManager : MonoBehaviour
{
    [System.Serializable]
    public class CheatKey
    {
        public KeyCode key;
        public string description;
        public System.Action cheatAction;
    }

    public CheatKey[] cheats;

    private void Awake()
    {
        InitializeCheats();
    }

    private void InitializeCheats()
    {
        cheats = new CheatKey[]
        {
            new CheatKey
            {
                key = KeyCode.G,
                description = "Add 5000 Currency",
                cheatAction = () => AddCurrency(5000)
            },
            new CheatKey
            {
                key = KeyCode.H,
                description = "Reset Player Stats",
                cheatAction = ResetPlayerStats
            },
            new CheatKey
            {
                key = KeyCode.J,
                description = "Add 100 Experience",
                cheatAction = () => AddExperience(100)
            }
            // Add more cheats here as needed
        };
    }

    private void Update()
    {
        foreach (var cheat in cheats)
        {
            if (Input.GetKeyDown(cheat.key))
            {
                cheat.cheatAction?.Invoke();
                Debug.Log($"Cheat activated: {cheat.description}");
            }
        }
    }

    private void AddExperience(int amount)
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.LevelSystem.AddExperience(100);
            WeaponUI weaponShop = FindObjectOfType<WeaponUI>();
            if (weaponShop != null)
            {
                weaponShop.UpdateUI();
            }

            MainMenuPlayer mainMenuPlayer = FindObjectOfType<MainMenuPlayer>();
            if (mainMenuPlayer != null)
            {
                mainMenuPlayer.UpdateXPDisplay();
            }

            SkillTreeUI skillTreeUI = FindObjectOfType<SkillTreeUI>();
            if (skillTreeUI != null)
            {
                skillTreeUI.UpdateUI();
            }

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateLevelDisplay();
            }
            Debug.Log($"Added {amount} XP. Current Level: {PlayerInventory.Instance.LevelSystem.Level}, Skill Points: {PlayerInventory.Instance.LevelSystem.SkillPoints}");
        }
        else
        {
            Debug.LogError("PlayerInventory instance not found!");
        }
    }

    private void AddCurrency(int amount)
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddCurrency(amount);
            WeaponUI weaponShop = FindObjectOfType<WeaponUI>();
            if (weaponShop != null)
            {
                weaponShop.UpdateUI();
            }
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateSynthiumText();
            }
            Debug.Log($"Added {amount} currency. New total: {PlayerInventory.Instance.Currency}");
        }
        else
        {
            Debug.LogError("PlayerInventory instance not found!");
        }
    }

    private void ResetPlayerStats()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.ResetInventory();
            WeaponUI weaponShop = FindObjectOfType<WeaponUI>();
            if (weaponShop != null)
            {
                weaponShop.UpdateUI();
            }
            Debug.Log("Player stats reset.");
        }
        else
        {
            Debug.LogError("PlayerInventory instance not found!");
        }

        // Add any additional reset logic here, e.g., resetting health, position, etc.
    }
}