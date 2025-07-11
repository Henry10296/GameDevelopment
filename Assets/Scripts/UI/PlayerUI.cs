using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerUI : MonoBehaviour
{
    [Header("生命值显示")]
    public Slider healthSlider;
    public Image healthFill;
    public TextMeshProUGUI healthText;
    
    [Header("护甲显示")]
    public Slider armorSlider;
    public Image armorFill;
    public TextMeshProUGUI armorText;
    
    [Header("武器显示")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI ammoText;
    public Image weaponIcon;
    public GameObject weaponUI; // 武器UI容器
    
    [Header("准星")]
    public SimpleMouseCrosshair crosshair;
    
    [Header("受伤效果")]
    public Image damageOverlay;
    public CanvasGroup damageEffect;
    
    [Header("击中反馈")]
    public Image hitMarker;
    
    [Header("HUD容器")] // 新增：按阶段显示/隐藏
    public GameObject explorationHUD;
    public GameObject homeHUD;
    
    [Header("颜色配置")]
    public Color healthyColor = Color.green;
    public Color lowHealthColor = Color.red;
    public Color armorColor = Color.blue;
    
    private Player player;
    private WeaponManager weaponManager;
    private Coroutine damageEffectCoroutine;
    private Coroutine hitMarkerCoroutine;
    private GamePhase currentPhase;
    
    public void Initialize()
    {
        // 获取玩家引用
        player = Player.Instance;
        if (player != null)
        {
            weaponManager = player.GetWeaponManager();
            
            // 订阅玩家事件
            player.OnHealthChanged += UpdateHealth;
            player.OnDamaged += OnPlayerDamaged;
        }
        else
        {
            // 如果没有Player实例，尝试查找PlayerController
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                weaponManager = playerController.GetComponent<WeaponManager>();
            }
        }
        
        InitializeUI();
    }
    
    void InitializeUI()
    {
        if (damageOverlay) damageOverlay.color = new Color(1, 0, 0, 0);
        if (hitMarker) hitMarker.gameObject.SetActive(false);
        
        // 默认隐藏所有HUD
        SetHUDVisibility(false);
    }
    
    void Update()
    {
        // 只在探索阶段更新HUD
        if (currentPhase == GamePhase.Exploration)
        {
            UpdateHealthDisplay();
            UpdateArmorDisplay();
            UpdateWeaponDisplay();
        }
    }
    
    public void OnPhaseChanged(GamePhase newPhase)
    {
        currentPhase = newPhase;
        
        // 根据阶段显示/隐藏HUD
        bool showHUD = (newPhase == GamePhase.Exploration);
        SetHUDVisibility(showHUD);
    }
    
    void SetHUDVisibility(bool show)
    {
        if (explorationHUD) explorationHUD.SetActive(show);
        if (homeHUD) homeHUD.SetActive(false); // 暂时不用
        
        // 准星只在探索时显示
        if (crosshair) crosshair.gameObject.SetActive(show);
    }
    
    void UpdateHealthDisplay()
    {
        if (player == null) return;
        
        float healthPercent = player.GetHealthPercentage();
        
        if (healthSlider) healthSlider.value = healthPercent;
        if (healthText) healthText.text = $"{player.currentHealth:F0}/{player.maxHealth:F0}";
        
        if (healthFill)
        {
            healthFill.color = Color.Lerp(lowHealthColor, healthyColor, healthPercent);
        }
    }
    
    void UpdateArmorDisplay()
    {
        if (player == null) return;
        
        float armorPercent = player.GetArmorPercentage();
        
        if (armorSlider) 
        {
            armorSlider.value = armorPercent;
            armorSlider.gameObject.SetActive(player.currentArmor > 0);
        }
        
        if (armorText) armorText.text = $"{player.currentArmor:F0}/{player.maxArmor:F0}";
        if (armorFill) armorFill.color = armorColor;
    }
    
    void UpdateWeaponDisplay()
    {
        if (weaponManager == null) return;
        
        var currentWeapon = weaponManager.GetCurrentWeapon();
        bool hasWeapon = currentWeapon != null && !weaponManager.IsEmptyHands();
        
        // 武器UI容器显示/隐藏
        if (weaponUI) weaponUI.SetActive(hasWeapon);
        
        if (hasWeapon)
        {
            if (weaponNameText) weaponNameText.text = currentWeapon.weaponName;
            if (ammoText) ammoText.text = $"{currentWeapon.CurrentAmmo}/{currentWeapon.MaxAmmo}";
            
            // 更新武器图标
            if (weaponIcon && currentWeapon.weaponData?.weaponIcon)
            {
                weaponIcon.sprite = currentWeapon.weaponData.weaponIcon;
                weaponIcon.enabled = true;
            }
        }
        else
        {
            // 空手状态
            if (weaponNameText) weaponNameText.text = "";
            if (ammoText) ammoText.text = "";
            if (weaponIcon) weaponIcon.enabled = false;
        }
    }
    
    // 外部调用接口 - 保持与UIManager的兼容性
    public void UpdateAmmoDisplay(int current, int max)
    {
        if (ammoText) ammoText.text = $"{current}/{max}";
    }
    
    public void UpdateHealthDisplay(float current, float max)
    {
        if (healthSlider) healthSlider.value = current / max;
        if (healthText) healthText.text = $"{current:F0}/{max:F0}";
    }
    
    public void UpdateHealth(float newHealth)
    {
        UpdateHealthDisplay();
    }
    
    public void OnPlayerDamaged(float damage)
    {
        ShowDamageEffect();
    }
    
    public void OnWeaponHit()
    {
        ShowHitMarker();
        
        // 通知准星
        if (crosshair) crosshair.OnTargetHit();
    }
    
    public void OnWeaponFired()
    {
        // 武器开火时的UI反馈
        if (crosshair) crosshair.ShowHitFeedback();
    }
    
    void ShowDamageEffect()
    {
        if (damageEffectCoroutine != null)
            StopCoroutine(damageEffectCoroutine);
        damageEffectCoroutine = StartCoroutine(DamageEffectCoroutine());
    }
    
    System.Collections.IEnumerator DamageEffectCoroutine()
    {
        if (damageOverlay)
        {
            damageOverlay.color = new Color(1, 0, 0, 0.5f);
            
            float timer = 0f;
            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0.5f, 0f, timer / 0.5f);
                damageOverlay.color = new Color(1, 0, 0, alpha);
                yield return null;
            }
            
            damageOverlay.color = new Color(1, 0, 0, 0);
        }
    }
    
    void ShowHitMarker()
    {
        if (hitMarkerCoroutine != null)
            StopCoroutine(hitMarkerCoroutine);
        hitMarkerCoroutine = StartCoroutine(HitMarkerCoroutine());
    }
    
    System.Collections.IEnumerator HitMarkerCoroutine()
    {
        if (hitMarker)
        {
            hitMarker.gameObject.SetActive(true);
            hitMarker.color = Color.white;
            
            yield return new WaitForSeconds(0.1f);
            
            float timer = 0f;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / 0.2f);
                hitMarker.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            
            hitMarker.gameObject.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHealth;
            player.OnDamaged -= OnPlayerDamaged;
        }
    }
}