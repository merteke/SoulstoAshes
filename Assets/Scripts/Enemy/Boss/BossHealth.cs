using UnityEngine;

// Add this script to your boss GameObject
public class BossHealth : MonoBehaviour
{
    [Header("Boss Settings")]
    public float maxHealth = 1000f;
    private float currentHealth;
    
    [Header("Death Effects")]
    public GameObject deathEffect;
    public float deathEffectDuration = 3f;
    
    [Header("Victory Notification")]
    public string victoryText = "BOSS DEFEATED!";
    public float textDuration = 3f;
    public float textFloatSpeed = 1.5f;

    // Cached reference
    private Animator animator;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;
        
        currentHealth -= damage;
        
        // Optional: Play hit animation
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
        
        // Check if boss is defeated
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // Optional: Play death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
            
            // If your death animation needs time to play, you can use a coroutine
            // StartCoroutine(DeathSequence());
            // return;
        }
        
        // Spawn death effect if available
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, deathEffectDuration);
        }
        
        // Display victory text
        GameManager.GenerateFloatingText(victoryText, transform, textDuration, textFloatSpeed);
        
        // Notify GameManager about boss defeat
        if (GameManager.instance != null)
        {
            GameManager.instance.OnBossDefeated();
        }
        
        // Destroy the boss
        Destroy(gameObject);
    }
    
    /* If you need to wait for animation to finish
    private IEnumerator DeathSequence()
    {
        // Wait for animation to complete
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        
        // Rest of the death logic
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, deathEffectDuration);
        }
        
        GameManager.GenerateFloatingText(victoryText, transform, textDuration, textFloatSpeed);
        
        if (GameManager.instance != null)
        {
            GameManager.instance.OnBossDefeated();
        }
        
        Destroy(gameObject);
    }
    */
}