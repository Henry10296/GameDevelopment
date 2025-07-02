using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemInfoPanel : MonoBehaviour
{
    public static ItemInfoPanel Instance;
    
    [Header("UI组件")]
    public GameObject panel;
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemTypeText;
    public TextMeshProUGUI descriptionText;
    public Button closeButton;
    public Button useButton;
    public Button dropButton;
    
    private ItemData currentItemData;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (panel) panel.SetActive(false);
        
        // 设置按钮事件
        if (closeButton) closeButton.onClick.AddListener(HideItemInfo);
        if (useButton) useButton.onClick.AddListener(UseCurrentItem);
        if (dropButton) dropButton.onClick.AddListener(DropCurrentItem);
    }
    
    public void ShowItemInfo(ItemData itemData)
    {
        if (itemData == null) return;
        
        currentItemData = itemData;
        
        if (itemIcon) itemIcon.sprite = itemData.icon;
        if (itemNameText) itemNameText.text = itemData.itemName;
        if (itemTypeText) itemTypeText.text = GetItemTypeString(itemData.itemType);
        if (descriptionText) descriptionText.text = itemData.description;
        
        // 更新按钮状态
        UpdateButtonStates();
        
        if (panel) panel.SetActive(true);
    }
    
    public void HideItemInfo()
    {
        if (panel) panel.SetActive(false);
        currentItemData = null;
    }
    
    void UpdateButtonStates()
    {
        if (useButton)
        {
            bool canUse = currentItemData != null && 
                         (currentItemData.itemType == ItemType.Food || 
                          currentItemData.itemType == ItemType.Water || 
                          currentItemData.itemType == ItemType.Medicine);
            useButton.interactable = canUse;
        }
        
        if (dropButton)
        {
            dropButton.interactable = currentItemData != null;
        }
    }
    
    void UseCurrentItem()
    {
        if (currentItemData != null && InventoryManager.Instance)
        {
            InventoryManager.Instance.UseItem(currentItemData.itemName);
            HideItemInfo();
        }
    }
    
    void DropCurrentItem()
    {
        if (currentItemData != null && InventoryManager.Instance)
        {
            InventoryManager.Instance.RemoveItem(currentItemData.itemName, 1);
            // 这里可以在世界中生成掉落的物品
            HideItemInfo();
        }
    }
    
    string GetItemTypeString(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Food: return "食物";
            case ItemType.Water: return "水";
            case ItemType.Medicine: return "药品";
            case ItemType.Weapon: return "武器";
            case ItemType.Ammo: return "弹药";
            case ItemType.Key: return "钥匙";
            case ItemType.Material: return "材料";
            default: return "未知";
        }
    }
}