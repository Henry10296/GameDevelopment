using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class Player : Singleton<Player>, IDamageable
{
    [Header("生命值系统")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 0f;
    public float healthRegenDelay = 5f;
    
    [Header("护甲系统")]
    public float maxArmor = 100f;
    public float currentArmor = 0f;
    public float armorAbsorption = 0.5f;
    
    [Header("音效")]
    public AudioClip[] hurtSounds;
    public AudioClip deathSound;
    public AudioClip healSound;
    
    
    // 组件引用
    private PlayerController playerController;
    private WeaponManager weaponManager;
    private AudioSource audioSource;
    
    // 状态变量
    private bool isDead = false;
    private float lastDamageTime;
    
    // 事件
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDeath;
    public System.Action<float> OnDamaged;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        weaponManager = GetComponent<WeaponManager>();
        audioSource = GetComponent<AudioSource>();
        
        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        currentHealth = maxHealth;
    }
    
    void Update()
    {
        if (isDead) return;
        
        HandleHealthRegen();
    }
    
    void HandleHealthRegen()
    {
        if (healthRegenRate > 0 && currentHealth < maxHealth)
        {
            if (Time.time - lastDamageTime > healthRegenDelay)
            {
                Heal(healthRegenRate * Time.deltaTime, false);
            }
        }
    }
    
    // IDamageable 实现
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        lastDamageTime = Time.time;
        
        // 护甲吸收
        float actualDamage = CalculateArmorDamage(damage);
        
        currentHealth -= actualDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        // 播放受伤音效
        PlayRandomSound(hurtSounds);
        
        // 触发事件
        OnHealthChanged?.Invoke(currentHealth);
        OnDamaged?.Invoke(damage);
        
        // 相机震动
        if (playerController != null)
        {
            playerController.TriggerDamageShake(damage);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
        
        Debug.Log($"[Player] Took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }
    
    private float CalculateArmorDamage(float damage)
    {
        if (currentArmor <= 0) return damage;
        
        float armorDamage = damage * armorAbsorption;
        float healthDamage = damage * (1f - armorAbsorption);
        
        float armorLost = Mathf.Min(currentArmor, armorDamage);
        currentArmor -= armorLost;
        
        if (armorLost < armorDamage)
        {
            healthDamage += (armorDamage - armorLost);
        }
        
        return healthDamage;
    }
    
    public void Heal(float amount, bool playSound = true)
    {
        if (isDead || currentHealth >= maxHealth) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        if (playSound && healSound)
        {
            audioSource.PlayOneShot(healSound);
        }
        
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void AddArmor(float amount)
    {
        if (isDead) return;
        
        currentArmor += amount;
        currentArmor = Mathf.Clamp(currentArmor, 0f, maxArmor);
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        
        if (deathSound)
            audioSource.PlayOneShot(deathSound);
        
        OnPlayerDeath?.Invoke();
        
        // 禁用控制器
        if (playerController)
            playerController.enabled = false;
        
        // 通知游戏管理器
        if (GameManager.Instance)
        {
            GameManager.Instance.ChangePhase(GamePhase.GameEnd);
        }
    }
    
    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && audioSource)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    // IDamageable 接口
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => !isDead;
    
    // 公共访问方法
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetArmorPercentage() => currentArmor / maxArmor;
    public bool IsDead() => isDead;
    public PlayerController GetController() => playerController;
    public WeaponManager GetWeaponManager() => weaponManager;
    
    // 调试方法
    [ContextMenu("Take Damage")]
    void DebugTakeDamage() => TakeDamage(25f);
    
    [ContextMenu("Full Heal")]
    void DebugFullHeal() => Heal(maxHealth);
    
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentArmor = 0f;
        
        if (playerController)
            playerController.enabled = true;
            
        OnHealthChanged?.Invoke(currentHealth);
    }
}