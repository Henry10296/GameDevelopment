using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI组件")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public Image background;
    public Image highlight; // 高亮效果
    public Button slotButton;
    
    [Header("颜色设置")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color highlightColor = new Color(1f, 1f, 1f, 0.1f);
    public Color selectedColor = new Color(0.8f, 0.8f, 0f, 0.3f);
    
    private InventoryItem currentItem;
    private int slotIndex;
    private InventoryUI inventoryUI;
    private bool isSelected = false;
    
    public void Initialize(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;
        
        if (slotButton) 
            slotButton.onClick.AddListener(OnSlotClick);
        
        if (highlight) 
            highlight.gameObject.SetActive(false);
        
        ClearSlot();
    }
    
    public void SetItem(InventoryItem item)
    {
        currentItem = item;
        
        if (item?.itemData != null)
        {
            if (itemIcon)
            {
                itemIcon.sprite = item.itemData.icon;
                itemIcon.color = Color.white;
                itemIcon.gameObject.SetActive(true);
            }
            
            if (quantityText)
            {
                bool showQuantity = item.quantity > 1;
                quantityText.text = showQuantity ? item.quantity.ToString() : "";
                quantityText.gameObject.SetActive(showQuantity);
            }
            
            if (background)
            {
                background.color = GetItemTypeColor(item.itemData.itemType);
            }
        }
        else
        {
            ClearSlot();
        }
    }
    
    public void ClearSlot()
    {
        currentItem = null;
        
        if (itemIcon)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
            itemIcon.gameObject.SetActive(false);
        }
        
        if (quantityText) 
            quantityText.gameObject.SetActive(false);
        
        if (background) 
            background.color = normalColor;
        
        SetSelected(false);
    }
    
    Color GetItemTypeColor(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Food => new Color(1f, 0.8f, 0.4f, 0.3f),      // 橙色
            ItemType.Water => new Color(0.4f, 0.8f, 1f, 0.3f),     // 蓝色
            ItemType.Medicine => new Color(0.8f, 1f, 0.4f, 0.3f),  // 绿色
            ItemType.Weapon => new Color(1f, 0.4f, 0.4f, 0.3f),    // 红色
            ItemType.Ammo => new Color(1f, 1f, 0.4f, 0.3f),        // 黄色
            ItemType.Key => new Color(1f, 0.4f, 1f, 0.3f),         // 紫色
            ItemType.Material => new Color(0.6f, 0.4f, 0.2f, 0.3f), // 棕色
            _ => new Color(0.8f, 0.8f, 0.8f, 0.3f)                 // 灰色
        };
    }
    
    void OnSlotClick()
    {
        if (inventoryUI != null)
        {
            inventoryUI.OnSlotClicked(currentItem);
            SetSelected(currentItem != null);
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (highlight)
        {
            highlight.gameObject.SetActive(selected);
            highlight.color = selected ? selectedColor : highlightColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClick();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected && highlight)
        {
            highlight.gameObject.SetActive(true);
            highlight.color = highlightColor;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected && highlight)
        {
            highlight.gameObject.SetActive(false);
        }
    }
    
    public InventoryItem GetItem() => currentItem;
    public bool HasItem() => currentItem != null;
    public int GetSlotIndex() => slotIndex;
}
