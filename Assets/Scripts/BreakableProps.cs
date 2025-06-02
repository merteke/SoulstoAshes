using System.Collections;
using UnityEngine;

public class BreakableProps : MonoBehaviour
{
    public float health = 100f;

    public Sprite brokenSprite; // Kirik halin sprite’i
    private SpriteRenderer spriteRenderer;
    private bool isBroken = false;

    public float fadeDuration = 2f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(float dmg)
    {
        if (isBroken) return;

        health -= dmg;

        if (health <= 0)
        {
            Kill();
        }
    }

    public void Kill()
    {
        isBroken = true;

        if (brokenSprite != null)
        {
            spriteRenderer.sprite = brokenSprite;
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float elapsed = 0f;
        Color originalColor = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
