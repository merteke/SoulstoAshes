using UnityEngine;

/// <summary>
/// Bu sınıf, sprite'ların Y eksenine göre otomatik olarak sıralanmasını sağlamak için
/// diğer sınıflar tarafından alt sınıf olarak kullanılabilir.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public abstract class Sortable : MonoBehaviour
{
    SpriteRenderer sortedRenderer; // 'sorted' yerine daha açıklayıcı bir isim: 'sortedRenderer'
    public bool sortingActive = true; // Bu, belirli objelerde sıralamayı devre dışı bırakmamızı sağlar.
    public const float MIN_DISTANCE = 0.2f; // Sıralama hassasiyeti için minimum mesafe. Değiştirilebilir.
    int lastCalculatedSortOrder = 0; // En son hesaplanan sıralama düzeni

    // Start, ilk frame güncellemesinden önce çağrılır
    protected virtual void Start()
    {
        sortedRenderer = GetComponent<SpriteRenderer>();
        if (sortedRenderer == null)
        {
            
            sortingActive = false; // SpriteRenderer yoksa sıralamayı devre dışı bırak
        }
        else
        {
            // Başlangıçta lastCalculatedSortOrder'ı mevcut sıralama düzeniyle senkronize et
            // veya hesaplanmış bir değerle başlat.
            // Bu, ilk frame'de gereksiz bir güncelleme olmasını engelleyebilir.
            // Şimdilik 0 olarak bırakmak da bir sorun teşkil etmeyebilir,
            // ancak ilk LateUpdate'te bir sıralama ayarı yapılacaktır.
            // lastCalculatedSortOrder = (int)(-transform.position.y / MIN_DISTANCE);
            // sortedRenderer.sortingOrder = lastCalculatedSortOrder;
        }
    }

    // LateUpdate, her frame güncellemesinden sonra çağrılır
    // Sıralama işlemleri genellikle LateUpdate'te yapılır çünkü tüm hareketler tamamlanmış olur.
    protected virtual void LateUpdate()
    {
        // --- DÜZELTME: sortingActive kontrolü eklendi ---
        if (!sortingActive || sortedRenderer == null)
        {
            return; // Sıralama aktif değilse veya SpriteRenderer yoksa hiçbir şey yapma
        }
        // --- DÜZELTME SONU ---

        // Y pozisyonuna göre yeni sıralama düzenini hesapla
        // Y değeri arttıkça sıralama değeri azalır (daha önde görünür)
        // Y değeri azaldıkça sıralama değeri artar (daha arkada görünür)
        int newSortOrder = (int)(-transform.position.y / MIN_DISTANCE);

        // Sadece hesaplanan sıralama düzeni değiştiyse SpriteRenderer'ın sortingOrder'ını güncelle
        // Bu, gereksiz yere sortingOrder atamalarını engeller.
        if (lastCalculatedSortOrder != newSortOrder)
        {
            sortedRenderer.sortingOrder = newSortOrder;
            lastCalculatedSortOrder = newSortOrder; // En son hesaplanan sıralama düzenini güncelle
        }
    }
}