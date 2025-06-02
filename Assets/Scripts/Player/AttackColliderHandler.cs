using System.Collections.Generic;
using UnityEngine;

public class AttackColliderHandler : MonoBehaviour
{
    // DİKKAT: Bu alanın tipi 'CharacterData' olarak güncellendi.
    // Eğer PlayerStats bulunamazsa veya başka amaçlar için bir yedek olarak tutuluyorsa,
    // bu alanın CharacterData ScriptableObject'inize Inspector üzerinden atanması gerekebilir.
    // Mevcut CalculateDamage metodunda aktif olarak kullanılmıyor (yorum satırları hariç).
    public CharacterData characterData; // Önceki 'CharacterScriptableObject' yerine 'CharacterData' kullanıldı.

    private PlayerStats playerStats;
    // HashSet referansı değişmeyeceği için readonly olarak işaretlenebilir.
    private readonly HashSet<EnemyStats> damagedEnemies = new HashSet<EnemyStats>();

    void Awake()
    {
        // Ebeveyn obje üzerinde PlayerStats bileşenini bulmaya çalışır.
        // Bu saldırı collider'ının bir Player objesinin alt objesi olduğu varsayılır.
        playerStats = GetComponentInParent<PlayerStats>();
        if (playerStats == null)
        {
            
        }
    }

    private float CalculateDamage()
    {
        if (playerStats != null)
        {
            // PlayerStats mevcutsa, normal hesaplamayı yap.
            // PlayerStats.CurrentBaseDamage ve PlayerStats.CurrentMight özelliklerinin
            // PlayerStats script'inizde doğru şekilde tanımlandığından emin olun.
            // (CurrentBaseDamage, CharacterData.Stats.baseDamage'den gelmeli)
            // (CurrentMight, CharacterData.Stats.might'tan gelmeli)
            return playerStats.CurrentBaseDamage * playerStats.Stats.might;
        }
        else
        {
            // PlayerStats eksikse, oyuncuya özel statları alamayız.
            // Seçenek 1: CharacterData üzerinden yedek bir hasar hesapla (eğer characterData atanmışsa).
            if (characterData != null)
            {
                
                // CharacterData.stats.baseDamage ve CharacterData.stats.might alanlarının
                // CharacterData ScriptableObject'inizde tanımlı olduğunu varsayar.
                return characterData.stats.baseDamage + (0.5f * characterData.stats.might);
            }
            else
            {
                // Seçenek 2: Sabit kodlanmış bir varsayılan kullan.
                
                return 10f; // Mutlak yedek.
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        float damageAmount = CalculateDamage();
        if (damageAmount <= 0f) // Hasar miktarı 0 veya daha azsa işlem yapma.
        {
            return;
        }

        // Düşmanlara hasar verme
        if (other.CompareTag("Enemy") || other.CompareTag("Boss")) // Çarpışılan objenin tag'i "Enemy" ise
        {
            // Potansiyel olarak eksik bileşenler için TryGetComponent kullanmak daha güvenlidir.
            // EnemyStats script'inin ve TakeDamage metodunun projenizde tanımlı olduğundan emin olun.
            if (other.TryGetComponent(out EnemyStats enemy) && !damagedEnemies.Contains(enemy))
            {
                // Aynı düşmana bu saldırı döngüsünde tekrar hasar vermemek için kontrol.
                enemy.TakeDamage(damageAmount, transform.position); // Düşmanın hasar alma metodu çağrılır.
                damagedEnemies.Add(enemy); // Düşman, bu saldırıda hasar almış olarak işaretlenir.

                // İsteğe Bağlı: Debug log'larını yayınlanmış build'lerden çıkarmak için koşullu derleme kullanın.
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                
                #endif
            }
        }
        // Kırılabilir objelere hasar verme
        else if (other.CompareTag("Prop")) // Çarpışılan objenin tag'i "Prop" ise
        {
            // BreakableProps script'inin ve TakeDamage metodunun projenizde tanımlı olduğundan emin olun.
            if (other.TryGetComponent(out BreakableProps prop))
            {
                prop.TakeDamage(damageAmount); // Kırılabilir objenin hasar alma metodu çağrılır.
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                
                #endif
            }
        }
    }

    // Bu metot, saldırı animasyonu bittiğinde, saldırı collider'ı devre dışı bırakıldığında
    // veya yeni bir saldırı başladığında çağrılmalıdır.
    // Böylece bir sonraki saldırıda aynı düşmanlara tekrar vurulabilir.
    public void ResetDamagedEnemies()
    {
        damagedEnemies.Clear();
        // Debug.Log("Hasar almış düşman listesi sıfırlandı.", gameObject); // Test için log
    }

    // ÖNEMLİ: Bu script'in düzgün çalışması için projenizde aşağıdaki scriptlerin/tiplerin
    // doğru şekilde tanımlanmış olması gerekir:
    // 1. PlayerStats.cs (CurrentBaseDamage ve CurrentMight özellikleriyle birlikte)
    // 2. CharacterData.cs (ScriptableObject, stats.baseDamage ve stats.might alanlarıyla birlikte)
    // 3. EnemyStats.cs (TakeDamage(float damageAmount, Vector3 hitPosition) metoduyla birlikte)
    // 4. BreakableProps.cs (TakeDamage(float damageAmount) metoduyla birlikte)
}