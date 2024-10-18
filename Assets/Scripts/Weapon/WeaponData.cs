using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData: ScriptableObject
{
    [Header("Weapon Info")]
    public string weaponName;
    public string weaponId;
    public GameObject bulletPrefab;
    public GameObject weaponModel;
    public GameObject muzzleFlash;
    public GameObject ejectBullet;
    public Transform ejectPoint;
    public Vector3 holdPosition;
    public Vector3 holdRotation;
    public int damage;
    public int clipSize;
    public int ammo;
    public float reloadTime;
    public float fireRate;
    public float bulletLifeSpan;
    public float maxSpreadAngle;
    public int bulletCount = 1;
    public float recoil;
    public List<string> shotAudioClips;
    public List<string> reloadAudioClips;

    [Header("Weapon Shoot Mode")]
    public bool isAutomatic;
    public bool isBurst;
    public bool isInfiniteAmmo;


    [Header("Weapon Type")]
    public bool isAssaultRifle;
    public bool isSubmachineGun;
    public bool isShotgun;
    public bool isSniper;
    public bool isRocketLauncher;
    public bool isPistol;

    [Header("Weapon Type Specifics")]
    public bool isPiercing;
    public float piercingDamageReduction;
    public int pierceLimit;
    public bool isExplosive;
    public float explosionRadius;
    public float explosionForce;

    [Header("Shop Info")]
    public int weaponPrice;
    public int ammoPrice;
    public int upgradeCost = 100;

    [Header("Leveling and Upgrades")]
    public int requiredLevel = 1;
    public int currentLevel = 1;
    public int maxLevel = 10;
    public float upgradeCostMultiplier = 1.5f;

    [Header("Upgrade Multipliers")]
    public float damageUpgradeMultiplier = 1.1f;
    public float fireRateUpgradeMultiplier = 0.95f;
    public float reloadTimeUpgradeMultiplier = 0.95f;
    public float clipSizeUpgradeMultiplier = 1.1f;

    public int GetCurrentLevelDamage(int level) => Mathf.RoundToInt(damage * Mathf.Pow(damageUpgradeMultiplier, level - 1));
    public float GetCurrentLevelFireRate(int level) => fireRate * Mathf.Pow(fireRateUpgradeMultiplier, level - 1);
    public float GetCurrentLevelReloadTime(int level) => reloadTime * Mathf.Pow(reloadTimeUpgradeMultiplier, level - 1);
    public int GetCurrentLevelClipSize(int level) => Mathf.RoundToInt(clipSize * Mathf.Pow(clipSizeUpgradeMultiplier, level - 1));

    public int GetNextLevelDamage() => Mathf.RoundToInt(damage * Mathf.Pow(damageUpgradeMultiplier, currentLevel));
    public float GetNextLevelFireRate() => fireRate * Mathf.Pow(fireRateUpgradeMultiplier, currentLevel);
    public float GetNextLevelReloadTime() => reloadTime * Mathf.Pow(reloadTimeUpgradeMultiplier, currentLevel);
    public int GetNextLevelClipSize() => Mathf.RoundToInt(clipSize * Mathf.Pow(clipSizeUpgradeMultiplier, currentLevel));

    public int GetUpgradeCost(int level) => Mathf.RoundToInt(upgradeCost * Mathf.Pow(upgradeCostMultiplier, level - 1));

    public void UpgradeWeapon()
    {
        if (currentLevel >= maxLevel) return;

        currentLevel++;

        damage = Mathf.RoundToInt(damage * damageUpgradeMultiplier);
        fireRate *= fireRateUpgradeMultiplier;
        reloadTime *= reloadTimeUpgradeMultiplier;
        clipSize = Mathf.RoundToInt(clipSize * clipSizeUpgradeMultiplier);
    }

    public bool CanUpgrade()
    {
        return currentLevel < maxLevel;
    }

    public (int damage, float fireRate, float reloadTime, int clipSize) GetNextLevelStats()
    {
        return (
            Mathf.RoundToInt(damage * damageUpgradeMultiplier),
            fireRate * fireRateUpgradeMultiplier,
            reloadTime * reloadTimeUpgradeMultiplier,
            Mathf.RoundToInt(clipSize * clipSizeUpgradeMultiplier)
        );
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WeaponData);
    }

    public bool Equals(WeaponData other)
    {
        if (other is null)
            return false;

        return this.weaponId == other.weaponId;
    }

    public override int GetHashCode()
    {
        return (weaponId != null ? weaponId.GetHashCode() : 0);
    }

    public static bool operator ==(WeaponData left, WeaponData right)
    {
        if (ReferenceEquals(left, null))
        {
            return ReferenceEquals(right, null);
        }

        return left.Equals(right);
    }

    public static bool operator !=(WeaponData left, WeaponData right)
    {
        return !(left == right);
    }
}

