using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimationController : MonoBehaviour
{
    private Animator animator;
    private Enemy enemy;

    // Animation parameter names
    private const string IS_MOVING = "IsMoving";
    private const string TRIGGER_ATTACK = "Attack";

    private void Start()
    {
        animator = GetComponent<Animator>();
        enemy = GetComponentInParent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError("Enemy component not found!");
            return;
        }
    }

    private void Update()
    {
        // Update movement animation based on velocity
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        bool isMoving = enemy.isMoving;
        animator.SetBool(IS_MOVING, isMoving);
    }

    // Called by Enemy script to trigger attack animation
    public void TriggerAttack()
    {
        animator.SetTrigger(TRIGGER_ATTACK);
    }

    // This will be called by the animation event
    public void OnAttackAnimationHit()
    {
        enemy.OnAttackAnimationHit();
    }
}
