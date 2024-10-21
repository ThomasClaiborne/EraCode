using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "New Strike", menuName = "Abilities/Strike")]
public class StrikeAbility : Ability
{
    [Header("Strike Settings")]
    public float damage = 100f;
    public float damageRadius = 5f;
    public float delayBeforeDamage = 1f;
    public int strikeCount = 1;
    public float strikesDelay = 0.5f;
    public float strikeRadius = 5f;

    public GameObject strikePrefab;
    public GameObject targetingIndicatorPrefab;
    public GameObject impactEffectPrefab;

    private GameObject activeTargetingIndicator;

    public override void Initialize(PlayerController player)
    {
        base.Initialize(player);
        isPassive = false;
        requiresTargetArea = true;
        currentCooldown = cooldownTime;
    }

    public override void StartTargeting()
    {
        base.StartTargeting();
        CurrentState = AbilityState.Targeting;
        if (targetingIndicatorPrefab != null && activeTargetingIndicator == null)
        {
            activeTargetingIndicator = Object.Instantiate(targetingIndicatorPrefab);
            activeTargetingIndicator.transform.localScale = new Vector3(damageRadius * 2, 0.1f, damageRadius * 2);
        }
    }

    public override void UpdateTargeting(Vector3 targetPosition)
    {
        base.UpdateTargeting(targetPosition);
        if (activeTargetingIndicator != null)
        {
            activeTargetingIndicator.transform.position = targetPosition;
        }
    }

    public override void CancelTargeting()
    {
        base.CancelTargeting();
        if (activeTargetingIndicator != null)
        {
            Object.Destroy(activeTargetingIndicator);
            activeTargetingIndicator = null;
        }
        CurrentState = AbilityState.Ready;
    }

    public override void Activate(Vector3 targetPosition)
    {
        base.Activate(targetPosition);
        if (CurrentState != AbilityState.Targeting) return;

        if (activeTargetingIndicator != null)
        {
            Object.Destroy(activeTargetingIndicator);
            activeTargetingIndicator = null;
        }

        playerController.StartCoroutine(PerformMultipleStrikes(targetPosition));
    }

    private IEnumerator PerformMultipleStrikes(Vector3 targetPosition)
    {
        for (int i = 0; i < strikeCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * strikeRadius;
            Vector3 strikePosition = targetPosition + new Vector3(randomOffset.x, 0, randomOffset.y);

            SpawnStrike(strikePosition);
            playerController.StartCoroutine(DealDamageAfterDelay(strikePosition));

            if (i < strikeCount - 1)
            {
                yield return new WaitForSeconds(strikesDelay);
            }
        }
    }

    private void SpawnStrike(Vector3 position)
    {
        if (strikePrefab != null)
        {
            GameObject currentStrike = Object.Instantiate(strikePrefab, position, Quaternion.identity);
            Object.Destroy(currentStrike, 2f);
        }
    }

    private IEnumerator DealDamageAfterDelay(Vector3 targetPosition)
    {
        yield return new WaitForSeconds(delayBeforeDamage);

        Collider[] hitColliders = Physics.OverlapSphere(targetPosition, damageRadius);
        foreach (var hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.takeDamage((int)damage, false);
            }
        }

        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Object.Instantiate(impactEffectPrefab, targetPosition, Quaternion.identity);
            Object.Destroy(impactEffect, 1.5f);
        }
    }

    public override void UpdateAbility()
    {
        base.UpdateAbility();
        if (CurrentState == AbilityState.Cooldown)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0)
            {
                CurrentState = AbilityState.Ready;
                currentCooldown = cooldownTime;
            }
        }
    }
}