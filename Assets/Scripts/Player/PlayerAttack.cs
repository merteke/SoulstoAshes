using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public AttackColliderHandler attackColliderHandler; // Inspector’dan atanacak

    public Animator playerAnimator;
    public Collider2D attackCollider;

    public SpriteRenderer sr;               // Sprite yönü
    public Transform attackColliderTransform; // Collider'ı hareket ettireceğimiz nesne

    private bool canAttack = true;
    // public float attackCooldownTime = 0.5f; // Saldırı bekleme süresi (opsiyonel)

    [Header("Slash Efekti Ayarları")]
    public GameObject slashEffectPrefab;    // Inspector'dan SlashEfektiPrefab'ını buraya sürükleyin
    public Transform slashSpawnPoint;       // Slash efektinin çıkacağı nokta (karakterin elinde boş bir GameObject olabilir)
                                            // Eğer bu atanmazsa, karakterin pozisyonuna göre bir ofset kullanılacak.
    public float slashEffectYOffset = 0.8f; // slashSpawnPoint kullanılmazsa Y ekseninde eklenecek ofset
    public float slashEffectXOffset = 0.5f;

    void Awake()
    {
        DisableAttackCollider();
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        // Saldırı Girişi (Sol Tık ile)
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            PerformAttack();

            // Opsiyonel: Saldırıdan sonra kısa bir bekleme süresi ekleyebilirsiniz.
            // canAttack = false;
            // Invoke(nameof(ResetAttackCooldown), attackCooldownTime);
        }

        // Collider yönünü Sprite yönüne göre ayarla
        if (attackColliderTransform != null && sr != null)
        {
            // Pozisyonu ayarla
            Vector3 pos = attackColliderTransform.localPosition;
            pos.x = Mathf.Abs(pos.x) * (sr.flipX ? -1 : 1);
            attackColliderTransform.localPosition = pos;

            // Aynalama için scale kullan
            Vector3 scale = attackColliderTransform.localScale;
            scale.x = (sr.flipX ? -1 : 1) * Mathf.Abs(scale.x);
            attackColliderTransform.localScale = scale;
        }
    }

    void PerformAttack()
    {
        if (playerAnimator != null)
        {
            // Animator'deki trigger adını "Attack1" yerine daha genel bir şey yapabilirsiniz,
            // örneğin sadece "Attack". Animator Controller'da da bu trigger'ı kullanmanız gerekir.
            playerAnimator.SetTrigger("Attack1"); // "Attack1" yerine "Attack" olarak değiştirebilirsiniz.
            

            // Opsiyonel: Burada canAttack = false; ayarlayıp animasyon event'i ile veya Invoke ile true yapabilirsiniz.
        }
    }

    // Opsiyonel: Saldırı bekleme süresi sıfırlama fonksiyonu
    // void ResetAttackCooldown()
    // {
    //     canAttack = true;
    // }

    public void EnableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
            attackColliderHandler?.ResetDamagedEnemies();
        }
    }

    public void DisableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    public void TriggerSlashEffect()
    {
        if (slashEffectPrefab == null)
        {
            
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity; // Genellikle 2D efektler için varsayılan rotasyon yeterlidir.

        // SpriteRenderer'ı al (karakterin yönünü belirlemek için)
        SpriteRenderer characterSr = GetComponent<SpriteRenderer>(); // Veya GetComponentInChildren<SpriteRenderer>()
        if (characterSr == null) {
            // Eğer PlayerAnimatorr script'inde sr referansınız varsa onu da kullanabilirsiniz:
            // PlayerAnimatorr playerAnim = GetComponent<PlayerAnimatorr>();
            // if(playerAnim != null) characterSr = playerAnim.sr;
        }


        if (slashSpawnPoint != null)
        {
            spawnPosition = slashSpawnPoint.position;
            // Opsiyonel: Çıkış noktasının rotasyonunu da kullanabilirsiniz
            // spawnRotation = slashSpawnPoint.rotation;
        }
        else
        {
            // slashSpawnPoint atanmamışsa, karakterin pozisyonuna göre bir ofset hesapla
            float actualXOffset = slashEffectXOffset;
            if (characterSr != null && characterSr.flipX) // Karakter sola bakıyorsa X ofsetini ters çevir
            {
                actualXOffset = -slashEffectXOffset;
            }
            spawnPosition = transform.position + new Vector3(actualXOffset, slashEffectYOffset, 0);
        }

        // Slash efektini yarat
        GameObject spawnedSlash = Instantiate(slashEffectPrefab, spawnPosition, spawnRotation);

        // Efektin karakterin yönüne göre dönmesi/flip olması (opsiyonel, efektin tasarımına bağlı)
        if (characterSr != null && characterSr.flipX)
        {
            // Eğer slash efektiniz sağa bakacak şekilde tasarlandıysa ve karakter sola dönükse,
            // efektin X scale'ini ters çevirerek görsel olarak flip edebilirsiniz.
            Vector3 localScale = spawnedSlash.transform.localScale;
            localScale.x *= -1;
            spawnedSlash.transform.localScale = localScale;
            // Veya Y ekseninde 180 derece döndürebilirsiniz (sprite pivotuna göre değişir):
            // spawnedSlash.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        
    }
    
}