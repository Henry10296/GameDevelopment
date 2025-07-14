using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : UIPanel
{
    [Header("=== 背包设置 ===")]
    public GameObject inventoryPanel;
    public Transform slotsContainer;
    public GameObject slotPrefab;
    public TextMeshProUGUI inventoryTitle;
    
    [Header("=== 物品信息 ===")]
    public GameObject itemInfoPanel;
    public Image itemInfoIcon;
    public TextMeshProUGUI itemInfoName;
    public TextMeshProUGUI itemInfoDescription;
    public Button useButton;
    public Button dropButton;
    public Button closeButton; // 新增关闭按钮
    
    [Header("=== 统计信息 ===")]
    public TextMeshProUGUI slotsUsedText;
    public TextMeshProUGUI inventoryWeight; // 重量显示
    
    [Header("=== 输入设置 ===")]
    public KeyCode toggleKey = KeyCode.Tab;
    
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private InventoryItem selectedItem;
    private bool isInitialized = false;
    
    public override void Initialize()
    {
        if (isInitialized) 
        {
            Debug.LogWarning("[InventoryUI] Already initialized, skipping...");
            return;
        }
    
        base.Initialize();
    
        // 确保InventoryManager存在
        if (!InventoryManager.Instance)
        {
            Debug.LogError("[InventoryUI] InventoryManager not found! Cannot initialize inventory UI.");
            return;
        }
    
        CreateInventorySlots();
        SetupEventListeners();
        SetupButtons();
    
        // 初始隐藏，但确保面板对象存在
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryUI] inventoryPanel is null! Please assign it in inspector.");
            return;
        }
    
        Hide();
        isInitialized = true;
    
        Debug.Log("[InventoryUI] 初始化完成");
    }
    
    void SetupEventListeners()
    {
        // 订阅背包变化事件
        if (InventoryManager.Instance)
        {
            InventoryManager.OnInventoryChanged -= OnInventoryChanged; // 防止重复订阅
            InventoryManager.OnInventoryChanged += OnInventoryChanged;
        }
    }
    
    void SetupButtons()
    {
        useButton?.onClick.AddListener(UseSelectedItem);
        dropButton?.onClick.AddListener(DropSelectedItem);
        closeButton?.onClick.AddListener(Hide);
    }
    
    void Update()
    {
        // 处理输入
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
        
        // ESC键关闭
        if (isVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }
    
    public void ToggleInventory()
    {
        if (isVisible)
            Hide();
        else
            Show();
    }
    
    void CreateInventorySlots()
    {
        if (slotsContainer == null || slotPrefab == null) 
        {
            Debug.LogError("[InventoryUI] SlotsContainer 或 SlotPrefab 未设置");
            return;
        }
        
        // 清理现有槽位
        foreach (Transform child in slotsContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        inventorySlots.Clear();
        
        int maxSlots = InventoryManager.Instance ? InventoryManager.Instance.maxSlots : 9;
        
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            
            if (slot == null)
                slot = slotObj.AddComponent<InventorySlot>();
            
            slot.Initialize(i, this);
            inventorySlots.Add(slot);
        }
        
        Debug.Log($"[InventoryUI] 创建了 {maxSlots} 个背包槽位");
    }
    
    void OnInventoryChanged(List<InventoryItem> items)
    {
        // 更新所有槽位
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < items.Count)
                inventorySlots[i].SetItem(items[i]);
            else
                inventorySlots[i].ClearSlot();
        }
        
        // 更新统计信息
        UpdateStatistics(items);
    }
    
    void UpdateStatistics(List<InventoryItem> items)
    {
        if (slotsUsedText)
        {
            int usedSlots = items.Count;
            int totalSlots = inventorySlots.Count;
            slotsUsedText.text = $"槽位: {usedSlots}/{totalSlots}";
        }
        
        /*if (inventoryWeight)
        {
            float totalWeight = CalculateTotalWeight(items);
            inventoryWeight.text = $"重量: {totalWeight:F1} kg";
        }*/
    }
    
    /*float CalculateTotalWeight(List<InventoryItem> items)
    {
        float weight = 0f;
        foreach (var item in items)
        {
            if (item?.itemData != null)
            {
                weight += item.itemData.weight * item.quantity;
            }
        }
        return weight;
    }*/
    
    public void OnSlotClicked(InventoryItem item)
    {
        selectedItem = item;
        if (item != null)
            ShowItemInfo(item);
        else
            HideItemInfo();
    }
    
    void ShowItemInfo(InventoryItem item)
    {
        if (itemInfoPanel == null || item?.itemData == null) return;
        
        itemInfoPanel.SetActive(true);
        
        if (itemInfoIcon) itemInfoIcon.sprite = item.itemData.icon;
        if (itemInfoName) itemInfoName.text = $"{item.itemData.itemName} x{item.quantity}";
        if (itemInfoDescription) itemInfoDescription.text = item.itemData.description;
        
        // 按钮状态
        if (useButton) useButton.interactable = CanUseItem(item);
        if (dropButton) dropButton.interactable = true;
    }
    
    bool CanUseItem(InventoryItem item)
    {
        if (item?.itemData == null) return false;
        
        return item.itemData.itemType == ItemType.Food ||
               item.itemData.itemType == ItemType.Water ||
               item.itemData.itemType == ItemType.Medicine ||
               item.itemData.IsWeapon;
    }
    
    void UseSelectedItem()
    {
        if (selectedItem?.itemData != null && InventoryManager.Instance)
        {
            bool success = InventoryManager.Instance.UseItem(selectedItem.itemData.itemName);
            if (success)
            {
                if (UIManager.Instance)
                {
                    UIManager.Instance.ShowMessage($"使用了 {selectedItem.itemData.itemName}", 2f);
                }
                HideItemInfo();
            }
        }
    }
    
    void DropSelectedItem()
    {
        if (selectedItem?.itemData != null)
        {
            // 在玩家前方丢弃物品
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                Vector3 dropPos = player.transform.position + player.transform.forward * 1.5f + Vector3.up * 0.5f;
                DropItemAtPosition(selectedItem, dropPos);
                
                // 从背包移除
                if (InventoryManager.Instance)
                {
                    InventoryManager.Instance.RemoveItem(selectedItem.itemData.itemName, 1);
                }
                
                if (UIManager.Instance)
                {
                    UIManager.Instance.ShowMessage($"丢弃了 {selectedItem.itemData.itemName}", 2f);
                }
            }
            
            HideItemInfo();
        }
    }
    
    void DropItemAtPosition(InventoryItem item, Vector3 position)
    {
        // 创建地面物品
        GameObject droppedItem = new GameObject($"Dropped_{item.itemData.itemName}");
        droppedItem.transform.position = position;
        
        // 添加PickupItem组件
        PickupItem pickup = droppedItem.AddComponent<PickupItem>();
        pickup.SetItemData(item.itemData, 1);
        
        // 添加碰撞器
        SphereCollider collider = droppedItem.AddComponent<SphereCollider>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
        
        // 添加物理效果
        Rigidbody rb = droppedItem.AddComponent<Rigidbody>();
        Vector3 randomForce = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(1f, 3f),
            Random.Range(-2f, 2f)
        );
        rb.AddForce(randomForce, ForceMode.Impulse);
        
        // 添加视觉效果
        WorldItemDisplay display = droppedItem.AddComponent<WorldItemDisplay>();
        display.SetItemData(item.itemData);
        
        Debug.Log($"[InventoryUI] 丢弃了物品: {item.itemData.itemName} 在位置: {position}");
    }
    
    void HideItemInfo()
    {
        selectedItem = null;
        if (itemInfoPanel) itemInfoPanel.SetActive(false);
    }
    
    public override void Show()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[InventoryUI] Not initialized, calling Initialize()");
            Initialize();
        }
    
        base.Show();
    
        // 确保面板激活
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
    
        // 暂停游戏
        if (UIManager.Instance)
        {
            UIManager.Instance.PauseGame();
        }
    
        // 强制刷新背包数据
        if (InventoryManager.Instance)
        {
            var items = InventoryManager.Instance.GetItems();
            OnInventoryChanged(items);
            Debug.Log($"[InventoryUI] Refreshed inventory with {items.Count} items");
        }
    
        // 显示鼠标光标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    
        Debug.Log("[InventoryUI] 背包界面已显示");
    }

    
    public override void Hide()
    {
        base.Hide();
    
        // 隐藏物品信息
        HideItemInfo();
    
        // 恢复游戏
        if (UIManager.Instance)
        {
            UIManager.Instance.ResumeGame();
        }
    
        // 隐藏鼠标光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    
        Debug.Log("[InventoryUI] 背包界面已隐藏");
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (InventoryManager.Instance)
        {
            InventoryManager.OnInventoryChanged -= OnInventoryChanged;
        }
    }
}