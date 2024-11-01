using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public string levelId;      // e.g., "OPZ1-1"
    public string levelName;    // e.g., "Operation Zone 1 - 1"
    public string sceneName;    // The Unity scene name for this level
    public bool isFirstLevel;   // Is this the first level in the sequence
    public string nextLevelId;  // The ID of the next level in sequence
}
