using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    public List<WeaponData> allWeapons = new List<WeaponData>();
    public List<WeaponData> assaultRifles = new List<WeaponData>();
    public List<WeaponData> submachineGuns = new List<WeaponData>();
    public List<WeaponData> shotguns = new List<WeaponData>();
    public List<WeaponData> snipers = new List<WeaponData>();
    public List<WeaponData> rocketLaunchers = new List<WeaponData>();
    public List<WeaponData> pistols = new List<WeaponData>();

    public int WeaponCount => allWeapons.Count;
    public int AssaultRifleCount => assaultRifles.Count;
    public int SubmachineGunCount => submachineGuns.Count;
    public int ShotgunCount => shotguns.Count;
    public int SniperCount => snipers.Count;
    public int RocketLauncherCount => rocketLaunchers.Count;
    public int PistolCount => pistols.Count;

    public void AddWeapon(WeaponData weapon)
    {
        if (!allWeapons.Contains(weapon))
        {
            allWeapons.Add(weapon);

            if (weapon.isAssaultRifle) assaultRifles.Add(weapon);
            if (weapon.isSubmachineGun) submachineGuns.Add(weapon);
            if (weapon.isShotgun) shotguns.Add(weapon);
            if (weapon.isSniper) snipers.Add(weapon);
            if (weapon.isRocketLauncher) rocketLaunchers.Add(weapon);
            if (weapon.isPistol) pistols.Add(weapon);
        }
    }

    public void RemoveWeapon(WeaponData weapon)
    {
        allWeapons.Remove(weapon);
        assaultRifles.Remove(weapon);
        submachineGuns.Remove(weapon);
        shotguns.Remove(weapon);
        snipers.Remove(weapon);
        rocketLaunchers.Remove(weapon);
        pistols.Remove(weapon);
    }

    public List<WeaponData> GetWeaponsByType(string type)
    {
        switch (type.ToLower())
        {
            case "assaultrifle": return assaultRifles;
            case "submachinegun": return submachineGuns;
            case "shotgun": return shotguns;
            case "sniper": return snipers;
            case "rocketlauncher": return rocketLaunchers;
            case "pistol": return pistols;
            default: return allWeapons;
        }
    }
}