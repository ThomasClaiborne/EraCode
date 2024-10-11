using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Enemy : MonoBehaviour, IDamage
{
    private enum EnemyState { Traveling, Attacking }
    private EnemyState currentState;

    [SerializeField] private Image healthBar;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] Renderer model;
    [SerializeField] Material tempMat;
    [SerializeField] GameObject deathEffect;

    [SerializeField] int maxHealth = 100; 
    [SerializeField] float speed = 3f; 
    [SerializeField] int damageAmount = 20;
    [SerializeField] float attackInterval = 1.0f;
    [SerializeField] float attackTimer = 0;

    [SerializeField] private int currencyReward = 10;
    [SerializeField] private GameObject currencyTextPrefab;
    [SerializeField] private Transform currencyTextSpawnPoint;

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
    }

    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Traveling:
                MoveAlongPath();
                break;
            case EnemyState.Attacking:
                AttackWall();
                break;
        }
    }

    void MoveAlongPath()
    {
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        transform.LookAt(new Vector3(targetWaypoint.position.x, transform.position.y, targetWaypoint.position.z));

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                currentState = EnemyState.Attacking;
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
            HUDManager.Instance.UpdateCurrencyText();
            DisplayCurrencyReward();
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);
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
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("PlayerWall"))
        {
            collision.gameObject.GetComponent<IDamage>().takeDamage(20, false);
            //Destroy(gameObject); 
        }
    }
}
