using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public int Currency { get; private set; }
    public List<WeaponData> OwnedWeapons { get; private set; }
    public WeaponData[] EquippedWeapons { get; private set; }

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

    private void InitializeInventory()
    {
        Currency = 0;
        OwnedWeapons = new List<WeaponData>();
        EquippedWeapons = new WeaponData[3];
    }

    public void AddCurrency(int amount)
    {
        Currency += amount;
    }

    public bool SpendCurrency(int amount)
    {
        if (Currency >= amount)
        {
            Currency -= amount;
            return true;
        }
        return false;
    }

    public void AddWeapon(WeaponData weapon)
    {
        if (!OwnedWeapons.Contains(weapon))
        {
            OwnedWeapons.Add(weapon);
        }
    }

    public bool OwnsWeapon(WeaponData weapon)
    {
        return OwnedWeapons.Contains(weapon);
    }

    public void EquipWeapon(WeaponData weapon, int slot)
    {
        if (slot >= 0 && slot < EquippedWeapons.Length && OwnedWeapons.Contains(weapon))
        {
            EquippedWeapons[slot] = weapon;
        }
    }

    public void SaveInventory()
    {
        // Implement saving logic (e.g., using PlayerPrefs or a more robust saving system)
    }

    public void LoadInventory()
    {
        // Implement loading logic
    }
}