using UnityEngine;

public class WeaponPickup : BaseInteractable
{
    [Header("武器数据")]
    public WeaponData weaponData;
    public int currentAmmo;
    
    [Header("显示")]
    public SpriteRenderer weaponSprite;
    public GameObject weaponModel;
    
    [Header("动画设置")]
    public bool enableFloatAnimation = true;
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;
    public float rotationSpeed = 45f;
    
    private Vector3 originalPosition;
    private bool isInitialized = false;
    
    protected override void Start()
    {
        // 先调用基类的Start
        base.Start();
        
        // 确保碰撞体设置正确
        SetupCollider();
        
        // 如果有weaponData就立即初始化
        if (weaponData != null)
        {
            Initialize();
        }
        else
        {
            // 如果没有weaponData，尝试自动查找
            AutoFindWeaponData();
        }
    }
    
    void AutoFindWeaponData()
    {
        // 尝试从游戏配置中找到weaponData
        if (GameManager.Instance?.gameConfig?.WeaponConfig?.pistol != null)
        {
            // 默认给一个手枪数据作为测试
            SetupWeapon(GameManager.Instance.gameConfig.WeaponConfig.pistol, 15);
            Debug.Log("[WeaponPickup] Auto-assigned pistol data for testing");
        }
        else
        {
            Debug.LogError("[WeaponPickup] No weapon data found! Please assign weaponData in inspector.");
        }
    }
    
    void SetupCollider()
    {
        // 确保有正确的碰撞体
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        
        col.isTrigger = true;
        col.radius = Mathf.Max(1.5f, interactionRange * 0.5f);
        
        Debug.Log($"[WeaponPickup] {gameObject.name} - Collider setup: radius={col.radius}, isTrigger={col.isTrigger}");
    }
    
    public void SetupWeapon(WeaponData data, int ammo)
    {
        weaponData = data;
        currentAmmo = ammo;
        Initialize();
    }
    
    void Initialize()
    {
        if (weaponData == null)
        {
            Debug.LogError($"[WeaponPickup] {gameObject.name} - WeaponData is null!");
            return;
        }
        
        isInitialized = true;
        
        // 设置名称
        gameObject.name = $"Pickup_{weaponData.weaponName}";
        
        // 创建武器显示
        CreateWeaponDisplay();
        
        // 开始动画
        if (enableFloatAnimation)
        {
            originalPosition = transform.position;
            StartCoroutine(FloatAnimation());
        }
        
        Debug.Log($"[WeaponPickup] Initialized {weaponData.weaponName} with {currentAmmo} ammo");
    }
    
    void CreateWeaponDisplay()
    {
        // 如果已经有精灵渲染器，使用它
        if (weaponSprite == null)
        {
            weaponSprite = GetComponent<SpriteRenderer>();
        }
        
        if (weaponSprite == null)
        {
            // 创建新的精灵显示
            GameObject spriteObj = new GameObject("WeaponSprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            
            weaponSprite = spriteObj.AddComponent<SpriteRenderer>();
        }
        
        // 设置精灵
        if (weaponData.weaponIcon != null)
        {
            weaponSprite.sprite = weaponData.weaponIcon;
            weaponSprite.color = Color.cyan; // 武器用青色标识
            weaponSprite.transform.localScale = Vector3.one * 1.5f;
            weaponSprite.sortingOrder = 10;
        }
        
        // 面向相机
        StartCoroutine(FaceCamera());
    }
    
    System.Collections.IEnumerator FaceCamera()
    {
        while (this != null && weaponSprite != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 directionToCamera = mainCam.transform.position - weaponSprite.transform.position;
                directionToCamera.y = 0; // 只在水平面旋转
                
                if (directionToCamera != Vector3.zero)
                {
                    weaponSprite.transform.rotation = Quaternion.LookRotation(directionToCamera);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    System.Collections.IEnumerator FloatAnimation()
    {
        while (this != null && isInitialized)
        {
            float time = Time.time * floatSpeed;
            
            // 上下浮动
            Vector3 newPos = originalPosition + Vector3.up * Mathf.Sin(time) * floatHeight;
            transform.position = newPos;
            
            // 旋转
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            
            yield return null;
        }
    }
    
    // 重写交互文本
    public override string GetInteractionText()
    {
        if (weaponData != null)
        {
            return $"拾取 {weaponData.weaponName}";
        }
        return "拾取武器";
    }
    
    public override void OnInteract()
    {
        Debug.Log($"[WeaponPickup] {gameObject.name} - OnInteract called!");
        
        if (!isInitialized || weaponData == null)
        {
            Debug.LogError("[WeaponPickup] 武器未正确初始化!");
            ShowMessage("武器数据错误!");
            return;
        }

        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            ShowMessage("未找到武器管理器!");
            return;
        }

        // 检查是否已有相同类型武器
        WeaponType weaponType = GetWeaponType();
        
        if (weaponManager.HasWeaponType(weaponType))
        {
            // 已有武器，补充弹药
            HandleAmmoPickup();
        }
        else
        {
            // 添加新武器
            if (TryAddWeapon(weaponManager))
            {
                ShowMessage($"获得了 {weaponData.weaponName}!");
                StartCoroutine(DestroyAfterPickup());
            }
            else
            {
                ShowMessage("无法拾取武器!");
            }
        }
    }
    
    bool TryAddWeapon(WeaponManager weaponManager)
    {
        try
        {
            // 在weaponHolder下创建武器
            GameObject weaponObj = new GameObject(weaponData.weaponName);
            weaponObj.transform.SetParent(weaponManager.weaponHolder);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;

            // 根据武器类型添加控制器
            WeaponController controller = null;
            WeaponType weaponType = GetWeaponType();

            switch (weaponType)
            {
                case WeaponType.Pistol:
                    controller = weaponObj.AddComponent<PistolController>();
                    break;
                case WeaponType.Rifle:
                    controller = weaponObj.AddComponent<AutoRifleController>();
                    break;
                default:
                    Debug.LogWarning($"未知武器类型: {weaponType}，使用通用控制器");
                    controller = weaponObj.AddComponent<WeaponController>();
                    break;
            }

            if (controller != null)
            {
                // 设置武器数据
                controller.weaponData = weaponData;
                controller.currentAmmo = currentAmmo;
                controller.weaponType = weaponType;

                // 添加到武器管理器
                weaponManager.AddWeapon(controller);
                
                Debug.Log($"[WeaponPickup] 成功添加武器: {weaponData.weaponName}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WeaponPickup] 添加武器失败: {e.Message}");
        }

        return false;
    }
    
    WeaponType GetWeaponType()
    {
        if (weaponData == null) return WeaponType.Pistol;
        
        string name = weaponData.weaponName.ToLower();
        
        if (name.Contains("pistol") || name.Contains("手枪"))
            return WeaponType.Pistol;
        if (name.Contains("rifle") || name.Contains("步枪") || name.Contains("自动"))
            return WeaponType.Rifle;
        if (name.Contains("knife") || name.Contains("刀"))
            return WeaponType.Knife;
            
        return WeaponType.Pistol; // 默认
    }
    
    void HandleAmmoPickup()
    {
        if (InventoryManager.Instance && currentAmmo > 0)
        {
            // 查找对应弹药
            ItemData ammoItem = FindAmmoItem(weaponData.ammoType);
            if (ammoItem != null)
            {
                if (InventoryManager.Instance.AddItem(ammoItem, currentAmmo))
                {
                    ShowMessage($"获得了 {currentAmmo} 发 {ammoItem.itemName}");
                    StartCoroutine(DestroyAfterPickup());
                }
                else
                {
                    ShowMessage("背包已满!");
                }
            }
            else
            {
                ShowMessage($"未找到对应弹药: {weaponData.ammoType}");
            }
        }
        else
        {
            ShowMessage("已拥有该武器，且没有额外弹药");
        }
    }
    
    ItemData FindAmmoItem(string ammoType)
    {
        // 从GameConfig查找弹药
        if (GameManager.Instance?.gameConfig?.ItemConfig?.allItems != null)
        {
            foreach (var item in GameManager.Instance.gameConfig.ItemConfig.allItems)
            {
                if (item != null && item.IsAmmo && IsMatchingAmmoType(item.ammoType, ammoType))
                {
                    return item;
                }
            }
        }

        Debug.LogWarning($"[WeaponPickup] 未找到弹药类型: {ammoType}");
        return null;
    }
    
    
    public void SetupFromDroppedWeapon(WeaponController droppedWeapon)
    {
        if (droppedWeapon == null || droppedWeapon.weaponData == null)
        {
            Debug.LogError("[WeaponPickup] Invalid dropped weapon data!");
            return;
        }
        
        // 设置武器数据和当前弹药
        weaponData = droppedWeapon.weaponData;
        currentAmmo = droppedWeapon.CurrentAmmo; // 保留丢弃时的弹药数量
        
        Debug.Log($"[WeaponPickup] Setup from dropped weapon: {weaponData.weaponName} with {currentAmmo} ammo");
        
        // 初始化显示
        Initialize();
    }
    
    bool IsMatchingAmmoType(string itemAmmoType, string weaponAmmoType)
    {
        if (string.IsNullOrEmpty(itemAmmoType) || string.IsNullOrEmpty(weaponAmmoType))
            return false;

        return itemAmmoType.Equals(weaponAmmoType, System.StringComparison.OrdinalIgnoreCase) ||
               itemAmmoType.Contains(weaponAmmoType) ||
               weaponAmmoType.Contains(itemAmmoType);
    }
    
    void ShowMessage(string message)
    {
        Debug.Log($"[WeaponPickup] {message}");
        
        if (UIManager.Instance)
            UIManager.Instance.ShowMessage(message, 2f);
    }
    
    System.Collections.IEnumerator DestroyAfterPickup()
    {
        // 停止所有动画
        StopAllCoroutines();
        
        // 播放拾取动画
        if (weaponSprite != null)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + Vector3.up * 1f;
            Vector3 startScale = transform.localScale;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                if (weaponSprite != null)
                {
                    Color color = weaponSprite.color;
                    color.a = 1f - t;
                    weaponSprite.color = color;
                }
                
                yield return null;
            }
        }
        
        Destroy(gameObject);
    }
    
    // 调试信息
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;
        
        if (weaponData != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
                GUI.color = Color.green;
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, 20), 
                    $"{weaponData.weaponName} ({currentAmmo})");
                GUI.color = Color.white;
            }
        }
    }
    
}