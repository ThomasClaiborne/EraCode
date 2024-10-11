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
                description = "Add 10 Currency",
                cheatAction = () => AddCurrency(10)
            },
            new CheatKey
            {
                key = KeyCode.H,
                description = "Reset Player Stats",
                cheatAction = ResetPlayerStats
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

    private void AddCurrency(int amount)
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddCurrency(amount);
            WeaponShop weaponShop = FindObjectOfType<WeaponShop>();
            if (weaponShop != null)
            {
                weaponShop.UpdateUI();
            }
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateCurrencyText();
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
            WeaponShop weaponShop = FindObjectOfType<WeaponShop>();
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