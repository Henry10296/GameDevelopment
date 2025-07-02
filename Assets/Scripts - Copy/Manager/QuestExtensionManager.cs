using System.Collections.Generic;
using System.Linq;

public class QuestExtensionManager : Singleton<QuestExtensionManager>
{
    protected override int InitializationOrder => 25; // 在GameEventManager之后
    
    public RandomEvent GetQuest(string questId)
    {
        return GameEventManager.Instance?.GetQuest(questId);
    }
    
    public void OnQuestStarted(RandomEvent quest)
    {
        GameEventManager.Instance?.OnQuestStarted(quest);
    }
    
    public void OnQuestCompleted(RandomEvent quest)
    {
        GameEventManager.Instance?.OnQuestCompleted(quest);
    }
    
    // 提供给UI的查询方法
    public List<RandomEvent> GetAvailableQuests()
    {
        return GameEventManager.Instance?.allQuests?
            .Where(q => q.questStatus == QuestStatus.Available || q.questStatus == QuestStatus.NotStarted)
            .ToList() ?? new List<RandomEvent>();
    }
    
    public List<RandomEvent> GetActiveQuests()
    {
        return GameEventManager.Instance?.allQuests?
            .Where(q => q.questStatus == QuestStatus.InProgress)
            .ToList() ?? new List<RandomEvent>();
    }
}