using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
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

    [SerializeField] private WeaponData playerPistol;
    [SerializeField] private WeaponInventory AllWeaponsPrefab;

    private Dictionary<string,int> weaponAmmo = new Dictionary<string, int>();
    private Dictionary<string, int> weaponLevels = new Dictionary<string, int>();


    private const string CURRENCY_KEY = "PlayerCurrency";
    private const string OWNED_WEAPONS_KEY = "OwnedWeapons";
    private const string EQUIPPED_WEAPONS_KEY = "EquippedWeapons";
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
        _equippedWeapons = new WeaponData[3];
        equippedAbilities = new Ability[3];
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
        HUDManager.Instance.UpdateCurrencyText();

        Debug.Log($"Level completed! Player earned {xpReward} XP and {currencyReward} Currency. " +
                  $"Total XP: {LevelSystem.Experience}, Total Currency: {Currency}");
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
        PlayerPrefs.SetInt(CURRENCY_KEY, Currency);

        string ownedWeaponsString = string.Join(",", OwnedWeapons.Select(w => w.weaponId));
        PlayerPrefs.SetString(OWNED_WEAPONS_KEY, ownedWeaponsString);

        string equippedWeaponsString = string.Join(",", EquippedWeapons.Select(w => w != null ? w.weaponId : "null"));
        PlayerPrefs.SetString(EQUIPPED_WEAPONS_KEY, equippedWeaponsString);

        string ammoString = string.Join(",", weaponAmmo.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        PlayerPrefs.SetString(WEAPON_AMMO_KEY, ammoString);

        string weaponLevelsString = string.Join(",", weaponLevels.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        PlayerPrefs.SetString(WEAPON_LEVELS_KEY, weaponLevelsString);

        PlayerPrefs.SetInt(LEVEL_KEY, LevelSystem.Level);
        PlayerPrefs.SetInt(EXPERIENCE_KEY, LevelSystem.Experience);
        PlayerPrefs.SetInt(STAT_POINTS_KEY, LevelSystem.StatPoints);

        PlayerPrefs.Save();
    }

    public void LoadInventory()
    {
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
            for (int i = 0; i < 3; i++)
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

    private void UICheck()
    {
        MainMenuPlayer mainMenuPlayer = FindObjectOfType<MainMenuPlayer>();
        if (mainMenuPlayer != null)
        {
            mainMenuPlayer.UpdateXPDisplay();
        }
    }

    public void ResetInventory()
    {
        Currency = 0;
        OwnedWeapons.Clear();
        EquippedWeapons = new WeaponData[3];
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

        PlayerPrefs.DeleteKey(CURRENCY_KEY);
        PlayerPrefs.DeleteKey(OWNED_WEAPONS_KEY);
        PlayerPrefs.DeleteKey(EQUIPPED_WEAPONS_KEY);
        PlayerPrefs.DeleteKey(WEAPON_AMMO_KEY);
        PlayerPrefs.DeleteKey(WEAPON_LEVELS_KEY);
        PlayerPrefs.Save();

        SaveInventory();

        UICheck();
    }
}