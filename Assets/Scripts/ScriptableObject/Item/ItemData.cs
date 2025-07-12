using UnityEngine;

public enum ItemType
{
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
    
    [Header("描述")]
    [TextArea(3, 5)]
    public string description;
    
    // 便捷方法
    public bool IsWeapon => itemType == ItemType.Weapon && weaponData != null;
    public bool IsAmmo => isAmmo && !string.IsNullOrEmpty(ammoType);
    public int GetPickupAmount() => IsAmmo ? defaultPickupAmount : 1;
}

// 物品配置数据库
