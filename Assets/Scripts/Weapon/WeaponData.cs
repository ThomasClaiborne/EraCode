using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponData: MonoBehaviour
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
    public int bulletCount;
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

