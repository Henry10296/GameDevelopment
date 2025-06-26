using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("当前状态")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isDead = false;
    
    // 配置引用
    private EnemyConfig config;
    private Animator animator;
    private AudioSource audioSource;
    
    // 属性访问器
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead() => isDead;
    public float GetHealthPercentage() => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    
    void Start()
    {
        if (config == null)
        {
            // 尝试从EnemyAI获取配置
            if (TryGetComponent<EnemyAI>(out var ai) && ai.enemyConfig != null)
            {
                Initialize(ai.enemyConfig);
            }
            else
            {
                Debug.LogWarning($"[EnemyHealth] {gameObject.name} - No config found, using default values");
                maxHealth = 100f;
                currentHealth = maxHealth;
            }
        }
        
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }
    
    public void Initialize(EnemyConfig enemyConfig)
    {
        config = enemyConfig;
        maxHealth = config.health;
        currentHealth = maxHealth;
        
        Debug.Log($"[EnemyHealth] {gameObject.name} initialized with {maxHealth} health");
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        // 播放受伤动画和音效
        if (animator) animator.SetTrigger("TakeDamage");
        PlayHurtSound();
        
        Debug.Log($"[EnemyHealth] {gameObject.name} took {damage} damage, health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"[EnemyHealth] {gameObject.name} healed for {amount}, health: {currentHealth}/{maxHealth}");
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // 播放死亡动画和音效
        if (animator) animator.SetTrigger("Die");
        PlayDeathSound();
        
        // 掉落物品
        DropItems();
        
        // 禁用组件
        if (TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        
        // 延迟销毁
        Destroy(gameObject, 3f);
        
        Debug.Log($"[EnemyHealth] {gameObject.name} died");
    }
    
    void DropItems()
    {
        if (config?.dropItems != null && config.dropItems.Length > 0 && 
            Random.Range(0f, 1f) < config.dropChance)
        {
            GameObject itemToDrop = config.dropItems[Random.Range(0, config.dropItems.Length)];
            if (itemToDrop != null)
            {
                Instantiate(itemToDrop, transform.position, Quaternion.identity);
                Debug.Log($"[EnemyHealth] {gameObject.name} dropped {itemToDrop.name}");
            }
        }
    }
    
    void PlayHurtSound()
    {
        AudioClip soundToPlay = config?.GetRandomHurtSound();
        if (audioSource && soundToPlay)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    void PlayDeathSound()
    {
        AudioClip soundToPlay = config?.deathSound;
        if (audioSource && soundToPlay)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    // 调试方法
    [ContextMenu("Take 50 Damage")]
    public void DebugTakeDamage()
    {
        TakeDamage(50f);
    }
    
    [ContextMenu("Kill")]
    public void DebugKill()
    {
        TakeDamage(currentHealth);
    }
    
    [ContextMenu("Full Heal")]
    public void DebugFullHeal()
    {
        Heal(maxHealth);
    }
}