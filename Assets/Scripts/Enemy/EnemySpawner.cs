using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] GameObject enemyPrefab; 
    [SerializeField] float spawnInterval = 5f; 
    [SerializeField] int numberofEnemies = 1;
    public Transform[] waypoints;

    private float spawnTimer;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        spawnTimer += Time.deltaTime;

        // Spawn enemy at intervals
        if (spawnTimer >= spawnInterval && numberofEnemies > 0)
        {
            SpawnEnemy();
            spawnTimer = 0f;
            numberofEnemies--;
        }
    }

    void SpawnEnemy()
    {
        GameObject newEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();

        enemyScript.waypoints = waypoints;

        Debug.Log("Enemy spawned and waypoints assigned.");
    }
}
