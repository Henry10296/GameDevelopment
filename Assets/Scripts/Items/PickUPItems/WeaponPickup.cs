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
    private bool isBeingPickedUp = false; // 防止重复拾取
    
    protected override void Start()
    {
        base.Start();
        SetupCollider();
        
        if (weaponData != null)
        {
            Initialize();
        }
        else
        {
            AutoFindWeaponData();
        }
    }
    
    void AutoFindWeaponData()
    {
        if (GameManager.Instance?.gameConfig?.WeaponConfig?.pistol != null)
        {
            SetupWeapon(GameManager.Instance.gameConfig.WeaponConfig.pistol, 15);
            Debug.Log("[WeaponPickup] Auto-assigned pistol data for testing");
        }
        else
        {
            Debug.LogError("[WeaponPickup] No weapon data found!");
        }
    }
    
    void SetupCollider()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        
        col.isTrigger = true;
        col.radius = Mathf.Max(1.5f, interactionRange * 0.5f);
        
        Debug.Log($"[WeaponPickup] {gameObject.name} - Collider setup: radius={col.radius}");
    }
    
    public void SetupWeapon(WeaponData data, int ammo)
    {
        weaponData = data;
        currentAmmo = ammo;
        Initialize();
    }
    
    public void Initialize()
    {
        if (weaponData == null)
        {
            Debug.LogError($"[WeaponPickup] {gameObject.name} - WeaponData is null!");
            return;
        }
        
        isInitialized = true;
        gameObject.name = $"Pickup_{weaponData.weaponName}";
        
        CreateWeaponDisplay();
        
        if (enableFloatAnimation)
        {
            originalPosition = transform.position;
            StartCoroutine(FloatAnimation());
        }
        
        Debug.Log($"[WeaponPickup] Initialized {weaponData.weaponName} with {currentAmmo} ammo");
    }
    
    void CreateWeaponDisplay()
    {
        if (weaponSprite == null)
        {
            weaponSprite = GetComponent<SpriteRenderer>();
        }
        
        if (weaponSprite == null)
        {
            GameObject spriteObj = new GameObject("WeaponSprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            weaponSprite = spriteObj.AddComponent<SpriteRenderer>();
        }
        
        if (weaponData.weaponIcon != null)
        {
            weaponSprite.sprite = weaponData.weaponIcon;
            weaponSprite.color = Color.cyan;
            weaponSprite.transform.localScale = Vector3.one * 0.1f;
            weaponSprite.sortingOrder = 10;
        }
        
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
                directionToCamera.y = 0;
                
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
        while (this != null && isInitialized && !isBeingPickedUp)
        {
            float time = Time.time * floatSpeed;
            Vector3 newPos = originalPosition + Vector3.up * Mathf.Sin(time) * floatHeight;
            transform.position = newPos;
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            yield return null;
        }
    }
    
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
        // 防止重复拾取
        if (isBeingPickedUp)
        {
            Debug.Log("[WeaponPickup] Already being picked up, ignoring");
            return;
        }
        
        isBeingPickedUp = true;
        
        Debug.Log($"[WeaponPickup] {gameObject.name} - OnInteract called!");
    
        if (!isInitialized || weaponData == null)
        {
            Debug.LogError("[WeaponPickup] 武器未正确初始化!");
            ShowMessage("武器数据错误!");
            isBeingPickedUp = false;
            return;
        }

        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            ShowMessage("未找到武器管理器!");
            isBeingPickedUp = false;
            return;
        }

        WeaponType weaponType = GetWeaponType();
        bool pickupSuccessful = false;
    
        if (weaponManager.HasWeaponType(weaponType))
        {
            // 已有武器，补充弹药
            pickupSuccessful = HandleAmmoPickup();
        }
        else
        {
            // 添加新武器
            pickupSuccessful = TryAddWeapon(weaponManager);
        }
    
        Debug.Log($"[WeaponPickup] Pickup result: {pickupSuccessful}");
    
        // 重要修复：确保拾取成功才销毁
        if (pickupSuccessful)
        {
            Debug.Log("[WeaponPickup] Starting destroy sequence");
            StartCoroutine(DestroyAfterPickup());
        }
        else
        {
            Debug.Log("[WeaponPickup] Pickup failed, not destroying");
            isBeingPickedUp = false;
        }
    }
    
    bool HandleAmmoPickup()
    {
        if (InventoryManager.Instance && currentAmmo > 0)
        {
            ItemData ammoItem = FindAmmoItem(weaponData.ammoType);
            if (ammoItem != null)
            {
                if (InventoryManager.Instance.AddItem(ammoItem, currentAmmo))
                {
                    ShowMessage($"获得了 {currentAmmo} 发 {ammoItem.itemName}");
                    Debug.Log($"[WeaponPickup] Successfully added {currentAmmo} ammo to inventory");
                    return true;
                }
                else
                {
                    ShowMessage("背包已满!");
                    return false;
                }
            }
            else
            {
                ShowMessage($"未找到对应弹药: {weaponData.ammoType}");
                return false;
            }
        }
        else
        {
            ShowMessage("已拥有该武器，且没有额外弹药");
            return false;
        }
    }
    
    bool TryAddWeapon(WeaponManager weaponManager)
    {
        try
        {
            GameObject weaponObj = new GameObject(weaponData.weaponName);
            weaponObj.transform.SetParent(weaponManager.weaponHolder);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;

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
                    controller = weaponObj.AddComponent<WeaponController>();
                    break;
            }

            if (controller != null)
            {
                controller.weaponData = weaponData;
                controller.currentAmmo = currentAmmo;
                controller.weaponType = weaponType;

                weaponManager.AddWeapon(controller);
                ShowMessage($"获得了 {weaponData.weaponName}!");
                
                Debug.Log($"[WeaponPickup] 成功添加武器: {weaponData.weaponName}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WeaponPickup] 添加武器失败: {e.Message}");
        }

        ShowMessage("无法拾取武器!");
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
            
        return WeaponType.Pistol;
    }
    
    ItemData FindAmmoItem(string ammoType)
    {
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
    
    bool IsMatchingAmmoType(string itemAmmoType, string weaponAmmoType)
    {
        if (string.IsNullOrEmpty(itemAmmoType) || string.IsNullOrEmpty(weaponAmmoType))
            return false;

        return itemAmmoType.Equals(weaponAmmoType, System.StringComparison.OrdinalIgnoreCase) ||
               itemAmmoType.Contains(weaponAmmoType) ||
               weaponAmmoType.Contains(itemAmmoType);
    }
    
    // 重要修复：从丢弃武器设置数据
    public void SetupFromDroppedWeapon(WeaponController droppedWeapon)
    {
        if (droppedWeapon == null || droppedWeapon.weaponData == null)
        {
            Debug.LogError("[WeaponPickup] Invalid dropped weapon data!");
            return;
        }
        
        weaponData = droppedWeapon.weaponData;
        currentAmmo = droppedWeapon.CurrentAmmo; // 保留丢弃时的弹药数量
        
        Debug.Log($"[WeaponPickup] Setup from dropped weapon: {weaponData.weaponName} with {currentAmmo} ammo");
        
        Initialize();
    }
    
    void ShowMessage(string message)
    {
        Debug.Log($"[WeaponPickup] {message}");
        
        if (UIManager.Instance)
            UIManager.Instance.ShowMessage(message, 2f);
    }
    
    System.Collections.IEnumerator DestroyAfterPickup()
    {
        Debug.Log("[WeaponPickup] DestroyAfterPickup started");
        
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
            
            while (elapsed < duration && this != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                if (transform != null)
                {
                    transform.position = Vector3.Lerp(startPos, targetPos, t);
                    transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                }
                
                if (weaponSprite != null)
                {
                    Color color = weaponSprite.color;
                    color.a = 1f - t;
                    weaponSprite.color = color;
                }
                
                yield return null;
            }
        }
        
        Debug.Log("[WeaponPickup] Destroying pickup object");
        
        // 确保对象被销毁
        if (this != null && gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
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