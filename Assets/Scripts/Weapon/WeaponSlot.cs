using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSlot : MonoBehaviour
{
    public event System.Action<WeaponData> OnWeaponChanged;
    public event System.Action OnWeaponFired;

    [SerializeField] private Transform weaponHolder; 
    [SerializeField] private WeaponData[] weaponSlots = new WeaponData[5]; 
    [SerializeField] private int currentSlotIndex = 0;

    private GameObject currentWeaponObject;
    private WeaponData currentWeapon;
    private int currentClipSize;

    private bool isReloading = false;
    private Dictionary<WeaponData, int> ammoCountPerWeapon = new Dictionary<WeaponData, int>();
    private Dictionary<int, float> weaponCooldowns = new Dictionary<int, float>();
    private Dictionary<int, bool> isWeaponCoolingDown = new Dictionary<int, bool>();
    private Coroutine reloadCoroutine;

    public WeaponData CurrentWeapon => currentWeapon;

    private void Start()
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            weaponCooldowns[i] = 0f;
            isWeaponCoolingDown[i] = false;
        }
        SwitchToSlot(0);
        HUDManager.Instance.PopulateActionBar(weaponSlots);
    }

    private void Update()
    {

        if (!GameManager.Instance.isPaused)
        {
            UpdateWeaponCooldowns();

            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSlot(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToSlot(4);

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

    private void UpdateWeaponCooldowns()
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (isWeaponCoolingDown[i])
            {
                weaponCooldowns[i] -= Time.deltaTime;

                if (weaponCooldowns[i] <= 0)
                {
                    weaponCooldowns[i] = 0;
                    isWeaponCoolingDown[i] = false;
                }
            }
        }
    }

    private bool CanShoot()
    {
        if(GameManager.Instance.abilitySlot.selectingAbilityIndex != -1) return false;

        if (isWeaponCoolingDown[currentSlotIndex]) return false;

        return !isReloading && currentClipSize > 0;
    }

    public bool CanReload()
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
        StartWeaponCooldown(currentSlotIndex);
        OnWeaponFired?.Invoke();

        Transform shootPoint = currentWeaponObject.transform.Find("ShootPoint");

        if (shootPoint == null)
        {
            Debug.LogError("ShootPoint not found on weapon: " + currentWeapon.weaponName);
            return;
        }

        if (currentWeapon.isShotgun && currentWeapon.bulletCount > 1)
        {
            SpreadShot(shootPoint);
        }
        else
        {
            RegularShot(shootPoint);
        }
        currentClipSize--;
        HUDManager.Instance.ConsumeBullet();

        if (currentClipSize <= 0 && CanReload())
            reloadCoroutine = StartCoroutine(Reload());
    }

    private void RegularShot(Transform shootPoint)
    {
        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, shootPoint.position, GameManager.Instance.player.transform.rotation);
        BulletController bulletController = bullet.GetComponent<BulletController>();

        bulletController.damageAmount = currentWeapon.damage;
        bulletController.timeToDestroy = currentWeapon.bulletLifeSpan;

        bulletController.isPiercing = currentWeapon.isPiercing;
        bulletController.piercingDamageReduction = currentWeapon.piercingDamageReduction;
        bulletController.pierceLimit = currentWeapon.pierceLimit;
        bulletController.isExplosive = currentWeapon.isExplosive;
        bulletController.explosionRadius = currentWeapon.explosionRadius;
        bulletController.explosionForce = currentWeapon.explosionForce;

        bullet.transform.forward = GameManager.Instance.player.transform.forward;
    }

    private void SpreadShot(Transform shootPoint)
    {
        float totalSpreadAngle = currentWeapon.maxSpreadAngle * 2;
        float angleStep = totalSpreadAngle / (currentWeapon.bulletCount - 1);

        Vector3 baseDirection = GameManager.Instance.player.transform.forward;
        Quaternion baseRotation = Quaternion.LookRotation(baseDirection);

        for (int i = 0; i < currentWeapon.bulletCount; i++)
        {
            float spreadAngle = -currentWeapon.maxSpreadAngle + (i * angleStep);

            // Create rotation for spread
            Quaternion spreadRotation = baseRotation * Quaternion.Euler(0, spreadAngle, 0);

            // Instantiate bullet with the calculated spread rotation
            GameObject bullet = Instantiate(currentWeapon.bulletPrefab, shootPoint.position, spreadRotation);
            BulletController bulletController = bullet.GetComponent<BulletController>();
            bulletController.damageAmount = currentWeapon.damage;
            bulletController.timeToDestroy = currentWeapon.bulletLifeSpan;
        }
    }

    private void StartWeaponCooldown(int slotIndex)
    {
        if (weaponSlots[slotIndex] != null)
        {
            weaponCooldowns[slotIndex] = weaponSlots[slotIndex].fireRate;
            isWeaponCoolingDown[slotIndex] = true;
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        HUDManager.Instance.StartReloadIndicator(currentWeapon.reloadTime);

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
        UpdateAmmoDisplay();
        HUDManager.Instance.ResetAllBullets();
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
            HUDManager.Instance.CancelReloadIndicator();
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

        HUDManager.Instance.HighlightSlot(slotIndex);
    }

    public float GetWeaponCooldown(int slotIndex)
    {
        if (weaponCooldowns.ContainsKey(slotIndex))
        {
            return weaponCooldowns[slotIndex];
        }
        return 0f;
    }

    public void ResetWeaponCooldowns()
    {
        foreach (var slot in weaponCooldowns.Keys)
        {
            weaponCooldowns[slot] = 0f;
            isWeaponCoolingDown[slot] = false;
        }
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
        OnWeaponChanged?.Invoke(currentWeapon);

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

        HUDManager.Instance.UpdateCurrentWeapon(currentWeapon, currentClipSize, PlayerInventory.Instance.GetAmmo(currentWeapon.weaponId));
    }

    public void AddWeaponToSlot(WeaponData weapon, int slotIndex)
    {
        if (slotIndex >= weaponSlots.Length) return; // Can't modify slot 0 (default weapon)

        weaponSlots[slotIndex] = ScriptableObject.Instantiate(weapon);
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

    private void UpdateAmmoDisplay()
    {
        if (currentWeapon)
        {
            int totalAmmo = PlayerInventory.Instance.GetAmmo(currentWeapon.weaponId);
            HUDManager.Instance.ammoCountText.text = currentWeapon.isInfiniteAmmo ? "\u221E" : totalAmmo.ToString();
        }
    }

    // Additional helper methods as needed
}
