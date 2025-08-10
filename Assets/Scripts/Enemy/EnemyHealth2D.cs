using UnityEngine;

public class EnemyHealth2D : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private GameObject deathEffect;
    
    [Header("Damage Effects")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.1f;
    
    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private FlyingMobAI2D flyingAI;
    
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        flyingAI = GetComponent<FlyingMobAI2D>();
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        // Trigger damage effects
        StartCoroutine(DamageFlash());
        
        // Trigger AI hurt animation
        if (flyingAI != null)
        {
            flyingAI.TakeDamage(damage);
        }
        
        if (IsDead)
        {
            Die();
        }
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(damageFlashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    private void Die()
    {
        // Create death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // Disable AI
        if (flyingAI != null)
        {
            flyingAI.enabled = false;
        }
        
        // Disable physics
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Disable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Destroy or disable
        if (destroyOnDeath)
        {
            Destroy(gameObject, 0.1f);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    public void Heal(int amount)
    {
        if (IsDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
}
