using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class WaveSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gameManager = (GameManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave System", EditorStyles.boldLabel);

        if (GUILayout.Button("Add New Wave"))
        {
            gameManager.enemyWaves.Add(new EnemyWave(60f)); // Default 60 second wave
        }

        for (int i = 0; i < gameManager.enemyWaves.Count; i++)
        {
            EnemyWave wave = gameManager.enemyWaves[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Wave {i + 1}", EditorStyles.boldLabel);

            wave.duration = EditorGUILayout.FloatField("Duration", wave.duration);
            wave.waveType = (WaveType)EditorGUILayout.EnumPopup("Wave Type", wave.waveType);

            if (GUILayout.Button("Add Spawn Interval"))
            {
                wave.spawnIntervals.Add(new SpawnInterval());
            }

            for (int j = 0; j < wave.spawnIntervals.Count; j++)
            {
                SpawnInterval interval = wave.spawnIntervals[j];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Spawn Interval {j + 1}", EditorStyles.boldLabel);

                interval.startTime = EditorGUILayout.FloatField("Start Time", interval.startTime);
                interval.endTime = EditorGUILayout.FloatField("End Time", interval.endTime);
                interval.enemyID = EditorGUILayout.IntField("Enemy ID", interval.enemyID);
                interval.spawnRate = EditorGUILayout.FloatField("Spawn Rate", interval.spawnRate);

                SerializedProperty spawnerIDsProp = new SerializedObject(target).FindProperty($"enemyWaves.Array.data[{i}].spawnIntervals.Array.data[{j}].spawnerIDs");
                EditorGUILayout.PropertyField(spawnerIDsProp, true);

                if (GUILayout.Button("Remove Interval"))
                {
                    wave.spawnIntervals.RemoveAt(j);
                    j--;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Remove Wave"))
            {
                gameManager.enemyWaves.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndVertical();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(gameManager);
        }
    }
}