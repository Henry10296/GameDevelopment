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
    public Button equipButton; // 改为装备按钮（仅限武器）
    public Button dropButton;
    public Button closeButton;
    
    [Header("=== 统计信息 ===")]
    public TextMeshProUGUI slotsUsedText;
    public TextMeshProUGUI inventoryWeight;
    
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
        
        // 强制初始化时刷新一次
        RefreshInventory();
    
        Debug.Log("[InventoryUI] 初始化完成");
    }
    
    void SetupEventListeners()
    {
        // 订阅背包变化事件
        if (InventoryManager.Instance)
        {
            InventoryManager.OnInventoryChanged -= OnInventoryChanged;
            InventoryManager.OnInventoryChanged += OnInventoryChanged;
        }
    }
    
    void SetupButtons()
    {
        equipButton?.onClick.AddListener(EquipSelectedItem); // 改为装备
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
        Debug.Log($"[InventoryUI] OnInventoryChanged called with {items.Count} items");
        
        // 更新所有槽位
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < items.Count && items[i] != null)
            {
                inventorySlots[i].SetItem(items[i]);
                Debug.Log($"[InventoryUI] Slot {i}: {items[i].itemData?.itemName} x{items[i].quantity}");
            }
            else
            {
                inventorySlots[i].ClearSlot();
            }
        }
        
        // 更新统计信息
        UpdateStatistics(items);
    }
    
    // 新增：手动刷新背包方法
    public void RefreshInventory()
    {
        if (InventoryManager.Instance)
        {
            var items = InventoryManager.Instance.GetItems();
            OnInventoryChanged(items);
            Debug.Log($"[InventoryUI] Manual refresh: {items.Count} items");
        }
    }
    
    void UpdateStatistics(List<InventoryItem> items)
    {
        if (slotsUsedText)
        {
            int usedSlots = 0;
            foreach (var item in items)
            {
                if (item != null && item.itemData != null) usedSlots++;
            }
            int totalSlots = inventorySlots.Count;
            slotsUsedText.text = $"槽位: {usedSlots}/{totalSlots}";
        }
    }
    
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
        
        // 按钮状态 - 只有武器可以装备
        bool canEquip = IsWeapon(item);
        if (equipButton) 
        {
            equipButton.interactable = canEquip;
            equipButton.GetComponentInChildren<TextMeshProUGUI>().text = canEquip ? "装备" : "不可装备";
        }
        if (dropButton) dropButton.interactable = true;
    }
    
    bool IsWeapon(InventoryItem item)
    {
        if (item?.itemData == null) return false;
        return item.itemData.itemType == ItemType.Weapon || item.itemData.IsWeapon;
    }
    
    void EquipSelectedItem() // 新的装备方法
    {
        if (selectedItem?.itemData != null && IsWeapon(selectedItem))
        {
            // 获取武器管理器
            WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
            if (weaponManager == null)
            {
                ShowMessage("未找到武器管理器！");
                return;
            }
            
            // 尝试装备武器
            bool equipped = TryEquipWeapon(weaponManager, selectedItem);
            
            if (equipped)
            {
                // 从背包移除武器
                if (InventoryManager.Instance)
                {
                    InventoryManager.Instance.RemoveItem(selectedItem.itemData.itemName, 1);
                }
                
                ShowMessage($"装备了 {selectedItem.itemData.itemName}");
                HideItemInfo();
            }
            else
            {
                ShowMessage("无法装备该武器！");
            }
        }
    }
    
    bool TryEquipWeapon(WeaponManager weaponManager, InventoryItem weaponItem)
    {
        // 根据武器名称查找对应的WeaponData
        WeaponData weaponData = FindWeaponData(weaponItem.itemData.itemName);
        if (weaponData == null)
        {
            Debug.LogError($"未找到武器数据: {weaponItem.itemData.itemName}");
            return false;
        }
        
        // 检查武器类型
        WeaponType weaponType = GetWeaponTypeFromName(weaponData.weaponName);
        
        // 检查是否已有相同类型武器
        if (weaponManager.HasWeaponType(weaponType))
        {
            ShowMessage("已装备相同类型武器！");
            return false;
        }
        
        // 在weaponHolder下创建武器
        GameObject weaponObj = new GameObject(weaponData.weaponName);
        weaponObj.transform.SetParent(weaponManager.weaponHolder);
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;
        
        // 添加对应的武器控制器
        WeaponController controller = AddWeaponController(weaponObj, weaponType);
        if (controller != null)
        {
            controller.weaponData = weaponData;
            controller.currentAmmo = weaponData.maxAmmo; // 满弹药
            controller.weaponType = weaponType;
            
            // 添加到武器管理器
            weaponManager.AddWeapon(controller);
            
            Debug.Log($"成功装备武器: {weaponData.weaponName}");
            return true;
        }
        
        return false;
    }
    
    WeaponData FindWeaponData(string weaponName)
    {
        // 从GameConfig查找武器数据
        if (GameManager.Instance?.gameConfig?.WeaponConfig != null)
        {
            var weaponConfig = GameManager.Instance.gameConfig.WeaponConfig;
            
            if (weaponConfig.pistol != null && weaponConfig.pistol.weaponName == weaponName)
                return weaponConfig.pistol;
                
            if (weaponConfig.rifle != null && weaponConfig.rifle.weaponName == weaponName)
                return weaponConfig.rifle;
        }
        
        // 从ItemDatabase查找
        var itemDatabase = FindObjectOfType<ItemDatabase>();
        if (itemDatabase != null)
        {
            return itemDatabase.GetWeapon(weaponName);
        }
        
        return null;
    }
    
    WeaponType GetWeaponTypeFromName(string weaponName)
    {
        string name = weaponName.ToLower();
        
        if (name.Contains("pistol") || name.Contains("手枪"))
            return WeaponType.Pistol;
        if (name.Contains("rifle") || name.Contains("步枪") || name.Contains("自动"))
            return WeaponType.Rifle;
        if (name.Contains("knife") || name.Contains("刀"))
            return WeaponType.Knife;
            
        return WeaponType.Pistol; // 默认
    }
    
    WeaponController AddWeaponController(GameObject weaponObj, WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Pistol => weaponObj.AddComponent<PistolController>(),
            WeaponType.Rifle => weaponObj.AddComponent<AutoRifleController>(),
            _ => weaponObj.AddComponent<WeaponController>()
        };
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
                
                ShowMessage($"丢弃了 {selectedItem.itemData.itemName}");
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
    
    void ShowMessage(string message)
    {
        if (UIManager.Instance)
            UIManager.Instance.ShowMessage(message, 2f);
        else
            Debug.Log($"[InventoryUI] {message}");
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
        RefreshInventory();
    
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