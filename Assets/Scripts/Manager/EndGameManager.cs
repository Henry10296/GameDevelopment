using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndGameManager : Singleton<EndGameManager>
{
    [Header("结局UI")]
    public GameObject endGamePanel;
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI endingDescriptionText;
    public TextMeshProUGUI statisticsText;
    public Image endingImage;
    
    [Header("结局配置")]
    public EndingData[] possibleEndings;
    
    [Header("音效")]
    public AudioClip goodEndingMusic;
    public AudioClip badEndingMusic;
    
    private EndingData currentEnding;
    
    public void ShowEnding(bool isGoodEnding)
    {
        if (endGamePanel) endGamePanel.SetActive(true);
        
        // 计算具体结局
        EndingType endingType = CalculateEndingType(isGoodEnding);
        currentEnding = GetEndingData(endingType);
        
        // 显示结局内容
        DisplayEndingContent();
        
        // 播放结局音乐
        PlayEndingMusic(isGoodEnding);
        
        // 记录到日志
        RecordEndingToJournal();
        
        Debug.Log($"[EndGameManager] Game ended with: {endingType}");
    }
    
    EndingType CalculateEndingType(bool hasRadio)
    {
        var familyStatus = FamilyManager.Instance?.GetOverallStatus() ?? FamilyStatus.AllDead;
        var stats = GameData.Instance?.GetStatistics();
        
        if (familyStatus == FamilyStatus.AllDead)
            return EndingType.AllDead;
        
        if (hasRadio && familyStatus == FamilyStatus.Stable)
            return EndingType.PerfectRescue;
        
        if (hasRadio)
            return EndingType.GoodRescue;
        
        if (familyStatus == FamilyStatus.Stable)
            return EndingType.Survival;
        
        return EndingType.BadSurvival;
    }
    
    EndingData GetEndingData(EndingType type)
    {
        foreach (var ending in possibleEndings)
        {
            if (ending.endingType == type)
                return ending;
        }
        
        // 默认结局
        return new EndingData
        {
            endingType = type,
            title = "未知结局",
            description = "发生了意外的情况...",
            isGoodEnding = false
        };
    }
    
    void DisplayEndingContent()
    {
        if (currentEnding == null) return;
        
        if (endingTitleText) endingTitleText.text = currentEnding.title;
        if (endingDescriptionText) endingDescriptionText.text = currentEnding.description;
        if (endingImage && currentEnding.endingImage) endingImage.sprite = currentEnding.endingImage;
        
        // 显示统计信息
        DisplayStatistics();
    }
    
    void DisplayStatistics()
    {
        if (statisticsText == null) return;
        
        var stats = GameData.Instance?.GetStatistics();
        if (stats == null) return;
        
        string statsText = $"游戏统计:\n";
        statsText += $"存活天数: {stats.daysCompleted}/5\n";
        statsText += $"收集物品: {stats.itemsCollected}\n";
        statsText += $"击败敌人: {stats.enemiesDefeated}\n";
        statsText += $"探索时间: {stats.explorationTime:F1}分钟\n";
        statsText += $"触发事件: {stats.eventsTriggered}\n";
        
        if (stats.hasFoundRadio)
            statsText += "✓ 找到了无线电设备\n";
        
        statisticsText.text = statsText;
    }
    
    void PlayEndingMusic(bool isGoodEnding)
    {
        if (AudioManager.Instance)
        {
            AudioClip musicToPlay = isGoodEnding ? goodEndingMusic : badEndingMusic;
            if (musicToPlay)
                AudioManager.Instance.PlayMusic(musicToPlay.name);
        }
    }
    
    void RecordEndingToJournal()
    {
        if (JournalManager.Instance && currentEnding != null)
        {
            JournalManager.Instance.AddEntry(
                "游戏结束",
                $"{currentEnding.title}\n\n{currentEnding.description}",
                JournalEntryType.Critical
            );
        }
    }
    
    public void RestartGame()
    {
        // 重置游戏数据
        GameData.Instance?.ResetData();
        
        // 切换到主菜单
        if (SceneTransitionManager.Instance)
            SceneTransitionManager.Instance.LoadSceneWithFade("0_MainMenu");
    }
    
    public void QuitToMainMenu()
    {
        if (SceneTransitionManager.Instance)
            SceneTransitionManager.Instance.LoadSceneWithFade("0_MainMenu");
    }
}

[System.Serializable]
public class EndingData
{
    public EndingType endingType;
    public string title;
    [TextArea(3, 5)] public string description;
    public Sprite endingImage;
    public bool isGoodEnding;
}

public enum EndingType
{
    PerfectRescue,    // 完美救援：找到无线电+家人健康
    GoodRescue,       // 良好救援：找到无线电但家人有损失
    Survival,         // 普通生存：没找到无线电但家人健康
    BadSurvival,      // 艰难生存：没找到无线电且家人状况不佳
    AllDead          // 全员死亡：最坏结局
}