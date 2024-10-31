using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("--Level Info--")]
    public string LevelName;

    [Header("--EnemySpawners--")]
    public EnemySpawner[] enemySpawners;

    [Header("--EnemyWaves--")]
    public List<EnemyWave> enemyWaves;
    public float waveCooldown = 3f;

    [Header("--Level Rewards--")]
    public int xpReward = 100;
    public int currencyReward = 100;

    public int PlayerKills { get; private set; }
    public int SynthiumEarned { get; private set; }

    private int currentWaveIndex = 0;
    private float currentWaveTime;
    private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();
    private int enemiesRemaining;
    private bool isWaveInProgress;

    private List<BaseEnemy> waitingEnemies = new List<BaseEnemy>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeEnemySpawners();
    }

    private void Start()
    {
        StartNextWave();
        PlayerKills = 0;
        SynthiumEarned = 0;
        AudioManager.Instance.PlayMusic("LevelMusic");
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
        yield return StartCoroutine(WaveCooldown(wave.waveType));
        isWaveInProgress = true;
        currentWaveTime = 0f;

        foreach (var interval in wave.spawnIntervals)
        {
            activeSpawnCoroutines.Add(StartCoroutine(HandleSpawnInterval(interval)));
        }

        while (currentWaveTime < wave.duration || enemiesRemaining > 0)
        {
            currentWaveTime += Time.deltaTime;
            yield return null;
        }

        foreach (var coroutine in activeSpawnCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeSpawnCoroutines.Clear();

        isWaveInProgress = false;
        currentWaveIndex++;
        StartNextWave();
    }

    IEnumerator HandleSpawnInterval(SpawnInterval interval)
    {
        yield return new WaitForSeconds(interval.startTime);

        while (currentWaveTime < interval.endTime)
        {
            foreach (int spawnerID in interval.spawnerID)
            {
                if (spawnerID >= 0 && spawnerID < enemySpawners.Length)
                {
                    enemySpawners[spawnerID].SpawnEnemy(interval.enemyID);
                    enemiesRemaining++;
                }
            }
            yield return new WaitForSeconds(interval.spawnRate);
        }
    }
    IEnumerator WaveCooldown(WaveType waveType)
    {
        string message;
        Color messageColor;

        switch (waveType)
        {
            case WaveType.Horde:
                message = "Enemy Horde Incoming!";
                messageColor = Color.red;
                break;
            case WaveType.Boss:
                message = "Boss Incoming!!";
                messageColor = Color.magenta;
                break;
            default:
                message = currentWaveIndex < enemyWaves.Count - 1
                    ? $"Wave {currentWaveIndex + 1} incoming!"
                    : "Final wave incoming!";
                messageColor = Color.white;
                break;
        }

        UIManager.Instance.UpdateWaveDisplay(message, messageColor);
        yield return new WaitForSeconds(waveCooldown);
    }

    public void OnEnemyDefeated()
    {
        PlayerKills++;
        enemiesRemaining--;
        if (enemiesRemaining <= 0 && !isWaveInProgress && currentWaveIndex >= enemyWaves.Count)
        {
            CheckForWinCondition();
        }
    }

    public void AddSynthium(int amount)
    {
        SynthiumEarned += amount;
    }

    private void CheckForWinCondition()
    {
        if (currentWaveIndex >= enemyWaves.Count && enemiesRemaining <= 0)
        {
            Debug.Log("Victory! All waves and enemies cleared.");
            GameManager.Instance.WinGame();
        }
    }
    public (int xp, int currency) GetLevelRewards()
    {
        return (xpReward, currencyReward);
    }

    public void AddEnemyToWaitingList(BaseEnemy enemy)
    {
        waitingEnemies.Add(enemy);
    }

    public void NotifyAttackPointAvailable()
    {
        if (waitingEnemies.Count > 0)
        {
            BaseEnemy nextEnemy = waitingEnemies[0];
            waitingEnemies.RemoveAt(0);
            //nextEnemy.FindAttackPoint();
        }
    }

}
