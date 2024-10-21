using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using System.IO;

public class WeaponShop : MonoBehaviour
{
    public WeaponInventory weaponInventory;
    public GameObject buttonPrefab;
    public Transform scrollViewContent;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponLevelText;
    public TextMeshProUGUI weaponStatsText;
    public TextMeshProUGUI weaponTypeText;
    public TextMeshProUGUI weaponModeText;
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI ammoText;

    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public Button optionButton1;
    public Button optionButton2;
    public TextMeshProUGUI optionButton1Text;
    public TextMeshProUGUI optionButton2Text;
    public Button buyAmmoButton;
    public TextMeshProUGUI buyAmmoButtonText;

    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    public GameObject levelRequirementObject;
    public TextMeshProUGUI levelRequirementText;

    public Color purchaseColor = Color.green;
    public Color equipColor = Color.cyan;
    public Color equippedColor = Color.gray;
    public Color ammoButtonColor = Color.blue;


    private WeaponData selectedWeapon;
    private enum ActionState { Purchase, Equip, Equipped, ConfirmPurchase, ConfirmEquip }
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

                buttonText.text = weapon.weaponName;
                button.onClick.AddListener(() => SelectWeapon(weapon)); 
            }
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
        UpdateUI();
    }

    public void UpdateUI()
    {
        currencyText.text = $"Currency: {PlayerInventory.Instance.Currency}";

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
    }

    private void UpdateAmmoButton()
    {
        ToggleAmmoButton(true);
        int currentAmmo = PlayerInventory.Instance.GetAmmo(selectedWeapon.weaponId);
        buyAmmoButtonText.text = $"Buy Ammo +{selectedWeapon.clipSize}";
        ammoText.text = $"Ammo: {currentAmmo}";
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
        buyAmmoButtonText.text = $"Price: ${selectedWeapon.ammoPrice}";
    }

    private void OnAmmoButtonHoverExit()
    {
        isPointerOverAmmoButton = false;
        buyAmmoButtonText.text = $"Buy Ammo +{PlayerInventory.Instance.GetWeaponClipSize(selectedWeapon.weaponId)}";
    }

    private void UpdateUpgradeButton()
    {
        ToggleUpgradeButton(true);
        int upgradeCost = PlayerInventory.Instance.GetWeaponUpgradeCost(selectedWeapon.weaponId);
        upgradeButtonText.text = $"Upgrade to level ({PlayerInventory.Instance.GetWeaponLevel(selectedWeapon.weaponId) + 1})";
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
            //selectedWeapon.UpgradeWeapon();
            UpdateUI();
        }
        else
        {
            Debug.Log("Not enough currency to upgrade weapon.");
            StartColorLerp(upgradeButtonText, Color.red, 0.3f);
        }
    }

    private void OnUpgradeButtonHoverEnter()
    {
        isPointerOverUpgradeButton = true;

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
        HideOptionButtons();
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
            case ActionState.ConfirmPurchase:
                actionButtonText.text = "Confirm Purchase?";
                actionButton.onClick.RemoveAllListeners();
                actionButton.GetComponent<Image>().color = Color.yellow;
                break;
            case ActionState.ConfirmEquip:
                actionButtonText.text = "Cancel";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(CancelAction);
                actionButton.GetComponent<Image>().color = Color.red;
                break;
        }

        SetOptionButtonsState(state);
    }

    private void SetOptionButtonsState(ActionState state)
    {
        switch (state)
        {
            case ActionState.ConfirmPurchase:
                ShowOptionButtons("Yes", "No", ConfirmPurchase, CancelAction, Color.green, Color.red);
                break;
            case ActionState.ConfirmEquip:
                ShowOptionButtons("Slot 2", "Slot 3", () => EquipWeapon(1), () => EquipWeapon(2), equipColor, equipColor);
                break;
            default:
                HideOptionButtons();
                break;
        }
    }

    private void ShowOptionButtons(string text1, string text2, UnityEngine.Events.UnityAction action1, UnityEngine.Events.UnityAction action2, Color color1, Color color2)
    {
        optionButton1.gameObject.SetActive(true);
        optionButton2.gameObject.SetActive(true);
        optionButton1Text.text = text1;
        optionButton2Text.text = text2;
        optionButton1.onClick.RemoveAllListeners();
        optionButton2.onClick.RemoveAllListeners();
        optionButton1.onClick.AddListener(action1);
        optionButton2.onClick.AddListener(action2);
        optionButton1.GetComponent<Image>().color = color1;
        optionButton2.GetComponent<Image>().color = color2;
    }

    private void HideOptionButtons()
    {
        optionButton1.gameObject.SetActive(false);
        optionButton2.gameObject.SetActive(false);
    }

    private void OnPurchaseClicked()
    {
        if (PlayerInventory.Instance.Currency >= selectedWeapon.weaponPrice)
        {
            SetActionButtonState(ActionState.ConfirmPurchase, true);
        }
        else
        {
            Debug.Log("Not enough currency to purchase weapon.");
            StartColorLerp(actionButtonText, Color.red, 0.3f);
        }
    }

    private void OnBuyAmmoClicked()
    {
        if (PlayerInventory.Instance.SpendCurrency(selectedWeapon.ammoPrice))
        {
            PlayerInventory.Instance.AddAmmo(selectedWeapon.weaponId, PlayerInventory.Instance.GetWeaponClipSize(selectedWeapon.weaponId));
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
        SetActionButtonState(ActionState.ConfirmEquip, true);
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
        return PlayerInventory.Instance.EquippedWeapons[1] == weapon || PlayerInventory.Instance.EquippedWeapons[2] == weapon;
    }

    private void ConfirmPurchase()
    {
        if (PlayerInventory.Instance.SpendCurrency(selectedWeapon.weaponPrice))
        {
            PlayerInventory.Instance.AddWeapon(selectedWeapon);
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
        UpdateUI();

    }

    private void CancelAction()
    {
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