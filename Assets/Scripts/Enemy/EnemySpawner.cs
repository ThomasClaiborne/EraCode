using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] GameObject enemyPrefab;

    [Header("Waypoints")]
    [SerializeField] Color waypointGizmoColor;
    [SerializeField] float waypointGizmoSize = 0.5f;

    private float spawnTimer;

    public Transform[] waypoints;
    public float spawnInterval = 5f; 
    public int numberofEnemies;
    public int spawnerID;

    public void SpawnEnemy()
    {
        GameObject newEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();

        if (waypoints != null && waypoints.Length > 0)
        {
            enemyScript.waypoints = waypoints;
        }

        enemyScript.OnDeath += HandleEnemyDeath;

        Debug.Log("Enemy spawned and waypoints assigned.");
    }

    private void HandleEnemyDeath()
    {
        GameManager.Instance.OnEnemyDefeated();
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = waypointGizmoColor;
        waypointGizmoColor.a = 1f;

        Gizmos.DrawSphere(transform.position, waypointGizmoSize);
        if (waypoints[0] != null)
        {
            Gizmos.DrawLine(transform.position, waypoints[0].position);
        }

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        foreach (var waypoint in waypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawSphere(waypoint.position, waypointGizmoSize);
            }
        }
    }

}
