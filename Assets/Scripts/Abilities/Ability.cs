using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Ability : ScriptableObject
{
    public string abilityName;
    public string description;
    public bool isPassive;
    public bool requiresTargetArea;
    public float cooldownTime;
    public float currentCooldown;
    public Sprite icon;

    public string abilityID;

    protected PlayerController playerController;
    protected AbilitySlot abilitySlot;

    public enum AbilityState { Ready, Targeting, Active, Cooldown }
    public AbilityState CurrentState { get; set; }

    public virtual void Initialize(PlayerController player)
    {
        playerController = player;
        CurrentState = AbilityState.Ready;
    }

    public virtual void StartTargeting() { }
    public virtual void UpdateTargeting(Vector3 targetPosition) { }
    public virtual void CancelTargeting() { }
    public virtual void Activate(Vector3 targetPosition) { }
    public virtual void Deactivate() { }
    public virtual void UpdateAbility() { }

    protected virtual void StartCooldown()
    {
        currentCooldown = cooldownTime;
        CurrentState = AbilityState.Cooldown;
    }

}
