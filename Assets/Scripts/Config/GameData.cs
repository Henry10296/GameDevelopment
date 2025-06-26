using UnityEngine;

public class GameData : Singleton<GameData>
{
    [Header("游戏数据")]
    public float timeBonus = 0f;
    public int totalItemsCollected = 0;
    public int totalEnemiesDefeated = 0;
    public float totalExplorationTime = 0f;
    
    [Header("统计信息")]
    public int daysCompleted = 0;
    public int eventsTriggered = 0;
    public bool hasFoundRadio = false;
    
    public void AddTimeBonus(float bonus)
    {
        timeBonus += bonus;
        Debug.Log($"[GameData] Time bonus added: {bonus}, Total: {timeBonus}");
    }
    
    public void AddItemCollected(int count = 1)
    {
        totalItemsCollected += count;
    }
    
    public void AddEnemyDefeated(int count = 1)
    {
        totalEnemiesDefeated += count;
    }
    
    public void AddExplorationTime(float time)
    {
        totalExplorationTime += time;
    }
    
    public void CompleteDay()
    {
        daysCompleted++;
    }
    
    public void TriggerEvent()
    {
        eventsTriggered++;
    }
    
    public GameStatistics GetStatistics()
    {
        return new GameStatistics
        {
            daysCompleted = this.daysCompleted,
            itemsCollected = this.totalItemsCollected,
            enemiesDefeated = this.totalEnemiesDefeated,
            explorationTime = this.totalExplorationTime,
            eventsTriggered = this.eventsTriggered,
            timeBonus = this.timeBonus,
            hasFoundRadio = this.hasFoundRadio
        };
    }
    
    public void ResetData()
    {
        timeBonus = 0f;
        totalItemsCollected = 0;
        totalEnemiesDefeated = 0;
        totalExplorationTime = 0f;
        daysCompleted = 0;
        eventsTriggered = 0;
        hasFoundRadio = false;
        
        Debug.Log("[GameData] Game data reset");
    }
}

[System.Serializable]
public class GameStatistics
{
    public int daysCompleted;
    public int itemsCollected;
    public int enemiesDefeated;
    public float explorationTime;
    public int eventsTriggered;
    public float timeBonus;
    public bool hasFoundRadio;
}