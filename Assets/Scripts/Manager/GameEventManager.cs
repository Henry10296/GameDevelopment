using UnityEngine;
using System.Collections.Generic;
using System;
[CreateAssetMenu(fileName = "RandomEvent", menuName = "Game/Random Event")]
public class RandomEvent : ScriptableObject
{
    [Header("事件基础信息")]
    public string eventName;
    [TextArea(3, 5)] public string eventDescription;
    public Sprite eventIcon;
    public EventPriority priority = EventPriority.Normal;
    
    [Header("触发条件")]
    public int minDay = 1;
    public int maxDay = 5;
    [Range(0f, 1f)] public float baseTriggerChance = 0.3f;
    public EventCondition[] triggerConditions;
    
    [Header("事件类型")]
    public EventType eventType;
    public bool requiresChoice = true;
    public bool canRepeat = false;
    
    [Header("选择项")]
    public EventChoice[] choices;
    
    [Header("自动效果（无选择时）")]
    public EventEffect[] automaticEffects;
    
    [Header("后续事件")]
    public RandomEvent followupEvent;
    public float followupDelay = 1f; // 天数延迟
    
    public bool CanTrigger()
    {
        int currentDay = GameManager.Instance.CurrentDay;
        
        // 检查日期范围
        if (currentDay < minDay || currentDay > maxDay)
            return false;
        
        // 检查触发条件
        foreach (var condition in triggerConditions)
        {
            if (!condition.IsMet())
                return false;
        }
        
        return true;
    }
    
    public float CalculateActualTriggerChance()
    {
        float chance = baseTriggerChance;
        
        // 根据优先级调整
        chance *= priority switch
        {
            EventPriority.Critical => 2.0f,
            EventPriority.High => 1.5f,
            EventPriority.Normal => 1.0f,
            EventPriority.Low => 0.5f,
            _ => 1.0f
        };
        
        return Mathf.Clamp01(chance);
    }
}
[CreateAssetMenu(fileName = "EventDayGroup", menuName = "Game/Random Event Group")]


public class EventGroup : ScriptableObject
{
    public int day;
    public List<RandomEvent> events;
}


public enum EventType
{
    ResourceGain,    // 资源获得
    ResourceLoss,    // 资源损失  
    FamilyHealth,    // 家人健康相关
    StoryProgression, // 剧情推进
    RadioReminder,   // 无线电提醒
    NeighborRequest, // 邻居请求
    MilitaryDrop,    // 军方投放
    RandomMisfortune // 随机灾难
}

public enum EventPriority
{
    Critical,  // 关键事件，必须触发
    High,      // 高优先级
    Normal,    // 普通事件
    Low        // 低优先级，填充用
}

[System.Serializable]
public class EventChoice  
{
    [Header("选择信息")]
    public string choiceText;
    [TextArea(2, 4)] public string resultDescription;
    
    [Header("需求条件")]
    public ResourceRequirement[] requirements;
    
    [Header("选择效果")]
    public EventEffect[] effects;
    
    [Header("UI设置")]
    public Color buttonColor = Color.white;
    public bool isRecommended = false;
    
    public bool CanChoose()
    {
        foreach (var requirement in requirements)
        {
            if (!requirement.IsMet())
                return false;
        }
        return true;
    }
    
    public string GetRequirementText()
    {
        if (requirements.Length == 0) return "";
        
        string text = "需要: ";
        for (int i = 0; i < requirements.Length; i++)
        {
            text += requirements[i].GetDisplayText();
            if (i < requirements.Length - 1) text += ", ";
        }
        return text;
    }
}

[System.Serializable]
public class ResourceRequirement
{
    public string resourceType; // "food", "water", "medicine"
    public int amount;
    
    public bool IsMet()
    {
        if (!FamilyManager.Instance) return false;
        
        return resourceType.ToLower() switch
        {
            "food" => FamilyManager.Instance.Food >= amount,
            "water" => FamilyManager.Instance.Water >= amount,
            "medicine" => FamilyManager.Instance.Medicine >= amount,
            _ => true
        };
    }
    
    public string GetDisplayText()
    {
        string resourceName = resourceType.ToLower() switch
        {
            "food" => "食物",
            "water" => "水",
            "medicine" => "药品",
            _ => resourceType
        };
        
        return $"{resourceName} x{amount}";
    }
}

[System.Serializable]
public class EventEffect
{
    [Header("效果类型")]
    public EffectType type;
    
    [Header("资源效果")]
    public string resourceType;
    public int resourceAmount;
    
    [Header("健康效果")]
    public bool affectAllFamily;
    public int healthChange;
    public bool cureIllness;
    public bool causeIllness;
    
    [Header("其他效果")]
    public string customMessage;
    public bool unlockMap;
    public string mapToUnlock;
    
    public void Execute()
    {
        switch (type)
        {
            case EffectType.ModifyResource:
                ExecuteResourceEffect();
                break;
                
            case EffectType.ModifyHealth:
                ExecuteHealthEffect();
                break;
                
            case EffectType.AddJournalEntry:
                ExecuteJournalEffect();
                break;
                
            case EffectType.UnlockContent:
                ExecuteUnlockEffect();
                break;
        }
    }
    
    void ExecuteResourceEffect()
    {
        if (FamilyManager.Instance)
            FamilyManager.Instance.AddResource(resourceType, resourceAmount);
    }
    
    void ExecuteHealthEffect()
    {
        if (!FamilyManager.Instance) return;
        
        if (affectAllFamily)
        {
            foreach (var member in FamilyManager.Instance.FamilyMembers)
            {
                if (healthChange != 0)
                    member.Heal(healthChange);
                
                if (cureIllness)
                    member.CureSickness();
                
                if (causeIllness)
                {
                    member.isSick = true;
                    member.sickDaysLeft = 3;
                }
            }
        }
    }
    
    void ExecuteJournalEffect()
    {
        if (JournalManager.Instance && !string.IsNullOrEmpty(customMessage))
            JournalManager.Instance.AddEntry("事件结果", customMessage);
    }
    
    void ExecuteUnlockEffect()
    {
        if (unlockMap && MapManager.Instance)
        {
            MapManager.Instance.UnlockMap(mapToUnlock);
        }
    }
}

public enum EffectType
{
    ModifyResource,   // 修改资源
    ModifyHealth,     // 修改健康状态
    AddJournalEntry,  // 添加日志条目
    UnlockContent,    // 解锁内容
    TriggerEvent      // 触发其他事件
}

[System.Serializable]
public class EventCondition
{
    public ConditionType type;
    public string stringValue;
    public int intValue;
    public bool boolValue;
    
    public bool IsMet()
    {
        return type switch
        {
            ConditionType.HasItem => InventoryManager.Instance?.HasItem(stringValue, intValue) ?? false,
            ConditionType.ResourceMinimum => CheckResourceMinimum(),
            ConditionType.FamilyHealthBelow => CheckFamilyHealth(),
            ConditionType.RadioFound => RadioManager.Instance?.hasRadio ?? false,
            ConditionType.DayExact => GameManager.Instance.CurrentDay == intValue,
            ConditionType.Custom => boolValue,
            _ => true
        };
    }
    
    bool CheckResourceMinimum()
    {
        if (!FamilyManager.Instance) return false;
        
        return stringValue.ToLower() switch
        {
            "food" => FamilyManager.Instance.Food >= intValue,
            "water" => FamilyManager.Instance.Water >= intValue,
            "medicine" => FamilyManager.Instance.Medicine >= intValue,
            _ => true
        };
    }
    
    bool CheckFamilyHealth()
    {
        if (!FamilyManager.Instance) return false;
        
        foreach (var member in FamilyManager.Instance.FamilyMembers)
        {
            if (member.health < intValue)
                return true;
        }
        return false;
    }
}

public enum ConditionType
{
    HasItem,           // 拥有指定物品
    ResourceMinimum,   // 资源最低要求
    FamilyHealthBelow, // 家人健康低于
    RadioFound,        // 找到无线电
    DayExact,          // 确切日期
    Custom             // 自定义条件
}


public enum GameEventType
{
    FamilyIllness,     // 家人生病
    ResourceLoss,      // 资源丢失
    ResourceGain,      // 资源获得
    RadioBroadcast,    // 电台广播
    WeatherChange,     // 天气变化
    EnemyIncrease,     // 敌人增加
    SpecialEncounter   // 特殊遭遇
}

public class GameEventManager : Singleton<GameEventManager>
{
    
    [Header("事件配置")]
    public List<RandomEvent> configuredEvents;
    
    [Header("UI")]
    public GameObject eventPopupPanel;
    public TMPro.TextMeshProUGUI eventTitleText;
    public TMPro.TextMeshProUGUI eventDescriptionText;
    public UnityEngine.UI.Button eventConfirmButton;
    
    public static event Action<GameEvent> OnEventTriggered;
    
    void Start()
    {
        foreach (var evt in configuredEvents)
        {
            if (evt.CanTrigger())
            {
                float chance = evt.CalculateActualTriggerChance();
                if (!evt.requiresChoice || UnityEngine.Random.value < chance)
                {
                    TriggerEvent(evt);
                }
            }
        }
    }
    
    void InitializeEvents()
    {
        Debug.Log($"[GameEventManager] 已加载配置事件数量: {configuredEvents.Count}");
    }
    
    void OnDayChanged(int newDay)
    {
        CheckDayEvents(newDay);
    }
    
    void CheckDayEvents(int day)
    {
        foreach (var evt in configuredEvents)
        {
            if (!evt.CanTrigger()) continue;
            if (day < evt.minDay || day > evt.maxDay) continue;

            float chance = evt.CalculateActualTriggerChance();
            if (!evt.requiresChoice || UnityEngine.Random.value < chance)
            {
                TriggerEvent(evt);  // 用 RandomEvent 替代原 GameEvent
            }
        }
    }
    
    void TriggerEvent(RandomEvent evt)
    {
        Debug.Log($"[事件] 触发：{evt.eventName}");

        if (evt.requiresChoice && evt.choices != null && evt.choices.Length > 0)
        {
            ShowEventPopup(evt);  // 弹出 UI，保留函数名
        }
        else
        {
            foreach (var effect in evt.automaticEffects)
            {
                effect.Execute();
            }

            ShowEventPopup(evt);  // 也可以用于展示无选项的简短描述
        }

        // 支持后续事件链
        if (evt.followupEvent != null)
        {
            GameManager.Instance.ScheduleEvent(evt.followupEvent, evt.followupDelay);
        }
    }
    public void TriggerEventExternally(RandomEvent evt)
    {
        TriggerEvent(evt); // 调用内部方法
    }

    /*void ApplyEventEffects(GameEvent gameEvent)
    {
        switch (gameEvent.eventType)
        {
            case GameEventType.ResourceLoss:
            case GameEventType.ResourceGain:
                if (FamilyManager.Instance)
                {
                    FamilyManager.Instance.AddResource(gameEvent.resourceType, gameEvent.resourceChange);
                }
                break;
                
            case GameEventType.FamilyIllness:
                // 修复：使用公共属性访问器
                if (FamilyManager.Instance && FamilyManager.Instance.FamilyMembers.Count > 0)
                {
                    int randomMember = UnityEngine.Random.Range(0, FamilyManager.Instance.FamilyMembers.Count);
                    FamilyManager.Instance.FamilyMembers[randomMember].isSick = true;
                    FamilyManager.Instance.FamilyMembers[randomMember].sickDaysLeft = 3;
                }
                break;
                
            case GameEventType.RadioBroadcast:
                // 提醒玩家无线电的重要性
                if (UIManager.Instance)
                {
                    UIManager.Instance.ShowInteractionPrompt("检查公园的无线电设备！");
                }
                break;
        }
    }*/
    
    void ShowEventPopup(RandomEvent evt)
    {
        if (eventPopupPanel)
        {
            eventPopupPanel.SetActive(true);
            if (eventTitleText) eventTitleText.text = evt.eventName;
            if (eventDescriptionText) eventDescriptionText.text = evt.eventDescription;
        }

        if (eventConfirmButton)
        {
            eventConfirmButton.onClick.RemoveAllListeners();
            eventConfirmButton.onClick.AddListener(CloseEventPopup);
        }
    }
    
    void CloseEventPopup()
    {
        if (eventPopupPanel)
            eventPopupPanel.SetActive(false);
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy(); // 调用 Singleton 的 OnDestroy
    }
}