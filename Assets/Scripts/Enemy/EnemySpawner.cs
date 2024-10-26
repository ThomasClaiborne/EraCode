using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] GameObject[] enemyPrefabs;

    [Header("Waypoints")]
    public Transform[] waypoints;
    [SerializeField] Color waypointGizmoColor;
    [SerializeField] float waypointGizmoSize = 0.5f;

    private float spawnTimer;

    public float spawnInterval = 5f; 
    public int numberofEnemies;
    public int spawnerID;

    public void SpawnEnemy(int enemyID)
    {

        if (enemyID < 0 || enemyID >= enemyPrefabs.Length)
        {
            Debug.LogError("Invalid enemy ID: " + enemyID);
            return; // Handle invalid ID
        }

        GameObject enemyObject = Instantiate(enemyPrefabs[enemyID], transform.position, transform.rotation);

        BaseEnemy enemy = enemyObject.GetComponent<BaseEnemy>();

        if (enemy == null)
        {
            Debug.LogError($"Spawned enemy {enemyObject.name} doesn't have a BaseEnemy component!");
            Destroy(enemyObject);
            return;
        }

        // Set up the enemy
        InitializeEnemy(enemy);
    }

    private void InitializeEnemy(BaseEnemy enemy)
    {
        // Set waypoints
        if (waypoints != null && waypoints.Length > 0)
        {
            enemy.SetWaypoints(waypoints);
        }
        else
        {
            Debug.LogWarning($"No waypoints set for spawner {spawnerID}!");
        }

        // Subscribe to death event
        enemy.OnDeath += HandleEnemyDeath;
    }

    private void HandleEnemyDeath()
    {
        LevelManager.Instance.OnEnemyDefeated();
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
