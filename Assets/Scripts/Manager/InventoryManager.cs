using UnityEngine;
using System.Collections.Generic;
using System;
public class InventoryManager : Singleton<InventoryManager>
{
    [Header("背包设置")]
    public int maxSlots = 9;

    [SerializeField]private List<InventoryItem> items = new List<InventoryItem>();
    
    [Header("物品事件")] 
    public StringGameEvent onItemChanged; // 物品变化事件

    public static event Action<List<InventoryItem>> OnInventoryChanged;
    
    
    protected override void OnSingletonApplicationQuit()
    {
        // 清理事件
        OnInventoryChanged = null;
        onItemChanged = null;
    
        Debug.Log("[InventoryManager] Application quit cleanup completed");
    }
    /*public bool AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null) return false;
        bool result = AddItemInternal(itemData, quantity); 
        // 检查是否可以堆叠到现有物品
        foreach (var item in items)
        {
            if (item.itemData == itemData && item.quantity < itemData.maxStackSize)
            {
                int addAmount = Mathf.Min(quantity, itemData.maxStackSize - item.quantity);
                item.quantity += addAmount;
                quantity -= addAmount;

                if (quantity <= 0)
                {
                    UpdateUI();
                    return true;
                }
            }
        }

        // 添加新物品槽
        while (quantity > 0 && items.Count < maxSlots)
        {
            int addAmount = Mathf.Min(quantity, itemData.maxStackSize);
            items.Add(new InventoryItem(itemData, addAmount));
            quantity -= addAmount;
        }

        UpdateUI();
        if (result)
        {
            GameEventManager.UpdateQuestProgress("collect", itemData.itemName, quantity);
        }
        return quantity <= 0;
    }*/
    private bool AddItemInternal(ItemData itemData, int quantity)
    {
        // 现有的AddItem逻辑
        if (itemData == null) return false;

        // 检查是否可以堆叠到现有物品
        foreach (var item in items)
        {
            if (item.itemData == itemData && item.quantity < itemData.maxStackSize)
            {
                int addAmount = Mathf.Min(quantity, itemData.maxStackSize - item.quantity);
                item.quantity += addAmount;
                quantity -= addAmount;

                if (quantity <= 0)
                {
                    UpdateUI();
                    return true;
                }
            }
        }

        // 添加新物品槽
        while (quantity > 0 && items.Count < maxSlots)
        {
            int addAmount = Mathf.Min(quantity, itemData.maxStackSize);
            items.Add(new InventoryItem(itemData, addAmount));
            quantity -= addAmount;
        }

        UpdateUI();
        return quantity <= 0;
    }
    public bool RemoveItem(string itemName, int quantity = 1)
    {
        int remainingToRemove = quantity;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].itemData.itemName == itemName)
            {
                if (items[i].quantity >= remainingToRemove)
                {
                    items[i].quantity -= remainingToRemove;
                    if (items[i].quantity <= 0)
                    {
                        items.RemoveAt(i);
                    }
                    UpdateUI();
                    return true;
                }
                else
                {
                    remainingToRemove -= items[i].quantity;
                    items.RemoveAt(i);
                }
            }
        }

        UpdateUI();
        return remainingToRemove <= 0;
    }

    public bool HasItem(string itemName, int quantity = 1)
    {
        int totalCount = 0;
        foreach (var item in items)
        {
            if (item.itemData.itemName == itemName)
            {
                totalCount += item.quantity;
            }
        }
        return totalCount >= quantity;
    }

    public int GetItemCount(string itemName)
    {
        int totalCount = 0;
        foreach (var item in items)
        {
            if (item.itemData.itemName == itemName)
            {
                totalCount += item.quantity;
            }
        }
        return totalCount;
    }

    public List<InventoryItem> GetItems()
    {
        return new List<InventoryItem>(items);
    }

    public bool IsFull()
    {
        return items.Count >= maxSlots;
    }

    void UpdateUI()
    {
        OnInventoryChanged?.Invoke(GetItems());
        onItemChanged?.Raise($"ItemCount:{GetItems().Count}");
    }

    // 使用物品
    public bool UseItem(string itemName)
    {
        foreach (var item in items)
        {
            if (item.itemData.itemName == itemName)
            {
                // 根据物品类型执行不同操作
                switch (item.itemData.itemType)
                {
                    case ItemType.Food:
                        if (FamilyManager.Instance)
                            FamilyManager.Instance.AddResource("food", 1);
                        break;
                    case ItemType.Water:
                        if (FamilyManager.Instance)
                            FamilyManager.Instance.AddResource("water", 1);
                        break;
                    case ItemType.Medicine:
                        if (FamilyManager.Instance)
                            FamilyManager.Instance.AddResource("medicine", 1);
                        break;
                }

                return RemoveItem(itemName, 1);
            }
        }
        return false;
    }

    public void ClearInventory()
    {
        items.Clear();
        UpdateUI();
    }

// 在你的InventoryManager.cs中添加这些方法：

#region 弹药系统
public int GetAmmoCount(string ammoType)
{
    int totalAmmo = 0;
    foreach (var item in items)
    {
        if (item.itemData.IsAmmo)
        {
            // 修复：支持模糊匹配，"9mm"可以匹配"9mm_Ammo"
            if (item.itemData.ammoType == ammoType || 
                item.itemData.ammoType.Contains(ammoType) || 
                ammoType.Contains(item.itemData.ammoType) ||
                item.itemData.itemName.Contains(ammoType))
            {
                totalAmmo += item.quantity;
            }
        }
    }
    
    Debug.Log($"[InventoryManager] GetAmmoCount({ammoType}): {totalAmmo}");
    return totalAmmo;
}

public bool ConsumeAmmo(string ammoType, int amount = 1)
{
    Debug.Log($"[InventoryManager] ConsumeAmmo({ammoType}, {amount})");
    
    if (GetAmmoCount(ammoType) < amount) 
    {
        Debug.LogWarning($"[InventoryManager] Not enough ammo. Required: {amount}, Available: {GetAmmoCount(ammoType)}");
        return false;
    }

    int remainingToConsume = amount;
    for (int i = items.Count - 1; i >= 0 && remainingToConsume > 0; i--)
    {
        var item = items[i];
        if (item.itemData.IsAmmo)
        {
            // 修复：支持模糊匹配
            bool isMatchingAmmo = item.itemData.ammoType == ammoType || 
                                  item.itemData.ammoType.Contains(ammoType) || 
                                  ammoType.Contains(item.itemData.ammoType) ||
                                  item.itemData.itemName.Contains(ammoType);
                                 
            if (isMatchingAmmo)
            {
                int consumeFromThis = Mathf.Min(remainingToConsume, item.quantity);
                item.quantity -= consumeFromThis;
                remainingToConsume -= consumeFromThis;
                
                Debug.Log($"[InventoryManager] Consumed {consumeFromThis} from {item.itemData.itemName}. Remaining in slot: {item.quantity}");

                if (item.quantity <= 0)
                {
                    Debug.Log($"[InventoryManager] Removing empty slot: {item.itemData.itemName}");
                    items.RemoveAt(i);
                }
            }
        }
    }

    UpdateUI();
    
    bool success = remainingToConsume == 0;
    Debug.Log($"[InventoryManager] ConsumeAmmo result: {success}, remaining to consume: {remainingToConsume}");
    return success;
}

public bool HasAmmo(string ammoType, int amount = 1)
{
    bool hasAmmo = GetAmmoCount(ammoType) >= amount;
    Debug.Log($"[InventoryManager] HasAmmo({ammoType}, {amount}): {hasAmmo}");
    return hasAmmo;
}

public string GetAmmoDisplayName(string ammoType)
{
    foreach (var item in items)
    {
        if (item.itemData.IsAmmo && item.itemData.ammoType == ammoType)
        {
            return item.itemData.itemName;
        }
    }
    return ammoType + "弹药";
}
#endregion

#region 武器系统
public bool HasWeapon(string weaponName)
{
    foreach (var item in items)
    {
        if (item.itemData.IsWeapon && item.itemData.weaponData.weaponName == weaponName)
        {
            return true;
        }
    }
    return false;
}

public ItemData GetWeaponItem(string weaponName)
{
    foreach (var item in items)
    {
        if (item.itemData.IsWeapon && item.itemData.weaponData.weaponName == weaponName)
        {
            return item.itemData;
        }
    }
    return null;
}
#endregion

// 修改AddItem方法以支持自动堆叠弹药
    public bool AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null) 
        {
            Debug.LogWarning("[InventoryManager] ItemData is null!");
            return false;
        }

        Debug.Log($"[InventoryManager] Adding {quantity}x {itemData.itemName} (Type: {itemData.itemType})");

        // 特殊处理弹药 - 自动堆叠同类型弹药
        if (itemData.IsAmmo)
        {
            return AddAmmoItem(itemData, quantity);
        }

        // 检查是否可以堆叠到现有物品
        if (itemData.stackable)
        {
            foreach (var item in items)
            {
                if (item.itemData == itemData && item.quantity < itemData.maxStackSize)
                {
                    int addAmount = Mathf.Min(quantity, itemData.maxStackSize - item.quantity);
                    item.quantity += addAmount;
                    quantity -= addAmount;

                    Debug.Log($"[InventoryManager] Stacked {addAmount} to existing item. New quantity: {item.quantity}");

                    if (quantity <= 0)
                    {
                        UpdateUI();
                        return true;
                    }
                }
            }
        }

        // 添加新物品槽
        while (quantity > 0 && items.Count < maxSlots)
        {
            int addAmount = Mathf.Min(quantity, itemData.maxStackSize);
            items.Add(new InventoryItem(itemData, addAmount));
            quantity -= addAmount;
        
            Debug.Log($"[InventoryManager] Created new slot with {addAmount} items");
        }

        UpdateUI();
    
        bool success = quantity <= 0;
        Debug.Log($"[InventoryManager] AddItem result: {success}, remaining: {quantity}");
        return success;
    }

    bool AddAmmoItem(ItemData ammoData, int quantity)
    {
        Debug.Log($"[InventoryManager] Adding ammo: {quantity}x {ammoData.itemName} (Type: {ammoData.ammoType})");
    
        // 查找相同弹药类型的物品进行堆叠
        foreach (var item in items)
        {
            if (item.itemData.IsAmmo && item.itemData.ammoType == ammoData.ammoType)
            {
                int addAmount = Mathf.Min(quantity, ammoData.maxStackSize - item.quantity);
                item.quantity += addAmount;
                quantity -= addAmount;
            
                Debug.Log($"[InventoryManager] Stacked {addAmount} ammo to existing {item.itemData.itemName}. New quantity: {item.quantity}");

                if (quantity <= 0)
                {
                    UpdateUI();
                    return true;
                }
            }
        }

        // 添加新的弹药槽
        while (quantity > 0 && items.Count < maxSlots)
        {
            int addAmount = Mathf.Min(quantity, ammoData.maxStackSize);
            items.Add(new InventoryItem(ammoData, addAmount));
            quantity -= addAmount;
        
            Debug.Log($"[InventoryManager] Created new ammo slot: {addAmount}x {ammoData.itemName}");
        }

        UpdateUI();
    
        bool success = quantity <= 0;
        Debug.Log($"[InventoryManager] AddAmmoItem result: {success}, remaining: {quantity}");
        return success;
    }
}

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int quantity;

    public InventoryItem(ItemData data, int qty)
    {
        itemData = data;
        quantity = qty;
    }
}
