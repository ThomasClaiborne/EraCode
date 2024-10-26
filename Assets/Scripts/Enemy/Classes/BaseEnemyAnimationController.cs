using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BaseEnemyAnimationController : MonoBehaviour
{
    private Animator animator;
    private BaseEnemy enemy;

    // Animation parameter names
    private const string IS_MOVING = "IsMoving";
    private const string TRIGGER_ATTACK = "Attack";

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        enemy = GetComponentInParent<BaseEnemy>();

        if (animator == null)
        {
            Debug.LogError("Animator component not found!");
        }
    }

    public virtual void SetMovementAnimation(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool(IS_MOVING, isMoving);
        }
    }

    public virtual void TriggerAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(TRIGGER_ATTACK);
        }
    }

    // This will be called by animation events
    public virtual void OnAttackAnimationHit()
    {
        enemy?.OnAnimationDamageEvent();
    }
}
