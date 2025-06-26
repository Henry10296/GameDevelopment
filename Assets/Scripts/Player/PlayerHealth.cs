using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : Singleton<PlayerHealth>, IDamageable
{
    [Header("血量设置")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    public Image healthFill;
    public Color healthyColor = Color.green;
    public Color lowHealthColor = Color.red;

    [Header("受伤效果")]
    public Image damageOverlay;
    public float overlayFadeSpeed = 5f;

    [Header("事件")]
    public GameEvent onHealthChanged;
    public GameEvent onPlayerDeath;

    private float targetOverlayAlpha = 0f;

    protected override void Awake()
    {
        base.Awake();
        currentHealth = maxHealth;
    }

    void Start()
    {
        UpdateHealthUI();
    }

    void Update()
    {
        // 更新受伤覆盖层
        if (damageOverlay)
        {
            Color color = damageOverlay.color;
            color.a = Mathf.Lerp(color.a, targetOverlayAlpha, overlayFadeSpeed * Time.deltaTime);
            damageOverlay.color = color;

            if (targetOverlayAlpha > 0)
                targetOverlayAlpha = 0;
        }
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // 已经死亡
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // 显示受伤效果
        if (damageOverlay)
            targetOverlayAlpha = 0.5f;

        UpdateHealthUI();
        onHealthChanged?.Raise();

        Debug.Log($"玩家受到 {damage} 伤害，剩余血量: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0) return; // 已经死亡，无法治疗
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();
        onHealthChanged?.Raise();
        
        Debug.Log($"玩家恢复 {amount} 血量，当前血量: {currentHealth}");
    }

    void UpdateHealthUI()
    {
        if (healthSlider)
        {
            healthSlider.value = currentHealth / maxHealth;
        }

        if (healthFill)
        {
            float healthPercent = currentHealth / maxHealth;
            healthFill.color = Color.Lerp(lowHealthColor, healthyColor, healthPercent);
        }
        
        // 更新UI管理器
        if (UIManager.Instance)
        {
            UIManager.Instance.UpdateHealthDisplay(currentHealth, maxHealth);
        }
    }

    void Die()
    {
        Debug.Log("玩家死亡");
        onPlayerDeath?.Raise();
        
        // 触发游戏结束
        if (GameStateManager.Instance)
        {
            GameStateManager.Instance.TransitionToPhase(GamePhase.GameEnd);
        }
    }

    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    // 调试方法
    [ContextMenu("Take 25 Damage")]
    public void DebugTakeDamage()
    {
        TakeDamage(25f);
    }

    [ContextMenu("Full Heal")]
    public void DebugFullHeal()
    {
        Heal(maxHealth);
    }
}