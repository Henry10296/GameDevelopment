using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;

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
    ,
    HealthEvent,
    Discovery,
    Encounter
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
    
    
    [Header("任务扩展")]
    public List<RandomEvent> allQuests = new(); // 任务列表
    public static event Action<GameEvent> OnEventTriggered;
    
    protected override void Awake()
    {
        base.Awake();
        LoadQuests();
    }
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
     void LoadQuests()
    {
        // 从现有的configuredEvents中筛选出任务
        allQuests = configuredEvents.Where(e => e.isQuest).ToList();
    }
    
    public RandomEvent GetQuest(string questId)
    {
        return allQuests.FirstOrDefault(q => q.eventName == questId || q.name == questId);
    }
    
    public void OnQuestStarted(RandomEvent quest)
    {
        Debug.Log($"[Quest] Started: {quest.eventName}");
        
        // 添加到日志
        JournalManager.Instance?.AddEntry($"任务开始: {quest.eventName}", 
            quest.eventDescription, JournalEntryType.Important);
    }
    
    public void OnQuestCompleted(RandomEvent quest)
    {
        Debug.Log($"[Quest] Completed: {quest.eventName}");
        
        // 给予奖励（复用现有的EventEffect系统）
        if (quest.automaticEffects != null)
        {
            foreach (var effect in quest.automaticEffects)
            {
                effect.Execute();
            }
        }
        
        // 添加到日志
        JournalManager.Instance?.AddEntry($"任务完成: {quest.eventName}", 
            "任务奖励已发放", JournalEntryType.Success);
    }
    
    // 更新任务进度的全局方法
    public static void UpdateQuestProgress(string objectiveType, string targetId, int amount = 1)
    {
        if (Instance == null) return;
        
        foreach (var quest in Instance.allQuests)
        {
            if (quest.questStatus == QuestStatus.InProgress && quest.questObjectives != null)
            {
                foreach (var objective in quest.questObjectives)
                {
                    if (ShouldUpdateObjective(objective, objectiveType, targetId))
                    {
                        quest.UpdateQuestProgress(objective.objectiveId, amount);
                    }
                }
            }
        }
    }
    
    static bool ShouldUpdateObjective(QuestObjective objective, string type, string targetId)
    {
        return type.ToLower() switch
        {
            "collect" => objective.type == QuestObjectiveType.CollectItem && objective.targetId == targetId,
            "kill" => objective.type == QuestObjectiveType.KillEnemies && objective.targetId == targetId,
            "explore" => objective.type == QuestObjectiveType.ExploreArea && objective.targetId == targetId,
            _ => false
        };
    }
    protected override void OnDestroy()
    {
        base.OnDestroy(); // 调用 Singleton 的 OnDestroy
    }
}


[System.Serializable]
public class EventNode
{
    public Vector2 position = Vector2.zero;
    public Vector2 size = new Vector2(180, 120);
    public bool isExpanded = true;
    
    // 编辑器专用
    public bool isSelected = false;
    public bool isDragging = false;
}

[System.Serializable]
public class EventConnection
{
    public RandomEvent targetEvent;
    public ConnectionType connectionType;
    public float weight = 1.0f; // 连接权重
    public string condition = ""; // 连接条件
}

public enum ConnectionType
{
    Sequence,      // 顺序执行
    Alternative,   // 二选一
    Parallel,      // 并行
    Conditional,   // 条件触发
    Random         // 随机选择
}
// 任务目标数据结构（复用现有EventEffect结构）
[System.Serializable]
public class QuestObjective
{
    public string objectiveId;
    public string description;
    public QuestObjectiveType type;
    public int targetAmount = 1;
    public string targetId; // 物品名、敌人类型等
    public bool isCompleted = false;
}

public enum QuestObjectiveType
{
    CollectItem,    // 收集物品
    KillEnemies,    // 击杀敌人  
    ExploreArea,    // 探索区域
    SurviveDays,    // 生存天数
    Custom          // 自定义
}

public enum QuestStatus
{
    NotStarted,
    Available, 
    InProgress,
    Completed,
    Failed
}