using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancedInventoryUI : UIPanel
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
    
    [Header("=== 统计信息 ===")]
    public TextMeshProUGUI slotsUsedText; // "8/9"
    
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private InventoryItem selectedItem;
    
    void Start()
    {
        Initialize();
    }
    
    public void Initialize()
    {
        CreateInventorySlots();
        
        // 订阅背包变化事件
        if (InventoryManager.Instance)
        {
            InventoryManager.OnInventoryChanged += OnInventoryChanged;
        }
        
        // 按钮事件
        useButton?.onClick.AddListener(UseSelectedItem);
        dropButton?.onClick.AddListener(DropSelectedItem);
        
        // 初始隐藏
        inventoryPanel?.SetActive(false);
        itemInfoPanel?.SetActive(false);
    }
    
    void CreateInventorySlots()
    {
        if (slotsContainer == null || slotPrefab == null) return;
        
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
        
        // 更新统计
        if (slotsUsedText)
        {
            int usedSlots = items.Count;
            int totalSlots = inventorySlots.Count;
            slotsUsedText.text = $"{usedSlots}/{totalSlots}";
        }
    }
    
    public void OnSlotClicked(InventoryItem item)
    {
        selectedItem = item;
        ShowItemInfo(item);
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
        return item.itemData.itemType == ItemType.Food ||
               item.itemData.itemType == ItemType.Water ||
               item.itemData.itemType == ItemType.Medicine;
    }
    
    void UseSelectedItem()
    {
        if (selectedItem?.itemData != null && InventoryManager.Instance)
        {
            InventoryManager.Instance.UseItem(selectedItem.itemData.itemName);
            HideItemInfo();
        }
    }
    
    void DropSelectedItem()
    {
        if (selectedItem?.itemData != null)
        {
            // 在玩家前方丢弃物品
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                Vector3 dropPos = player.transform.position + player.transform.forward * 1.5f;
                DropItemAtPosition(selectedItem, dropPos);
                
                // 从背包移除
                if (InventoryManager.Instance)
                {
                    InventoryManager.Instance.RemoveItem(selectedItem.itemData.itemName, 1);
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
        
        PickupItem pickup = droppedItem.AddComponent<PickupItem>();
        pickup.SetItemData(item.itemData, 1);
        
        // 添加物理效果
        Rigidbody rb = droppedItem.AddComponent<Rigidbody>();
        rb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        
        Debug.Log($"丢弃了物品: {item.itemData.itemName}");
    }
    
    void HideItemInfo()
    {
        selectedItem = null;
        if (itemInfoPanel) itemInfoPanel.SetActive(false);
    }
    
    public void Show()
    {
        inventoryPanel?.SetActive(true);
        Time.timeScale = 0f; // 暂停游戏
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void Hide()
    {
        inventoryPanel?.SetActive(false);
        HideItemInfo();
        Time.timeScale = 1f; // 恢复游戏
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}