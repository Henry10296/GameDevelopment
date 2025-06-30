using UnityEngine;

public class PickupItem : BaseInteractable // 修改现有继承
{
    // 现有字段保持不变
    [Header("物品设置")]
    public ItemData itemData;
    public int quantity = 1;
    [Header("配置引用")] // 添加到现有字段后
    public InputSettings inputSettings;
    public UITextSettings textSettings;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 从GameManager获取配置（如果没有直接引用）
        if (inputSettings == null && GameManager.Instance != null)
            inputSettings = GameManager.Instance.inputSettings;
        if (textSettings == null && GameManager.Instance != null)
            textSettings = GameManager.Instance.uiTextSettings;
        
        // 使用配置设置交互键
        if (inputSettings != null)
            pickupKey = inputSettings.pickupKey;
        
        if (pickupPrompt)
            pickupPrompt.SetActive(false);
    }
    void OnRangeChanged(bool inRange)
    {
        if (pickupPrompt) pickupPrompt.SetActive(inRange);
        
        if (inRange && UIManager.Instance && textSettings != null && inputSettings != null)
        {
            string promptText = textSettings.GetText("PICKUP_PROMPT", inputSettings.pickupKey);
            UIManager.Instance.ShowInteractionPrompt(promptText);
        }
        else if (!inRange && UIManager.Instance)
        {
            UIManager.Instance.HideInteractionPrompt();
        }
    }
    // 移除现有重复的字段和方法，使用基类
    
    protected override void OnInteract() // 重写基类方法
    {
        TryPickup(); // 调用现有方法
    }
    
    
    void TryPickup()
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
                string fullBagText = textSettings?.GetText("INVENTORY_FULL") ?? "背包已满!";
                Debug.Log(fullBagText);
                UIManager.Instance?.ShowMessage(fullBagText);
            }
        }
    }
    /*void TryPickup() // 现有方法保持不变
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
    }*/
}
