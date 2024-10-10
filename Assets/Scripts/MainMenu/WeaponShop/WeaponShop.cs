using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WeaponShop : MonoBehaviour
{
    public WeaponInventory weaponInventory;
    public GameObject buttonPrefab;
    public Transform scrollViewContent;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponStatsText;
    public TextMeshProUGUI weaponTypeText;
    public TextMeshProUGUI weaponModeText;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public Button slot2Button;
    public Button slot3Button;
    public Color purchaseColor = Color.green;
    public Color equipColor = Color.yellow;
    public Color equippedColor = Color.gray;

    private WeaponData selectedWeapon;

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

    private void UpdateUI()
    {
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
                actionButtonText.text = "Purchase";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(PurchaseWeapon);
                actionButton.GetComponent<Image>().color = purchaseColor;
            }
            else if (!equipped)
            {
                actionButtonText.text = "Equip";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(ShowEquipOptions);
                actionButton.GetComponent<Image>().color = equipColor;
            }
            else
            {
                actionButtonText.text = "Equipped";
                actionButton.onClick.RemoveAllListeners();
                actionButton.GetComponent<Image>().color = equippedColor;
            }

            slot2Button.gameObject.SetActive(false);
            slot3Button.gameObject.SetActive(false);
        }
        else
        {
            weaponNameText.text = "Select a weapon";
            weaponStatsText.text = "";
            weaponTypeText.text = "";
            weaponModeText.text = "";
            actionButton.gameObject.SetActive(false);
            slot2Button.gameObject.SetActive(false);
            slot3Button.gameObject.SetActive(false);
        }
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

    private void PurchaseWeapon()
    {
        if (selectedWeapon != null && !PlayerInventory.Instance.OwnsWeapon(selectedWeapon))
        {
            // Implement purchase logic (e.g., check currency, deduct cost)
            PlayerInventory.Instance.AddWeapon(selectedWeapon);
            UpdateUI();
        }
    }

    private void ShowEquipOptions()
    {
        slot2Button.gameObject.SetActive(true);
        slot3Button.gameObject.SetActive(true);
        slot2Button.onClick.RemoveAllListeners();
        slot3Button.onClick.RemoveAllListeners();
        slot2Button.onClick.AddListener(() => EquipWeapon(1));
        slot3Button.onClick.AddListener(() => EquipWeapon(2));
    }

    private void EquipWeapon(int slot)
    {
        if (selectedWeapon != null && PlayerInventory.Instance.OwnsWeapon(selectedWeapon))
        {
            PlayerInventory.Instance.EquipWeapon(selectedWeapon, slot);
            UpdateUI();
        }
    }
}