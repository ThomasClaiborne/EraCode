using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using System.IO;

public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    public WeaponInventory weaponInventory;
    public GameObject buttonPrefab;
    public Transform scrollViewContent;
    public TextMeshProUGUI currencyText;

    [Header("Weapon Info")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponLevelText;
    public TextMeshProUGUI weaponStatsText;
    public TextMeshProUGUI weaponTypeText;
    public TextMeshProUGUI weaponModeText;
    public Image weaponIcon;
    public TextMeshProUGUI ammoText;

    [Header("Action Button")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    [Header("Ammo & Upgrade")]
    public Button buyAmmoButton;
    public TextMeshProUGUI buyAmmoButtonText;
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;

    [Header("Level Requirement")]
    public GameObject levelRequirementObject;
    public TextMeshProUGUI levelRequirementText;

    [Header("Ammo Limit")]
    public int ammoLimit = 999;

    [Header("Action Bar")]
    [SerializeField] private ActionBarSlot[] actionBarSlots;
    [SerializeField] private ConfirmationPrompt confirmPrompt;

    [Header("Colors")]
    public Color purchaseColor = Color.green;
    public Color equipColor = Color.cyan;
    public Color equippedColor = Color.gray;
    public Color ammoButtonColor = Color.blue;

    private WeaponData selectedWeapon;
    private enum ActionState { Normal, Purchase, Equip, Equipped, ConfirmEquip }
    private ActionState currentState;

    private Coroutine currentLerpCoroutine;
    private Color originalTextColor;
    private bool isPointerOverAmmoButton = false;
    private bool isPointerOverUpgradeButton = false;



    private void Start()
    {
        PopulateWeaponList();
        SetupAmmoButtonHover();
        SetupUpgradeButtonHover();
        UpdateActionBar();
        UpdateUI();
    }

    private void PopulateWeaponList()
    {
        foreach (var weapon in weaponInventory.allWeapons)
        {
            if (weapon.name != "Pistol")
            {
                GameObject buttonObj = Instantiate(buttonPrefab, scrollViewContent);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                Image image = buttonObj.transform.Find("ICON_Next").GetComponent<Image>();

                buttonText.text = weapon.weaponName;
                image.sprite = weapon.actionBarIcon;
                button.onClick.AddListener(() => SelectWeapon(weapon)); 
            }
        }
    }

    private void UpdateActionBar()
    {
        WeaponData[] equippedWeapons = PlayerInventory.Instance.EquippedWeapons;

        for (int i = 0; i < actionBarSlots.Length; i++)
        {
            if (i < equippedWeapons.Length)
            {
                actionBarSlots[i].SetWeapon(equippedWeapons[i]);
                actionBarSlots[i].SetInteractable(i > 0 && equippedWeapons[i] != null);
            }
            else
            {
                actionBarSlots[i].SetWeapon(null);
                actionBarSlots[i].SetInteractable(false);
            }
        }
    }

    public void OnActionBarSlotClicked(int slotIndex)
    {
        if (currentState == ActionState.ConfirmEquip)
        {
            if (slotIndex >= 1 && slotIndex <= 4)
            {
                EquipWeapon(slotIndex);
                currentState = ActionState.Normal;
                SetActionBarSlotsHighlighted(false);
                UpdateUI();
            }
        }
        else if (slotIndex > 0 && PlayerInventory.Instance.EquippedWeapons[slotIndex] != null)
        {
            WeaponData weaponToUnequip = PlayerInventory.Instance.EquippedWeapons[slotIndex];
            confirmPrompt.Show(
                $"Unequip {weaponToUnequip.weaponName} from slot {slotIndex + 1}?",
                () => UnequipWeapon(slotIndex),
                UpdateUI
            );
        }
    }

    private void UnequipWeapon(int slotIndex)
    {
        PlayerInventory.Instance.UnequipWeapon(slotIndex);
        UpdateActionBar();
        UpdateUI();
    }

    private void SetActionBarSlotsHighlighted(bool highlighted)
    {
        for (int i = 1; i < actionBarSlots.Length; i++)
        {
            actionBarSlots[i].SetHighlighted(highlighted && currentState == ActionState.ConfirmEquip);
            actionBarSlots[i].SetInteractable(highlighted || (PlayerInventory.Instance.EquippedWeapons[i] != null));
        }
    }

    private void SetupAmmoButtonHover()
    {
        EventTrigger trigger = buyAmmoButton.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnAmmoButtonHoverEnter(); });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnAmmoButtonHoverExit(); });
        trigger.triggers.Add(exitEntry);
    }

    private void SetupUpgradeButtonHover()
    {
        EventTrigger trigger = upgradeButton.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnUpgradeButtonHoverEnter(); });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnUpgradeButtonHoverExit(); });
        trigger.triggers.Add(exitEntry);
    }

    private void SelectWeapon(WeaponData weapon)
    {
        selectedWeapon = weapon;
        currentState = ActionState.Normal;
        UpdateUI();
    }

    public void UpdateUI()
    {
        currencyText.text = $"{PlayerInventory.Instance.Currency}";

        if (selectedWeapon != null)
        {
            weaponNameText.text = $"Name: {selectedWeapon.weaponName}";
            int currentLevel = PlayerInventory.Instance.GetWeaponLevel(selectedWeapon.weaponId);
            weaponLevelText.text = $"Level: {currentLevel}";

            UpdateWeaponStats();

            bool owned = PlayerInventory.Instance.OwnsWeapon(selectedWeapon);
            bool equipped = IsWeaponEquipped(selectedWeapon);
            bool meetsLevelRequirement = PlayerInventory.Instance.LevelSystem.Level >= selectedWeapon.requiredLevel;

            actionButton.gameObject.SetActive(true);
            if (!owned)
            {
                SetActionButtonState(ActionState.Purchase, true);
                ToggleAmmoButton(false);
                ToggleUpgradeButton(false);
                ShowLevelRequirement(!meetsLevelRequirement, selectedWeapon.requiredLevel);
            }
            else if (!equipped)
            {
                SetActionButtonState(ActionState.Equip, true);
                UpdateAmmoButton();
                UpdateUpgradeButton();
                ShowLevelRequirement(false, 0);
            }
            else
            {
                SetActionButtonState(ActionState.Equipped, false);
                UpdateAmmoButton();
                UpdateUpgradeButton();
                ShowLevelRequirement(false, 0);
            }
        }
        else
        {
            ClearWeaponInfo();
            ToggleAmmoButton(false);
            ToggleUpgradeButton(false);
            ShowLevelRequirement(false, 0);
        }
    }

    private void UpdateWeaponStats()
    {
        int damage = PlayerInventory.Instance.GetWeaponDamage(selectedWeapon.weaponId);
        float fireRate = PlayerInventory.Instance.GetWeaponFireRate(selectedWeapon.weaponId);
        int clipSize = PlayerInventory.Instance.GetWeaponClipSize(selectedWeapon.weaponId);
        float reloadTime = PlayerInventory.Instance.GetWeaponReloadTime(selectedWeapon.weaponId);

        weaponStatsText.text = $"Damage: {damage}\n" +
                               $"Fire Rate: {fireRate:F2}\n" +
                               $"Clip Size: {clipSize}\n" +
                               $"Reload Time: {reloadTime:F2}";
        weaponIcon.sprite = selectedWeapon.weaponIcon;
        weaponIcon.gameObject.SetActive(true);
    }

    private void UpdateAmmoButton()
    {
        ToggleAmmoButton(true);

        int currentAmmo = PlayerInventory.Instance.GetAmmo(selectedWeapon.weaponId);
        ammoText.text = $"Ammo: {currentAmmo}";
        if (currentAmmo >= ammoLimit)
        {
            buyAmmoButtonText.text = "MAX AMMO";
            buyAmmoButton.interactable = false;
            return;
        }
        else
        {
            buyAmmoButton.interactable = true;
        }

        buyAmmoButtonText.text = $"Buy Ammo +{selectedWeapon.clipSize}";
        buyAmmoButton.onClick.RemoveAllListeners();
        buyAmmoButton.onClick.AddListener(OnBuyAmmoClicked);
        buyAmmoButton.GetComponent<Image>().color = ammoButtonColor;

        if (isPointerOverAmmoButton)
        {
            buyAmmoButtonText.text = $"Price: ${selectedWeapon.ammoPrice}";
        }
        else
        {
            buyAmmoButtonText.text = $"Buy Ammo +{PlayerInventory.Instance.GetWeaponClipSize(selectedWeapon.weaponId)}";
        }
    }

    private void ToggleAmmoButton(bool active)
    {
        buyAmmoButton.gameObject.SetActive(active);
        ammoText.gameObject.SetActive(active);
    }

    private void OnAmmoButtonHoverEnter()
    {
        isPointerOverAmmoButton = true;

        int currentAmmo = PlayerInventory.Instance.GetAmmo(selectedWeapon.weaponId);
        if (currentAmmo >= 999)
        {
            buyAmmoButtonText.text = "MAX AMMO";
            buyAmmoButton.interactable = false;
            return;
        }

        buyAmmoButtonText.text = $"Price: ${selectedWeapon.ammoPrice}";
    }

    private void OnAmmoButtonHoverExit()
    {
        isPointerOverAmmoButton = false;

        int currentAmmo = PlayerInventory.Instance.GetAmmo(selectedWeapon.weaponId);
        if (currentAmmo >= 999)
        {
            buyAmmoButtonText.text = "MAX AMMO";
            buyAmmoButton.interactable = false;
            return;
        }

        buyAmmoButtonText.text = $"Buy Ammo +{PlayerInventory.Instance.GetWeaponClipSize(selectedWeapon.weaponId)}";
    }

    private void UpdateUpgradeButton()
    {
        ToggleUpgradeButton(true);
        int currentLevel = PlayerInventory.Instance.GetWeaponLevel(selectedWeapon.weaponId);

        if (currentLevel >= selectedWeapon.maxLevel)
        {
            upgradeButtonText.text = "MAX LEVEL";
            upgradeButton.interactable = false;
            return;
        }

        upgradeButtonText.text = $"Upgrade to level ({currentLevel + 1})";
        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        upgradeButton.interactable = selectedWeapon.currentLevel < selectedWeapon.maxLevel;

        if(isPointerOverUpgradeButton)
        {
            OnUpgradeButtonHoverEnter();
        }
    }

        private void ToggleUpgradeButton(bool active)
    {
        upgradeButton.gameObject.SetActive(active);
    }

    private void OnUpgradeClicked()
    {
        int upgradeCost = PlayerInventory.Instance.GetWeaponUpgradeCost(selectedWeapon.weaponId);
        if (PlayerInventory.Instance.SpendCurrency(upgradeCost))
        {
            PlayerInventory.Instance.UpgradeWeapon(selectedWeapon.weaponId);
            UpdateUI();
        }
        else
        {
            StartColorLerp(upgradeButtonText, Color.red, 0.3f);
        }
    }

    private void OnUpgradeButtonHoverEnter()
    {
        isPointerOverUpgradeButton = true;

        int currentLevel = PlayerInventory.Instance.GetWeaponLevel(selectedWeapon.weaponId);
        if (currentLevel >= selectedWeapon.maxLevel)
        {
            upgradeButtonText.text = "MAX LEVEL";
            upgradeButton.interactable = false;
            return;
        }

        int currentDamage = PlayerInventory.Instance.GetWeaponDamage(selectedWeapon.weaponId);
        float currentFireRate = PlayerInventory.Instance.GetWeaponFireRate(selectedWeapon.weaponId);
        int currentClipSize = PlayerInventory.Instance.GetWeaponClipSize(selectedWeapon.weaponId);
        float currentReloadTime = PlayerInventory.Instance.GetWeaponReloadTime(selectedWeapon.weaponId);

        weaponStatsText.text = $"Damage: {currentDamage} -> {PlayerInventory.Instance.GetNextLevelWeaponDamage(selectedWeapon.weaponId)}\n" +
                               $"Fire Rate: {currentFireRate:F2} -> {PlayerInventory.Instance.GetNextLevelWeaponFireRate(selectedWeapon.weaponId) :F2}\n" +
                               $"Clip Size: {currentClipSize} -> {PlayerInventory.Instance.GetNextLevelWeaponClipSize(selectedWeapon.weaponId)}\n" +
                               $"Reload Time: {currentReloadTime:F2} -> {PlayerInventory.Instance.GetNextLevelWeaponReloadTime(selectedWeapon.weaponId):F2}";

        upgradeButtonText.text = $"Price: ${PlayerInventory.Instance.GetWeaponUpgradeCost(selectedWeapon.weaponId)}";
    }

    private void OnUpgradeButtonHoverExit()
    {
        isPointerOverUpgradeButton = false;

        int currentLevel = PlayerInventory.Instance.GetWeaponLevel(selectedWeapon.weaponId);
        if (currentLevel >= selectedWeapon.maxLevel)
        {
            upgradeButtonText.text = "MAX LEVEL";
            upgradeButton.interactable = false;
            return;
        }

        UpdateWeaponStats();
        upgradeButtonText.text = $"Upgrade to level ({PlayerInventory.Instance.GetWeaponLevel(selectedWeapon.weaponId) + 1})";
    }

    private void ShowLevelRequirement(bool show, int requiredLevel)
    {
        levelRequirementObject.SetActive(show);
        if (show)
        {
            levelRequirementText.text = $"Need to be Level {requiredLevel} to purchase";
            actionButton.interactable = false;
            actionButton.GetComponent<Image>().color = Color.gray;
        }

    }

    private void ClearWeaponInfo()
    {
        weaponNameText.text = "Select a weapon";
        weaponStatsText.text = "";
        weaponTypeText.text = "";
        weaponModeText.text = "";
        actionButton.gameObject.SetActive(false);
        weaponIcon.sprite = null;
        weaponIcon.gameObject.SetActive(false);
    }

    private void SetActionButtonState(ActionState state, bool interactable)
    {
        currentState = state;
        actionButton.interactable = interactable;

        switch (state)
        {
            case ActionState.Purchase:
                actionButtonText.text = $"Purchase ({selectedWeapon.weaponPrice})";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnPurchaseClicked);
                actionButton.GetComponent<Image>().color = interactable ? purchaseColor : Color.red;
                break;
            case ActionState.Equip:
                actionButtonText.text = "Equip";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnEquipClicked);
                actionButton.GetComponent<Image>().color = equipColor;
                break;
            case ActionState.Equipped:
                actionButtonText.text = "Equipped";
                actionButton.onClick.RemoveAllListeners();
                actionButton.GetComponent<Image>().color = equippedColor;
                break;
            case ActionState.ConfirmEquip:
                actionButtonText.text = "Cancel";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(CancelAction);
                actionButton.GetComponent<Image>().color = Color.red;
                break;
        }
    }

    private void OnPurchaseClicked()
    {
        if (PlayerInventory.Instance.Currency >= selectedWeapon.weaponPrice)
        {
            confirmPrompt.Show(
                $"Purchase {selectedWeapon.weaponName} for {selectedWeapon.weaponPrice} currency?",
                ConfirmPurchase,
                CancelAction
            );
        }
        else
        {
            Debug.Log("Not enough currency to purchase weapon.");
            StartColorLerp(actionButtonText, Color.red, 0.3f);
        }
    }

    private void OnBuyAmmoClicked()
    {
        int currentAmmo = PlayerInventory.Instance.GetAmmo(selectedWeapon.weaponId);
        int clipSize = PlayerInventory.Instance.GetWeaponClipSize(selectedWeapon.weaponId);

        if (PlayerInventory.Instance.SpendCurrency(selectedWeapon.ammoPrice))
        {
            if(currentAmmo + clipSize > ammoLimit)
            {
                PlayerInventory.Instance.AddAmmo(selectedWeapon.weaponId, ammoLimit - currentAmmo);
            }
            else
            {
                PlayerInventory.Instance.AddAmmo(selectedWeapon.weaponId, clipSize);
            }
            UpdateUI();
            OnAmmoButtonHoverEnter();
        }
        else
        {
            Debug.Log("Not enough currency to purchase ammo.");
            StartColorLerp(buyAmmoButtonText, Color.red, 0.3f);
        }
    }

    private void OnEquipClicked()
    {
        currentState = ActionState.ConfirmEquip;
        SetActionButtonState(ActionState.ConfirmEquip, true);
        SetActionBarSlotsHighlighted(true);
    }

    private string GetWeaponType(WeaponData weapon)
    {
        if (weapon.isAssaultRifle) return "Assault Rifle";
        if (weapon.isSubmachineGun) return "Submachine Gun";
        if (weapon.isShotgun) return "Shotgun";
        if (weapon.isSniper) return "Sniper";
        if (weapon.isRocketLauncher) return "Rocket Launcher";
        return "Unknown";
    }

    private string GetWeaponMode(WeaponData weapon)
    {
        if (weapon.isAutomatic) return "Automatic";
        if (weapon.isBurst) return "Burst";
        return "Semi-Automatic";
    }

    private bool IsWeaponEquipped(WeaponData weapon)
    {
        WeaponData[] equippedWeapons = PlayerInventory.Instance.EquippedWeapons;
        for (int i = 1; i <= 4; i++)
        {
            if (equippedWeapons[i] == weapon)
                return true;
        }
        return false;
    }

    private void ConfirmPurchase()
    {
        if (PlayerInventory.Instance.SpendCurrency(selectedWeapon.weaponPrice))
        {
            PlayerInventory.Instance.AddWeapon(selectedWeapon);
            currentState = ActionState.Normal;
            SetActionButtonState(ActionState.Equip, true);
        }
        else
        {
            Debug.Log("Not enough currency to purchase weapon.");
            CancelAction();
        }
        UpdateUI();
    }

    private void EquipWeapon(int slot)
    {
        PlayerInventory.Instance.EquipWeapon(selectedWeapon, slot);
        UpdateActionBar();
        UpdateUI();

    }

    private void CancelAction()
    {
        currentState = ActionState.Normal;
        SetActionBarSlotsHighlighted(false);
        UpdateUI();
    }

    private void StartColorLerp(TextMeshProUGUI textElement, Color targetColor, float duration)
    {
        if (currentLerpCoroutine != null)
        {
            StopCoroutine(currentLerpCoroutine);
            textElement.color = originalTextColor;
        }

        if (originalTextColor == Color.clear)
        {
            originalTextColor = textElement.color;
        }
        currentLerpCoroutine = StartCoroutine(LerpTextColor(textElement, targetColor, duration));
    }

    private IEnumerator LerpTextColor(TextMeshProUGUI textElement, Color targetColor, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            textElement.color = Color.Lerp(originalTextColor, targetColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textElement.color = targetColor;

        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            textElement.color = Color.Lerp(targetColor, originalTextColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textElement.color = originalTextColor;

        currentLerpCoroutine = null;
    }
}