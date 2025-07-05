using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeEventManager : Singleton<HomeEventManager>
{
    [Header("事件配置")]
    public RandomEvent[] availableEvents;
    public RandomEvent[] criticalEvents;  // 关键事件（无线电提醒等）
    
    [Header("UI")]
    public EventChoicePanel eventChoiceUI;
    
    [Header("事件")]
    public GameEvent onEventTriggered;
    public GameEvent onEventCompleted;
    
    
    private Queue<RandomEvent> scheduledEvents = new Queue<RandomEvent>();
    private List<RandomEvent> triggeredEvents = new List<RandomEvent>();
    private RandomEvent currentEvent;
    
    [Header("队列设置")]
    public int maxQueueSize = 5;
    private Queue<RandomEvent> eventQueue = new Queue<RandomEvent>();
    private bool _isProcessingEvent = false;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeEvents();
    }
    
    void Start()
    {
        SubscribeToEvents();
    }
    
    void SubscribeToEvents()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.onDayChanged.RegisterListener(
                GetComponent<IntGameEventListener>());
        }
    }
    
    void InitializeEvents()
    {
        // 预设关键事件
        ScheduleCriticalEvents();
    }
    
    void ScheduleCriticalEvents()
    {
        // 第3天和第5天的无线电提醒事件
        var radioReminderDay3 = CreateRadioReminderEvent(3);
        var radioReminderDay5 = CreateRadioReminderEvent(5);
        
        // 添加到关键事件列表
        var criticalEventsList = new List<RandomEvent>(criticalEvents)
        {
            radioReminderDay3,
            radioReminderDay5
        };
        
        criticalEvents = criticalEventsList.ToArray();
    }
    
    RandomEvent CreateRadioReminderEvent(int day)
    {
        var reminderEvent = ScriptableObject.CreateInstance<RandomEvent>();
        reminderEvent.eventName = "无线电信号";
        reminderEvent.eventDescription = $"今天是第{day}天，这是发送救援信号的关键时刻！如果你找到了无线电设备，现在就是使用它的时候。";
        reminderEvent.minDay = day;
        reminderEvent.maxDay = day;
        reminderEvent.baseTriggerChance = 1.0f; // 必定触发
        reminderEvent.priority = GameEventPriority.Critical;
        reminderEvent.requiresChoice = false;
        
        // 添加触发条件：必须拥有无线电
        reminderEvent.triggerConditions = new EventCondition[]
        {
            new EventCondition
            {
                type = ConditionType.RadioFound,
                boolValue = true
            }
        };
        
        return reminderEvent;
    }
    protected override void OnSingletonApplicationQuit()
    {
        // 停止事件处理
        StopAllCoroutines();
    
        // 清理事件队列
        scheduledEvents?.Clear();
        triggeredEvents?.Clear();
    
        // 清理事件
        onEventTriggered = null;
        onEventCompleted = null;
    
        Debug.Log("[HomeEventManager] Application quit cleanup completed");
    }
    public void ProcessDailyEvents()
    {
        int currentDay = GameManager.Instance.CurrentDay;
        
        // 处理关键事件
        ProcessCriticalEvents(currentDay);
        
        // 处理预定事件
        ProcessScheduledEvents();
        
        // 检查随机事件
        CheckRandomEvents(currentDay);
        
        Debug.Log($"[HomeEventManager] Processed events for day {currentDay}");
    }
    
    void ProcessCriticalEvents(int currentDay)
    {
        foreach (var criticalEvent in criticalEvents)
        {
            if (criticalEvent.minDay == currentDay && 
                criticalEvent.CanTrigger() && 
                !triggeredEvents.Contains(criticalEvent))
            {
                TriggerEvent(criticalEvent);
                break; // 每天只触发一个关键事件
            }
        }
    }
    
    void ProcessScheduledEvents()
    {
        while (scheduledEvents.Count > 0)
        {
            var scheduledEvent = scheduledEvents.Dequeue();
            TriggerEvent(scheduledEvent);
        }
    }
    
    void CheckRandomEvents(int currentDay)
    {
        // 如果已经有事件在处理，跳过随机事件
        if (currentEvent != null) return;
        
        List<RandomEvent> possibleEvents = new List<RandomEvent>();
        
        foreach (var randomEvent in availableEvents)
        {
            if (randomEvent.CanTrigger() && 
                (!triggeredEvents.Contains(randomEvent) || randomEvent.canRepeat))
            {
                possibleEvents.Add(randomEvent);
            }
        }
        
        if (possibleEvents.Count > 0)
        {
            // 按优先级排序
            possibleEvents.Sort((a, b) => b.priority.CompareTo(a.priority));
            
            // 检查是否触发事件
            foreach (var eventToCheck in possibleEvents)
            {
                float actualChance = eventToCheck.CalculateActualTriggerChance();
                if (Random.Range(0f, 1f) < actualChance)
                {
                    TriggerEvent(eventToCheck);
                    break; // 每天只触发一个随机事件
                }
            }
        }
    }
    
    public void TriggerEvent(RandomEvent eventData)
    {
        if (eventData == null) return;
        
        currentEvent = eventData;
        triggeredEvents.Add(eventData);
        
        onEventTriggered?.Raise();
        
        if (eventData.requiresChoice && eventData.choices.Length > 0)
        {
            // 显示选择界面
            if (UIManager.Instance)
            {
                UIManager.Instance.ShowEventChoice(eventData);
            }
        }
        else
        {
            // 自动执行效果
            ExecuteEventEffects(eventData.automaticEffects);
            CompleteEvent();
        }
        
        // 记录到日志
        JournalManager.Instance?.AddEntry(eventData.eventName, eventData.eventDescription);
        
        Debug.Log($"[HomeEventManager] Triggered event: {eventData.eventName}");
    }
    
    public void OnEventChoiceSelected(EventChoice choice)
    {
        if (currentEvent == null || choice == null) return;
        
        // 检查选择条件
        if (!choice.CanChoose())
        {
            UIManager.Instance?.ShowMessage("无法选择此选项：资源不足");
            return;
        }
        
        // 消耗资源
        foreach (var requirement in choice.requirements)
        {
            ConsumeResource(requirement);
        }
        
        // 执行选择效果
        ExecuteEventEffects(choice.effects);
        
        // 记录选择结果
        JournalManager.Instance?.AddEntry($"{currentEvent.eventName} - 结果", choice.resultDescription);
        
        CompleteEvent();
    }
    
    void ConsumeResource(ResourceRequirement requirement)
    {
        if (FamilyManager.Instance)
        {
            switch (requirement.resourceType.ToLower())
            {
                case "food":
                    FamilyManager.Instance.UseFood(requirement.amount);
                    break;
                case "water":
                    FamilyManager.Instance.UseWater(requirement.amount);
                    break;
                case "medicine":
                    FamilyManager.Instance.UseMedicine(requirement.amount);
                    break;
            }
        }
    }
    
    public void ExecuteEventEffects(EventEffect[] effects)
    {
        foreach (var effect in effects)
        {
            effect.Execute();
        }
    }
    
    void CompleteEvent()
    {
        if (currentEvent == null) return;
        
        // 检查是否有后续事件
        if (currentEvent.followupEvent != null)
        {
            ScheduleEvent(currentEvent.followupEvent, currentEvent.followupDelay);
        }
        
        onEventCompleted?.Raise();
        currentEvent = null;
        
        Debug.Log("[HomeEventManager] Event completed");
    }
    
    public void ScheduleEvent(RandomEvent eventToSchedule, float delayInDays)
    {
        // 简单实现：立即加入队列（可以改进为基于时间的调度）
        scheduledEvents.Enqueue(eventToSchedule);
    }
    
    // 创建预设事件的工厂方法
    public RandomEvent CreateNeighborHelpEvent()
    {
        var neighborEvent = ScriptableObject.CreateInstance<RandomEvent>();
        neighborEvent.eventName = "邻居求助";
        neighborEvent.eventDescription = "邻居家的孩子生病了，他们请求你分享一些药品。这可能会帮助建立社区关系。";
        neighborEvent.baseTriggerChance = 0.4f;
        neighborEvent.requiresChoice = true;
        
        neighborEvent.choices = new EventChoice[]
        {
            new EventChoice
            {
                choiceText = "分享药品",
                resultDescription = "你帮助了邻居，他们非常感激。社区关系得到改善。",
                requirements = new ResourceRequirement[]
                {
                    new ResourceRequirement { resourceType = "medicine", amount = 1 }
                },
                effects = new EventEffect[]
                {
                    new EventEffect
                    {
                        type = EffectType.AddJournalEntry,
                        customMessage = "帮助邻居让我们感到温暖，在这个冷酷的世界里，互助是珍贵的。"
                    }
                }
            },
            new EventChoice
            {
                choiceText = "拒绝帮助",
                resultDescription = "你选择保留药品给自己的家人。邻居理解但失望。",
                effects = new EventEffect[]
                {
                    new EventEffect
                    {
                        type = EffectType.AddJournalEntry,
                        customMessage = "我必须先照顾好自己的家人。这个决定让我感到内疚。"
                    }
                }
            }
        };
        
        return neighborEvent;
    }
    
    public RandomEvent CreateMilitaryDropEvent()
    {
        var militaryEvent = ScriptableObject.CreateInstance<RandomEvent>();
        militaryEvent.eventName = "军方物资投放";
        militaryEvent.eventDescription = "军方在附近投放了救援物资！你可以选择冒险去收集，但可能遇到其他幸存者。";
        militaryEvent.baseTriggerChance = 0.3f;
        militaryEvent.requiresChoice = true;
        
        militaryEvent.choices = new EventChoice[]
        {
            new EventChoice
            {
                choiceText = "立即前往收集",
                resultDescription = "你成功收集到了一些物资，但也遇到了其他竞争者。",
                effects = new EventEffect[]
                {
                    new EventEffect
                    {
                        type = EffectType.ModifyResource,
                        resourceType = "food",
                        resourceAmount = Random.Range(3, 6)
                    },
                    new EventEffect
                    {
                        type = EffectType.ModifyResource,
                        resourceType = "water",
                        resourceAmount = Random.Range(2, 4)
                    }
                }
            },
            new EventChoice
            {
                choiceText = "等待时机",
                resultDescription = "你决定等待更安全的时机，但其他人已经抢走了大部分物资。",
                effects = new EventEffect[]
                {
                    new EventEffect
                    {
                        type = EffectType.ModifyResource,
                        resourceType = "food",
                        resourceAmount = Random.Range(1, 3)
                    }
                }
            },
            new EventChoice
            {
                choiceText = "忽略物资投放",
                resultDescription = "你认为风险太大，选择不参与争夺。",
                effects = new EventEffect[]
                {
                    new EventEffect
                    {
                        type = EffectType.AddJournalEntry,
                        customMessage = "有时候最安全的选择就是什么都不做。"
                    }
                }
            }
        };
        
        return militaryEvent;
    }
    
    public RandomEvent CreateRatStealEvent()
    {
        var ratEvent = ScriptableObject.CreateInstance<RandomEvent>();
        ratEvent.eventName = "老鼠偷食";
        ratEvent.eventDescription = "老鼠闯入了你们的储藏室，偷走了一些食物。你需要想办法防止这种情况再次发生。";
        ratEvent.baseTriggerChance = 0.25f;
        ratEvent.requiresChoice = false;
        
        ratEvent.automaticEffects = new EventEffect[]
        {
            new EventEffect
            {
                type = EffectType.ModifyResource,
                resourceType = "food",
                resourceAmount = -Random.Range(1, 3)
            },
            new EventEffect
            {
                type = EffectType.AddJournalEntry,
                customMessage = "该死的老鼠！我们需要更好地保护食物储藏。"
            }
        };
        
        return ratEvent;
    }
    /*public void TriggerEvent(RandomEvent eventData)
    {
        if (eventData == null) return;
        
        // 如果正在处理事件，加入队列
        if (isProcessingEvent)
        {
            QueueEvent(eventData);
            return;
        }
        
        ProcessEventImmediate(eventData);
    }
    
    private void QueueEvent(RandomEvent eventData)
    {
        if (eventQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning("[HomeEventManager] Event queue full, skipping event: " + eventData.eventName);
            return;
        }
        
        eventQueue.Enqueue(eventData);
        Debug.Log($"[HomeEventManager] Queued event: {eventData.eventName}");
    }
    
    private void ProcessEventImmediate(RandomEvent eventData)
    {
        currentEvent = eventData;
        isProcessingEvent = true;
        triggeredEvents.Add(eventData);
        
        onEventTriggered?.Raise();
        
        if (eventData.requiresChoice && eventData.choices.Length > 0)
        {
            if (UIManager.Instance)
            {
                UIManager.Instance.ShowEventChoice(eventData);
            }
        }
        else
        {
            ExecuteEventEffects(eventData.automaticEffects);
            CompleteEvent();
        }
        
        JournalManager.Instance?.AddEntry(eventData.eventName, eventData.eventDescription);
        Debug.Log($"[HomeEventManager] Processing event: {eventData.eventName}");
    }
    
    void CompleteEvent()
    {
        if (currentEvent == null) return;
        
        if (currentEvent.followupEvent != null)
        {
            QueueEvent(currentEvent.followupEvent);
        }
        
        onEventCompleted?.Raise();
        currentEvent = null;
        isProcessingEvent = false;
        
        // 处理队列中的下一个事件
        ProcessNextQueuedEvent();
        
        Debug.Log("[HomeEventManager] Event completed");
    }
    
    private void ProcessNextQueuedEvent()
    {
        if (eventQueue.Count > 0 && !isProcessingEvent)
        {
            var nextEvent = eventQueue.Dequeue();
            StartCoroutine(ProcessQueuedEventWithDelay(nextEvent, 1f));
        }
    }
    
    private IEnumerator ProcessQueuedEventWithDelay(RandomEvent eventData, float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessEventImmediate(eventData);
    }*/
}