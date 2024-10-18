using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Enemy : MonoBehaviour, IDamage
{
    private enum EnemyState { Traveling, MovingToAttackPoint, Attacking }
    private EnemyState currentState;

    [Header("--Objects--")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] Renderer model;
    [SerializeField] Material tempMat;
    [SerializeField] GameObject deathEffect;

    [Header("--Stats--")]
    [SerializeField] int maxHealth = 100; 
    [SerializeField] float speed = 3f; 
    [SerializeField] int damageAmount = 20;
    [SerializeField] float attackInterval = 1.0f;
    [SerializeField] float attackTimer = 0;

    [Header("--Rewards--")]
    [SerializeField] private int currencyReward = 10;
    [SerializeField] private int experienceReward = 10;

    [Header("--Currency Text--")]
    [SerializeField] private GameObject currencyTextPrefab;
    [SerializeField] private Transform currencyTextSpawnPoint;

    private Transform targetAttackPoint;
    //private bool isMovingToAttackPoint = false;
    private Transform player;
    private Rigidbody rb;

    public Transform[] waypoints
    {
        get;
        set;
    }

    public delegate void EnemyDeath();
    public event EnemyDeath OnDeath;

    private int currentWaypointIndex = 0;

    private int currentHealth;
    private bool isFlashing;
    private bool isDead; 

    private

    void Start()
    {
        currentHealth = maxHealth;
        currentState = EnemyState.Traveling;

        player = GameManager.Instance.player.transform;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Traveling:
                MoveAlongPath();
                break;
            case EnemyState.MovingToAttackPoint:
                MoveToAttackPoint();
                break;
            case EnemyState.Attacking:
                AttackWall();
                FacePlayer();
                break;
        }
    }

    void MoveAlongPath()
    {
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z), Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.fixedDeltaTime));
        }

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                currentState = EnemyState.MovingToAttackPoint;
                FindAttackPoint();
            }
        }
    }

    public void FindAttackPoint()
    {
        PlayerWall playerWall = GameManager.Instance.playerWall;
        Transform closestPoint = null;
        float closestDistance = float.MaxValue;

        // Check front row first
        foreach (Transform point in playerWall.frontRowAttackPoints)
        {
            if (point.gameObject.activeSelf)
            {
                float distance = Vector3.Distance(transform.position, point.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }
        }

        // If no point found in front row, check back row
        if (closestPoint == null)
        {
            foreach (Transform point in playerWall.backRowAttackPoints)
            {
                if (point.gameObject.activeSelf)
                {
                    float distance = Vector3.Distance(transform.position, point.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPoint = point;
                    }
                }
            }
        }

        if (closestPoint != null)
        {
            targetAttackPoint = closestPoint;
            //isMovingToAttackPoint = true;
            closestPoint.gameObject.SetActive(false); // Mark the point as occupied
        }
        else
        {
            Debug.LogError("No available attack points found!");
            LevelManager.Instance.AddEnemyToWaitingList(this);
        }
    }

    void MoveToAttackPoint()
    {
        if (targetAttackPoint == null)
        {
            Debug.LogError("Target attack point is null!");
            return;
        }

        Vector3 direction = (targetAttackPoint.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        float distanceToTarget = Vector3.Distance(transform.position, targetAttackPoint.position);


        if (distanceToTarget < 0.1f)
        {
            currentState = EnemyState.Attacking;
            rb.isKinematic = true;
        }
    }

    void FacePlayer()
    {
        if (GameManager.Instance.player != null)
        {
            Vector3 directionToPlayer = GameManager.Instance.player.transform.position - transform.position;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
            }
        }
    }

    void AttackWall()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            if (!GameManager.Instance.isWallDestroyed)
            {
                IDamage wall = GameManager.Instance.playerWall.GetComponent<IDamage>();
                if (wall != null)
                {
                    wall.takeDamage(damageAmount, false); 
                }
            }
            attackTimer = 0f;
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            if (healthBarBackground.IsActive() == false)
                healthBarBackground.gameObject.SetActive(true);

            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    public void takeDamage(int amount, bool headshot)
    {
        if (headshot)
            amount *= 2;

        currentHealth -= amount;
        UpdateHealthBar();
        StartCoroutine(flashMat());

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if(OnDeath != null && !isDead)
        {
            isDead = true;
            OnDeath.Invoke();
            PlayerInventory.Instance.AddCurrency(currencyReward);
            PlayerInventory.Instance.LevelSystem.AddExperience(experienceReward);
            HUDManager.Instance.UpdateCurrencyText();
            HUDManager.Instance.UpdateLevelDisplay();
            DisplayCurrencyReward();
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);

            if (targetAttackPoint != null)
            {
                targetAttackPoint.gameObject.SetActive(true); // Mark the point as available
                LevelManager.Instance.NotifyAttackPointAvailable();
            }
        }
        Destroy(gameObject);
    }

    IEnumerator flashMat()
    {
        if (isFlashing)
            yield break;

        isFlashing = true;

        Material tempColor = model.material;
        model.material = tempMat;
        yield return new WaitForSeconds(0.1f);
        model.material = tempColor;

        isFlashing = false;
    }

    void DisplayCurrencyReward()
    {
        if (currencyTextPrefab != null && currencyTextSpawnPoint != null)
        {
            GameObject currencyTextObject = Instantiate(currencyTextPrefab, currencyTextSpawnPoint.position, Quaternion.identity);
            TextMeshPro currencyText = currencyTextObject.GetComponent<TextMeshPro>();
            Color textColor = currencyTextPrefab.GetComponent<FloatingText>().Color;
            if (currencyText != null)
            {
                currencyText.text = $"+${currencyReward}";
                currencyText.color = textColor;
            }   
            else
            {
                Debug.LogError("Currency text prefab does not have a TextMeshPro component!");
                Destroy(currencyTextObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        //if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("PlayerWall"))
        //{
        //    collision.gameObject.GetComponent<IDamage>().takeDamage(20, false);
        //    //Destroy(gameObject); 
        //}
    }
}
