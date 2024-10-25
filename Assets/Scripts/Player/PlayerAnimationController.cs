using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private WeaponSlot weaponSlot;

    private const string PISTOL_LAYER = "Pistol";
    private const string RIFLE_LAYER = "Rifle";
    private const string ROCKET_LAYER = "Rocket";

    // Animation trigger
    private const string TRIGGER_FIRE = "Fire";

    private void Start()
    {
        animator = GetComponent<Animator>();
        weaponSlot = GetComponentInParent<WeaponSlot>();

        if (weaponSlot == null)
        {
            Debug.LogError("WeaponSlot not found on player!");
            return;
        }

        weaponSlot.OnWeaponChanged += HandleWeaponChanged;
        weaponSlot.OnWeaponFired += HandleWeaponFired;

        if (weaponSlot.CurrentWeapon != null)
        {
            UpdateAnimationLayers(weaponSlot.CurrentWeapon);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (weaponSlot != null)
        {
            weaponSlot.OnWeaponChanged -= HandleWeaponChanged;
            weaponSlot.OnWeaponFired -= HandleWeaponFired;
        }
    }

    private void HandleWeaponChanged(WeaponData weapon)
    {
        UpdateAnimationLayers(weapon);
    }

    private void HandleWeaponFired()
    {
        animator.ResetTrigger(TRIGGER_FIRE);
        animator.SetTrigger(TRIGGER_FIRE);
    }

    private void UpdateAnimationLayers(WeaponData weapon)
    {
        // Disable all layers first
        animator.SetLayerWeight(animator.GetLayerIndex(PISTOL_LAYER), 0);
        animator.SetLayerWeight(animator.GetLayerIndex(RIFLE_LAYER), 0);
        animator.SetLayerWeight(animator.GetLayerIndex(ROCKET_LAYER), 0);

        // Enable the appropriate layer based on weapon type
        string activeLayer = GetLayerForWeapon(weapon);
        int layerIndex = animator.GetLayerIndex(activeLayer);

        if (layerIndex != -1)
        {
            animator.SetLayerWeight(layerIndex, 1);
        }
        else
        {
            Debug.LogError($"Layer {activeLayer} not found in Animator!");
        }
    }

    private string GetLayerForWeapon(WeaponData weapon)
    {
        if (weapon.isPistol) return PISTOL_LAYER;
        if (weapon.isRocketLauncher) return ROCKET_LAYER;

        // All other weapon types use the rifle animations
        if (weapon.isAssaultRifle || weapon.isSubmachineGun ||
            weapon.isShotgun || weapon.isSniper)
        {
            return RIFLE_LAYER;
        }

        // Default to pistol if unknown weapon type
        return PISTOL_LAYER;
    }
}
