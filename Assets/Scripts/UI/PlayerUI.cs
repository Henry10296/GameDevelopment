using UnityEngine;
using System;
using System.Collections;

public class PlayerUI : Singleton<PlayerUI>, IDamageable
{
    [Header("生命值设置")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float currentHealth = 100f;
    [SerializeField] private float healthRegenRate = 0f;
    [SerializeField] private float healthRegenDelay = 5f;
    [SerializeField] private float healthRegenThreshold = 0.5f; // 50%以下才回复
    
    [Header("护甲设置")]
    [SerializeField] public float maxArmor = 100f;
    [SerializeField] public float currentArmor = 0f;
    [SerializeField] private float armorAbsorption = 0.5f; // 护甲吸收50%伤害
    
    [Header("伤害设置")]
    [SerializeField] private float invincibilityDuration = 0.1f;
    [SerializeField] private bool enableInvincibilityFrames = true;
    [SerializeField] private AnimationCurve damageShakeCurve;
    
    [Header("死亡设置")]
    [SerializeField] private float deathCameraTime = 2f;
    [SerializeField] private bool enableDeathCam = true;
    [SerializeField] private GameObject ragdollPrefab;
    
    [Header("音效")]
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] armorHitSounds;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip armorPickupSound;
    
    // 事件
    public event Action OnHealthChanged;
    public event Action<float, Vector3> OnDamaged;
    public event Action OnArmorChanged;
    public event Action OnDeath;
    public event Action OnRevive;
    
    // 内部变量
    private bool isInvincible = false;
    private bool isDead = false;
    private float lastDamageTime = 0f;
    private Coroutine regenCoroutine;
    private AudioSource audioSource;
    private PlayerController playerController;
    
    protected override void Awake()
    {
        base.Awake();
        currentHealth = maxHealth;
        currentArmor = 0f;
    }
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        playerController = GetComponent<PlayerController>();
    }
    
    void Update()
    {
        // 健康回复
        if (healthRegenRate > 0 && !isDead)
        {
            if (Time.time - lastDamageTime > healthRegenDelay && 
                GetHealthPercentage() < healthRegenThreshold)
            {
                Heal(healthRegenRate * Time.deltaTime, false);
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, Vector3.zero, DamageType.Normal);
    }
    
    public void TakeDamage(float damage, Vector3 damagePoint, DamageType damageType = DamageType.Normal)
    {
        if (isDead || isInvincible) return;
        
        // 计算伤害方向
        Vector3 damageDirection = (transform.position - damagePoint).normalized;
        
        // 护甲吸收
        float actualDamage = damage;
        if (currentArmor > 0)
        {
            float armorDamage = damage * armorAbsorption;
            float healthDamage = damage * (1f - armorAbsorption);
            
            // 扣除护甲
            float armorLost = Mathf.Min(currentArmor, armorDamage);
            currentArmor -= armorLost;
            
            // 如果护甲不够，剩余伤害转到生命值
            if (armorLost < armorDamage)
            {
                healthDamage += (armorDamage - armorLost);
            }
            
            actualDamage = healthDamage;
            
            // 护甲击中音效
            if (armorHitSounds.Length > 0)
            {
                PlayRandomSound(armorHitSounds);
            }
            
            OnArmorChanged?.Invoke();
        }
        
        // 扣除生命值
        currentHealth -= actualDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        // 更新最后受伤时间
        lastDamageTime = Time.time;
        
        // 停止生命回复
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        
        // 触发事件
        OnHealthChanged?.Invoke();
        OnDamaged?.Invoke(damage, damageDirection);
        
        // 播放受伤音效
        if (hurtSounds.Length > 0 && currentHealth > 0)
        {
            PlayRandomSound(hurtSounds);
        }
        
        // 相机震动
        if (damageShakeCurve != null && CameraShaker.Instance)
        {
            float shakeIntensity = Mathf.Clamp01(damage / 50f);
            CameraShaker.Instance.Shake(0.3f, shakeIntensity);
        }
        
        // 无敌帧
        if (enableInvincibilityFrames && currentHealth > 0)
        {
            StartCoroutine(InvincibilityFrames());
        }
        
        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
        
        Debug.Log($"[PlayerHealth] Took {damage} damage ({damageType}). Health: {currentHealth}/{maxHealth}, Armor: {currentArmor}/{maxArmor}");
    }
    
    public void Heal(float amount, bool playSound = true)
    {
        if (isDead || currentHealth >= maxHealth) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        OnHealthChanged?.Invoke();
        
        if (playSound && healSound)
        {
            audioSource.PlayOneShot(healSound);
        }
        
        Debug.Log($"[PlayerHealth] Healed {amount}. Health: {currentHealth}/{maxHealth}");
    }
    
    public void AddArmor(float amount)
    {
        if (isDead) return;
        
        currentArmor += amount;
        currentArmor = Mathf.Clamp(currentArmor, 0f, maxArmor);
        
        OnArmorChanged?.Invoke();
        
        if (armorPickupSound)
        {
            audioSource.PlayOneShot(armorPickupSound);
        }
        
        Debug.Log($"[PlayerHealth] Added {amount} armor. Armor: {currentArmor}/{maxArmor}");
    }
    
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        OnHealthChanged?.Invoke();
        
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    public void SetArmor(float armor)
    {
        currentArmor = Mathf.Clamp(armor, 0f, maxArmor);
        OnArmorChanged?.Invoke();
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        
        // 播放死亡音效
        if (deathSound)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // 触发死亡事件
        OnDeath?.Invoke();
        
        // 禁用玩家控制
        if (playerController)
        {
            playerController.enabled = false;
        }
        
        // 死亡相机效果
        if (enableDeathCam)
        {
            StartCoroutine(DeathCameraSequence());
        }
        
        // 创建布娃娃（如果有）
        if (ragdollPrefab)
        {
            CreateRagdoll();
        }
        
        // 通知游戏管理器
        if (GameManager.Instance)
        {
            GameManager.Instance.ChangePhase(GamePhase.GameEnd);
        }
        
        Debug.Log("[PlayerHealth] Player died!");
    }
    
    public void Revive(float healthAmount = 100f)
    {
        if (!isDead) return;
        
        isDead = false;
        currentHealth = Mathf.Min(healthAmount, maxHealth);
        currentArmor = 0f;
        
        // 重新启用玩家控制
        if (playerController)
        {
            playerController.enabled = true;
        }
        
        // 触发事件
        OnRevive?.Invoke();
        OnHealthChanged?.Invoke();
        OnArmorChanged?.Invoke();
        
        Debug.Log("[PlayerHealth] Player revived!");
    }
    
    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        
        // 闪烁效果（可选）
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float timer = 0f;
        
        while (timer < invincibilityDuration)
        {
            timer += Time.deltaTime;
            
            // 闪烁
            bool visible = Mathf.FloorToInt(timer * 20f) % 2 == 0;
            foreach (var renderer in renderers)
            {
                renderer.enabled = visible;
            }
            
            yield return null;
        }
        
        // 确保渲染器可见
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }
        
        isInvincible = false;
    }
    
    IEnumerator DeathCameraSequence()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;
        
        // 慢动作
        Time.timeScale = 0.3f;
        
        // 相机下降效果
        Transform cameraTransform = mainCamera.transform;
        Vector3 startPos = cameraTransform.position;
        Vector3 endPos = startPos - Vector3.up * 1f;
        Quaternion startRot = cameraTransform.rotation;
        Quaternion endRot = Quaternion.Euler(startRot.eulerAngles.x - 30f, startRot.eulerAngles.y, 15f);
        
        float timer = 0f;
        while (timer < deathCameraTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / deathCameraTime;
            
            cameraTransform.position = Vector3.Lerp(startPos, endPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
            
            yield return null;
        }
        
        // 恢复时间缩放
        Time.timeScale = 1f;
    }
    
    void CreateRagdoll()
    {
        GameObject ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
        
        // 复制速度到布娃娃
        if (playerController)
        {
            Rigidbody[] ragdollBodies = ragdoll.GetComponentsInChildren<Rigidbody>();
            Vector3 velocity = playerController.GetVelocity();
            
            foreach (var rb in ragdollBodies)
            {
                rb.velocity = velocity;
            }
        }
        
        // 隐藏原始模型
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
    }
    
    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips.Length > 0 && audioSource)
        {
            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    // IDamageable 接口实现
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => !isDead;
    
    // 公共属性和方法
    public float GetHealthPercentage() => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public float GetArmorPercentage() => maxArmor > 0 ? currentArmor / maxArmor : 0f;
    public bool IsDead() => isDead;
    public bool IsFullHealth() => currentHealth >= maxHealth;
    public bool HasArmor() => currentArmor > 0;
    
    // 调试方法
    [ContextMenu("Take 25 Damage")]
    public void DebugTakeDamage()
    {
        TakeDamage(25f, transform.position + transform.forward);
    }
    
    [ContextMenu("Full Heal")]
    public void DebugFullHeal()
    {
        Heal(maxHealth);
        AddArmor(maxArmor);
    }
    
    [ContextMenu("Kill")]
    public void DebugKill()
    {
        TakeDamage(currentHealth + currentArmor * 2);
    }
}

// 伤害类型枚举
public enum DamageType
{
    Normal,
    Explosion,
    Fire,
    Poison,
    Fall,
    Melee,
    Projectile
}

// 相机震动器
public class CameraShaker : Singleton<CameraShaker>
{
    [Header("震动设置")]
    [SerializeField] private float traumaDecay = 1f;
    [SerializeField] private float maxAngle = 10f;
    [SerializeField] private float maxOffset = 0.5f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private float trauma = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform cameraTransform;
    
    void Start()
    {
        cameraTransform = Camera.main.transform;
        originalPosition = cameraTransform.localPosition;
        originalRotation = cameraTransform.localRotation;
    }
    
    void Update()
    {
        if (trauma > 0)
        {
            trauma = Mathf.Max(0, trauma - traumaDecay * Time.deltaTime);
            
            float shake = shakeCurve.Evaluate(trauma);
            
            // 位置偏移
            float offsetX = maxOffset * shake * UnityEngine.Random.Range(-1f, 1f);
            float offsetY = maxOffset * shake * UnityEngine.Random.Range(-1f, 1f);
            
            // 旋转偏移
            float angleX = maxAngle * shake * UnityEngine.Random.Range(-1f, 1f);
            float angleY = maxAngle * shake * UnityEngine.Random.Range(-1f, 1f);
            float angleZ = maxAngle * shake * UnityEngine.Random.Range(-1f, 1f) * 0.5f;
            
            cameraTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
            cameraTransform.localRotation = originalRotation * Quaternion.Euler(angleX, angleY, angleZ);
        }
        else
        {
            // 恢复原始位置和旋转
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, originalPosition, Time.deltaTime * 5f);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, originalRotation, Time.deltaTime * 5f);
        }
    }
    
    public void Shake(float duration = 0.5f, float intensity = 0.5f)
    {
        trauma = Mathf.Clamp01(trauma + intensity);
    }
}