using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private Transform playerSpawn;

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private Transform[] spawnPoints;

    private int enemyCount;

    private PlayerController player;

    public static GameManager Instance;

    private void Awake()
    {
        if (Instance != null)
            Destroy(this);

        Instance = this;
    }

    private void Start()
    {
        player = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity, null).GetComponent<PlayerController>();
        StartWave();
    }

    private void StartWave()
    {

    }

    private void FinishWave()
    {
        //player.OnWaveComplete();
    }

    public void OnEnemyKilled()
    {
        enemyCount -= 1;

        if (enemyCount <= 0)
            FinishWave();
    }

    public void Quit()
    {

    }
}
