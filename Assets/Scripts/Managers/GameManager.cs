using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    public GameObject player;
    public PlayerController playerController;
    public PlayerWall playerWall;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] Transform[][] waypointPaths;
    [SerializeField] Transform[] wallSegments;

    public bool isPaused;
    public bool isWallDestroyed;

    int enemyCount;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        player = GameObject.FindWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerWall = GameObject.FindWithTag("PlayerWall").GetComponent<PlayerWall>();
    }
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
