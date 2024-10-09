using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSlot : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder; 
    [SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private WeaponData[] weaponSlots = new WeaponData[3]; 
    [SerializeField] private int currentSlotIndex = 0;

    private GameObject currentWeaponObject;
    private WeaponData currentWeapon;
    private int currentClipSize;

    private bool canShoot = true;
    private bool isShooting = false;
    private bool isReloading = false;
    private Dictionary<WeaponData, int> ammoCountPerWeapon = new Dictionary<WeaponData, int>();

    private void Start()
    {
        weaponSlots[0] = defaultWeapon;
        SwitchToSlot(0);
        UpdateHUD();
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);

        if (currentWeapon.isAutomatic)
        {
            if (Input.GetMouseButton(0) && CanShoot())
            {
                Shoot();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && CanShoot())
            {
                Shoot();
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && CanReload())
        {
            StartCoroutine(Reload());
        }
    }

    private bool CanShoot()
    {
        if (currentClipSize <= 0)
            HUDManager.Instance.TriggerTextLerp(HUDManager.Instance.weaponAmmoText, Color.red, 0.2f);

        return canShoot && !isReloading && currentClipSize > 0 &&
               (currentWeapon.isAutomatic || (!currentWeapon.isAutomatic && !isShooting));
    }

    private bool CanReload()
    {
        return !isReloading && currentClipSize < currentWeapon.clipSize &&
               (currentWeapon.ammo > 0 || currentWeapon.isInfiniteAmmo);
    }

    private void Shoot()
    {
        isShooting = true;

        Transform shootPoint = currentWeaponObject.transform.Find("ShootPoint");

        if (shootPoint == null)
        {
            Debug.LogError("ShootPoint not found on weapon: " + currentWeapon.weaponName);
            return;
        }

        if (currentWeapon.isShotgun && currentWeapon.bulletCount > 1)
        {
            float totalSpreadAngle = currentWeapon.maxSpreadAngle * 2; 
            float angleStep = totalSpreadAngle / (currentWeapon.bulletCount - 1);

            for (int i = 0; i < currentWeapon.bulletCount; i++)
            {
                GameObject bullet = Instantiate(currentWeapon.bulletPrefab, shootPoint.position, shootPoint.rotation);
                BulletController bulletController = bullet.GetComponent<BulletController>();

                bulletController.damageAmount = currentWeapon.damage;
                bulletController.timeToDestroy = currentWeapon.bulletLifeSpan;

                float spreadAngle = -currentWeapon.maxSpreadAngle + (i * angleStep);
                Quaternion spreadRotation = Quaternion.Euler(0, spreadAngle, 0);

                Vector3 spreadDirection = spreadRotation * shootPoint.forward;
                bullet.transform.forward = spreadDirection;
            }
        }
        else
        {
            GameObject bullet = Instantiate(currentWeapon.bulletPrefab, shootPoint.position, shootPoint.rotation);
            BulletController bulletController = bullet.GetComponent<BulletController>();

            bulletController.damageAmount = currentWeapon.damage;
            bulletController.timeToDestroy = currentWeapon.bulletLifeSpan;

            bullet.transform.forward = shootPoint.forward;
        }
        currentClipSize--;
        // Visual and audio effects
        //Instantiate(currentWeapon.muzzleFlash, currentWeapon.shootPoint.position, currentWeapon.shootPoint.rotation);
        //if (currentWeapon.ejectBullet)
        //{
        //    Instantiate(currentWeapon.ejectBullet, currentWeapon.ejectPoint.position, currentWeapon.ejectPoint.rotation);
        //}
        //PlayRandomShotSound();

        // Ammo management

        UpdateHUD();
        // Apply recoil
        //ApplyRecoil();

        // Start cooldown
        StartCoroutine(ShootCooldown());
    }

    private IEnumerator ShootCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(currentWeapon.fireRate);
        canShoot = true;
        isShooting = false;
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        // Play reload animation and audio
        //PlayReloadAudio();
        StartCoroutine(reloadingVisuals());

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        if (currentWeapon.isInfiniteAmmo)
        {
            currentClipSize = currentWeapon.clipSize;
        }
        else
        {
            int ammoToReload = Mathf.Min(currentWeapon.clipSize - currentClipSize, currentWeapon.ammo);
            currentClipSize += ammoToReload;
            currentWeapon.ammo -= ammoToReload;
        }

        isReloading = false;
        UpdateHUD();
    }

    IEnumerator reloadingVisuals()
    {
        HUDManager.Instance.weaponReloading.text = "Reloading";
        yield return new WaitForSeconds(currentWeapon.reloadTime / 4);
        HUDManager.Instance.weaponReloading.text = "Reloading.";
        yield return new WaitForSeconds(currentWeapon.reloadTime / 4);
        HUDManager.Instance.weaponReloading.text = "Reloading..";
        yield return new WaitForSeconds(currentWeapon.reloadTime / 4);
        HUDManager.Instance.weaponReloading.text = "Reloading...";
        yield return new WaitForSeconds(currentWeapon.reloadTime / 4);
        HUDManager.Instance.weaponReloading.text = "";
    }


    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;
        if (weaponSlots[slotIndex] == null && slotIndex != 0) return; // Don't switch to empty slots, except slot 0

        currentSlotIndex = slotIndex;
        EquipWeapon(weaponSlots[slotIndex]);
    }

    private void EquipWeapon(WeaponData newWeapon)
    {
        if (currentWeaponObject != null) Destroy(currentWeaponObject);

        if (currentWeapon != null) ammoCountPerWeapon[currentWeapon] = currentClipSize;

        currentWeapon = newWeapon;
        currentClipSize = ammoCountPerWeapon.ContainsKey(currentWeapon) ?
                          ammoCountPerWeapon[currentWeapon] : currentWeapon.clipSize;

        currentWeaponObject = Instantiate(currentWeapon.weaponModel, weaponHolder);
        currentWeaponObject.transform.localPosition = currentWeapon.holdPosition;
        currentWeaponObject.transform.localRotation = Quaternion.Euler(currentWeapon.holdRotation);

        UpdateHUD();
    }

    public void AddWeaponToSlot(WeaponData weapon, int slotIndex)
    {
        if (slotIndex <= 0 || slotIndex >= weaponSlots.Length) return; // Can't modify slot 0 (default weapon)
        weaponSlots[slotIndex] = weapon;
        UpdateHUD();
    }

    private void PlayRandomShotSound()
    {
        if (currentWeapon.shotAudioClips.Count > 0)
        {
            string clipName = currentWeapon.shotAudioClips[UnityEngine.Random.Range(0, currentWeapon.shotAudioClips.Count)];
            //AudioManager.instance.PlaySFX(clipName);
        }
    }

    private void PlayReloadAudio()
    {
        // Play reload audio clips in sequence or randomly
    }

    private void ApplyRecoil()
    {
        // Apply recoil effect based on currentWeapon.recoil
    }

    private void UpdateHUD()
    {
        HUDManager.Instance.weaponNameText.text = currentWeapon.weaponName;
        HUDManager.Instance.weaponAmmoText.text = currentClipSize + " / " + (currentWeapon.isInfiniteAmmo ? "\u221E" : currentWeapon.ammo.ToString());
    }

    // Additional helper methods as needed
}
