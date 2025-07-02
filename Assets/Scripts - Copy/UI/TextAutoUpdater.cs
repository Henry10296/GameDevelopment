using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextAutoUpdater : MonoBehaviour
{
    [Header("更新设置")]
    public TextMeshProUGUI targetText;
    public string textFormat = "{0}"; // 如 "食物: {0}"
    public ManagerType managerType;
    public string propertyName;
    
    [Header("事件绑定")]
    public GameEvent updateTrigger; // 绑定到对应管理器的事件
    
    public enum ManagerType
    {
        FamilyManager,
        GameManager,
        InventoryManager
    }
    
    private void Start()
    {
        if (targetText == null)
            targetText = GetComponent<TextMeshProUGUI>();
        
        // 绑定事件
        if (updateTrigger != null)
        {
            var listener = gameObject.AddComponent<GameEventListener>();
            listener.gameEvent = updateTrigger;
            listener.response.AddListener(UpdateText);
        }
        
        // 初始更新
        UpdateText();
    }
    
    public void UpdateText()
    {
        if (targetText == null) return;
        
        object value = GetValue();
        if (value != null)
        {
            targetText.text = string.Format(textFormat, value);
        }
    }
    
    private object GetValue()
    {
        return managerType switch
        {
            ManagerType.FamilyManager when FamilyManager.Instance != null => GetFamilyValue(),
            ManagerType.GameManager when GameManager.Instance != null => GetGameValue(),
            ManagerType.InventoryManager when InventoryManager.Instance != null => GetInventoryValue(),
            _ => null
        };
    }
    
    private object GetFamilyValue()
    {
        return propertyName switch
        {
            "Food" => FamilyManager.Instance.Food,
            "Water" => FamilyManager.Instance.Water,
            "Medicine" => FamilyManager.Instance.Medicine,
            "AliveMembers" => FamilyManager.Instance.AliveMembers,
            _ => null
        };
    }
    
    private object GetGameValue()
    {
        return propertyName switch
        {
            "CurrentDay" => GameManager.Instance.CurrentDay,
            "PhaseTimer" => Mathf.CeilToInt(GameManager.Instance.PhaseTimer),
            _ => null
        };
    }
    
    private object GetInventoryValue()
    {
        return propertyName switch
        {
            "ItemCount" => InventoryManager.Instance.GetItems().Count,
            "MaxSlots" => InventoryManager.Instance.maxSlots,
            _ => null
        };
    }
}
