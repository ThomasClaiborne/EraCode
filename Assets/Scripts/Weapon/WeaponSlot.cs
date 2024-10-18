using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSlot : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder; 
    //[SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private WeaponData[] weaponSlots = new WeaponData[3]; 
    [SerializeField] private int currentSlotIndex = 0;

    private GameObject currentWeaponObject;
    private WeaponData currentWeapon;
    private int currentClipSize;

    private bool canShoot = true;
    private bool isShooting = false;
    private bool isReloading = false;
    private Dictionary<WeaponData, int> ammoCountPerWeapon = new Dictionary<WeaponData, int>();
    private Coroutine reloadCoroutine;
    private Coroutine reloadVisualsCoroutine;

    private void Start()
    {
        //weaponSlots[0] = defaultWeapon;
        SwitchToSlot(0);
        UpdateHUD();
    }

    private void Update()
    {

        if (!GameManager.Instance.isPaused)
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
                reloadCoroutine = StartCoroutine(Reload());
            } 
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
        if (isReloading || currentClipSize >= currentWeapon.clipSize)
            return false;

        if (currentWeapon.isInfiniteAmmo)
            return true;

        int totalAmmo = PlayerInventory.Instance.GetAmmo(currentWeapon.weaponId);
        return totalAmmo > 0;
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

            bulletController.isPiercing = currentWeapon.isPiercing;
            bulletController.piercingDamageReduction = currentWeapon.piercingDamageReduction;
            bulletController.pierceLimit = currentWeapon.pierceLimit;
            bulletController.isExplosive = currentWeapon.isExplosive;
            bulletController.explosionRadius = currentWeapon.explosionRadius;
            bulletController.explosionForce = currentWeapon.explosionForce;

            bullet.transform.forward = shootPoint.forward;
        }
        currentClipSize--;
        UpdateHUD();
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
        reloadVisualsCoroutine = StartCoroutine(reloadingVisuals());

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        if (!isReloading) yield break;

        if (currentWeapon.isInfiniteAmmo)
        {
            currentClipSize = currentWeapon.clipSize;
        }
        else
        {
            int availableAmmo = PlayerInventory.Instance.GetAmmo(currentWeapon.weaponId);

            if (availableAmmo > 0)
            {
                int ammoToReload = Mathf.Min(currentWeapon.clipSize - currentClipSize, availableAmmo);
                currentClipSize += ammoToReload;
                PlayerInventory.Instance.AddAmmo(currentWeapon.weaponId, -ammoToReload);
            }
            else
            {
                CanReload();
                yield break;
            }
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

    private void CancelReload()
    {
        if (isReloading)
        {
            isReloading = false;
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }
            if (reloadVisualsCoroutine != null)
            {
                StopCoroutine(reloadVisualsCoroutine);
                reloadVisualsCoroutine = null;
            }
            HUDManager.Instance.weaponReloading.text = "";
        }
    }


    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;
        if (weaponSlots[slotIndex] == null && slotIndex != 0)
        {
            ShowEmptySlotMessage(slotIndex);
            return;
        }

        CancelReload();

        currentSlotIndex = slotIndex;
        EquipWeapon(weaponSlots[slotIndex]);
    }

    private void ShowEmptySlotMessage(int slotIndex)
    {
        string message = $"Slot {slotIndex + 1} is empty";
        HUDManager.Instance.messageText.text = message;
        HUDManager.Instance.TriggerTextLerp(HUDManager.Instance.messageText, Color.red, 0.5f);
        StartCoroutine(ClearMessageAfterDelay(1.0f));
    }

    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HUDManager.Instance.messageText.text = "";
    }

    private void EquipWeapon(WeaponData newWeapon)
    {
        if (currentWeaponObject != null) Destroy(currentWeaponObject);

        if (currentWeapon != null) ammoCountPerWeapon[currentWeapon] = currentClipSize;

        currentWeapon = ScriptableObject.Instantiate(newWeapon);

        if (ammoCountPerWeapon.TryGetValue(currentWeapon, out int savedClipSize))
        {
            currentClipSize = savedClipSize;
        }
        else
        {
            currentClipSize = PlayerInventory.Instance.GetWeaponClipSize(newWeapon.weaponId);
        }

        currentWeaponObject = Instantiate(currentWeapon.weaponModel, weaponHolder);
        currentWeaponObject.transform.localPosition = currentWeapon.holdPosition;
        currentWeaponObject.transform.localRotation = Quaternion.Euler(currentWeapon.holdRotation);

        currentWeapon.damage = PlayerInventory.Instance.GetWeaponDamage(newWeapon.weaponId);
        currentWeapon.fireRate = PlayerInventory.Instance.GetWeaponFireRate(newWeapon.weaponId);
        currentWeapon.reloadTime = PlayerInventory.Instance.GetWeaponReloadTime(newWeapon.weaponId);
        currentWeapon.clipSize = PlayerInventory.Instance.GetWeaponClipSize(newWeapon.weaponId);

        UpdateHUD();
    }

    public void AddWeaponToSlot(WeaponData weapon, int slotIndex)
    {
        if (slotIndex >= weaponSlots.Length) return; // Can't modify slot 0 (default weapon)
        weaponSlots[slotIndex] = ScriptableObject.Instantiate(weapon);
        Debug.Log("Weapon name after Scriptable: " + weaponSlots[slotIndex].weaponName);
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
        if (currentWeapon)
        {
            HUDManager.Instance.weaponNameText.text = currentWeapon.weaponName;
            int totalAmmo = PlayerInventory.Instance.GetAmmo(currentWeapon.weaponId);
            HUDManager.Instance.weaponAmmoText.text = $"{currentClipSize} / {(currentWeapon.isInfiniteAmmo ? "\u221E" : totalAmmo.ToString())}";
        }
    }

    // Additional helper methods as needed
}
