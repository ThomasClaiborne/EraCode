using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AbilitySlot : MonoBehaviour
{
    public Ability[] equippedAbilities = new Ability[3];
    public List<Ability> passiveAbilities = new List<Ability>();
    public int selectingAbilityIndex = -1;
    private bool isActivatingAbility = false; 

    private PlayerController playerController;


    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        InitializeAbilities();
    }

    private void Update()
    {
        if (!GameManager.Instance.isPaused)
        {
            HandleAbilityInput();
            UpdateAbilities();
        }
    }

    private void InitializeAbilities()
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] != null)
            {
                equippedAbilities[i].Initialize(playerController);
            }
        }

        foreach (var ability in passiveAbilities)
        {
            ability.Initialize(playerController);
        }
    }

    private void HandleAbilityInput()
    {
        if (Input.GetKeyDown(KeyCode.Z)) HandleAbilityActivation(0);
        if (Input.GetKeyDown(KeyCode.X)) HandleAbilityActivation(1);
        if (Input.GetKeyDown(KeyCode.C)) HandleAbilityActivation(2);

        if (selectingAbilityIndex != -1 && Input.GetMouseButtonDown(0))
        {
            isActivatingAbility = true;
            ActivateTargetedAbility();
            StartCoroutine(ResetActivationFlag());
        }
    }

    private IEnumerator ResetActivationFlag()
    {
        yield return new WaitForEndOfFrame();
        isActivatingAbility = false;
    }

    private void HandleAbilityActivation(int index)
    {
        if (equippedAbilities[index] == null) return;

        Ability ability = equippedAbilities[index];

        if (ability.CurrentState == Ability.AbilityState.Cooldown) return;

        if (selectingAbilityIndex == index)
        {
            CancelTargetSelection();
        }
        else if (ability.requiresTargetArea)
        {
            StartTargetSelection(index);
        }
        else
        {
            ActivateAbility(index);
        }
    }

    private void StartTargetSelection(int index)
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (i != index && equippedAbilities[i] != null)
            {
                equippedAbilities[i].CancelTargeting();
            }
        }

        selectingAbilityIndex = index;
        equippedAbilities[index].StartTargeting();
        // TODO: Implement visual indicator for target selection
    }

    private void CancelTargetSelection()
    {
        if (selectingAbilityIndex != -1)
        {
            equippedAbilities[selectingAbilityIndex].CancelTargeting();
            selectingAbilityIndex = -1;
        }
        // TODO: Remove visual indicator for target selection
    }

    private void ActivateTargetedAbility()
    {
        if (selectingAbilityIndex != -1)
        {
            Ability ability = equippedAbilities[selectingAbilityIndex];
            if (ability.CurrentState != Ability.AbilityState.Cooldown)
            {
                Vector3 targetPosition = GetMouseWorldPosition();
                ActivateAbility(selectingAbilityIndex, targetPosition);
                selectingAbilityIndex = -1;
            }
        }
    }

    private void ActivateAbility(int index, Vector3? targetPosition = null)
    {
        if (equippedAbilities[index] != null)
        {
            equippedAbilities[index].Activate(targetPosition ?? Vector3.zero);
            equippedAbilities[index].CurrentState = Ability.AbilityState.Cooldown;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            return ray.GetPoint(rayDistance);
        }

        return Vector3.zero;
    }

    private void UpdateAbilities()
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] != null)
            {
                equippedAbilities[i].UpdateAbility();
                if (selectingAbilityIndex == i)
                {
                    equippedAbilities[i].UpdateTargeting(GetMouseWorldPosition());
                }
            }
        }

        foreach (var ability in passiveAbilities)
        {
            ability.UpdateAbility();
        }
    }

    public void EquipAbility(Ability ability, int slot)
    {
        if (slot >= 0 && slot < equippedAbilities.Length)
        {
            equippedAbilities[slot] = ability;
            ability.Initialize(playerController);
        }
    }

    public void AddPassiveAbility(Ability ability)
    {
        if (!passiveAbilities.Contains(ability))
        {
            passiveAbilities.Add(ability);
            ability.Initialize(playerController);
        }
    }
    public bool IsAbilityActive()
    {
        return selectingAbilityIndex != -1 || isActivatingAbility;
    }
}