using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SpawnEvent
{
    public float spawnTime;
    public int spawnerID;
    public int numberOfEnemies;
}


[System.Serializable]
public class EnemyWave
{
    public float duration;
    public List<SpawnEvent> spawnEvents;

    public EnemyWave(float duration)
    {
        this.duration = duration;
        spawnEvents = new List<SpawnEvent>();
    }
}
