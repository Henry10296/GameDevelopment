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
    public GameObject weaponPanel; // 武器面板容器
    public TextMeshProUGUI weaponNameText;
    public Image weaponIcon;
    
    [Header("弹药显示")]
    public TextMeshProUGUI weaponAmmoText;    // 武器弹药 "12/30"
    public TextMeshProUGUI backpackAmmoText;  // 背包弹药 "备弹: 45"
    public GameObject ammoPanel; // 弹药面板容器
    
    [Header("特殊提示")]
    public GameObject reloadPromptUI;         // 换弹提示UI
    public TextMeshProUGUI reloadPromptText;  // 换弹提示文本
    public GameObject noAmmoWarning;          // 无弹药警告
    public GameObject lowAmmoWarning;         // 低弹药警告
    
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
    public Color normalAmmoColor = Color.white;
    public Color lowAmmoColor = Color.yellow;
    public Color noAmmoColor = Color.red;
    
    private Player player;
    private WeaponManager weaponManager;
    private Coroutine damageEffectCoroutine;
    private Coroutine hitMarkerCoroutine;
    private GamePhase currentPhase;
    
    // 状态缓存
    private WeaponController lastWeapon;
    private bool lastEmptyHandsState;
    private int lastWeaponAmmo = -1;
    private int lastBackpackAmmo = -1;
    
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
        
        // 订阅武器管理器事件
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += OnWeaponChanged;
            weaponManager.OnGoEmptyHands += OnGoEmptyHands;
        }
        
        InitializeUI();
        
        Debug.Log("[PlayerUI] 初始化完成");
    }
    
    void InitializeUI()
    {
        if (damageOverlay) damageOverlay.color = new Color(1, 0, 0, 0);
        if (hitMarker) hitMarker.gameObject.SetActive(false);
        if (reloadPromptUI) reloadPromptUI.SetActive(false);
        if (noAmmoWarning) noAmmoWarning.SetActive(false);
        if (lowAmmoWarning) lowAmmoWarning.SetActive(false);
        
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
            UpdateAmmoDisplay();
            CheckAmmoWarnings();
        }
    }
    
    public void OnPhaseChanged(GamePhase newPhase)
    {
        currentPhase = newPhase;
        
        // 根据阶段显示/隐藏HUD
        bool showHUD = (newPhase == GamePhase.Exploration);
        SetHUDVisibility(showHUD);
        
        Debug.Log($"[PlayerUI] 阶段变更: {newPhase}, HUD显示: {showHUD}");
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
        
        // 护甲显示
        if (armorSlider) armorSlider.value = player.GetArmorPercentage();
        if (armorText) armorText.text = $"{player.currentArmor:F0}/{player.maxArmor:F0}";
    }
    
    void UpdateWeaponDisplay()
    {
        if (weaponManager == null) return;
        
        var currentWeapon = weaponManager.GetCurrentWeapon();
        bool hasWeapon = currentWeapon != null && !weaponManager.IsEmptyHands();
        
        // 武器面板显示/隐藏
        if (weaponPanel) weaponPanel.SetActive(hasWeapon);
        if (ammoPanel) ammoPanel.SetActive(hasWeapon);
        
        if (hasWeapon)
        {
            // 武器名称
            if (weaponNameText) 
                weaponNameText.text = currentWeapon.weaponName;
            
            // 武器图标
            if (weaponIcon && currentWeapon.weaponData?.weaponIcon)
            {
                weaponIcon.sprite = currentWeapon.weaponData.weaponIcon;
                weaponIcon.enabled = true;
                weaponIcon.color = Color.white;
            }
        }
        else
        {
            // 空手状态
            ClearWeaponDisplay();
        }
    }
    
    void UpdateAmmoDisplay()
    {
        if (weaponManager == null) return;
        
        var currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon == null || weaponManager.IsEmptyHands()) return;
        
        // 武器弹药显示
        if (weaponAmmoText)
        {
            weaponAmmoText.text = $"{currentWeapon.CurrentAmmo}/{currentWeapon.MaxAmmo}";
            
            // 根据弹药量设置颜色
            float ammoRatio = (float)currentWeapon.CurrentAmmo / currentWeapon.MaxAmmo;
            if (currentWeapon.CurrentAmmo == 0)
            {
                weaponAmmoText.color = noAmmoColor;
            }
            else if (ammoRatio <= 0.3f)
            {
                weaponAmmoText.color = lowAmmoColor;
            }
            else
            {
                weaponAmmoText.color = normalAmmoColor;
            }
        }
        
        // 背包弹药显示
        if (backpackAmmoText && InventoryManager.Instance)
        {
            string ammoType = GetCurrentWeaponAmmoType(currentWeapon);
            int backpackAmmo = InventoryManager.Instance.GetAmmoCount(ammoType);
            
            backpackAmmoText.text = $"备弹: {backpackAmmo}";
            
            // 根据备弹量设置颜色
            if (backpackAmmo == 0)
            {
                backpackAmmoText.color = noAmmoColor;
            }
            else if (backpackAmmo <= 30)
            {
                backpackAmmoText.color = lowAmmoColor;
            }
            else
            {
                backpackAmmoText.color = normalAmmoColor;
            }
        }
    }
    
    void CheckAmmoWarnings()
    {
        if (weaponManager == null) return;
        
        var currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            if (noAmmoWarning) noAmmoWarning.SetActive(false);
            if (lowAmmoWarning) lowAmmoWarning.SetActive(false);
            if (reloadPromptUI) reloadPromptUI.SetActive(false);
            return;
        }
        
        bool hasNoAmmo = currentWeapon.CurrentAmmo == 0;
        bool hasLowAmmo = currentWeapon.CurrentAmmo <= currentWeapon.MaxAmmo * 0.2f;
        
        // 无弹药警告
        if (noAmmoWarning) noAmmoWarning.SetActive(hasNoAmmo);
        
        // 低弹药警告
        if (lowAmmoWarning) lowAmmoWarning.SetActive(hasLowAmmo && !hasNoAmmo);
        
        // 换弹提示
        UpdateReloadPrompt(currentWeapon);
    }
    
    void UpdateReloadPrompt(WeaponController weapon)
    {
        if (weapon == null || reloadPromptUI == null) return;
        
        bool shouldShowPrompt = false;
        string promptText = "";
        Color promptColor = Color.white;
        
        if (weapon.CurrentAmmo == 0 && InventoryManager.Instance)
        {
            string ammoType = GetCurrentWeaponAmmoType(weapon);
            if (InventoryManager.Instance.HasAmmo(ammoType, 1))
            {
                shouldShowPrompt = true;
                promptText = "按 R 键换弹";
                promptColor = lowAmmoColor;
            }
            else
            {
                shouldShowPrompt = true;
                string ammoDisplayName = InventoryManager.Instance.GetAmmoDisplayName(ammoType);
                promptText = $"没有{ammoDisplayName}";
                promptColor = noAmmoColor;
            }
        }
        
        reloadPromptUI.SetActive(shouldShowPrompt);
        
        if (reloadPromptText && shouldShowPrompt)
        {
            reloadPromptText.text = promptText;
            reloadPromptText.color = promptColor;
        }
    }
    
    void ClearWeaponDisplay()
    {
        if (weaponNameText) weaponNameText.text = "";
        if (weaponAmmoText) weaponAmmoText.text = "";
        if (backpackAmmoText) backpackAmmoText.text = "";
        if (weaponIcon) weaponIcon.enabled = false;
        if (reloadPromptUI) reloadPromptUI.SetActive(false);
    }
    
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
    
    // 武器管理器事件响应
    void OnWeaponChanged(WeaponController newWeapon)
    {
        Debug.Log($"[PlayerUI] 武器变更: {newWeapon?.weaponName}");
        lastWeapon = newWeapon;
        lastEmptyHandsState = false;
        
        // 立即更新显示
        UpdateWeaponDisplay();
        UpdateAmmoDisplay();
    }
    
    void OnGoEmptyHands()
    {
        Debug.Log("[PlayerUI] 切换到空手");
        lastWeapon = null;
        lastEmptyHandsState = true;
        
        // 立即清除显示
        ClearWeaponDisplay();
    }
    
    // 外部调用接口 - 保持与UIManager的兼容性
    public void UpdateAmmoDisplay(int current, int max)
    {
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
        
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -= OnWeaponChanged;
            weaponManager.OnGoEmptyHands -= OnGoEmptyHands;
        }
    }
}