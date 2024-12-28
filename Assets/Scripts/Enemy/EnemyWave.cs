using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType { Normal, Horde, Boss }


[System.Serializable]
public class SpawnInterval
{
    public float startTime;
    public float endTime;
    public int enemyID;
    public List<int> spawnerID;
    public float spawnRate;
    public SpawnInterval()
    {
        spawnerID = new List<int>();
    }
}

[System.Serializable]
public class EnemyWave
{
    public float duration;
    public WaveType waveType = WaveType.Normal;
    public List<SpawnInterval> spawnIntervals;

    public EnemyWave(float duration)
    {
        this.duration = duration;
        spawnIntervals = new List<SpawnInterval>();
    }
}
