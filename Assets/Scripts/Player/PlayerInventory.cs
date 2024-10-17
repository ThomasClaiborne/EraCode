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

    [SerializeField] private WeaponData playerPistol;
    [SerializeField] private WeaponInventory AllWeaponsPrefab;

    private Dictionary<string,int> weaponAmmo = new Dictionary<string, int>();

    private const string CURRENCY_KEY = "PlayerCurrency";
    private const string OWNED_WEAPONS_KEY = "OwnedWeapons";
    private const string EQUIPPED_WEAPONS_KEY = "EquippedWeapons";
    private const string WEAPON_AMMO_KEY = "WeaponAmmo";
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
        _equippedWeapons = new WeaponData[3];
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

    public void SaveInventory()
    {
        PlayerPrefs.SetInt(CURRENCY_KEY, Currency);

        string ownedWeaponsString = string.Join(",", OwnedWeapons.Select(w => w.weaponId));
        PlayerPrefs.SetString(OWNED_WEAPONS_KEY, ownedWeaponsString);

        string equippedWeaponsString = string.Join(",", EquippedWeapons.Select(w => w != null ? w.weaponId : "null"));
        PlayerPrefs.SetString(EQUIPPED_WEAPONS_KEY, equippedWeaponsString);

        string ammoString = string.Join(",", weaponAmmo.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        PlayerPrefs.SetString(WEAPON_AMMO_KEY, ammoString);

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

        int savedLevel = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        int savedExperience = PlayerPrefs.GetInt(EXPERIENCE_KEY, 0);
        int savedStatPoints = PlayerPrefs.GetInt(STAT_POINTS_KEY, 0);
        LevelSystem = new LevelSystem(savedLevel, savedExperience, savedStatPoints);
    }

    private WeaponData FindWeaponById(string weaponId)
    {
        if (AllWeaponsPrefab == null)
        {
            Debug.LogError("AllWeapons is null in PlayerInventory");
            return null;
        }

        if (AllWeaponsPrefab.allWeapons == null)
        {
            Debug.LogError("AllWeapons.allWeapons is null in PlayerInventory");
            return null;
        }

        if (AllWeaponsPrefab.allWeapons.Count == 0)
        {
            Debug.LogWarning("AllWeapons.allWeapons is empty in PlayerInventory");
            return null;
        }

        return AllWeaponsPrefab.allWeapons.FirstOrDefault(w => w != null && w.weaponId == weaponId);
    }

    public void ResetInventory()
    {
        Currency = 0;
        OwnedWeapons.Clear();
        EquippedWeapons = new WeaponData[3];
        weaponAmmo.Clear();

        foreach (var weapon in AllWeaponsPrefab.allWeapons)
        {
            weaponAmmo[weapon.weaponId] = weapon.ammo;
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
        PlayerPrefs.Save();

        SaveInventory();
    }
}