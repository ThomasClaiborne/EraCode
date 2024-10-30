using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private string playerName;
    public static PlayerInventory Instance { get; private set; }
    public int Currency { get; private set; }
    public LevelSystem LevelSystem { get; private set; }
    public List<WeaponData> OwnedWeapons { get; private set; }
    private WeaponData[] _equippedWeapons;
    public WeaponData[] EquippedWeapons
    {
        get { return _equippedWeapons; }
        private set { _equippedWeapons = value; }
    }

    public Ability[] equippedAbilities;
    public List<Ability> passiveAbilities;

    public List<string> unlockedAbilityIDs = new List<string>();
    public Dictionary<string, string> chosenPathsByCategory = new Dictionary<string, string>();

    [SerializeField] private WeaponData playerPistol;
    [SerializeField] private WeaponInventory AllWeaponsPrefab;
    [SerializeField] private AbilityInventory AllAbilitiesPrefab;

    private Dictionary<string,int> weaponAmmo = new Dictionary<string, int>();
    private Dictionary<string, int> weaponLevels = new Dictionary<string, int>();

    private const string CURRENCY_KEY = "PlayerCurrency";
    private const string OWNED_WEAPONS_KEY = "OwnedWeapons";
    private const string EQUIPPED_WEAPONS_KEY = "EquippedWeapons";
    private const string EQUIPPED_ABILITIES_KEY = "EquippedAbilities";
    private const string UNLOCKED_ABILITIES_KEY = "UnlockedAbilities";
    private const string WEAPON_AMMO_KEY = "WeaponAmmo";
    private const string WEAPON_LEVELS_KEY = "WeaponLevels";
    private const string LEVEL_KEY = "PlayerLevel";
    private const string EXPERIENCE_KEY = "PlayerExperience";
    private const string STAT_POINTS_KEY = "PlayerStatPoints";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();   
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
        SaveInventory(); 
    }
    public void AddAmmo(string weaponId, int amount)
    {
        if (!weaponAmmo.ContainsKey(weaponId))
        {
            WeaponData weapon = FindWeaponById(weaponId);
            weaponAmmo[weaponId] = weapon != null ? weapon.ammo : 0;
        }
        weaponAmmo[weaponId] += amount;
        SaveInventory();
    }

    public int GetAmmo(string weaponId)
    {
        if (!weaponAmmo.ContainsKey(weaponId))
        {
            WeaponData weapon = FindWeaponById(weaponId);
            weaponAmmo[weaponId] = weapon != null ? weapon.ammo : 0;
        }
        return weaponAmmo[weaponId];
    }

    private void InitializeInventory()
    {
        OwnedWeapons = new List<WeaponData>();
        passiveAbilities = new List<Ability>();
        _equippedWeapons = new WeaponData[5];
        equippedAbilities = new Ability[3];
        unlockedAbilityIDs = new List<string>();

        LoadInventory();

        foreach (var weapon in AllWeaponsPrefab.allWeapons)
        {
            if (!weaponAmmo.ContainsKey(weapon.weaponId))
            {
                weaponAmmo[weapon.weaponId] = weapon.ammo;
            }
        }

        if (_equippedWeapons[0] == null)
        {
            EquipWeapon(playerPistol, 0);
        }
    }

    public void AddCurrency(int amount)
    {
        Currency += amount;
        SaveInventory();
    }

    public bool SpendCurrency(int amount)
    {
        if (Currency >= amount)
        {
            Currency -= amount;
            SaveInventory();
            return true;
        }
        return false;
    }

    public void AddLevelCompletionReward(int xpReward, int currencyReward)
    {
        LevelSystem.AddExperience(xpReward);

        Currency += currencyReward;

        SaveInventory();
        HUDManager.Instance.UpdateLevelDisplay();
        HUDManager.Instance.UpdateSynthiumText();

        Debug.Log($"Level completed! Player earned {xpReward} XP and {currencyReward} Currency. " +
                  $"Total XP: {LevelSystem.Experience}, Total Currency: {Currency}");
    }

    public bool IsAbilityUnlocked(string nodeID)
    {
        return unlockedAbilityIDs.Contains(nodeID);
    }

    public void UnlockAbility(string nodeID, Ability ability)
    {
        if (!unlockedAbilityIDs.Contains(nodeID))
        {
            unlockedAbilityIDs.Add(nodeID);
            if (ability.isPassive)
            {
                AddPassiveAbility(ability);
            }
            SaveInventory();
        }
    }

    public void SetChosenPath(string category, string pathName)
    {
        chosenPathsByCategory[category] = pathName;
    }

    public void AddWeapon(WeaponData weapon)
    {
        if (!OwnedWeapons.Contains(weapon))
        {
            OwnedWeapons.Add(weapon);
            SaveInventory();
        }
    }

    public bool OwnsWeapon(WeaponData weapon)
    {
        return OwnedWeapons.Contains(weapon);
    }

    public void EquipWeapon(WeaponData weapon, int slot)
    {
        if (slot >= 0 && slot < EquippedWeapons.Length &&
            OwnedWeapons.Contains(weapon) || weapon == playerPistol)
        {
            EquippedWeapons[slot] = weapon;
            SaveInventory();
        }
    }

    public void UnequipWeapon(int slot)
    {
        if (slot >= 0 && slot < EquippedWeapons.Length)
        {
            EquippedWeapons[slot] = null;
            SaveInventory();
        }
    }
    public void UpgradeWeapon(string weaponId)
    {
        if (!weaponLevels.ContainsKey(weaponId))
        {
            weaponLevels[weaponId] = 1;
        }
        weaponLevels[weaponId]++;

        SaveInventory();
    }

    public void EquipAbility(Ability ability, int slot)
    {
        if (slot >= 0 && slot < equippedAbilities.Length)
        {
            equippedAbilities[slot] = ability;
            SaveInventory();
        }
    }

    public void UnequipAbility(int slot)
    {
        if (slot >= 0 && slot < equippedAbilities.Length)
        {
            equippedAbilities[slot] = null;
            SaveInventory();
        }
    }

    public void AddPassiveAbility(Ability ability)
    {
        if (!passiveAbilities.Contains(ability))
        {
            passiveAbilities.Add(ability);
        }
    }

    public int GetWeaponLevel(string weaponId)
    {
        return weaponLevels.TryGetValue(weaponId, out int level) ? level : 1;
    }

    public int GetWeaponDamage(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetCurrentLevelDamage(level) : 0;
    }

    public float GetWeaponFireRate(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetCurrentLevelFireRate(level) : 0f;
    }

    public float GetWeaponReloadTime(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetCurrentLevelReloadTime(level) : 0f;
    }

    public int GetWeaponClipSize(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetCurrentLevelClipSize(level) : 0;
    }

    public int GetNextLevelWeaponDamage(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetNextLevelDamage(level) : 0;
    }

    public float GetNextLevelWeaponFireRate(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetNextLevelFireRate(level) : 0f;
    }

    public float GetNextLevelWeaponReloadTime(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetNextLevelReloadTime(level) : 0f;
    }

    public int GetNextLevelWeaponClipSize(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetNextLevelClipSize(level) : 0;
    }

    public int GetWeaponUpgradeCost(string weaponId)
    {
        WeaponData weapon = FindWeaponById(weaponId);
        int level = GetWeaponLevel(weaponId);
        return weapon != null ? weapon.GetUpgradeCost(level) : 0;
    }

    public void SaveInventory()
    {
        PlayerPrefs.SetString("PlayerName", playerName);

        PlayerPrefs.SetInt(CURRENCY_KEY, Currency);

        string ownedWeaponsString = string.Join(",", OwnedWeapons.Select(w => w.weaponId));
        PlayerPrefs.SetString(OWNED_WEAPONS_KEY, ownedWeaponsString);

        string equippedWeaponsString = string.Join(",", EquippedWeapons.Select(w => w != null ? w.weaponId : "null"));

        string equippedAbilitiesString = string.Join(",", equippedAbilities.Select(a => a != null ? a.abilityID : "null"));
        PlayerPrefs.SetString(EQUIPPED_ABILITIES_KEY, equippedAbilitiesString);

        // Save unlocked abilities
        string unlockedAbilitiesString = string.Join(",", unlockedAbilityIDs);
        PlayerPrefs.SetString(UNLOCKED_ABILITIES_KEY, unlockedAbilitiesString);

        PlayerPrefs.SetString(EQUIPPED_WEAPONS_KEY, equippedWeaponsString);

        string ammoString = string.Join(",", weaponAmmo.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        PlayerPrefs.SetString(WEAPON_AMMO_KEY, ammoString);

        string weaponLevelsString = string.Join(",", weaponLevels.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        PlayerPrefs.SetString(WEAPON_LEVELS_KEY, weaponLevelsString);

        PlayerPrefs.SetInt(LEVEL_KEY, LevelSystem.Level);
        PlayerPrefs.SetInt(EXPERIENCE_KEY, LevelSystem.Experience);
        PlayerPrefs.SetInt(STAT_POINTS_KEY, LevelSystem.SkillPoints);

        PlayerPrefs.Save();
    }

    public void LoadInventory()
    {
        playerName = PlayerPrefs.GetString("PlayerName", "");

        Currency = PlayerPrefs.GetInt(CURRENCY_KEY, 0);

        string ownedWeaponsString = PlayerPrefs.GetString(OWNED_WEAPONS_KEY, "");
        if (!string.IsNullOrEmpty(ownedWeaponsString))
        {
            string[] weaponIds = ownedWeaponsString.Split(',');
            OwnedWeapons = weaponIds
                .Select(FindWeaponById)
                .Where(w => w != null)
                .ToList();
        }

        string equippedWeaponsString = PlayerPrefs.GetString(EQUIPPED_WEAPONS_KEY, "");
        if (!string.IsNullOrEmpty(equippedWeaponsString))
        {
            string[] equippedIds = equippedWeaponsString.Split(',');
            for (int i = 0; i < 5; i++)
            {
                if (i < equippedIds.Length && equippedIds[i] != "null")
                {
                    _equippedWeapons[i] = FindWeaponById(equippedIds[i]);
                }
                else
                {
                    _equippedWeapons[i] = null;
                }
            }
        }

        string equippedAbilitiesString = PlayerPrefs.GetString(EQUIPPED_ABILITIES_KEY, "");
        if (!string.IsNullOrEmpty(equippedAbilitiesString))
        {
            string[] equippedIds = equippedAbilitiesString.Split(',');
            for (int i = 0; i < 3; i++)
            {
                if (i < equippedIds.Length && equippedIds[i] != "null")
                {
                    equippedAbilities[i] = FindAbilityById(equippedIds[i]);
                }
                else
                {
                    equippedAbilities[i] = null;
                }
            }
        }

        string unlockedAbilitiesString = PlayerPrefs.GetString(UNLOCKED_ABILITIES_KEY, "");
        if (!string.IsNullOrEmpty(unlockedAbilitiesString))
        {
            unlockedAbilityIDs = unlockedAbilitiesString.Split(',').ToList();

            // Reconstruct passive abilities list from unlocked abilities
            passiveAbilities.Clear();
            foreach (string abilityId in unlockedAbilityIDs)
            {
                Ability ability = FindAbilityById(abilityId);
                if (ability != null && ability.isPassive)
                {
                    passiveAbilities.Add(ability);
                }
            }
        }

        string ammoString = PlayerPrefs.GetString(WEAPON_AMMO_KEY, "");
        if (!string.IsNullOrEmpty(ammoString))
        {
            weaponAmmo = ammoString.Split(',')
                .Select(s => s.Split(':'))
                .ToDictionary(
                    kvp => kvp[0],
                    kvp => int.Parse(kvp[1])
                );
        }

        string weaponLevelsString = PlayerPrefs.GetString(WEAPON_LEVELS_KEY, "");
        if (!string.IsNullOrEmpty(weaponLevelsString))
        {
            weaponLevels = weaponLevelsString.Split(',')
                .Select(s => s.Split(':'))
                .ToDictionary(
                    kvp => kvp[0],
                    kvp => int.Parse(kvp[1])
                );
        }

        int savedLevel = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        int savedExperience = PlayerPrefs.GetInt(EXPERIENCE_KEY, 0);
        int savedStatPoints = PlayerPrefs.GetInt(STAT_POINTS_KEY, 0);
        LevelSystem = new LevelSystem(savedLevel, savedExperience, savedStatPoints);
    }

    private WeaponData FindWeaponById(string weaponId)
    {
        if (AllWeaponsPrefab == null || AllWeaponsPrefab.allWeapons == null || AllWeaponsPrefab.allWeapons.Count == 0)
        {
            Debug.LogError("Issue with AllWeaponsPrefab in PlayerInventory");
            return null;
        }

        return AllWeaponsPrefab.allWeapons.FirstOrDefault(w => w != null && w.weaponId == weaponId);
    }

    private Ability FindAbilityById(string abilityId)
    {
        if (AllAbilitiesPrefab == null || AllAbilitiesPrefab.allAbilities == null)
        {
            Debug.LogError("Issue with AllAbilitiesPrefab in PlayerInventory");
            return null;
        }
        return AllAbilitiesPrefab.allAbilities.FirstOrDefault(a => a != null && a.abilityID == abilityId);
    }

    private void UICheck()
    {
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
    }

    public void ResetInventory()
    {
        playerName = null;
        Currency = 0;
        OwnedWeapons.Clear();
        EquippedWeapons = new WeaponData[5];
        equippedAbilities = new Ability[3];
        passiveAbilities.Clear();
        unlockedAbilityIDs.Clear();
        weaponAmmo.Clear();
        weaponLevels.Clear();


        foreach (var weapon in AllWeaponsPrefab.allWeapons)
        {
            weaponAmmo[weapon.weaponId] = weapon.ammo;
            weaponLevels[weapon.weaponId] = 1;
        }

        EquipWeapon(playerPistol, 0);

        LevelSystem = new LevelSystem();
        PlayerPrefs.DeleteKey(LEVEL_KEY);
        PlayerPrefs.DeleteKey(EXPERIENCE_KEY);
        PlayerPrefs.DeleteKey(STAT_POINTS_KEY);
        PlayerPrefs.DeleteKey("PlayerName");

        PlayerPrefs.DeleteKey(CURRENCY_KEY);
        PlayerPrefs.DeleteKey(OWNED_WEAPONS_KEY);
        PlayerPrefs.DeleteKey(EQUIPPED_WEAPONS_KEY);
        PlayerPrefs.DeleteKey(WEAPON_AMMO_KEY);
        PlayerPrefs.DeleteKey(WEAPON_LEVELS_KEY);
        PlayerPrefs.DeleteKey(EQUIPPED_ABILITIES_KEY);
        PlayerPrefs.DeleteKey(UNLOCKED_ABILITIES_KEY);
        PlayerPrefs.Save();

        SaveInventory();

        UICheck();
    }
}