using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class JournalEntry
{
    public string title;
    public string content;
    public int day;
    public string timestamp;
    public JournalEntryType type;
    public Color entryColor = Color.white;
    
    public JournalEntry(string entryTitle, string entryContent, JournalEntryType entryType = JournalEntryType.General)
    {
        title = entryTitle;
        content = entryContent;
        day = GameManager.Instance ? GameManager.Instance.CurrentDay : 1;
        timestamp = System.DateTime.Now.ToString("HH:mm");
        type = entryType;
        entryColor = GetColorForType(entryType);
    }
    
    Color GetColorForType(JournalEntryType entryType)
    {
        return entryType switch
        {
            JournalEntryType.General => Color.white,
            JournalEntryType.Important => Color.yellow,
            JournalEntryType.Warning => new Color(1f, 0.5f, 0f), // 橙色
            JournalEntryType.Critical => Color.red,
            JournalEntryType.Success => Color.green,
            JournalEntryType.Event => Color.cyan,
            _ => Color.white
        };
    }
    
    public string GetFormattedEntry()
    {
        return $"[第{day}天 {timestamp}] {title}\n{content}";
    }
}

public enum JournalEntryType
{
    General,    // 一般记录
    Important,  // 重要信息
    Warning,    // 警告
    Critical,   // 危急
    Success,    // 成功
    Event       // 事件
}

public class JournalManager : Singleton<JournalManager>
{
    [Header("日志配置")]
    public int maxEntries = 100;
    public bool autoSave = true;
    
    [Header("事件")]
    public GameEvent onEntryAdded;
    public GameEvent onJournalUpdated;
    
    private List<JournalEntry> entries = new List<JournalEntry>();
    private Dictionary<int, List<JournalEntry>> entriesByDay = new Dictionary<int, List<JournalEntry>>();
    
    // 属性访问器
    public List<JournalEntry> AllEntries => new List<JournalEntry>(entries);
    public int EntryCount => entries.Count;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeJournal();
    }
    
    void Start()
    {
        SubscribeToEvents();
        LoadJournalData();
    }
    
    void SubscribeToEvents()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.onDayChanged.RegisterListener(
                GetComponent<IntGameEventListener>());
        }
    }
    
    void InitializeJournal()
    {
        // 添加初始日志条目
        AddEntry("游戏开始", "核战争爆发了。我们一家躲在地下室中，外面的世界变得危险而混乱。我必须保护好我的家人，寻找足够的物资来生存下去。", JournalEntryType.Important);
    }
    
    public void AddEntry(string title, string content, JournalEntryType type = JournalEntryType.General)
    {
        var entry = new JournalEntry(title, content, type);
        entries.Add(entry);
        
        // 按天组织条目
        int day = entry.day;
        if (!entriesByDay.ContainsKey(day))
        {
            entriesByDay[day] = new List<JournalEntry>();
        }
        entriesByDay[day].Add(entry);
        
        // 限制条目数量
        if (entries.Count > maxEntries)
        {
            var oldestEntry = entries[0];
            entries.RemoveAt(0);
            
            // 从按日分组中移除
            if (entriesByDay.ContainsKey(oldestEntry.day))
            {
                entriesByDay[oldestEntry.day].Remove(oldestEntry);
                if (entriesByDay[oldestEntry.day].Count == 0)
                {
                    entriesByDay.Remove(oldestEntry.day);
                }
            }
        }
        
        onEntryAdded?.Raise();
        onJournalUpdated?.Raise();
        
        if (autoSave)
        {
            SaveJournalData();
        }
        
        Debug.Log($"[JournalManager] Added entry: {title}");
    }
    
    public List<JournalEntry> GetEntriesForDay(int day)
    {
        return entriesByDay.ContainsKey(day) ? 
            new List<JournalEntry>(entriesByDay[day]) : 
            new List<JournalEntry>();
    }
    
    public List<JournalEntry> GetRecentEntries(int count = 10)
    {
        return entries.TakeLast(count).ToList();
    }
    
    public List<JournalEntry> GetEntriesByType(JournalEntryType type)
    {
        return entries.Where(e => e.type == type).ToList();
    }
    
    public void OnDayChanged(int newDay)
    {
        // 添加新一天的开始记录
        AddEntry($"第{newDay}天开始", GetDayStartMessage(newDay), JournalEntryType.Important);
        
        // 生成日常总结（如果不是第一天）
        if (newDay > 1)
        {
            GenerateDailySummary(newDay - 1);
        }
    }
    
    string GetDayStartMessage(int day)
    {
        return day switch
        {
            1 => "这是核战争后的第一天。我们必须适应这个新的现实。",
            2 => "第二天了。希望外面的情况有所好转。",
            3 => "第三天。如果我们找到了无线电，今天是发送信号的关键日子。",
            4 => "第四天。时间过得很快，我们必须抓紧时间。",
            5 => "最后一天了。如果我们找到了无线电，今天是最后的机会。",
            _ => $"第{day}天。我们继续为生存而奋斗。"
        };
    }
    
    void GenerateDailySummary(int day)
    {
        var dayEntries = GetEntriesForDay(day);
        if (dayEntries.Count == 0) return;
        
        var summary = $"第{day}天总结：";
        
        // 统计不同类型的事件
        var eventCount = dayEntries.Count(e => e.type == JournalEntryType.Event);
        var warningCount = dayEntries.Count(e => e.type == JournalEntryType.Warning);
        var successCount = dayEntries.Count(e => e.type == JournalEntryType.Success);
        
        if (eventCount > 0) summary += $" 发生了{eventCount}个重要事件。";
        if (warningCount > 0) summary += $" 遇到了{warningCount}个警告情况。";
        if (successCount > 0) summary += $" 取得了{successCount}个成功。";
        
        // 家庭状况总结
        if (FamilyManager.Instance)
        {
            var familyStatus = FamilyManager.Instance.GetOverallStatus();
            summary += $" 家庭状况：{GetFamilyStatusDescription(familyStatus)}";
        }
        
        AddEntry($"第{day}天总结", summary, JournalEntryType.General);
    }
    
    string GetFamilyStatusDescription(FamilyStatus status)
    {
        return status switch
        {
            FamilyStatus.Stable => "稳定",
            FamilyStatus.Concerning => "令人担忧",
            FamilyStatus.ResourceShortage => "资源短缺",
            FamilyStatus.Critical => "危急状况",
            FamilyStatus.AllDead => "全部死亡",
            _ => "未知"
        };
    }
    
    void SaveJournalData()
    {
        try
        {
            var journalData = new JournalSaveData
            {
                entries = entries,
                lastSaveTime = System.DateTime.Now.ToBinary().ToString()
            };
            
            string json = JsonUtility.ToJson(journalData, true);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "journal.json");
            System.IO.File.WriteAllText(filePath, json);
            
            Debug.Log("[JournalManager] Journal saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JournalManager] Failed to save journal: {e.Message}");
        }
    }
    
    void LoadJournalData()
    {
        try
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "journal.json");
            
            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                var journalData = JsonUtility.FromJson<JournalSaveData>(json);
                
                if (journalData?.entries != null)
                {
                    entries = journalData.entries;
                    
                    // 重建按日分组
                    entriesByDay.Clear();
                    foreach (var entry in entries)
                    {
                        if (!entriesByDay.ContainsKey(entry.day))
                        {
                            entriesByDay[entry.day] = new List<JournalEntry>();
                        }
                        entriesByDay[entry.day].Add(entry);
                    }
                    
                    Debug.Log($"[JournalManager] Loaded {entries.Count} journal entries");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JournalManager] Failed to load journal: {e.Message}");
        }
    }
    
    public void ClearJournal()
    {
        entries.Clear();
        entriesByDay.Clear();
        onJournalUpdated?.Raise();
        
        if (autoSave)
        {
            SaveJournalData();
        }
        
        Debug.Log("[JournalManager] Journal cleared");
    }
    
    public string ExportJournalAsText()
    {
        var exportText = "=== 核战生存日志 ===\n\n";
        
        foreach (var dayGroup in entriesByDay.OrderBy(kvp => kvp.Key))
        {
            exportText += $"=== 第{dayGroup.Key}天 ===\n";
            
            foreach (var entry in dayGroup.Value)
            {
                exportText += entry.GetFormattedEntry() + "\n\n";
            }
        }
        
        return exportText;
    }
    
    protected override void OnDestroy()
    {
        if (autoSave)
        {
            SaveJournalData();
        }
        
        base.OnDestroy();
    }
}

[System.Serializable]
public class JournalSaveData
{
    public List<JournalEntry> entries;
    public string lastSaveTime;
}