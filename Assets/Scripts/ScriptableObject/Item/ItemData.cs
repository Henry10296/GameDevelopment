using UnityEngine;

public enum ItemType
{
    All = -1,     // 用于显示所有物品
    Food=1,
    Water=2,
    Medicine=3,
    Weapon=4,
    Ammo=5,
    Key=6,
    Material=7,
}
// 扩展你现有的ItemData
[CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基础信息")]
    public string itemName;
    public string itemID; 
    public Sprite icon;
    public ItemType itemType;

    [Header("显示设置")]
    public GameObject worldPrefab;        // 世界中的3D模型
    public Sprite worldSprite;           // 世界中的2D精灵（用于Doom风格）
    public bool useSprite = true;        // 是否使用2D精灵显示
    
    [Header("拾取设置")]
    public int defaultPickupAmount = 1;  // 默认拾取数量
    public bool stackable = true;        // 是否可堆叠
    public int maxStackSize = 99;
    
    [Header("武器专用")]
    public WeaponData weaponData;        // 如果是武器，关联的武器数据
    
    [Header("弹药专用")]
    public string ammoType;              // 弹药类型 "9mm", "5.56mm"
    public bool isAmmo = false;
    
    [Header("数值")]
    public int value = 1;
    
    [Header("视觉效果")]
    public Color itemColor = Color.white;
    public bool hasGlow = false;
    public Color glowColor = Color.yellow;
    
    
    [Header("通用数值配置")]
    public int value1 = 0;  // 万能数值1（弹药数量、治疗量等）
    public int value2 = 0;  // 万能数值2（伤害、容量等）
    public string stringValue = "";  // 万能字符串（弹药类型等）
    [Header("显示配置")]
    public float worldSize = 1f;  // 世界中的大小倍数
    [Header("描述")]
    [TextArea(3, 5)]
    public string description;
    
    // 便捷方法
    public bool IsWeapon => itemType == ItemType.Weapon && weaponData != null;
    public bool IsAmmo => itemType == ItemType.Ammo && !string.IsNullOrEmpty(ammoType);

    public int GetPickupAmount() 
    {
        if (IsAmmo)
        {
            // 如果设置了value1，使用value1，否则使用defaultPickupAmount
            if (value1 > 0) return value1;
            if (defaultPickupAmount > 0) return defaultPickupAmount;
        
            // 根据弹药类型返回默认值
            return ammoType switch
            {
                "9mm" or "9mm_Ammo" => 15,
                "5.56mm" or "5.56mm_Ammo" => 30,
                "7.62mm" or "7.62mm_Ammo" => 20,
                _ => 10
            };
        }
        return 1;
    }

    public float GetWorldSize()
    {
        if (worldSize != 1f) return worldSize;
    
        // 自动大小
        return itemType switch
        {
            ItemType.Weapon => 1.5f,  // 武器大一些
            ItemType.Ammo => 0.8f,    // 弹药小一些
            ItemType.Medicine => 1.2f, // 医疗包大一些
            _ => 1f
        };
    }

// 简单的使用方法
    public void UseItem()
    {
        switch (itemType)
        {
            case ItemType.Food:
                if (FamilyManager.Instance)
                    FamilyManager.Instance.AddResource("food", value1 > 0 ? value1 : 1);
                break;
            
            case ItemType.Water:
                if (FamilyManager.Instance)
                    FamilyManager.Instance.AddResource("water", value1 > 0 ? value1 : 1);
                break;
            
            case ItemType.Medicine:
                if (FamilyManager.Instance)
                    FamilyManager.Instance.AddResource("medicine", value1 > 0 ? value1 : 1);
                // 如果有玩家，直接治疗
                if (Player.Instance && value2 > 0)
                    Player.Instance.Heal(value2);
                break;
        }
    }
}

// 物品配置数据库
