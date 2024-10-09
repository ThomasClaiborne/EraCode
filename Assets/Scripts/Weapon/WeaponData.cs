using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponData: MonoBehaviour
{
    public string weaponName;
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
    public bool isAutomatic;
    public bool isShotgun;
    public bool isSniper;
    public bool isInfiniteAmmo;
}
