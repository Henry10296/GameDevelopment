using UnityEngine;

public class PickupItem : BaseInteractable // 修改现有继承
{
    // 现有字段保持不变
    [Header("物品设置")]
    public ItemData itemData;
    public int quantity = 1;
    
    // 移除现有重复的字段和方法，使用基类
    
    protected override void OnInteract() // 重写基类方法
    {
        TryPickup(); // 调用现有方法
    }
    
    void TryPickup() // 现有方法保持不变
    {
        if (InventoryManager.Instance && itemData)
        {
            if (InventoryManager.Instance.AddItem(itemData, quantity))
            {
                Debug.Log($"拾取了 {quantity} 个 {itemData.itemName}");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("背包已满!");
            }
        }
    }
}
