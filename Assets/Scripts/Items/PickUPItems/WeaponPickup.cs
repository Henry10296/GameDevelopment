using UnityEngine;
using System.Collections;

public class WeaponPickup : BaseInteractable
{
    [Header("武器数据")]
    public WeaponData weaponData;
    public int currentAmmo;
    
    [Header("显示")]
    public SpriteRenderer weaponSprite;
    public GameObject weaponModel; // 3D模型（可选）
    
    [Header("动画设置")]
    public bool enableFloatAnimation = true;
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;
    public float rotationSpeed = 45f;
    
    private Vector3 originalPosition;
    private bool isInitialized = false;
    
    protected override void Start()
    {
        base.Start();
        
        if (!isInitialized && weaponData != null)
        {
            Initialize();
        }
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
            Debug.LogError("[WeaponPickup] WeaponData is null!");
            return;
        }
        
        isInitialized = true;
        
        // 设置名称
        gameObject.name = $"Pickup_{weaponData.weaponName}";
        
        // 设置碰撞器
        SetupCollider();
        
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
    
    void SetupCollider()
    {
        // 移除现有的碰撞器
        Collider[] existingColliders = GetComponents<Collider>();
        foreach (var col in existingColliders)
        {
            DestroyImmediate(col);
        }
        
        // 添加新的碰撞器
        SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
        sphereCol.radius = 1.5f;
        sphereCol.isTrigger = true;
        
        // 设置交互范围
        interactionRange = 3f;
    }
    
    void CreateWeaponDisplay()
    {
        // 优先使用3D模型
        if (weaponData.visualConfig != null && weaponData.visualConfig.worldSprite != null)
        {
            CreateSpriteDisplay();
        }
        else if (weaponData.weaponIcon != null)
        {
            CreateSpriteDisplay();
        }
        else
        {
            CreateFallbackDisplay();
        }
    }
    
    void CreateSpriteDisplay()
    {
        // 创建精灵显示
        GameObject spriteObj = new GameObject("WeaponSprite");
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = Vector3.zero;
        
        weaponSprite = spriteObj.AddComponent<SpriteRenderer>();
        
        // 设置精灵
        if (weaponData.visualConfig != null && weaponData.visualConfig.worldSprite != null)
        {
            weaponSprite.sprite = weaponData.visualConfig.worldSprite;
        }
        else
        {
            weaponSprite.sprite = weaponData.weaponIcon;
        }
        
        // 设置颜色和大小
        weaponSprite.color = Color.cyan; // 武器用青色标识
        weaponSprite.transform.localScale = Vector3.one * 1.5f;
        
        // 设置渲染层级
        weaponSprite.sortingOrder = 10;
        
        // 开始面向相机的协程
        StartCoroutine(FaceCamera());
    }
    
    void CreateFallbackDisplay()
    {
        // 创建简单的立方体作为备用显示
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = Vector3.one * 0.5f;
        
        // 销毁其碰撞器（我们已经有了主碰撞器）
        Destroy(cube.GetComponent<Collider>());
        
        // 设置材质
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.cyan;
        }
    }
    
    System.Collections.IEnumerator FaceCamera()
    {
        while (this != null && weaponSprite != null)
        {
            if (Camera.main != null)
            {
                Vector3 directionToCamera = Camera.main.transform.position - weaponSprite.transform.position;
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
    
    public override void OnInteract()
    {
        if (!isInitialized || weaponData == null)
        {
            Debug.LogError("[WeaponPickup] Cannot interact - not properly initialized!");
            return;
        }
        
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            ShowMessage("武器管理器未找到!");
            return;
        }
        
        // 检查是否已有此武器类型
        WeaponType weaponType = GetWeaponType();
        if (weaponManager.HasWeaponType(weaponType))
        {
            // 已有武器，补充弹药
            HandleAmmoPickup();
        }
        else
        {
            // 添加新武器
            if (AddWeaponToManager(weaponManager))
            {
                ShowMessage($"获得了 {weaponData.weaponName}!");
                
                // 成功拾取后销毁
                StartCoroutine(DestroyAfterPickup());
            }
            else
            {
                ShowMessage("无法拾取武器!");
            }
        }
    }
    
    void HandleAmmoPickup()
    {
        if (InventoryManager.Instance && currentAmmo > 0)
        {
            // 查找对应的弹药ItemData
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
                    ShowMessage("背包已满，无法拾取弹药!");
                }
            }
            else
            {
                ShowMessage($"未找到对应的弹药类型: {weaponData.ammoType}");
            }
        }
        else
        {
            ShowMessage("已拥有该武器，且没有额外弹药");
        }
    }
    
    bool AddWeaponToManager(WeaponManager weaponManager)
    {
        if (weaponManager.weaponHolder == null)
        {
            Debug.LogError("[WeaponPickup] WeaponManager.weaponHolder is null!");
            return false;
        }
        
        // 在weaponHolder下创建武器GameObject
        GameObject weaponObj = new GameObject(weaponData.weaponName);
        weaponObj.transform.SetParent(weaponManager.weaponHolder);
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;
        
        // 根据武器类型添加对应的Controller
        WeaponController controller = null;
        WeaponType weaponType = GetWeaponType();
        
        try
        {
            switch (weaponType)
            {
                case WeaponType.Pistol:
                    controller = weaponObj.AddComponent<PistolController>();
                    break;
                case WeaponType.Rifle:
                    controller = weaponObj.AddComponent<AutoRifleController>();
                    break;
                default:
                    // 创建基础武器控制器（需要实现一个具体的类）
                    controller = weaponObj.AddComponent<GenericWeaponController>();
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
                
                Debug.Log($"[WeaponPickup] Successfully added {weaponData.weaponName} to WeaponManager");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WeaponPickup] Error adding weapon: {e.Message}");
            if (weaponObj != null)
                Destroy(weaponObj);
        }
        
        return false;
    }
    
    WeaponType GetWeaponType()
    {
        // 根据武器名称或类型判断
        string name = weaponData.weaponName.ToLower();
        
        if (name.Contains("pistol") || name.Contains("手枪"))
            return WeaponType.Pistol;
        if (name.Contains("rifle") || name.Contains("步枪"))
            return WeaponType.Rifle;
        if (name.Contains("knife") || name.Contains("刀"))
            return WeaponType.Knife;
            
        // 默认返回手枪
        return WeaponType.Pistol;
    }
    
    ItemData FindAmmoItem(string ammoType)
    {
        // 尝试多种方式查找弹药ItemData
        
        // 方法1：从ItemDatabase查找
        ItemDatabase itemDB = FindObjectOfType<ItemDatabase>();
        if (itemDB != null)
        {
            foreach (var item in itemDB.ammoItems)
            {
                if (IsMatchingAmmoType(item.ammoType, ammoType))
                    return item;
            }
        }
        
        // 方法2：从Resources加载
        ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
        foreach (var item in allItems)
        {
            if (item.IsAmmo && IsMatchingAmmoType(item.ammoType, ammoType))
                return item;
        }
        
        // 方法3：从GameConfig查找
        if (GameManager.Instance?.gameConfig?.ItemConfig?.allItems != null)
        {
            foreach (var item in GameManager.Instance.gameConfig.ItemConfig.allItems)
            {
                if (item.IsAmmo && IsMatchingAmmoType(item.ammoType, ammoType))
                    return item;
            }
        }
        
        Debug.LogWarning($"[WeaponPickup] Could not find ammo item for type: {ammoType}");
        return null;
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
                
                // 上升并缩小
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                // 淡出
                if (weaponSprite != null)
                {
                    Color color = weaponSprite.color;
                    color.a = 1f - t;
                    weaponSprite.color = color;
                }
                
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 销毁对象
        Destroy(gameObject);
    }
    
    void OnDrawGizmosSelected()
    {
        // 显示交互范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 显示武器信息
        if (weaponData != null)
        {
            Gizmos.color = Color.white;
            Vector3 textPos = transform.position + Vector3.up * 2f;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(textPos, $"{weaponData.weaponName}\nAmmo: {currentAmmo}");
            #endif
        }
    }
}

// 添加一个通用武器控制器，用于处理基础武器类型
public class GenericWeaponController : WeaponController
{
    public override void Initialize(WeaponManager manager)
    {
        base.Initialize(manager);
        
        // 根据WeaponData设置具体属性
        if (weaponData != null)
        {
            damage = weaponData.damage;
            range = weaponData.range;
            fireRate = weaponData.fireRate;
            maxAmmo = weaponData.maxAmmo;
            isAutomatic = weaponData.isAutomatic;
        }
        
        Debug.Log($"[GenericWeaponController] Initialized {weaponName}");
    }
}