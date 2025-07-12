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
    public Image weaponIcon;
    public GameObject weaponUI; // 武器UI容器
    
    [Header("弹药显示")]
    public TextMeshProUGUI weaponAmmoText;    // 武器弹药 "12/30"
    public TextMeshProUGUI backpackAmmoText;  // 背包弹药 "备弹: 45"
    public TextMeshProUGUI ammoText;          // 旧版兼容
    public GameObject reloadPromptUI;         // 换弹提示UI
    public TextMeshProUGUI reloadPromptText;  // 换弹提示文本
    
    [Header("准星")]
    public SimpleMouseCrosshair crosshair;
    
    [Header("受伤效果")]
    public Image damageOverlay;
    public CanvasGroup damageEffect;
    
    [Header("击中反馈")]
    public Image hitMarker;
    
    [Header("HUD容器")]
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
        if (reloadPromptUI) reloadPromptUI.SetActive(false);
        
        // 默认隐藏所有HUD
        SetHUDVisibility(false);
    }
    
    void Update()
    {
        // 只在探索阶段更新HUD
        if (currentPhase == GamePhase.Exploration)
        {
            UpdateHealthDisplay();
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
        if (homeHUD) homeHUD.SetActive(false);
        
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
    
    void UpdateWeaponDisplay()
    {
        if (weaponManager == null) return;
        
        var currentWeapon = weaponManager.GetCurrentWeapon();
        bool hasWeapon = currentWeapon != null && !weaponManager.IsEmptyHands();
        
        // 武器UI容器显示/隐藏
        if (weaponUI) weaponUI.SetActive(hasWeapon);
        
        if (hasWeapon)
        {
            // 武器名称
            if (weaponNameText) 
                weaponNameText.text = currentWeapon.weaponName;
            
            // 更新弹药显示
            UpdateAmmoDisplay(currentWeapon);
            
            // 武器图标
            if (weaponIcon && currentWeapon.weaponData?.weaponIcon)
            {
                weaponIcon.sprite = currentWeapon.weaponData.weaponIcon;
                weaponIcon.enabled = true;
            }
            
            // 换弹提示
            UpdateReloadPrompt(currentWeapon);
        }
        else
        {
            // 空手状态
            ClearWeaponDisplay();
        }
    }
    
    void UpdateAmmoDisplay(WeaponController weapon)
    {
        if (weapon == null) return;
        
        // 武器弹药
        if (weaponAmmoText)
        {
            weaponAmmoText.text = $"{weapon.CurrentAmmo}/{weapon.MaxAmmo}";
            
            // 根据弹药量改变颜色
            if (weapon.CurrentAmmo == 0)
            {
                weaponAmmoText.color = Color.red;
            }
            else if (weapon.CurrentAmmo <= weapon.MaxAmmo * 0.3f)
            {
                weaponAmmoText.color = Color.yellow;
            }
            else
            {
                weaponAmmoText.color = Color.white;
            }
        }
        
        // 背包弹药
        if (backpackAmmoText && InventoryManager.Instance)
        {
            string ammoType = GetCurrentWeaponAmmoType(weapon);
            int backpackAmmo = InventoryManager.Instance.GetAmmoCount(ammoType);
            
            backpackAmmoText.text = $"备弹: {backpackAmmo}";
            
            // 根据备弹量改变颜色
            if (backpackAmmo == 0)
            {
                backpackAmmoText.color = Color.red;
            }
            else if (backpackAmmo <= 20)
            {
                backpackAmmoText.color = Color.yellow;
            }
            else
            {
                backpackAmmoText.color = Color.white;
            }
        }
        
        // 兼容旧的ammoText（如果存在）
        if (ammoText && !weaponAmmoText)
        {
            string weaponAmmo = $"{weapon.CurrentAmmo}/{weapon.MaxAmmo}";
            string backpackInfo = "";
            
            if (InventoryManager.Instance)
            {
                string ammoType = GetCurrentWeaponAmmoType(weapon);
                int backpackAmmo = InventoryManager.Instance.GetAmmoCount(ammoType);
                backpackInfo = $"\n备弹: {backpackAmmo}";
            }
            
            ammoText.text = weaponAmmo + backpackInfo;
        }
    }
    
    void UpdateReloadPrompt(WeaponController weapon)
    {
        if (weapon == null)
        {
            if (reloadPromptUI) reloadPromptUI.SetActive(false);
            return;
        }
        
        bool shouldShowPrompt = false;
        string promptText = "";
        
        if (weapon.CurrentAmmo == 0 && InventoryManager.Instance)
        {
            string ammoType = GetCurrentWeaponAmmoType(weapon);
            if (InventoryManager.Instance.HasAmmo(ammoType, 1))
            {
                shouldShowPrompt = true;
                promptText = "按 R 键换弹";
            }
            else
            {
                shouldShowPrompt = true;
                string ammoDisplayName = InventoryManager.Instance.GetAmmoDisplayName(ammoType);
                promptText = $"没有{ammoDisplayName}";
            }
        }
        
        // 显示或隐藏换弹提示
        if (reloadPromptUI)
        {
            reloadPromptUI.SetActive(shouldShowPrompt);
        }
        
        if (reloadPromptText && shouldShowPrompt)
        {
            reloadPromptText.text = promptText;
            
            // 根据提示类型设置颜色
            if (promptText.Contains("没有"))
            {
                reloadPromptText.color = Color.red;
            }
            else
            {
                reloadPromptText.color = Color.yellow;
            }
        }
    }
    
    void ClearWeaponDisplay()
    {
        if (weaponNameText) weaponNameText.text = "";
        if (weaponAmmoText) weaponAmmoText.text = "";
        if (backpackAmmoText) backpackAmmoText.text = "";
        if (ammoText) ammoText.text = "";
        if (weaponIcon) weaponIcon.enabled = false;
        if (reloadPromptUI) reloadPromptUI.SetActive(false);
    }
    
    /// <summary>
    /// 安全获取当前武器的弹药类型
    /// </summary>
    string GetCurrentWeaponAmmoType(WeaponController weapon)
    {
        if (weapon == null) return "9mm";
        
        // 优先从weaponData获取
        if (weapon.weaponData != null && !string.IsNullOrEmpty(weapon.weaponData.ammoType))
        {
            return weapon.weaponData.ammoType;
        }
        
        // 根据武器类型推断
        if (weapon is PistolController) return "9mm";
        if (weapon is AutoRifleController) return "5.56mm";
        
        // 根据武器名称推断
        if (weapon.weaponName.ToLower().Contains("pistol")) return "9mm";
        if (weapon.weaponName.ToLower().Contains("rifle")) return "5.56mm";
        
        return "9mm"; // 默认
    }
    
    // 外部调用接口 - 保持与UIManager的兼容性
    public void UpdateAmmoDisplay(int current, int max)
    {
        if (ammoText) ammoText.text = $"{current}/{max}";
        if (weaponAmmoText) weaponAmmoText.text = $"{current}/{max}";
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