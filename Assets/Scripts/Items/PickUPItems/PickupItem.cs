using UnityEngine;

public class PickupItem : BaseInteractable 
{
    [Header("物品设置")]
    public ItemData itemData;
    public WeaponData weaponData; // 如果是武器
    public int quantity = 1;
    
    [Header("显示组件")]
    public WorldItemDisplay worldDisplay;
    
    [Header("音效")]
    public AudioClip pickupSound;
    
    private AudioSource audioSource;
    private bool isWeapon = false;
    
    protected override void Start()
    {
        base.Start();
        
        // 获取音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D音效
        }
        
        // 自动获取WorldItemDisplay组件
        if (worldDisplay == null)
        {
            worldDisplay = GetComponentInChildren<WorldItemDisplay>();
        }
        
        // 如果没有WorldItemDisplay，创建一个
        if (worldDisplay == null)
        {
            CreateWorldDisplay();
        }
        
        SetupItemDisplay();
    }
    
    void CreateWorldDisplay()
    {
        // 创建子对象来显示2D精灵
        GameObject displayObj = new GameObject("ItemDisplay");
        displayObj.transform.SetParent(transform);
        displayObj.transform.localPosition = Vector3.zero;
        
        worldDisplay = displayObj.AddComponent<WorldItemDisplay>();
        displayObj.AddComponent<SpriteRenderer>();
    }
    
    void SetupItemDisplay()
    {
        if (worldDisplay == null) return;
        
        // 判断是武器还是普通道具
        if (weaponData != null)
        {
            isWeapon = true;
            worldDisplay.SetWeaponData(weaponData);
            
            // 武器可能有特殊颜色
            if (weaponData.weaponName.Contains("Rifle"))
            {
                worldDisplay.SetColor(Color.cyan); // 步枪用青色
            }
            else if (weaponData.weaponName.Contains("Pistol"))
            {
                worldDisplay.SetColor(Color.white); // 手枪用白色
            }
        }
        else if (itemData != null)
        {
            isWeapon = false;
            worldDisplay.SetItemData(itemData);
            
            // 根据道具类型设置颜色
            Color itemColor = itemData.itemType switch
            {
                ItemType.Food => Color.green,
                ItemType.Water => Color.blue,
                ItemType.Medicine => Color.red,
                ItemType.Ammo => Color.yellow,
                ItemType.Key => Color.magenta,
                _ => Color.white
            };
            worldDisplay.SetColor(itemColor);
        }
    }
    
    protected override void OnInteract()
    {
        if (TryPickup())
        {
            // 播放拾取动画
            if (worldDisplay != null)
            {
                worldDisplay.OnPickedUp();
            }
            
            // 播放音效
            PlayPickupSound();
            
            // 短暂延迟后销毁（给动画时间）
            Destroy(gameObject, 0.5f);
        }
    }
    
    bool TryPickup()
    {
        if (!InventoryManager.Instance) 
        {
            Debug.LogWarning("InventoryManager not found!");
            return false;
        }
        
        bool success = false;
        string message = "";
        
        if (isWeapon && weaponData != null)
        {
            // 武器拾取逻辑 - 可以考虑直接装备或放入背包
            // 这里简化为放入背包（需要ItemData包装）
            if (TryAddWeaponToInventory())
            {
                success = true;
                message = $"拾取了 {weaponData.weaponName}";
            }
            else
            {
                message = "背包已满!";
            }
        }
        else if (itemData != null)
        {
            // 普通道具拾取
            if (InventoryManager.Instance.AddItem(itemData, quantity))
            {
                success = true;
                message = $"拾取了 {quantity} 个 {itemData.itemName}";
                
                // 更新任务进度
                GameEventManager.UpdateQuestProgress("collect", itemData.itemName, quantity);
            }
            else
            {
                message = "背包已满!";
            }
        }
        
        // 显示拾取消息
        if (UIManager.Instance)
        {
            UIManager.Instance.ShowMessage(message, 2f);
        }
        
        Debug.Log(message);
        return success;
    }
    
    bool TryAddWeaponToInventory()
    {
        // 这里需要将武器转换为ItemData或者实现武器专用的背包系统
        // 简化实现：如果有对应的ItemData，就添加到背包
        if (weaponData != null)
        {
            // 尝试找到对应的武器ItemData
            var weaponItemData = FindWeaponItemData(weaponData.weaponName);
            if (weaponItemData != null)
            {
                return InventoryManager.Instance.AddItem(weaponItemData, 1);
            }
        }
        return false;
    }
    
    ItemData FindWeaponItemData(string weaponName)
    {
        // 在配置中查找对应的武器ItemData
        if (ConfigManager.Instance?.Item?.allItems != null)
        {
            foreach (var item in ConfigManager.Instance.Item.allItems)
            {
                if (item.itemType == ItemType.Weapon && item.itemName.Contains(weaponName))
                {
                    return item;
                }
            }
        }
        return null;
    }
    
    void PlayPickupSound()
    {
        AudioClip soundToPlay = pickupSound;
        
        // 如果没有指定音效，尝试从配置获取
        if (soundToPlay == null && ConfigManager.Instance?.Item?.pickupSound != null)
        {
            soundToPlay = ConfigManager.Instance.Item.pickupSound;
        }
        
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    // 设置物品数据（用于动态生成）
    public void SetItemData(ItemData data, int qty = 1)
    {
        itemData = data;
        quantity = qty;
        isWeapon = false;
        SetupItemDisplay();
    }
    
    public void SetWeaponData(WeaponData data)
    {
        weaponData = data;
        isWeapon = true;
        SetupItemDisplay();
    }
    
    // 调试方法
    void OnValidate()
    {
        if (Application.isPlaying && worldDisplay != null)
        {
            SetupItemDisplay();
        }
    }
}