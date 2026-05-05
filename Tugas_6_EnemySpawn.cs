using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    public int maxEnemies = 5;

    [Header("Area Spawn")]
    public float spawnRadius = 5f;

    private float timer;
    private int currentEnemies = 0;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval && currentEnemies < maxEnemies)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

        GameObject enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);

        currentEnemies++;

        // kurangi count saat enemy mati
        enemy.AddComponent<EnemyTracker>().spawner = this;
    }

    public void EnemyDied()
    {
        currentEnemies--;
    }
}