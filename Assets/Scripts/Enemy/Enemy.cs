using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamage
{
    private enum EnemyState { Traveling, Attacking }
    private EnemyState currentState;

    [SerializeField] Renderer model;
    [SerializeField] Material tempMat;

    [SerializeField] int maxHealth = 100; 
    [SerializeField] float speed = 3f; 
    [SerializeField] int damageAmount = 20;
    [SerializeField] float attackInterval = 1.0f;
    [SerializeField] float attackTimer = 0;

    public Transform[] waypoints
    {
        get;
        set;
    }
   
    private int currentWaypointIndex = 0;

    private int currentHealth;

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
                    wall.takeDamage(10, false); 
                    Debug.Log("Enemy attacks the wall. Wall health decreasing...");
                }
            }
            attackTimer = 0f;
        }
    }

    public void takeDamage(int amount, bool headshot)
    {
        if (headshot)
            amount *= 2;

        currentHealth -= amount;
        Debug.Log("Enemy took " + amount + " damage. Health left: " + currentHealth);
        StartCoroutine(flashMat());

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }

    IEnumerator flashMat()
    {
        Material tempColor = model.material;
        model.material = tempMat;
        yield return new WaitForSeconds(0.1f);
        model.material = tempColor;
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
