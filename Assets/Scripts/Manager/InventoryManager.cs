using UnityEngine;
using System.Collections.Generic;
using System;
public class InventoryManager : Singleton<InventoryManager>
{
    [Header("背包设置")]
    public int maxSlots = 9;

    private List<InventoryItem> items = new List<InventoryItem>();
    
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
    public bool AddItem(ItemData itemData, int quantity = 1)
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
    }
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

    #region 弹药

    public int GetAmmoCount(string ammoType)
    {
        int totalAmmo = 0;
        foreach (var item in items)
        {
            if (item.itemData.itemType == ItemType.Ammo && 
                item.itemData.itemName.Contains(ammoType))
            {
                totalAmmo += item.quantity;
            }
        }
        return totalAmmo;
    }
    public bool ConsumeAmmo(string ammoType, int amount = 1)
    {
        if (GetAmmoCount(ammoType) < amount) return false;
    
        int remainingToConsume = amount;
        for (int i = items.Count - 1; i >= 0 && remainingToConsume > 0; i--)
        {
            var item = items[i];
            if (item.itemData.itemType == ItemType.Ammo && 
                item.itemData.itemName.Contains(ammoType))
            {
                int consumeFromThis = Mathf.Min(remainingToConsume, item.quantity);
                item.quantity -= consumeFromThis;
                remainingToConsume -= consumeFromThis;
            
                if (item.quantity <= 0)
                {
                    items.RemoveAt(i);
                }
            }
        }
    
        UpdateUI();
        return remainingToConsume == 0;
    }
    public bool HasAmmo(string ammoType, int amount = 1)
    {
        return GetAmmoCount(ammoType) >= amount;
    }
    public string GetAmmoDisplayName(string ammoType)
    {
        foreach (var item in items)
        {
            if (item.itemData.itemType == ItemType.Ammo && 
                item.itemData.itemName.Contains(ammoType))
            {
                return item.itemData.itemName;
            }
        }
        return ammoType + "弹药";
    }

    #endregion
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
