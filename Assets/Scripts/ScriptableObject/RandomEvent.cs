using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomEvent", menuName = "Game/Random Event")]
public class RandomEvent : ScriptableObject
{
    [Header("事件基础信息")]
    public string eventName;
    [TextArea(3, 5)] public string eventDescription;
    public Sprite eventIcon;
    public GameEventPriority priority = GameEventPriority.Normal;
    
    [Header("触发条件")]
    public int minDay = 1;
    public int maxDay = 5;
    [Range(0f, 1f)] public float baseTriggerChance = 0.3f;
    public EventCondition[] triggerConditions;
    
    [Header("事件类型")]
    public GameEventType eventType;
    public bool requiresChoice = true;
    public bool canRepeat = false;
    
    [Header("选择项")]
    public EventChoice[] choices;
    
    [Header("自动效果（无选择时）")]
    public EventEffect[] automaticEffects;
    
    [Header("后续事件")]
    public RandomEvent followupEvent;
    public float followupDelay = 1f; // 天数延迟
    
    [Header("任务扩展")]
    public bool isQuest = false;
    public QuestObjective[] questObjectives;
    public string[] prerequisiteQuestIds;
    public string[] unlockQuestIds;
    
    // 任务状态（如果是任务）
    [System.NonSerialized]
    public QuestStatus questStatus = QuestStatus.NotStarted;
    [System.NonSerialized]
    public Dictionary<string, int> objectiveProgress = new();
    
    [Header("事件网络扩展")]
    public EventNode nodePosition = new EventNode(); // 编辑器中的位置
    public RandomEvent[] prerequisites; // 前置事件
    public RandomEvent[] unlockEvents; // 解锁的事件
    public EventConnection[] connections; // 连接关系
    
    [Header("任务扩展")]
    public bool isMainQuest = false;
    public bool isSideQuest = false;
    public string questChain = ""; // 任务链ID
    public int questOrder = 0; // 任务顺序
    
    [Header("事件标签")]
    public string[] tags = new string[0]; // 用于搜索和分类
    public Color editorColor = Color.white; // 编辑器中的颜色
    
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
    public bool CanTriggerAsQuest()
    {
        if (!isQuest) return CanTrigger();
        
        // 检查前置任务
        if (prerequisiteQuestIds != null)
        {
            foreach (string prereqId in prerequisiteQuestIds)
            {
                var prereq = QuestExtensionManager.Instance?.GetQuest(prereqId);
                if (prereq == null || prereq.questStatus != QuestStatus.Completed)
                    return false;
            }
        }
        
        return questStatus == QuestStatus.NotStarted && CanTrigger();
    }
    
    public void StartQuest()
    {
        if (!isQuest || !CanTriggerAsQuest()) return;
        
        questStatus = QuestStatus.InProgress;
        
        // 初始化目标进度
        if (questObjectives != null)
        {
            foreach (var objective in questObjectives)
            {
                objectiveProgress[objective.objectiveId] = 0;
            }
        }
        
        QuestExtensionManager.Instance?.OnQuestStarted(this);
    }
    
    // 更新任务进度
    public void UpdateQuestProgress(string objectiveId, int progress = 1)
    {
        if (!isQuest || questStatus != QuestStatus.InProgress) return;
        
        if (objectiveProgress.ContainsKey(objectiveId))
        {
            objectiveProgress[objectiveId] += progress;
            
            // 检查目标是否完成
            var objective = System.Array.Find(questObjectives, o => o.objectiveId == objectiveId);
            if (objective != null && objectiveProgress[objectiveId] >= objective.targetAmount)
            {
                objective.isCompleted = true;
                CheckQuestCompletion();
            }
        }
    }
    
    // 检查任务完成
    private void CheckQuestCompletion()
    {
        if (questObjectives.All(o => o.isCompleted))
        {
            CompleteQuest();
        }
    }
    
    // 完成任务
    public void CompleteQuest()
    {
        questStatus = QuestStatus.Completed;
        
        // 解锁新任务
        if (unlockQuestIds != null)
        {
            foreach (string unlockId in unlockQuestIds)
            {
                var unlockQuest = QuestExtensionManager.Instance?.GetQuest(unlockId);
                if (unlockQuest != null)
                {
                    unlockQuest.questStatus = QuestStatus.Available;
                }
            }
        }
        
        QuestExtensionManager.Instance?.OnQuestCompleted(this);
    }
    
    public float CalculateActualTriggerChance()
    {
        float chance = baseTriggerChance;
        
        // 根据优先级调整
        chance *= priority switch
        {
            GameEventPriority.Critical => 2.0f,
            GameEventPriority.High => 1.5f,
            GameEventPriority.Normal => 1.0f,
            GameEventPriority.Low => 0.5f,
            _ => 1.0f
        };
        
        return Mathf.Clamp01(chance);
    }
}
