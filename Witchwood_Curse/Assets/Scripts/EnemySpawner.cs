using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab; // The enemy prefab to spawn
    [SerializeField] private Transform[] spawnPoints; // Array of spawn points
    [SerializeField] private float spawnInterval = 30f; // Time between waves
    [SerializeField] private float totalDuration = 15f * 60f; // Total duration in seconds
    private int enemyNum = 0;

    private void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        float elapsedTime = 0f;

        while (elapsedTime < totalDuration)
        {
            SpawnWave();
            elapsedTime += spawnInterval;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnWave()
    {
        // You can adjust the number of enemies to spawn in each wave here
        int numberOfEnemies = Random.Range(1, 5); // Example: spawn between 1 to 5 enemies

        for (int i = 0; i < numberOfEnemies; i++)
        {
            // Select a random spawn point from the array
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            newEnemy.name = "Enemy" + enemyNum;
            enemyNum++;
        }
    }
} 