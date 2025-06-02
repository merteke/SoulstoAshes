using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public List<GameObject> terrainChunks;
    public GameObject player;
    public float checkerRadius;
    public LayerMask terrainMask;
    public GameObject currentChunk;
    Vector3 playerLastPosition;

    [Header("Optimization")]
    public List<GameObject> spawnedChunks;
    GameObject latestChunk;
    public float maxOpDist; //Must be greater than the length and width of the tilemap
    float opDist;
    float optimizerCooldown;
    public float optimizerCooldownDur;
    bool initialChunksSpawned = false;

    void Start()
    {
        playerLastPosition = player.transform.position;
    }

    void Update()
    {
        if (currentChunk != null && !initialChunksSpawned)
        {
            ForceCheckAllDirections();
            initialChunksSpawned = true;
        }
        ChunkChecker();
        ChunkOptimzer();
    }

    void ChunkChecker()
    {
        if (!currentChunk)
        {
            return;
        }

        Vector3 moveDir = player.transform.position - playerLastPosition;
        playerLastPosition = player.transform.position;

        string directionName = GetDirectionName(moveDir);

        CheckAndSpawnChunk(directionName);

        // Check additional adjacent directions for diagonal chunks
        if (directionName.Contains("Up"))
        {
            CheckAndSpawnChunk("Up");
        }
        if (directionName.Contains("Down"))
        {
            CheckAndSpawnChunk("Down");
        }
        if (directionName.Contains("Right"))
        {
            CheckAndSpawnChunk("Right");
        }
        if (directionName.Contains("Left"))
        {
            CheckAndSpawnChunk("Left");
        }
    }

    void CheckAndSpawnChunk(string direction)
    {
        if (!Physics2D.OverlapCircle(currentChunk.transform.Find(direction).position, checkerRadius, terrainMask))
        {
            SpawnChunk(currentChunk.transform.Find(direction).position);
        }
    }

    string GetDirectionName(Vector3 direction)
    {
        direction = direction.normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Moving horizontally more than vertically
            if (direction.y > 0.5f)
            {
                // Also moving upwards
                return direction.x > 0 ? "Right Up" : "Left Up";
            }
            else if (direction.y < -0.5f)
            {
                // Also moving downwards
                return direction.x > 0 ? "Right Down" : "Left Down";
            }
            else
            {
                // Moving straight horizontally
                return direction.x > 0 ? "Right" : "Left";
            }
        }
        else
        {
            // Moving vertically more than horizontally
            if (direction.x > 0.5f)
            {
                // Also moving right
                return direction.y > 0 ? "Right Up" : "Right Down";
            }
            else if (direction.x < -0.5f)
            {
                // Also moving left
                return direction.y > 0 ? "Left Up" : "Left Down";
            }
            else
            {
                // Moving straight vertically
                return direction.y > 0 ? "Up" : "Down";
            }
        }
    }

    void SpawnChunk(Vector3 spawnPosition)
    {
        int rand = Random.Range(0, terrainChunks.Count);
        latestChunk = Instantiate(terrainChunks[rand], spawnPosition, Quaternion.identity);
        spawnedChunks.Add(latestChunk);
    }

    void ChunkOptimzer()
    {
        optimizerCooldown -= Time.deltaTime;

        if (optimizerCooldown <= 0f)
        {
            optimizerCooldown = optimizerCooldownDur;   //Check every 1 second to save cost, change this value to lower to check more times
        }
        else
        {
            return;
        }

        foreach (GameObject chunk in spawnedChunks)
        {
            opDist = Vector3.Distance(player.transform.position, chunk.transform.position);
            if (opDist > maxOpDist)
            {
                chunk.SetActive(false);
            }
            else
            {
                chunk.SetActive(true);
            }
        }
    }

    void ForceCheckAllDirections()
{
    if (!currentChunk)
    {
        
        return;
    }

    // Kontrol edilecek ana yönler (Chunk prefab'larınızdaki child obje isimleriyle eşleşmeli)
    string[] allDirections = { "Up", "Down", "Left", "Right" };
    // Çapraz yönler için child objeleriniz varsa onları da ekleyebilirsiniz:
    // string[] allDirections = { "Up", "Down", "Left", "Right", "UpRight", "UpLeft", "DownRight", "DownLeft" };

    
    foreach (string dir in allDirections)
    {
        Transform directionAnchor = currentChunk.transform.Find(dir);
        if (directionAnchor != null)
        {
            if (!Physics2D.OverlapCircle(directionAnchor.position, checkerRadius, terrainMask))
            {
                
                SpawnChunk(directionAnchor.position);
            }
        }

    }
}
}