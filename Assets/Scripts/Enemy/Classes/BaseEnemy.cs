using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class EnemyStats
{
    public string enemyName;
    public int level;
    public int maxHealth;
    public float speed;
    public int damage;
    public float attackInterval;
}

[System.Serializable]
public class EnemyRewards
{
    public int synthiumReward;
    public int experienceReward;
}

public abstract class BaseEnemy : MonoBehaviour, IDamage
{
    [Header("Enemy Information")]
    [SerializeField] protected EnemyStats stats;
    [SerializeField] protected EnemyRewards rewards;

    [Header("UI Elements")]
    [SerializeField] protected TMPro.TextMeshProUGUI enemyNameText;
    [SerializeField] protected TMPro.TextMeshProUGUI enemyLevelText;
    [SerializeField] protected UnityEngine.UI.Slider healthBar;

    [Header("Effects")]
    [SerializeField] protected GameObject synthiumTextPrefab;
    [SerializeField] protected Transform synthiumTextSpawnPoint;
    [SerializeField] protected GameObject deathEffectPrefab;
    [SerializeField] protected Material hitFlashMaterial;

    protected Rigidbody rb;
    protected EnemyStateMachine stateMachine;
    protected BaseEnemyAnimationController animationController;

    protected Transform[] waypoints;
    protected int currentWaypointIndex;
    protected float attackTimer;
    protected bool isDead;
    protected int currentHealth;
    public bool isMoving { get; set; }
    protected bool isFlashing;

    public delegate void EnemyDeath();
    public event EnemyDeath OnDeath;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animationController = GetComponentInChildren<BaseEnemyAnimationController>();
        InitializeStateMachine();

        if (animationController == null)
        {
            Debug.LogError("BaseEnemyAnimationController not found on " + gameObject.name);
        }
    }

    protected virtual void Start()
    {
        currentHealth = stats.maxHealth;
        SetupRigidbody();
        InitializeUI();
    }

    protected virtual void Update()
    {
        if (!isDead)
        {
            stateMachine?.UpdateState();
            UpdateAttackTimer();
            UpdateAnimationState();
        }
    }

    protected virtual void UpdateAttackTimer()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    protected virtual void UpdateAnimationState()
    {
        animationController?.SetMovementAnimation(isMoving);
    }

    protected virtual void InitializeUI()
    {
        if (enemyNameText != null) enemyNameText.text = stats.enemyName;
        if (enemyLevelText != null) enemyLevelText.text = $"Lv.{stats.level}";
        if (healthBar != null)
        {
            healthBar.maxValue = stats.maxHealth;
            healthBar.value = currentHealth;
        }
    }

    protected virtual void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }
    }

    protected virtual void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
    }

    public virtual void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 0;
    }

    public virtual void takeDamage(int amount, bool headshot)
    {
        if (isDead) return;

        int actualDamage = headshot ? amount * 2 : amount;
        currentHealth -= actualDamage;
        UpdateHealthBar();
        OnDamaged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void OnDamaged()
    {
        StartCoroutine(FlashMaterialCoroutine());
    }

    protected virtual IEnumerator FlashMaterialCoroutine()
    {
        if (isFlashing) yield break;

        isFlashing = true;
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null && hitFlashMaterial != null)
        {
            Material originalMaterial = renderer.material;
            renderer.material = hitFlashMaterial;
            yield return new WaitForSeconds(0.1f);
            renderer.material = originalMaterial;
        }
        isFlashing = false;
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        OnDeath?.Invoke();
        GiveRewards();

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);
        }

        Destroy(gameObject);
    }

    protected virtual void GiveRewards()
    {
        PlayerInventory.Instance.AddCurrency(rewards.synthiumReward);
        PlayerInventory.Instance.LevelSystem.AddExperience(rewards.experienceReward);
        LevelManager.Instance.AddSynthium(rewards.synthiumReward);
        DisplaySynthiumReward();
        HUDManager.Instance.UpdateSynthiumText();
    }

    protected virtual void DisplaySynthiumReward()
    {
        if (synthiumTextPrefab != null && synthiumTextSpawnPoint != null)
        {
            GameObject synthiumText = Instantiate(synthiumTextPrefab,
                synthiumTextSpawnPoint.position,
                Quaternion.identity);
            TextMeshPro rewardText = synthiumText.GetComponent<TextMeshPro>();
            if (rewardText != null)
            {
                rewardText.text = $"+${rewards.synthiumReward}";
            }
        }
    }

    // Called by the animation event through BaseEnemyAnimationController
    public virtual void OnAnimationDamageEvent()
    {
        // Override in derived classes to implement specific damage logic when
        // animation hits the damage frame
    }

    // ... [rest of the BaseEnemy code remains the same]

    // Added helper method for triggering attack animation
    protected virtual void TriggerAttackAnimation()
    {
        animationController?.TriggerAttack();
    }

    protected abstract void InitializeStateMachine();
}
