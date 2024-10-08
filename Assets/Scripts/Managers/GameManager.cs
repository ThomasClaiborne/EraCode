using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    public GameObject player;
    public PlayerController playerController;
    public PlayerWall playerWall;
    public EnemySpawner[] enemySpawners;
    public List<EnemyWave> enemyWaves;

    public float waveCooldown = 3f;

    public bool isPaused;
    public bool isWallDestroyed;

    private int currentWaveIndex = 0;
    private int enemiesRemaining;
    private bool isWaveInProgress;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        player = GameObject.FindWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerWall = GameObject.FindWithTag("PlayerWall").GetComponent<PlayerWall>();

        InitializeEnemySpawners();
    }
    void Start()
    {
        StartNextWave();
    }

    void Update()
    {
        
    }

    private void StartNextWave()
    {
        if (currentWaveIndex < enemyWaves.Count)
        {
            EnemyWave currentWave = enemyWaves[currentWaveIndex];
            enemiesRemaining = 0;
            StartCoroutine(HandleWave(currentWave));
        }
        else
        {
            Debug.Log("All waves completed!");
            CheckForWinCondition();
        }
    }

    IEnumerator HandleWave(EnemyWave wave)
    {
        yield return StartCoroutine(WaveCooldown());
        isWaveInProgress = true;
        float startTime = Time.time;

        foreach (var spawnEvent in wave.spawnEvents)
        {
            float delay = spawnEvent.spawnTime;
            yield return new WaitForSeconds(delay);

            int spawnerID = spawnEvent.spawnerID;
            int numberOfEnemies = spawnEvent.numberOfEnemies;

            if (spawnerID >= 0 && spawnerID < enemySpawners.Length)
            {
                for (int i = 0; i < numberOfEnemies; i++)
                {
                    enemySpawners[spawnerID].SpawnEnemy();
                    enemiesRemaining++;
                }
            }
        }

        while (enemiesRemaining > 0)
        {
            yield return null;
        }

        isWaveInProgress = false;
        currentWaveIndex++;
        StartNextWave();
        Debug.Log("Wave completed!");
    }

    IEnumerator WaveCooldown()
    {
        if (currentWaveIndex < enemyWaves.Count - 1)
            UIManager.Instance.UpdateWaveDisplay("Wave " + (currentWaveIndex + 1) + " incoming!");
        else
            UIManager.Instance.UpdateWaveDisplay("Final wave incoming!");
        yield return new WaitForSeconds(waveCooldown);
        UIManager.Instance.HideWaveDisplay();
    }

    public void OnEnemyDefeated()
    {
        enemiesRemaining--;
        if (enemiesRemaining <= 0 && !isWaveInProgress && currentWaveIndex >= enemyWaves.Count)
        {
            CheckForWinCondition();
        }
    }

    private void CheckForWinCondition()
    {
        if (currentWaveIndex >= enemyWaves.Count && enemiesRemaining <= 0)
        {
            Debug.Log("Victory! All waves and enemies cleared.");
            WinGame();
        }
    }

    public void statePaused()
    {
        isPaused = !isPaused;
        Time.timeScale = 0;
        UIManager.Instance.ShowPauseMenu();
    }
    public void stateUnpaused()
    {
        isPaused = !isPaused;
        Time.timeScale = 1;
        UIManager.Instance.HideActiveMenu();
    }

    public void WinGame()
    {
        isPaused = true;
        Time.timeScale = 0;
        UIManager.Instance.ShowWinMenu();
    }

    public void LoseGame()
    {
        isPaused = true;
        Time.timeScale = 0;
        UIManager.Instance.ShowLoseMenu();
    }

    public void ResetGameState()
    {
        Debug.Log("Current Wave Index before restart: " + currentWaveIndex);
        enemySpawners = null;
        enemySpawners = FindObjectsOfType<EnemySpawner>();
        enemiesRemaining = 0;
        currentWaveIndex = 0;
        isWaveInProgress = false;

        StopAllCoroutines();
        StartNextWave();
    }

    void InitializeEnemySpawners()
    {
        enemySpawners = FindObjectsOfType<EnemySpawner>();

        List<int> usedIDs = new List<int>();

        foreach (var spawner in enemySpawners)
        {
            if (usedIDs.Contains(spawner.spawnerID))
            {
                int newID = FindNextAvailableID(usedIDs);
                spawner.spawnerID = newID;
            }

            usedIDs.Add(spawner.spawnerID);
        }
        enemySpawners = enemySpawners.OrderBy(s => s.spawnerID).ToArray();
    }

    int FindNextAvailableID(List<int> usedIDs)
    {
        int nextID = 1;
        while (usedIDs.Contains(nextID))
        {
            nextID++;
        }
        return nextID;
    }
}
