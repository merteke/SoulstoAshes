// AutoDestroyEffect.cs
using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    public float lifetime = 0.5f; // Efektin ekranda kalma süresi (saniye)

    void Start()
    {
        // Belirtilen süre sonunda bu GameObject'i yok et
        Destroy(gameObject, lifetime);
    }
}