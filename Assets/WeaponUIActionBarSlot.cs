// WeaponActionBarSlot.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUIActionBarSlot : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Button slotButton;
    [SerializeField] private int slotIndex;

    private Color defaultColor;
    private Color highlightColor = new Color(0.5f, 1f, 1f, 1f); // Cyan highlight

    private void Awake()
    {
        defaultColor = backgroundImage.color;
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    public void SetWeapon(WeaponData weapon)
    {
        if (weapon != null)
        {
            weaponIcon.sprite = weapon.actionBarIcon;
            weaponIcon.gameObject.SetActive(true);
        }
        else
        {
            weaponIcon.sprite = null;
            weaponIcon.gameObject.SetActive(false);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        backgroundImage.color = highlighted ? highlightColor : defaultColor;
        slotButton.interactable = highlighted;
    }

    public void SetInteractable(bool interactable)
    {
        slotButton.interactable = interactable;
    }

    private void OnSlotClicked()
    {
        WeaponUI weaponUI = GetComponentInParent<WeaponUI>();
        if (weaponUI != null)
        {
            weaponUI.OnActionBarSlotClicked(slotIndex);
        }
    }
}
