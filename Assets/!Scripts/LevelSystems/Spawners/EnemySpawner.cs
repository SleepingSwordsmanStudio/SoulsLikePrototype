using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    private GameObject currentEnemy;

    void OnEnable()
    {
        // Subskrybujemy wydarzenie odpoczynku
        BonfireUIManager.OnPlayerRest += RespawnEnemy;
    }

    void OnDisable()
    {
        // Odsubskrybowanie jest KLUCZOWE, żeby uniknąć błędów w pamięci
        BonfireUIManager.OnPlayerRest -= RespawnEnemy;
    }

    void Start()
    {
        RespawnEnemy();
    }

    public void RespawnEnemy()
    {
        // Jeśli przeciwnik żyje, usuwamy go
        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
        }

        // Spawnujemy nowego
        currentEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        
        Debug.Log("Przeciwnik odrodzony przez ognisko!");
    }
}