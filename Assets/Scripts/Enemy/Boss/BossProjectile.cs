using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private Vector2 direction;
    private string ownerTag;

    private Rigidbody2D rb;
    private Animator animator; // Animasyonları kontrol etmek için
    private Collider2D coll; // Collider'ı devre dışı bırakmak için

    private bool hasHit = false; // Merminin bir şeye çarpıp çarpmadığını takip eder

    [Header("Animasyon Ayarları")]
    public string impactAnimationTrigger = "Impact"; // Çarpma animasyonunu tetikleyen trigger adı
    public float impactAnimationDuration = 0.5f; // Çarpma animasyonunun yaklaşık süresi (yok etmeden önce beklemek için)

    [Header("Yön Ayarları")]
    public float rotationOffset = 0f; // Mermi sprite'ınızın varsayılan yönüne göre ek bir dönüş ofseti (örneğin sprite sağa bakıyorsa 0, yukarı bakıyorsa -90)


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError("Mermi üzerinde Rigidbody2D bulunamadı!");
        }
        if (animator == null)
        {
            Debug.LogWarning("Mermi üzerinde Animator bulunamadı! Animasyonlar çalışmayacak.");
        }
        if (coll == null)
        {
            Debug.LogError("Mermi üzerinde Collider2D bulunamadı!");
        }
    }

    public void Initialize(Vector2 dir, float spd, float dmg, float lifetime, string owner)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        ownerTag = owner;
        hasHit = false;

        // Mermiyi hareket yönüne doğru döndür
        if (direction != Vector2.zero) // Yön sıfır değilse (hataları önlemek için)
        {
            // Atan2 ile y ve x bileşenlerinden açıyı (radyan cinsinden) buluruz.
            // Mathf.Rad2Deg ile radyanı dereceye çeviririz.
            // rotationOffset, sprite'ınızın varsayılan yönüne göre ayarlama yapmanızı sağlar.
            // Örneğin, sprite'ınız varsayılan olarak sağa (pozitif X ekseni) bakıyorsa, offset 0 olmalıdır.
            // Eğer sprite'ınız yukarı (pozitif Y ekseni) bakıyorsa, offset -90 olmalıdır ki sağa doğru hareket ettiğinde doğru görünsün.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle + rotationOffset, Vector3.forward);
        }


        // Mermiyi belirli bir süre sonra yok et (eğer hiçbir şeye çarpmazsa)
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Eğer mermi bir şeye çarptıysa hareket etmeyi durdur
        if (hasHit)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // Mermiyi hareket ettir
        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
        {
            // Hareket yönü zaten Initialize'da ayarlandığı için burada tekrar yönü kullanıyoruz.
            // Rigidbody.velocity, dünya koordinat sistemine göre hızı ayarlar.
            // Merminin kendi lokal "ileri" yönünde gitmesi için transform.Translate kullanılabilir
            // veya hız vektörü, merminin mevcut dönüşüne göre ayarlanabilir.
            // Ancak, mermi zaten ateşlendiği anda hedefe doğru yönlendirildiği için
            // `direction` vektörünü kullanmak genellikle yeterlidir.
            rb.linearVelocity = direction * speed;
        }
        else if (rb == null)
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit || (coll != null && !coll.enabled))
        {
            return;
        }

        if (collision.CompareTag(ownerTag))
        {
            return;
        }

        bool shouldImpact = false;

        if (collision.CompareTag("Player"))
        {
            //Debug.Log("Mermi oyuncuya çarptı!"); // Test için
            PlayerStats playerStats = collision.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("Oyuncuda PlayerStats script'i bulunamadı!");
            }
            shouldImpact = true;
        }
        else if (collision.CompareTag("Environment") || collision.CompareTag("Enemy"))
        {
            if (ownerTag == "Boss" && collision.CompareTag("Enemy"))
            {
                return;
            }
            //Debug.Log("Mermi bir engele çarptı: " + collision.name); // Test için
            shouldImpact = true;
        }

        if (shouldImpact)
        {
            hasHit = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            if (coll != null)
            {
                coll.enabled = false;
            }

            if (animator != null && !string.IsNullOrEmpty(impactAnimationTrigger))
            {
                animator.SetTrigger(impactAnimationTrigger);
                Destroy(gameObject, impactAnimationDuration);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    public void DestroyAfterImpactAnimation()
    {
        Destroy(gameObject);
    }
}
