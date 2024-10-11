using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;

public class WeaponShop : MonoBehaviour
{
    public WeaponInventory weaponInventory;
    public GameObject buttonPrefab;
    public Transform scrollViewContent;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponStatsText;
    public TextMeshProUGUI weaponTypeText;
    public TextMeshProUGUI weaponModeText;
    public TextMeshProUGUI currencyText;

    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public Button optionButton1;
    public Button optionButton2;
    public TextMeshProUGUI optionButton1Text;
    public TextMeshProUGUI optionButton2Text;

    public Color purchaseColor = Color.green;
    public Color equipColor = Color.cyan;
    public Color equippedColor = Color.gray;

    private WeaponData selectedWeapon;
    private enum ActionState { Purchase, Equip, Equipped, ConfirmPurchase, ConfirmEquip }
    private ActionState currentState;

    private Coroutine currentLerpCoroutine;

    private void Start()
    {
        PopulateWeaponList();
        UpdateUI();
    }

    private void PopulateWeaponList()
    {
        foreach (var weapon in weaponInventory.allWeapons)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, scrollViewContent);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = weapon.weaponName;
            button.onClick.AddListener(() => SelectWeapon(weapon));
        }
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
            weaponStatsText.text = $"Damage: {selectedWeapon.damage}\n" +
                                   $"Fire Rate: {selectedWeapon.fireRate}\n" +
                                   $"Clip Size: {selectedWeapon.clipSize}\n" +
                                   $"Reload Time: {selectedWeapon.reloadTime}";
            weaponTypeText.text = GetWeaponType(selectedWeapon);
            weaponModeText.text = GetWeaponMode(selectedWeapon);

            bool owned = PlayerInventory.Instance.OwnsWeapon(selectedWeapon);
            bool equipped = IsWeaponEquipped(selectedWeapon);

            actionButton.gameObject.SetActive(true);
            if (!owned)
            {
                SetActionButtonState(ActionState.Purchase, true);
            }
            else if (!equipped)
            {
                SetActionButtonState(ActionState.Equip, true);
            }
            else
            {
                SetActionButtonState(ActionState.Equipped, false);
            }
        }
        else
        {
            ClearWeaponInfo();
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
                actionButtonText.text = $"Purchase ({selectedWeapon.price})";
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
        if (PlayerInventory.Instance.Currency >= selectedWeapon.price)
        {
            SetActionButtonState(ActionState.ConfirmPurchase, true);
        }
        else
        {
            Debug.Log("Not enough currency to purchase weapon.");
            StartColorLerp(actionButtonText, Color.red, 0.3f);
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
        if (PlayerInventory.Instance.SpendCurrency(selectedWeapon.price))
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
        }
        currentLerpCoroutine = StartCoroutine(LerpTextColor(textElement, targetColor, duration));
    }

    private IEnumerator LerpTextColor(TextMeshProUGUI textElement, Color targetColor, float duration)
    {
        Color originalColor = textElement.color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            textElement.color = Color.Lerp(originalColor, targetColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textElement.color = targetColor;

        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            textElement.color = Color.Lerp(targetColor, originalColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textElement.color = originalColor;

        currentLerpCoroutine = null;
        Debug.Log("Lerp complete");
    }
}