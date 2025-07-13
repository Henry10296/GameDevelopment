using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("所有物品")]
    public ItemData[] allItems;
    
    [Header("分类物品")]
    public ItemData[] weaponItems;
    public ItemData[] ammoItems;
    public ItemData[] consumableItems;
    public ItemData[] keyItems;
    
    [Header("武器数据")]
    public WeaponData[] allWeapons;
    
    private Dictionary<string, ItemData> itemLookup;
    private Dictionary<string, WeaponData> weaponLookup;
    
    void OnEnable()
    {
        BuildLookupTables();
    }
    
    void BuildLookupTables()
    {
        // 构建物品查找表
        itemLookup = new Dictionary<string, ItemData>();
        
        if (allItems != null)
        {
            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemName))
                {
                    itemLookup[item.itemName.ToLower()] = item;
                    if (!string.IsNullOrEmpty(item.itemID))
                    {
                        itemLookup[item.itemID.ToLower()] = item;
                    }
                }
            }
        }
        
        // 构建武器查找表
        weaponLookup = new Dictionary<string, WeaponData>();
        
        if (allWeapons != null)
        {
            foreach (var weapon in allWeapons)
            {
                if (weapon != null && !string.IsNullOrEmpty(weapon.weaponName))
                {
                    weaponLookup[weapon.weaponName.ToLower()] = weapon;
                }
            }
        }
        
        // 自动分类（如果分类数组为空）
        if (ShouldAutoCategorize())
        {
            AutoCategorizeItems();
        }
    }
    
    bool ShouldAutoCategorize()
    {
        return (weaponItems == null || weaponItems.Length == 0) &&
               (ammoItems == null || ammoItems.Length == 0) &&
               (consumableItems == null || consumableItems.Length == 0);
    }
    
    void AutoCategorizeItems()
    {
        if (allItems == null) return;
        
        List<ItemData> weapons = new List<ItemData>();
        List<ItemData> ammo = new List<ItemData>();
        List<ItemData> consumables = new List<ItemData>();
        List<ItemData> keys = new List<ItemData>();
        
        foreach (var item in allItems)
        {
            if (item == null) continue;
            
            switch (item.itemType)
            {
                case ItemType.Weapon:
                    weapons.Add(item);
                    break;
                case ItemType.Ammo:
                    ammo.Add(item);
                    break;
                case ItemType.Food:
                case ItemType.Water:
                case ItemType.Medicine:
                    consumables.Add(item);
                    break;
                case ItemType.Key:
                    keys.Add(item);
                    break;
            }
        }
        
        weaponItems = weapons.ToArray();
        ammoItems = ammo.ToArray();
        consumableItems = consumables.ToArray();
        keyItems = keys.ToArray();
        
        Debug.Log($"[ItemDatabase] Auto-categorized: {weapons.Count} weapons, {ammo.Count} ammo, {consumables.Count} consumables, {keys.Count} keys");
    }
    
    // 查找方法
    public ItemData GetItem(string nameOrId)
    {
        if (itemLookup == null) BuildLookupTables();
        
        if (string.IsNullOrEmpty(nameOrId)) return null;
        
        itemLookup.TryGetValue(nameOrId.ToLower(), out ItemData item);
        return item;
    }
    
    public WeaponData GetWeapon(string weaponName)
    {
        if (weaponLookup == null) BuildLookupTables();
        
        if (string.IsNullOrEmpty(weaponName)) return null;
        
        weaponLookup.TryGetValue(weaponName.ToLower(), out WeaponData weapon);
        return weapon;
    }
    
    public ItemData GetAmmoItem(string ammoType)
    {
        if (ammoItems == null) return null;
        
        foreach (var item in ammoItems)
        {
            if (item != null && item.IsAmmo)
            {
                if (IsMatchingAmmoType(item.ammoType, ammoType))
                    return item;
            }
        }
        
        return null;
    }
    
    bool IsMatchingAmmoType(string itemAmmoType, string targetAmmoType)
    {
        if (string.IsNullOrEmpty(itemAmmoType) || string.IsNullOrEmpty(targetAmmoType))
            return false;
            
        return itemAmmoType.Equals(targetAmmoType, System.StringComparison.OrdinalIgnoreCase) ||
               itemAmmoType.Contains(targetAmmoType) ||
               targetAmmoType.Contains(itemAmmoType);
    }
    
    // 获取分类物品
    public ItemData[] GetItemsByType(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Weapon => weaponItems ?? new ItemData[0],
            ItemType.Ammo => ammoItems ?? new ItemData[0],
            ItemType.Food or ItemType.Water or ItemType.Medicine => consumableItems ?? new ItemData[0],
            ItemType.Key => keyItems ?? new ItemData[0],
            _ => allItems?.Where(item => item != null && item.itemType == itemType).ToArray() ?? new ItemData[0]
        };
    }
    
    // 创建物品实例
    public GameObject CreateItemPickup(string itemName, Vector3 position, int quantity = 1)
    {
        ItemData itemData = GetItem(itemName);
        if (itemData == null)
        {
            Debug.LogError($"[ItemDatabase] Item not found: {itemName}");
            return null;
        }
        
        return CreateItemPickup(itemData, position, quantity);
    }
    
    public GameObject CreateItemPickup(ItemData itemData, Vector3 position, int quantity = 1)
    {
        if (itemData == null) return null;
        
        // 创建拾取物品GameObject
        GameObject pickupObj = new GameObject($"Pickup_{itemData.itemName}");
        pickupObj.transform.position = position;
        
        // 添加PickupItem组件
        PickupItem pickup = pickupObj.AddComponent<PickupItem>();
        pickup.SetItemData(itemData, quantity);
        
        return pickupObj;
    }
    
    public GameObject CreateWeaponPickup(string weaponName, Vector3 position, int ammo = -1)
    {
        WeaponData weaponData = GetWeapon(weaponName);
        if (weaponData == null)
        {
            Debug.LogError($"[ItemDatabase] Weapon not found: {weaponName}");
            return null;
        }
        
        return CreateWeaponPickup(weaponData, position, ammo);
    }
    
    public GameObject CreateWeaponPickup(WeaponData weaponData, Vector3 position, int ammo = -1)
    {
        if (weaponData == null) return null;
        
        // 创建武器拾取GameObject
        GameObject pickupObj = new GameObject($"WeaponPickup_{weaponData.weaponName}");
        pickupObj.transform.position = position;
        
        // 添加WeaponPickup组件
        WeaponPickup pickup = pickupObj.AddComponent<WeaponPickup>();
        int weaponAmmo = ammo >= 0 ? ammo : weaponData.maxAmmo;
        pickup.SetupWeapon(weaponData, weaponAmmo);
        
        return pickupObj;
    }
    
    // 验证数据完整性
    [ContextMenu("Validate Database")]
    public void ValidateDatabase()
    {
        List<string> errors = new List<string>();
        
        // 检查重复项
        HashSet<string> names = new HashSet<string>();
        foreach (var item in allItems)
        {
            if (item == null) continue;
            
            if (names.Contains(item.itemName))
            {
                errors.Add($"Duplicate item name: {item.itemName}");
            }
            else
            {
                names.Add(item.itemName);
            }
            
            // 检查必要字段
            if (string.IsNullOrEmpty(item.itemName))
                errors.Add($"Item has empty name");
            
            if (item.icon == null)
                errors.Add($"Item {item.itemName} has no icon");
                
            // 检查弹药类型
            if (item.IsAmmo && string.IsNullOrEmpty(item.ammoType))
                errors.Add($"Ammo item {item.itemName} has no ammo type");
        }
        
        // 检查武器数据
        foreach (var weapon in allWeapons)
        {
            if (weapon == null) continue;
            
            if (string.IsNullOrEmpty(weapon.weaponName))
                errors.Add($"Weapon has empty name");
                
            if (weapon.weaponIcon == null)
                errors.Add($"Weapon {weapon.weaponName} has no icon");
        }
        
        if (errors.Count > 0)
        {
            Debug.LogError($"[ItemDatabase] Found {errors.Count} validation errors:\n" + string.Join("\n", errors));
        }
        else
        {
            Debug.Log("[ItemDatabase] Validation passed - no errors found");
        }
    }
    
    // 统计信息
    public string GetStatistics()
    {
        string stats = "=== ItemDatabase Statistics ===\n";
        stats += $"Total Items: {allItems?.Length ?? 0}\n";
        stats += $"Weapons: {weaponItems?.Length ?? 0}\n";
        stats += $"Ammo: {ammoItems?.Length ?? 0}\n";
        stats += $"Consumables: {consumableItems?.Length ?? 0}\n";
        stats += $"Key Items: {keyItems?.Length ?? 0}\n";
        stats += $"Weapon Data: {allWeapons?.Length ?? 0}\n";
        
        return stats;
    }
}