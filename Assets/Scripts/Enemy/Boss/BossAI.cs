// ---------- BossAI.cs ----------
// Bu script'i Boss prefab'inize ekleyin.
// EnemyMovement script'ini de Boss prefab'inizde tutmaya devam edin veya
// bu script'i EnemyMovement'tan miras aldırın.
// Şimdilik EnemyMovement'ı component olarak tuttuğunuzu varsayıyorum.

using UnityEngine;
using System.Collections; // Coroutine için gerekli

public class BossAI : MonoBehaviour
{
    [Header("Boss Ayarları")]
    public float attackRange = 10f; // Boss'un saldırmaya başlayacağı menzil
    public float retreatDistance = 5f; // Boss'un oyuncudan uzaklaşmaya başlayacağı mesafe (isteğe bağlı)

    [Header("Mermi Ayarları")]
    public GameObject projectilePrefab; // Atanacak mermi prefab'ı
    public Transform firePoint; // Merminin çıkacağı nokta (Boss'un bir alt objesi olabilir)
    public float fireRate = 1f; // Saniyede atılacak mermi sayısı (1f = saniyede 1 mermi)
    public float projectileSpeed = 10f; // Merminin hızı
    public float projectileDamage = 10f; // Merminin vereceği hasar
    public float projectileLifetime = 5f; // Merminin yok olmadan önce ne kadar süre var olacağı

    private float nextFireTime = 0f; // Bir sonraki ateş etme zamanı
    private Transform player; // Oyuncu referansı
    private EnemyMovement enemyMovement; // Temel hareket script'i referansı
    private EnemyStats enemyStats; // Düşman istatistikleri referansı
    private SpriteRenderer spriteRenderer; // Sprite yönünü ayarlamak için

    void Start()
    {
        // Oyuncuyu bul
        PlayerStats playerStatsComponent = FindFirstObjectByType<PlayerStats>();
        if (playerStatsComponent != null)
        {
            player = playerStatsComponent.transform;
        }
        else
        {
            PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            if (allPlayers.Length > 0)
            {
                player = allPlayers[Random.Range(0, allPlayers.Length)].transform;
            }
            else
            {
                Debug.LogError("Oyuncu (PlayerStats veya PlayerMovement ile) bulunamadı! BossAI script'i oyuncu gerektirir.");
                enabled = false; // Script'i devre dışı bırak
                return;
            }
        }


        // Diğer component'leri al
        enemyMovement = GetComponent<EnemyMovement>();
        if (enemyMovement == null)
        {
            Debug.LogError("EnemyMovement script'i Boss üzerinde bulunamadı!");
            enabled = false;
            return;
        }
        enemyStats = GetComponent<EnemyStats>();
        if (enemyStats == null)
        {
            Debug.LogError("EnemyStats script'i Boss üzerinde bulunamadı!");
            enabled = false;
            return;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer Boss üzerinde bulunamadı. Sprite yönü ayarlanamayacak.");
        }


        // Eğer firePoint atanmamışsa, boss'un kendi transformunu kullan
        if (firePoint == null)
        {
            Debug.LogWarning("Fire Point atanmamış. Boss'un pozisyonu mermi çıkış noktası olarak kullanılacak.");
            firePoint = transform;
        }
    }

    void Update()
    {
        if (player == null || enemyMovement == null) return; // Oyuncu veya enemyMovement yoksa bir şey yapma

        // Knockback durumunu kontrol et (EnemyMovement'tan)
        if (enemyMovement.IsKnockedBack) 
        {
            // Knockback sırasında saldırı veya özel hareketler yapma
            return;
        }

        // Oyuncuya olan mesafeyi hesapla
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Sprite yönünü ayarla
        if (spriteRenderer != null)
        {
            Vector3 directionToPlayer = player.position - transform.position;
            if (directionToPlayer.x != 0)
            {
                spriteRenderer.flipX = directionToPlayer.x < 0;
            }
        }


        // Saldırı menzilindeyse ve ateş etme zamanı geldiyse ateş et
        if (distanceToPlayer <= attackRange && Time.time >= nextFireTime)
        {
            Attack();
            nextFireTime = Time.time + 1f / fireRate; // Bir sonraki ateş etme zamanını ayarla
        }
        else
        {
            // Saldırı menzilinde değilse veya henüz ateş etme zamanı gelmediyse normal hareket et
            if (distanceToPlayer > attackRange)
            {
                // Oyuncuya doğru hareket et (EnemyMovement.Move() zaten Update içinde çağrılıyor olmalı)
            }
            else if (distanceToPlayer < retreatDistance && distanceToPlayer <= attackRange)
            {
                // Oyuncudan uzaklaş (isteğe bağlı)
            }
        }
    }

    void Attack()
    {
        if (projectilePrefab == null || firePoint == null || player == null)
        {
            Debug.LogWarning("Mermi prefabı, ateşleme noktası veya oyuncu atanmamış. Saldırı yapılamıyor.");
            return;
        }

        // Debug.Log(gameObject.name + " saldırıyor!"); // İsteğe bağlı: Saldırı logu

        // Merminin oyuncuya doğru yönünü hesapla
        Vector2 direction = (player.position - firePoint.position).normalized;

        // Mermiyi oluştur
        GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        BossProjectile projectileScript = projectileGO.GetComponent<BossProjectile>();

        if (projectileScript != null)
        {
            // Mermiyi ateşle
            projectileScript.Initialize(direction, projectileSpeed, projectileDamage, projectileLifetime, gameObject.tag);
        }
        else
        {
            Debug.LogError("Mermi prefabında BossProjectile script'i bulunamadı!");
            Rigidbody2D rb = projectileGO.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
                Destroy(projectileGO, projectileLifetime);
            }
            else
            {
                Debug.LogError("Mermi prefabında Rigidbody2D bulunamadı ve BossProjectile script'i de yok!");
                Destroy(projectileGO);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
}