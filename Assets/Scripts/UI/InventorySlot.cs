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
    public Button slotButton;
    
    private InventoryItem currentItem;
    [SerializeField]private int slotIndex;
    private InventoryUI inventoryUI;
    
    public void Initialize(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;
        
        if (slotButton) slotButton.onClick.AddListener(OnSlotClick);
        
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
            }
            
            if (quantityText)
            {
                quantityText.text = item.quantity > 1 ? item.quantity.ToString() : "";
                quantityText.gameObject.SetActive(item.quantity > 1);
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
        }
        
        if (quantityText) quantityText.gameObject.SetActive(false);
        if (background) background.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
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
            _ => new Color(0.8f, 0.8f, 0.8f, 0.3f)                 // 灰色
        };
    }
    
    void OnSlotClick()
    {
        if (currentItem != null && inventoryUI != null)
        {
            inventoryUI.OnSlotClicked(currentItem);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}