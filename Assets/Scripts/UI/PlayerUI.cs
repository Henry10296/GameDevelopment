// PlayerUI.cs - 专门处理玩家相关的UI显示
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    
    [Header("准星")]
    public SimpleMouseCrosshair crosshair;
    
    [Header("受伤效果")]
    public Image damageOverlay;
    public CanvasGroup damageEffect;
    
    [Header("击中反馈")]
    public Image hitMarker;
    
    [Header("颜色配置")]
    public Color healthyColor = Color.green;
    public Color lowHealthColor = Color.red;
    public Color armorColor = Color.blue;
    
    private Player player;
    private WeaponManager weaponManager;
    private Coroutine damageEffectCoroutine;
    private Coroutine hitMarkerCoroutine;
    
    void Start()
    {
        // 获取玩家引用
        player = Player.Instance;
        if (player != null)
        {
            weaponManager = player.GetWeaponManager();
        }
        
        // 初始化UI
        InitializeUI();
    }
    
    void InitializeUI()
    {
        if (damageOverlay) damageOverlay.color = new Color(1, 0, 0, 0);
        if (hitMarker) hitMarker.gameObject.SetActive(false);
        
        // 订阅玩家事件
        if (player != null)
        {
            player.OnHealthChanged += UpdateHealth;
            player.OnDamaged += OnPlayerDamaged;
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        UpdateHealthDisplay();
        UpdateArmorDisplay();
        UpdateWeaponDisplay();
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
        
        if (currentWeapon != null)
        {
            if (weaponNameText) weaponNameText.text = currentWeapon.weaponName;
            if (ammoText) ammoText.text = $"{currentWeapon.CurrentAmmo}/{currentWeapon.MaxAmmo}";
            
            // 更新武器图标
            if (weaponIcon && currentWeapon.weaponData?.weaponIcon)
            {
                weaponIcon.sprite = currentWeapon.weaponData.weaponIcon;
                weaponIcon.gameObject.SetActive(true);
            }
        }
        else
        {
            // 空手状态
            if (weaponNameText) weaponNameText.text = "空手";
            if (ammoText) ammoText.text = "";
            if (weaponIcon) weaponIcon.gameObject.SetActive(false);
        }
    }
    
    public void UpdateHealth(float newHealth)
    {
        // 这个方法由Player的事件系统调用
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
    
    void ShowDamageEffect()
    {
        if (damageEffectCoroutine != null)
            StopCoroutine(damageEffectCoroutine);
        damageEffectCoroutine = StartCoroutine(DamageEffectCoroutine());
    }
    
    System.Collections.IEnumerator DamageEffectCoroutine()
    {
        // 红色闪烁效果
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