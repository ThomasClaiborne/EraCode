// WeaponActionBarSlot.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionBarSlot : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image icon;
    [SerializeField] private Button slotButton;
    [SerializeField] private int slotIndex;
    [SerializeField] private Sprite emptySlotIcon;

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
            icon.sprite = weapon.actionBarIcon;
        }
        else
        {
            icon.sprite = emptySlotIcon;
        }
        icon.gameObject.SetActive(true);
    }

    public void SetAbility(Ability ability)
    {
        if (ability != null)
        {
            icon.sprite = ability.actionBarIcon;
        }
        else
        {
            icon.sprite = emptySlotIcon;
        }
        icon.gameObject.SetActive(true);
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

        SkillTreeUI skillTreeUI = GetComponentInParent<SkillTreeUI>();
        if (skillTreeUI != null)
        {
            skillTreeUI.OnActionBarSlotClicked(slotIndex);
        }
    }
}
