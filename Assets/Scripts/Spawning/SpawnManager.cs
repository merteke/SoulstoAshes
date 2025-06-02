using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{

    int currentWaveIndex; // Mevcut dalganın indeksi [Liste 0'dan başlar]
    int currentWaveSpawnCount = 0; // Mevcut dalganın ne kadar düşman spawn ettiğini takip eder.
    List<GameObject> existingSpawns = new List<GameObject>();

    public WaveData[] data; // Dalga verilerini tutar
    public Camera referenceCamera; // Referans kamera

    [Tooltip("Eğer bu sayıdan fazla düşman varsa, performans için daha fazla spawn etme.")]
    public int maximumEnemyCount = 300;
    float spawnTimer; // Bir sonraki düşman grubunu ne zaman spawn edeceğini belirlemek için kullanılan zamanlayıcı.
    float currentWaveDuration = 0f; // Mevcut dalganın ne kadar süredir aktif olduğu
    public bool boostedByCurse = true; // Lanet (curse) tarafından etkilenip etkilenmediği

    public static SpawnManager instance; // Singleton instance

    void Start()
    {
        if (instance && instance != this) // instance zaten varsa ve bu değilse
        {
            
            // Destroy(gameObject); // Opsiyonel: Fazla olanı yok et
        }
        else if (instance == null)
        {
            instance = this;
        }
        
        if (data == null || data.Length == 0)
        {
            
            enabled = false;
            return;
        }
        currentWaveIndex = 0; // İlk dalgadan başla
    }

    void Update()
    {
        // --- YENİ EKLENEN KISIM (Boss Savaşı Kontrolü) ---
        if (GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.BossFight)
        {
            if (enabled) 
            {
                
                enabled = false; 
            }
            return; 
        }
        // --- YENİ EKLENEN KISIM SONU ---

        if (!enabled || data == null || data.Length == 0 || currentWaveIndex >= data.Length)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        currentWaveDuration += Time.deltaTime;

        if (spawnTimer <= 0)
        {
            if (HasWaveEnded())
            {
                currentWaveIndex++;
                currentWaveDuration = 0; 
                currentWaveSpawnCount = 0;

                if (currentWaveIndex >= data.Length)
                {
                    
                    enabled = false;
                    return; 
                }
            }

            if (!CanSpawn())
            {
                ActivateCooldown(); 
                return;
            }

            // WaveData'nın null olmadığından ve currentWaveIndex'in geçerli olduğundan emin olalım
            if (data[currentWaveIndex] == null)
            {
                
                ActivateCooldown();
                return;
            }
            
            GameObject[] spawns = data[currentWaveIndex].GetSpawns(EnemyStats.count);

            if (spawns != null) // GetSpawns null dönebilir, kontrol edelim
            {
                foreach (GameObject prefab in spawns)
                {
                    if (prefab == null) continue; // Prefab null ise atla
                    if (!CanSpawn()) continue;

                    existingSpawns.Add(Instantiate(prefab, GeneratePosition(), Quaternion.identity));
                    currentWaveSpawnCount++;
                }
            }
            ActivateCooldown();
        }
    }

    public void ActivateCooldown()
    {
        if (data == null || data.Length == 0 || currentWaveIndex >= data.Length || currentWaveIndex < 0 || data[currentWaveIndex] == null)
        {
            spawnTimer = float.MaxValue; 
            return;
        }

        float curseBoost = 1f;
        if (boostedByCurse && GameManager.instance != null) 
        {
            // GetCumulativeCurse static olduğu için GameManager.instance.GetCumulativeCurse() de çalışır.
            curseBoost = GameManager.GetCumulativeCurse(); 
        }
        
        // WaveData'dan spawn interval alınıyor
        float spawnInterval = data[currentWaveIndex].GetSpawnInterval();
        spawnTimer += spawnInterval / Mathf.Max(0.001f, curseBoost); 
    }

    public bool CanSpawn()
    {
        if (data == null || data.Length == 0 || currentWaveIndex >= data.Length || currentWaveIndex < 0 || data[currentWaveIndex] == null) return false;

        if (HasExceededMaxEnemies()) return false;

        WaveData currentWave = data[currentWaveIndex]; 

        if (currentWave.totalSpawns > 0 && currentWaveSpawnCount >= currentWave.totalSpawns) return false;
        if (currentWave.duration > 0 && currentWaveDuration >= currentWave.duration) return false;
        
        return true;
    }

    public static bool HasExceededMaxEnemies()
    {
        if (!instance) return false; 
        // EnemyStats.count'un var olduğunu ve doğru güncellendiğini varsayıyoruz.
        if (EnemyStats.count >= instance.maximumEnemyCount) return true;
        return false;
    }

    public bool HasWaveEnded()
    {
        if (data == null || data.Length == 0 || currentWaveIndex >= data.Length || currentWaveIndex < 0 || data[currentWaveIndex] == null) return true; 

        WaveData currentWave = data[currentWaveIndex];

        // WaveData.ExitCondition enum'unuzun waveDuration ve reachedTotalSpawns üyelerini içerdiğini varsayıyoruz.
        if ((currentWave.exitConditions & WaveData.ExitCondition.waveDuration) > 0)
        {
            if (currentWave.duration > 0 && currentWaveDuration < currentWave.duration) return false;
        }
            
        if ((currentWave.exitConditions & WaveData.ExitCondition.reachedTotalSpawns) > 0)
        {
             if (currentWave.totalSpawns > 0 && currentWaveSpawnCount < currentWave.totalSpawns) return false;
        }
           
        // --- DÜZELTİLEN KISIM ---
        // Orijinal 'mustKillAll' boolean kontrolüne geri dönüldü.
        // WaveData script'inizde 'public bool mustKillAll;' şeklinde bir alan olmalıdır.
        existingSpawns.RemoveAll(item => item == null); 
        if (currentWave.mustKillAll && existingSpawns.Count > 0) 
        {
            return false;
        }
        // --- DÜZELTİLEN KISIM SONU ---
        
        // Eğer hiçbir "return false" tetiklenmediyse ve en az bir çıkış koşulu (exitConditions != 0 veya mustKillAll true ise)
        // karşılanmışsa dalga bitmiştir. Eğer hiçbir çıkış koşulu tanımlanmamışsa (örneğin exitConditions = 0 ve mustKillAll = false),
        // bu mantık dalganın hemen bitmesine neden olabilir. WaveData'daki çıkış koşullarınızın doğru yapılandırıldığından emin olun.
        // Eğer hiçbir koşul (waveDuration, reachedTotalSpawns, mustKillAll) dalgayı aktif tutmuyorsa, bitmiş sayılır.
        return true; 
    }

    void Reset()
    {
        if (Camera.main != null)
        {
            referenceCamera = Camera.main;
        }
    }

    public static Vector3 GeneratePosition()
    {
        if (instance == null)
        {
            
            return Vector3.zero; 
        }

        if (!instance.referenceCamera)
        {
            instance.referenceCamera = Camera.main;
            if (!instance.referenceCamera)
            {
                
                return Vector3.zero; 
            }
        }

        if (!instance.referenceCamera.orthographic)
            Debug.LogWarning("Referans kamera ortografik değil! Bu, düşman spawn'larının bazen kamera sınırları içinde görünmesine neden olabilir!");

        float randomValue = Random.value; 
        float x, y;
        int edge = Random.Range(0, 4);
        const float offset = 0.1f; 

        if (edge == 0) { x = -offset; y = randomValue; }
        else if (edge == 1) { x = 1f + offset; y = randomValue; }
        else if (edge == 2) { x = randomValue; y = -offset; }
        else { x = randomValue; y = 1f + offset; }
        
        // ViewportToWorldPoint için z değeri kameranın görüş alanında olmalı, nearClipPlane'den biraz ilerisi uygun.
        Vector3 spawnPos = instance.referenceCamera.ViewportToWorldPoint(new Vector3(x, y, instance.referenceCamera.nearClipPlane + 1f)); 
        spawnPos.z = 0f; // 2D oyunlar için genellikle Z ekseni 0'dır.
        return spawnPos;
    }

    public static bool IsWithinBoundaries(Transform checkedObject)
    {
        if (instance == null || checkedObject == null) return false;

        Camera c = instance.referenceCamera ? instance.referenceCamera : Camera.main;
        if (c == null) return false; 

        Vector3 viewportPoint = c.WorldToViewportPoint(checkedObject.position);
        
        const float boundaryOffset = 0.1f; 
        if (viewportPoint.x < 0 - boundaryOffset || viewportPoint.x > 1 + boundaryOffset) return false;
        if (viewportPoint.y < 0 - boundaryOffset || viewportPoint.y > 1 + boundaryOffset) return false;
        
        return true; 
    }
}
