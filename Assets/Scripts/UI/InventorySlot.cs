using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI组件")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public Image background;
    
    [Header("颜色设置")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    
    private InventoryItem currentItem;
    private bool hasItem = false;
    [Header("自动更新")] 
    public int slotIndex = -1; // 槽位索引
    
    private void Start()
    {
        // 订阅背包变化事件
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged += OnInventoryChanged;
        }
    }
    
    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged -= OnInventoryChanged;
        }
    }
    
    private void OnInventoryChanged(List<InventoryItem> items)
    {
        if (slotIndex >= 0 && slotIndex < items.Count)
        {
            SetItem(items[slotIndex]);
        }
        else
        {
            ClearSlot();
        }
    }
    public void SetItem(InventoryItem item)
    {
        if (item == null || item.itemData == null)
        {
            ClearSlot();
            return;
        }
        
        currentItem = item;
        hasItem = true;
        
        // 设置图标
        if (itemIcon)
        {
            itemIcon.sprite = item.itemData.icon;
            itemIcon.color = Color.white;
        }
        
        // 设置数量文本
        if (quantityText)
        {
            if (item.quantity > 1)
            {
                quantityText.text = item.quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        
        // 设置背景颜色（根据物品类型）
        if (background)
        {
            background.color = GetItemTypeColor(item.itemData.itemType);
        }
    }
    
    public void ClearSlot()
    {
        currentItem = null;
        hasItem = false;
        
        if (itemIcon)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
        }
        
        if (quantityText)
            quantityText.gameObject.SetActive(false);
        
        if (background)
            background.color = normalColor;
    }
    
    Color GetItemTypeColor(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Food: return new Color(1f, 0.8f, 0.6f, 0.3f); // 橙色
            case ItemType.Water: return new Color(0.6f, 0.8f, 1f, 0.3f); // 蓝色
            case ItemType.Medicine: return new Color(0.8f, 1f, 0.6f, 0.3f); // 绿色
            case ItemType.Weapon: return new Color(1f, 0.6f, 0.6f, 0.3f); // 红色
            case ItemType.Ammo: return new Color(1f, 1f, 0.6f, 0.3f); // 黄色
            default: return normalColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!hasItem) return;
        
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 右键使用物品
            UseItem();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 左键显示物品信息
            ShowItemInfo();
        }
    }
    
    void UseItem()
    {
        if (currentItem == null) return;
        
        if (InventoryManager.Instance)
        {
            InventoryManager.Instance.UseItem(currentItem.itemData.itemName);
        }
    }
    
    void ShowItemInfo()
    {
        if (currentItem == null) return;
        
        // 显示物品信息面板
        ItemInfoPanel.Instance?.ShowItemInfo(currentItem.itemData);
    }
}