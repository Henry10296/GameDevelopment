using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("所有物品")]
    public ItemData[] allItems;
    
    [Header("武器")]
    public ItemData[] weapons;
    
    [Header("弹药")]
    public ItemData[] ammoItems;
    
    [Header("消耗品")]
    public ItemData[] consumables;
    
    public ItemData GetItemByName(string itemName)
    {
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
                return item;
        }
        return null;
    }
    
    public ItemData GetItemByID(string itemID)
    {
        foreach (var item in allItems)
        {
            if (item.itemID == itemID)
                return item;
        }
        return null;
    }
    
    public ItemData[] GetItemsByType(ItemType itemType)
    {
        return System.Array.FindAll(allItems, item => item.itemType == itemType);
    }
}