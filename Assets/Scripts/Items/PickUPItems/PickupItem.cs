using UnityEngine;

public class PickupItem : BaseInteractable 
{
    [Header("物品设置")]
    public ItemData itemData;
    public int quantity = 1;
    
    [Header("UI提示")] 
    public GameObject pickupPrompt; // 保留原有的提示对象
    public KeyCode pickupKey = KeyCode.F; // 保留设置，但使用基类的交互键
    
    [Header("配置引用")]
    public InputSettings inputSettings;
    public UITextSettings textSettings;
    
    protected override void Start() // 修复：调用基类Start
    {
        base.Start(); // 调用基类初始化
        
        // 从GameManager获取配置（如果没有直接引用）
        if (inputSettings == null && GameManager.Instance != null)
            inputSettings = GameManager.Instance.inputSettings;
        if (textSettings == null && GameManager.Instance != null)
            textSettings = GameManager.Instance.uiTextSettings;
        
        // 使用配置设置交互键
        if (inputSettings != null)
            interactionKey = inputSettings.pickupKey; // 设置基类的交互键
        
        if (pickupPrompt)
            pickupPrompt.SetActive(false);
    }
    
    protected override void Update() // 修复：使用基类的Update逻辑
    {
        base.Update(); // 使用基类的交互检测
        
        // 更新提示显示
        if (pickupPrompt && playerInRange != pickupPrompt.activeSelf)
        {
            pickupPrompt.SetActive(playerInRange);
        }
    }
    
    protected override void OnInteract() // 实现基类的抽象方法
    {
        TryPickup();
    }
    
    void TryPickup() // 保持原有的拾取逻辑
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
}