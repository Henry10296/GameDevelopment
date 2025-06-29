using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class GameStatistics
{
    public int daysCompleted;
    public int itemsCollected;
    public int enemiesDefeated;
    public float explorationTime;
    public int eventsTriggered;
    public bool hasFoundRadio;
    public Dictionary<string, int> itemsUsed = new Dictionary<string, int>();
    public Dictionary<string, bool> achievementsUnlocked = new Dictionary<string, bool>();
}

public class GameData : Singleton<GameData>
{
    [Header("游戏统计")]
    [SerializeField] private GameStatistics statistics = new GameStatistics();
    
    [Header("游戏进度")]
    [SerializeField] private int highestDayReached = 1;
    [SerializeField] private float totalPlayTime = 0f;
    [SerializeField] private bool tutorialCompleted = false;
    
    [Header("解锁内容")]
    [SerializeField] private List<string> unlockedAchievements = new List<string>();
    [SerializeField] private List<string> unlockedEndings = new List<string>();
    
    // 属性访问器
    public GameStatistics Statistics => statistics;
    public int HighestDayReached => highestDayReached;
    public float TotalPlayTime => totalPlayTime;
    public bool TutorialCompleted => tutorialCompleted;
    
    protected override void Awake()
    {
        base.Awake();
        LoadGameData();
    }
    
    void Start()
    {
        // 开始计时
        InvokeRepeating(nameof(UpdatePlayTime), 1f, 1f);
    }
    
    void UpdatePlayTime()
    {
        totalPlayTime += 1f;
    }
    
    // 统计更新方法
    public void AddItemCollected(string itemName, int quantity = 1)
    {
        statistics.itemsCollected += quantity;
        
        if (!statistics.itemsUsed.ContainsKey(itemName))
            statistics.itemsUsed[itemName] = 0;
        statistics.itemsUsed[itemName] += quantity;
        
        CheckAchievements();
    }
    
    public void AddEnemyDefeated()
    {
        statistics.enemiesDefeated++;
        CheckAchievements();
    }
    
    public void AddExplorationTime(float time)
    {
        statistics.explorationTime += time;
    }
    
    public void AddTimeBonus(float efficiency)
    {
        // 根据效率给予奖励
        if (efficiency > 0.8f)
        {
            UnlockAchievement("SPEED_RUNNER");
        }
    }
    
    public void TriggerEvent()
    {
        statistics.eventsTriggered++;
    }
    
    public void SetRadioFound(bool found)
    {
        statistics.hasFoundRadio = found;
        if (found)
        {
            UnlockAchievement("RADIO_FINDER");
        }
    }
    
    public void SetDayCompleted(int day)
    {
        statistics.daysCompleted = day;
        highestDayReached = Mathf.Max(highestDayReached, day);
        
        CheckDayAchievements(day);
    }
    
    // 成就系统
    public void UnlockAchievement(string achievementId)
    {
        if (!unlockedAchievements.Contains(achievementId))
        {
            unlockedAchievements.Add(achievementId);
            statistics.achievementsUnlocked[achievementId] = true;
            
            ShowAchievementNotification(achievementId);
            Debug.Log($"[GameData] Achievement unlocked: {achievementId}");
        }
    }
    
    public bool IsAchievementUnlocked(string achievementId)
    {
        return unlockedAchievements.Contains(achievementId);
    }
    
    void CheckAchievements()
    {
        // 收集成就
        if (statistics.itemsCollected >= 50)
            UnlockAchievement("COLLECTOR");
        
        if (statistics.itemsCollected >= 100)
            UnlockAchievement("HOARDER");
        
        // 战斗成就
        if (statistics.enemiesDefeated >= 10)
            UnlockAchievement("FIGHTER");
        
        if (statistics.enemiesDefeated >= 25)
            UnlockAchievement("WARRIOR");
        
        // 探索成就
        if (statistics.explorationTime >= 60f * 60f) // 1小时
            UnlockAchievement("EXPLORER");
    }
    
    void CheckDayAchievements(int day)
    {
        switch (day)
        {
            case 3:
                UnlockAchievement("SURVIVOR_3_DAYS");
                break;
            case 5:
                UnlockAchievement("SURVIVOR_5_DAYS");
                break;
        }
    }
    
    void ShowAchievementNotification(string achievementId)
    {
        string achievementName = GetAchievementName(achievementId);
        string message = $"成就解锁: {achievementName}";
        
        if (UIManager.Instance)
        {
            UIManager.Instance.ShowMessage(message, 4f);
        }
        else if (UIManager.Instance)
        {
            UIManager.Instance.ShowMessage(message, 4f);
        }
    }
    
    string GetAchievementName(string achievementId)
    {
        return achievementId switch
        {
            "RADIO_FINDER" => "无线电专家",
            "COLLECTOR" => "收集者",
            "HOARDER" => "囤积者",
            "FIGHTER" => "战士",
            "WARRIOR" => "勇士",
            "EXPLORER" => "探索者",
            "SPEED_RUNNER" => "速度跑者",
            "SURVIVOR_3_DAYS" => "三日生存者",
            "SURVIVOR_5_DAYS" => "五日生存者",
            _ => achievementId
        };
    }
    
    // 结局系统
    public void UnlockEnding(string endingId)
    {
        if (!unlockedEndings.Contains(endingId))
        {
            unlockedEndings.Add(endingId);
            Debug.Log($"[GameData] Ending unlocked: {endingId}");
        }
    }
    
    public List<string> GetUnlockedEndings()
    {
        return new List<string>(unlockedEndings);
    }
    
    // 数据重置
    public void ResetData()
    {
        statistics = new GameStatistics();
        totalPlayTime = 0f;
        
        // 保留永久进度
        // highestDayReached 和成就不重置
        
        Debug.Log("[GameData] Game data reset");
    }
    
    public void ResetAllData()
    {
        statistics = new GameStatistics();
        highestDayReached = 1;
        totalPlayTime = 0f;
        tutorialCompleted = false;
        unlockedAchievements.Clear();
        unlockedEndings.Clear();
        
        SaveGameData();
        Debug.Log("[GameData] All data reset");
    }
    
    // 数据持久化
    public void SaveGameData()
    {
        GameDataSave saveData = new GameDataSave
        {
            statistics = statistics,
            highestDayReached = highestDayReached,
            totalPlayTime = totalPlayTime,
            tutorialCompleted = tutorialCompleted,
            unlockedAchievements = unlockedAchievements,
            unlockedEndings = unlockedEndings
        };
        
        string json = JsonUtility.ToJson(saveData, true);
        PlayerPrefs.SetString("GameData", json);
        PlayerPrefs.Save();
    }
    
    public void LoadGameData()
    {
        if (PlayerPrefs.HasKey("GameData"))
        {
            try
            {
                string json = PlayerPrefs.GetString("GameData");
                GameDataSave saveData = JsonUtility.FromJson<GameDataSave>(json);
                
                if (saveData != null)
                {
                    statistics = saveData.statistics ?? new GameStatistics();
                    highestDayReached = saveData.highestDayReached;
                    totalPlayTime = saveData.totalPlayTime;
                    tutorialCompleted = saveData.tutorialCompleted;
                    unlockedAchievements = saveData.unlockedAchievements ?? new List<string>();
                    unlockedEndings = saveData.unlockedEndings ?? new List<string>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameData] Failed to load game data: {e.Message}");
                // 使用默认数据
                statistics = new GameStatistics();
            }
        }
    }
    
    // 获取游戏统计信息
    public string GetStatisticsReport()
    {
        string report = "=== 游戏统计 ===\n";
        report += $"最高到达天数: {highestDayReached}\n";
        report += $"总游戏时间: {totalPlayTime / 60f:F1} 分钟\n";
        report += $"收集物品数: {statistics.itemsCollected}\n";
        report += $"击败敌人数: {statistics.enemiesDefeated}\n";
        report += $"探索时间: {statistics.explorationTime / 60f:F1} 分钟\n";
        report += $"触发事件数: {statistics.eventsTriggered}\n";
        report += $"解锁成就数: {unlockedAchievements.Count}\n";
        report += $"解锁结局数: {unlockedEndings.Count}\n";
        
        return report;
    }
    
    protected override void OnDestroy()
    {
        SaveGameData();
        base.OnDestroy();
    }
}

[System.Serializable]
public class GameDataSave
{
    public GameStatistics statistics;
    public int highestDayReached;
    public float totalPlayTime;
    public bool tutorialCompleted;
    public List<string> unlockedAchievements;
    public List<string> unlockedEndings;
}