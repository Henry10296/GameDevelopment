using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("血量显示")]
    public Image healthBar;
    public TextMeshProUGUI healthText;
    public Image healthBackground;
    
    [Header("弹药显示")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI weaponNameText;
    public Image weaponIcon;
    
    [Header("交互提示")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI interactionText;
    
    [Header("准星")]
    public Image crosshair;
    public RectTransform crosshairRect;
    
    [Header("颜色设置")]
    public Color healthyColor = Color.green;
    public Color lowHealthColor = Color.yellow;
    public Color criticalHealthColor = Color.red;
    public Color normalCrosshairColor = Color.white;
    public Color enemyCrosshairColor = Color.red;
    
    private Player playerHealth;
    private WeaponManager weaponManager;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeReferences();
        SetupUI();
    }
    
    void InitializeReferences()
    {
        // 查找玩家组件
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Player>();
            weaponManager = player.GetComponent<WeaponManager>();
        }
        
        // 如果没找到，尝试通过单例获取
        if (playerHealth == null)
        {
            playerHealth = Player.Instance;
        }
        
        isInitialized = (playerHealth != null);
        
        if (!isInitialized)
        {
            Debug.LogWarning("[SimplePlayerUI] PlayerHealth not found!");
        }
    }
    
    void SetupUI()
    {
        // 隐藏交互提示
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        // 设置初始准星颜色
        if (crosshair != null)
        {
            crosshair.color = normalCrosshairColor;
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        UpdateHealthDisplay();
        UpdateAmmoDisplay();
        UpdateCrosshair();
    }
    
    void UpdateHealthDisplay()
    {
        if (playerHealth == null) return;
        
        float healthPercent = playerHealth.GetHealthPercentage();
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.fillAmount = healthPercent;
            
            // 根据血量改变颜色
            Color healthColor = healthPercent switch
            {
                >= 0.6f => healthyColor,
                >= 0.3f => lowHealthColor,
                _ => criticalHealthColor
            };
            
            healthBar.color = healthColor;
        }
        
        // 更新血量文字
        if (healthText != null)
        {
            int currentHealth = Mathf.RoundToInt(playerHealth.currentHealth);
            int maxHealth = Mathf.RoundToInt(playerHealth.maxHealth);
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }
    
    void UpdateAmmoDisplay()
    {
        if (weaponManager == null) return;
        
        var currentWeapon = weaponManager.GetCurrentWeapon();
        
        if (currentWeapon != null && !weaponManager.IsEmptyHands())
        {
            // 显示弹药
            if (ammoText != null)
            {
                ammoText.text = $"{currentWeapon.CurrentAmmo}/{currentWeapon.MaxAmmo}";
                ammoText.gameObject.SetActive(true);
            }
            
            // 显示武器名称
            if (weaponNameText != null)
            {
                weaponNameText.text = currentWeapon.weaponName;
                weaponNameText.gameObject.SetActive(true);
            }
            
            // 显示武器图标（如果有WeaponData）
            if (weaponIcon != null)
            {
                // 这里需要从武器获取图标，可能需要在WeaponController中添加
                weaponIcon.gameObject.SetActive(true);
            }
        }
        else
        {
            // 空手状态，隐藏武器UI
            if (ammoText != null) ammoText.gameObject.SetActive(false);
            if (weaponNameText != null) weaponNameText.gameObject.SetActive(false);
            if (weaponIcon != null) weaponIcon.gameObject.SetActive(false);
        }
    }
    
    void UpdateCrosshair()
    {
        if (crosshair == null) return;
        
        // 检测是否瞄准敌人
        bool aimingAtEnemy = CheckAimingAtEnemy();
        
        Color targetColor = aimingAtEnemy ? enemyCrosshairColor : normalCrosshairColor;
        crosshair.color = Color.Lerp(crosshair.color, targetColor, Time.deltaTime * 10f);
        
        // 根据武器散布调整准星大小
        if (weaponManager != null && crosshairRect != null)
        {
            var currentWeapon = weaponManager.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                float spread = currentWeapon.GetCurrentSpread();
                float targetSize = 20f + spread * 300f; // 基础大小 + 散布影响
                
                Vector2 currentSize = crosshairRect.sizeDelta;
                Vector2 targetSizeVec = Vector2.one * targetSize;
                crosshairRect.sizeDelta = Vector2.Lerp(currentSize, targetSizeVec, Time.deltaTime * 8f);
            }
        }
    }
    
    bool CheckAimingAtEnemy()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = cam.ScreenPointToRay(screenCenter);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.collider.CompareTag("Enemy");
        }
        
        return false;
    }
    
    // 显示交互提示
    public void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            if (interactionText != null)
            {
                interactionText.text = text;
            }
        }
    }
    
    // 隐藏交互提示
    public void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    // 显示拾取提示
    public void ShowPickupPrompt(string itemName, int quantity = 1)
    {
        string promptText = quantity > 1 ? 
            $"按 F 拾取 {itemName} x{quantity}" : 
            $"按 F 拾取 {itemName}";
        ShowInteractionPrompt(promptText);
    }
    
    // 武器击中反馈
    public void OnWeaponHit()
    {
        if (crosshair != null)
        {
            // 简单的击中反馈 - 短暂变绿
            StartCoroutine(HitFeedbackCoroutine());
        }
    }
    
    System.Collections.IEnumerator HitFeedbackCoroutine()
    {
        Color originalColor = crosshair.color;
        crosshair.color = Color.green;
        
        yield return new WaitForSeconds(0.1f);
        
        crosshair.color = originalColor;
    }
    
    // 伤害反馈
    public void OnPlayerDamaged(float damage)
    {
        // 可以添加屏幕边缘红色闪烁等效果
        StartCoroutine(DamageFlashCoroutine());
    }
    
    System.Collections.IEnumerator DamageFlashCoroutine()
    {
        if (healthBackground != null)
        {
            Color originalColor = healthBackground.color;
            healthBackground.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                healthBackground.color = Color.Lerp(Color.red, originalColor, elapsed / 0.5f);
                yield return null;
            }
            
            healthBackground.color = originalColor;
        }
    }
}