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
        int currentDay = GameStateManager.Instance.CurrentDay;
        
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
            MapManager.Instance.UnlockMap(mapToUnlock);
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
            ConditionType.DayExact => GameStateManager.Instance.CurrentDay == intValue,
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
/*
[System.Serializable]
public partial class GameEvent
{
    public string eventName;
    public string eventDescription;
    public int dayToTrigger;
    public GameEventType eventType;
    public int resourceChange;
    public string resourceType;
    public bool isRandomEvent;
    public float triggerChance = 0.3f;
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

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance;
    
    [Header("事件配置")]
    public List<GameEvent> gameEvents;
    
    [Header("UI")]
    public GameObject eventPopupPanel;
    public TMPro.TextMeshProUGUI eventTitleText;
    public TMPro.TextMeshProUGUI eventDescriptionText;
    public UnityEngine.UI.Button eventConfirmButton;
    
    public static event Action<GameEvent> OnEventTriggered;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeEvents();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 订阅天数变化事件
        GameEvents.OnDayChanged += OnDayChanged;
        
        if (eventConfirmButton)
            eventConfirmButton.onClick.AddListener(CloseEventPopup);
    }
    
    void InitializeEvents()
    {
        gameEvents = new List<GameEvent>
        {
            // 第1天事件
            new GameEvent
            {
                eventName = "邻居求助",
                eventDescription = "邻居家的孩子生病了，他们请求你分享一些药品。",
                dayToTrigger = 1,
                eventType = GameEventType.ResourceLoss,
                resourceChange = -1,
                resourceType = "medicine",
                isRandomEvent = true,
                triggerChance = 0.4f
            },
            
            // 第2天事件
            new GameEvent
            {
                eventName = "军用物资投放",
                eventDescription = "军方在附近投放了救援物资！",
                dayToTrigger = 2,
                eventType = GameEventType.ResourceGain,
                resourceChange = 5,
                resourceType = "food",
                isRandomEvent = true,
                triggerChance = 0.6f
            },
            
            // 第3天固定事件
            new GameEvent
            {
                eventName = "无线电信号",
                eventDescription = "今天是发送救援信号的关键日子！",
                dayToTrigger = 3,
                eventType = GameEventType.RadioBroadcast,
                isRandomEvent = false
            },
            
            // 第4天事件
            new GameEvent
            {
                eventName = "老鼠偷食",
                eventDescription = "老鼠闯入了你们的储藏室，偷走了一些食物。",
                dayToTrigger = 4,
                eventType = GameEventType.ResourceLoss,
                resourceChange = -2,
                resourceType = "food",
                isRandomEvent = true,
                triggerChance = 0.3f
            },
            
            // 第5天固定事件
            new GameEvent
            {
                eventName = "最后的信号",
                eventDescription = "这是发送救援信号的最后机会！",
                dayToTrigger = 5,
                eventType = GameEventType.RadioBroadcast,
                isRandomEvent = false
            }
        };
    }
    
    void OnDayChanged(int newDay)
    {
        CheckDayEvents(newDay);
    }
    
    void CheckDayEvents(int day)
    {
        foreach (var gameEvent in gameEvents)
        {
            if (gameEvent.dayToTrigger == day)
            {
                if (!gameEvent.isRandomEvent || UnityEngine.Random.Range(0f, 1f) < gameEvent.triggerChance)
                {
                    TriggerEvent(gameEvent);
                }
            }
        }
    }
    
    void TriggerEvent(GameEvent gameEvent)
    {
        Debug.Log($"触发事件: {gameEvent.eventName}");
        
        // 应用事件效果
        ApplyEventEffects(gameEvent);
        
        // 显示事件弹窗
        ShowEventPopup(gameEvent);
        
        OnEventTriggered?.Invoke(gameEvent);
    }
    
    void ApplyEventEffects(GameEvent gameEvent)
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
    }
    
    void ShowEventPopup(GameEvent gameEvent)
    {
        if (eventPopupPanel)
        {
            eventPopupPanel.SetActive(true);
            
            if (eventTitleText) eventTitleText.text = gameEvent.eventName;
            if (eventDescriptionText) eventDescriptionText.text = gameEvent.eventDescription;
        }
    }
    
    void CloseEventPopup()
    {
        if (eventPopupPanel)
            eventPopupPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        GameEvents.OnDayChanged -= OnDayChanged;
    }
}*/