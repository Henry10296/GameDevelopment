using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    [Header("存档信息")]
    public string saveName;
    public string saveTime;
    public int saveVersion = 1;
    
    [Header("游戏状态")]
    public int currentDay;
    public GamePhase currentPhase;
    public float gameProgress;
    
    [Header("家庭数据")]
    public FamilyMemberSaveData[] familyMembers;
    public int food;
    public int water;
    public int medicine;
    
    [Header("库存数据")]
    public InventoryItemSaveData[] inventoryItems;//道具
    
    [Header("探索数据")]
    public bool[] unlockedMaps;
    public ExplorationStatsSaveData explorationStats;
    
    [Header("无线电数据")]//这里赘余，很多特殊道具都可以保存，需要改
    public bool hasRadio;
    public bool[] broadcastDays;
    public bool goodEndingUnlocked;
    
    [Header("事件数据")]
    public string[] triggeredEvents;
    public EventSaveData[] scheduledEvents;
    
    [Header("日志数据")]
    public JournalEntrySaveData[] journalEntries;
    
    public GameSaveData()
    {
        saveName = $"Save_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[System.Serializable]
public class FamilyMemberSaveData
{
    public string name;
    public CharacterRole role;
    public float health;
    public float hunger;
    public float thirst;
    public float mood;
    public bool isSick;
    public int sickDaysLeft;
    public string illnessType;
    public bool isInjured;
    public string[] statusEffects;
}

[System.Serializable]
public class InventoryItemSaveData
{
    public string itemName;
    public int quantity;
}

[System.Serializable]
public class ExplorationStatsSaveData
{
    public string lastExploredMap;
    public int totalItemsCollected;
    public int totalEnemiesDefeated;
    public float totalExplorationTime;
}

[System.Serializable]
public class EventSaveData
{
    public string eventName;
    public float scheduledDay;
    public bool hasTriggered;
}

[System.Serializable]
public class JournalEntrySaveData
{
    public string title;
    public string content;
    public int day;
    public string timestamp;
    public int entryType; // JournalEntryType as int
}

public class SaveManager : Singleton<SaveManager>
{
    [Header("存档配置")]
    public int maxSaveSlots = 10;
    public bool autoSave = true;
    public float autoSaveInterval = 60f; // 自动存档间隔（秒）
    
    [Header("事件")]
    public GameEvent onSaveCompleted;
    public GameEvent onLoadCompleted;
    public GameEvent onSaveFailed;
    public GameEvent onLoadFailed;
    
    private float autoSaveTimer;
    private string saveDirectory;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeSaveSystem();
    }
    
    void Start()
    {
        SubscribeToEvents();
    }
    
    void Update()
    {
        if (autoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                AutoSave();
                autoSaveTimer = 0f;
            }
        }
    }
    
    void InitializeSaveSystem()
    {
        saveDirectory = System.IO.Path.Combine(Application.persistentDataPath, "Saves");
        
        if (!System.IO.Directory.Exists(saveDirectory))
        {
            System.IO.Directory.CreateDirectory(saveDirectory);
        }
        
        Debug.Log($"[SaveManager] Save directory: {saveDirectory}");
    }
    
    void SubscribeToEvents()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.onDayChanged.RegisterListener(
                GetComponent<IntGameEventListener>());
        }
    }
    
    public async void SaveGame(int slotIndex, string saveName = "")
    {
        try
        {
            GameSaveData saveData = CollectSaveData();
            
            if (!string.IsNullOrEmpty(saveName))
                saveData.saveName = saveName;
            
            string fileName = GetSaveFileName(slotIndex);
            string filePath = System.IO.Path.Combine(saveDirectory, fileName);
            
            string json = JsonUtility.ToJson(saveData, true);
            await System.IO.File.WriteAllTextAsync(filePath, json);
            
            onSaveCompleted?.Raise();
            UIManager.Instance?.ShowMessage($"游戏已保存到槽位 {slotIndex + 1}", 2f);
            
            Debug.Log($"[SaveManager] Game saved to      slot {slotIndex}: {saveData.saveName}");
        }
        catch (System.Exception e)
        {
            onSaveFailed?.Raise();
            UIManager.Instance?.ShowMessage("保存失败!", 3f);
            Debug.LogError($"[SaveManager] Save failed: {e.Message}");
        }
    }
    
    public async void LoadGame(int slotIndex)
    {
        try
        {
            string fileName = GetSaveFileName(slotIndex);
            string filePath = System.IO.Path.Combine(saveDirectory, fileName);
            
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] Save file not found: {filePath}");
                onLoadFailed?.Raise();
                return;
            }
            
            string json = await System.IO.File.ReadAllTextAsync(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            
            if (saveData != null)
            {
                ApplySaveData(saveData);
                onLoadCompleted?.Raise();
                UIManager.Instance?.ShowMessage($"游戏已从槽位 {slotIndex + 1} 加载", 2f);
                
                Debug.Log($"[SaveManager] Game loaded from slot {slotIndex}: {saveData.saveName}");
            }
        }
        catch (System.Exception e)
        {
            onLoadFailed?.Raise();
            UIManager.Instance?.ShowMessage("加载失败!", 3f);
            Debug.LogError($"[SaveManager] Load failed: {e.Message}");
        }
    }
    
    GameSaveData CollectSaveData()
    {
        var saveData = new GameSaveData();
        
        // 游戏状态
        if (GameManager.Instance)
        {
            saveData.currentDay = GameManager.Instance.CurrentDay;
            saveData.currentPhase = GameManager.Instance.CurrentPhase;
            saveData.gameProgress = (float)saveData.currentDay / 5f;
        }
        
        // 家庭数据
        if (FamilyManager.Instance)
        {
            saveData.food = FamilyManager.Instance.Food;
            saveData.water = FamilyManager.Instance.Water;
            saveData.medicine = FamilyManager.Instance.Medicine;
            
            saveData.familyMembers = FamilyManager.Instance.FamilyMembers
                .Select(ConvertFamilyMemberToSaveData).ToArray();
        }
        
        // 库存数据
        if (InventoryManager.Instance)
        {
            saveData.inventoryItems = InventoryManager.Instance.GetItems()
                .Select(item => new InventoryItemSaveData 
                { 
                    itemName = item.itemData.itemName, 
                    quantity = item.quantity 
                }).ToArray();
        }
        
        // 无线电数据
        if (RadioManager.Instance)
        {
            saveData.hasRadio = RadioManager.Instance.hasRadio;
            saveData.broadcastDays = RadioManager.Instance.broadcastDays;
            saveData.goodEndingUnlocked = RadioManager.Instance.GetGoodEndingAchieved();
        }
        
        // 日志数据
        if (JournalManager.Instance)
        {
            saveData.journalEntries = JournalManager.Instance.AllEntries
                .Select(ConvertJournalEntryToSaveData).ToArray();
        }
        
        return saveData;
    }
    
    void ApplySaveData(GameSaveData saveData)
    {
        // 恢复游戏状态
        if (GameManager.Instance)
        {
            // 这里需要小心处理状态恢复
            // 可能需要特殊的加载状态来安全恢复
        }
        
        // 恢复家庭数据
        if (FamilyManager.Instance && saveData.familyMembers != null)
        {
            var familyMembers = FamilyManager.Instance.FamilyMembers;
            
            for (int i = 0; i < saveData.familyMembers.Length && i < familyMembers.Count; i++)
            {
                ApplyFamilyMemberSaveData(familyMembers[i], saveData.familyMembers[i]);
            }
            
            // 恢复资源
            FamilyManager.Instance.AddResource("food", saveData.food - FamilyManager.Instance.Food);
            FamilyManager.Instance.AddResource("water", saveData.water - FamilyManager.Instance.Water);
            FamilyManager.Instance.AddResource("medicine", saveData.medicine - FamilyManager.Instance.Medicine);
        }
        
        // 恢复库存数据
        if (InventoryManager.Instance && saveData.inventoryItems != null)
        {
            // 清空当前库存
            InventoryManager.Instance.ClearInventory();
            
            // 恢复保存的物品
            foreach (var itemSave in saveData.inventoryItems)
            {
                // 根据物品名查找ItemData并添加到库存
                // 这里需要物品数据库或查找系统
            }
        }
        
        // 恢复无线电数据
        if (RadioManager.Instance)
        {
            RadioManager.Instance.hasRadio = saveData.hasRadio;
            RadioManager.Instance.broadcastDays = saveData.broadcastDays ?? new bool[6];
            RadioManager.Instance.radioBroadcasted = saveData.goodEndingUnlocked;
        }
        
        // 恢复日志数据
        if (JournalManager.Instance && saveData.journalEntries != null)
        {
            JournalManager.Instance.ClearJournal();
            
            foreach (var entrySave in saveData.journalEntries)
            {
                var entryType = (JournalEntryType)entrySave.entryType;
                JournalManager.Instance.AddEntry(entrySave.title, entrySave.content, entryType);
            }
        }
    }
    
    FamilyMemberSaveData ConvertFamilyMemberToSaveData(FamilyMember member)
    {
        return new FamilyMemberSaveData
        {
            name = member.name,
            role = member.role,
            health = member.health,
            hunger = member.hunger,
            thirst = member.thirst,
            mood = member.mood,
            isSick = member.isSick,
            sickDaysLeft = member.sickDaysLeft,
            illnessType = member.illnessType,
            isInjured = member.isInjured,
            statusEffects = member.statusEffects.ToArray()
        };
    }
    
    void ApplyFamilyMemberSaveData(FamilyMember member, FamilyMemberSaveData saveData)
    {
        member.health = saveData.health;
        member.hunger = saveData.hunger;
        member.thirst = saveData.thirst;
        member.mood = saveData.mood;
        member.isSick = saveData.isSick;
        member.sickDaysLeft = saveData.sickDaysLeft;
        member.illnessType = saveData.illnessType;
        member.isInjured = saveData.isInjured;
        member.statusEffects = new List<string>(saveData.statusEffects ?? new string[0]);
    }
    
    JournalEntrySaveData ConvertJournalEntryToSaveData(JournalEntry entry)
    {
        return new JournalEntrySaveData
        {
            title = entry.title,
            content = entry.content,
            day = entry.day,
            timestamp = entry.timestamp,
            entryType = (int)entry.type
        };
    }
    
    string GetSaveFileName(int slotIndex)
    {
        return $"save_slot_{slotIndex:D2}.json";
    }
    
    public SaveSlotInfo[] GetSaveSlotInfos()
    {
        var saveSlots = new SaveSlotInfo[maxSaveSlots];
        
        for (int i = 0; i < maxSaveSlots; i++)
        {
            saveSlots[i] = GetSaveSlotInfo(i);
        }
        
        return saveSlots;
    }
    
    SaveSlotInfo GetSaveSlotInfo(int slotIndex)
    {
        string fileName = GetSaveFileName(slotIndex);
        string filePath = System.IO.Path.Combine(saveDirectory, fileName);
        
        if (!System.IO.File.Exists(filePath))
        {
            return new SaveSlotInfo { slotIndex = slotIndex, isEmpty = true };
        }
        
        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            
            return new SaveSlotInfo
            {
                slotIndex = slotIndex,
                isEmpty = false,
                saveName = saveData.saveName,
                saveTime = saveData.saveTime,
                currentDay = saveData.currentDay,
                gameProgress = saveData.gameProgress
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to read save slot {slotIndex}: {e.Message}");
            return new SaveSlotInfo { slotIndex = slotIndex, isEmpty = true, isCorrupted = true };
        }
    }
    
    public void DeleteSave(int slotIndex)
    {
        string fileName = GetSaveFileName(slotIndex);
        string filePath = System.IO.Path.Combine(saveDirectory, fileName);
        
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Debug.Log($"[SaveManager] Deleted save slot {slotIndex}");
        }
    }
    
    void AutoSave()
    {
        // 自动保存到特殊槽位
        SaveGame(maxSaveSlots - 1, "自动保存");
    }
    
    public void OnDayChanged(int newDay)
    {
        if (autoSave)
        {
            AutoSave();
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}

[System.Serializable]
public class SaveSlotInfo//信息
{
    public int slotIndex;
    public bool isEmpty;
    public bool isCorrupted;
    public string saveName;
    public string saveTime;
    public int currentDay;
    public float gameProgress;
}

