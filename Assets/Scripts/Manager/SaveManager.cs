
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public partial class GameSaveData
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

public partial class SaveManager : Singleton<SaveManager>
{
    [Header("存档配置")]
    public int maxSaveSlots = 10;
    public bool autoSave = true;
    public float autoSaveInterval = 60f;
    
    [Header("动态数据设置")]
    public bool enableDynamicDataSave = true;
    public bool captureScreenshot = true;
    public bool savePerformanceData = true;
    
    [Header("事件")]
    public GameEvent onSaveCompleted;
    public GameEvent onLoadCompleted;
    public GameEvent onSaveFailed;
    public GameEvent onLoadFailed;
    
    private float autoSaveTimer;
    private string saveDirectory;
    private bool isQuitting = false;
    private DynamicGameData currentDynamicData = new();
    
    // 销毁顺序：SaveManager应该较早销毁，避免依赖已销毁的管理器
    protected override int DestroyOrder => 100;
    
    protected override void OnSingletonAwake()
    {
        InitializeSaveSystem();
    }
    
    void Start()
    {
        SubscribeToEvents();
    }
    
    void Update()
    {
        if (autoSave && !isQuitting)
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
        if (GameManager.HasInstance)
        {
            var listener = GetComponent<IntGameEventListener>();
            if (listener != null && GameManager.Instance.onDayChanged != null)
            {
                GameManager.Instance.onDayChanged.RegisterListener(listener);
            }
        }
    }
    
    public async void SaveGame(int slotIndex, string saveName = "")
    {
        if (isQuitting)
        {
            Debug.Log("[SaveManager] Skipping save during application quit");
            return;
        }
        
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
            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowMessage($"游戏已保存到槽位 {slotIndex + 1}", 2f);
            }
            
            Debug.Log($"[SaveManager] Game saved to slot {slotIndex}: {saveData.saveName}");
        }
        catch (System.Exception e)
        {
            onSaveFailed?.Raise();
            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowMessage("保存失败!", 3f);
            }
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
                if (UIManager.HasInstance)
                {
                    UIManager.Instance.ShowMessage($"游戏已从槽位 {slotIndex + 1} 加载", 2f);
                }
                
                Debug.Log($"[SaveManager] Game loaded from slot {slotIndex}: {saveData.saveName}");
            }
        }
        catch (System.Exception e)
        {
            onLoadFailed?.Raise();
            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowMessage("加载失败!", 3f);
            }
            Debug.LogError($"[SaveManager] Load failed: {e.Message}");
        }
    }
    
    GameSaveData CollectSaveData()
    {
        var saveData = new GameSaveData();
        
        // 游戏状态 - 安全检查
        if (GameManager.HasInstance)
        {
            saveData.currentDay = GameManager.Instance.CurrentDay;
            saveData.currentPhase = GameManager.Instance.CurrentPhase;
            saveData.gameProgress = (float)saveData.currentDay / 5f;
        }
        
        // 家庭数据 - 安全检查
        if (FamilyManager.HasInstance)
        {
            var fm = FamilyManager.Instance;
            saveData.food = fm.Food;
            saveData.water = fm.Water;
            saveData.medicine = fm.Medicine;
            
            saveData.familyMembers = fm.FamilyMembers
                .Select(ConvertFamilyMemberToSaveData).ToArray();
        }
        
        // 库存数据 - 安全检查
        if (InventoryManager.HasInstance)
        {
            saveData.inventoryItems = InventoryManager.Instance.GetItems()
                .Select(item => new InventoryItemSaveData 
                { 
                    itemName = item.itemData.itemName, 
                    quantity = item.quantity 
                }).ToArray();
        }
        
        // 无线电数据 - 安全检查
        if (RadioManager.HasInstance)
        {
            var rm = RadioManager.Instance;
            saveData.hasRadio = rm.hasRadio;
            saveData.broadcastDays = rm.broadcastDays;
            saveData.goodEndingUnlocked = rm.GetGoodEndingAchieved();
        }
        
        // 日志数据 - 安全检查
        if (JournalManager.HasInstance)
        {
            saveData.journalEntries = JournalManager.Instance.AllEntries
                .Select(ConvertJournalEntryToSaveData).ToArray();
        }
        
        // 动态数据收集 - 仅在启用时
        if (enableDynamicDataSave && !isQuitting)
        {
            try
            {
                saveData.dynamicData = CollectDynamicData();
                saveData.sceneDynamicData = CollectSceneDynamicData();
                saveData.configState = CollectConfigState();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveManager] Failed to collect dynamic data: {e.Message}");
            }
        }
        
        // 添加元数据
        saveData.metadata = SaveMetadata.CreateCurrent();
        
        // 截图 - 仅在游戏运行时
        if (captureScreenshot && Application.isPlaying && !isQuitting)
        {
            try
            {
                saveData.metadata.screenshotData = CaptureScreenshot();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveManager] Failed to capture screenshot: {e.Message}");
            }
        }
        
        return saveData;
    }
    
    void ApplySaveData(GameSaveData saveData)
    {
        // 恢复游戏状态
        if (GameManager.HasInstance && saveData.currentDay > 0)
        {
            // 通过GameManager的公共方法恢复状态
            // 这里需要GameManager提供相应的方法
        }
        
        // 恢复家庭数据
        if (FamilyManager.HasInstance && saveData.familyMembers != null)
        {
            var familyMembers = FamilyManager.Instance.FamilyMembers;
            
            for (int i = 0; i < saveData.familyMembers.Length && i < familyMembers.Count; i++)
            {
                ApplyFamilyMemberSaveData(familyMembers[i], saveData.familyMembers[i]);
            }
            
            // 恢复资源
            var fm = FamilyManager.Instance;
            int foodDiff = saveData.food - fm.Food;
            int waterDiff = saveData.water - fm.Water;
            int medicineDiff = saveData.medicine - fm.Medicine;
            
            if (foodDiff != 0) fm.AddResource("food", foodDiff);
            if (waterDiff != 0) fm.AddResource("water", waterDiff);
            if (medicineDiff != 0) fm.AddResource("medicine", medicineDiff);
        }
        
        // 恢复库存数据
        if (InventoryManager.HasInstance && saveData.inventoryItems != null)
        {
            InventoryManager.Instance.ClearInventory();
            
            foreach (var itemSave in saveData.inventoryItems)
            {
                // 这里需要通过ItemDatabase或其他方式查找ItemData
                // 临时实现：通过名称查找
                var itemData = FindItemDataByName(itemSave.itemName);
                if (itemData != null)
                {
                    InventoryManager.Instance.AddItem(itemData, itemSave.quantity);
                }
            }
        }
        
        // 恢复无线电数据
        if (RadioManager.HasInstance)
        {
            var rm = RadioManager.Instance;
            rm.hasRadio = saveData.hasRadio;
            rm.broadcastDays = saveData.broadcastDays ?? new bool[6];
            rm.radioBroadcasted = saveData.goodEndingUnlocked;
        }
        
        // 恢复日志数据
        if (JournalManager.HasInstance && saveData.journalEntries != null)
        {
            JournalManager.Instance.ClearJournal();
            
            foreach (var entrySave in saveData.journalEntries)
            {
                var entryType = (JournalEntryType)entrySave.entryType;
                JournalManager.Instance.AddEntry(entrySave.title, entrySave.content, entryType);
            }
        }
        
        // 应用动态数据
        if (enableDynamicDataSave)
        {
            if (saveData.dynamicData != null)
            {
                ApplyDynamicData(saveData.dynamicData);
            }
            
            if (saveData.sceneDynamicData != null)
            {
                ApplySceneDynamicData(saveData.sceneDynamicData);
            }
            
            if (saveData.configState != null)
            {
                ApplyConfigState(saveData.configState);
            }
        }
    }
    
    // 查找ItemData的临时实现
    ItemData FindItemDataByName(string itemName)
    {
        // 这里应该通过ItemDatabase或Resources查找
        // 临时实现：通过Resources查找
        var allItems = Resources.LoadAll<ItemData>("Items");
        return System.Array.Find(allItems, item => item.itemName == itemName);
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
    
    // 动态数据收集
    private DynamicGameData CollectDynamicData()
    {
        var dynamicData = new DynamicGameData();
        
        if (GameManager.HasInstance)
        {
            dynamicData.currentPhaseElapsedTime = Time.time;
            dynamicData.totalExplorationTime = CalculateTotalExplorationTime();
        }
        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            dynamicData.playerPosition = player.transform.position;
            dynamicData.playerRotation = player.transform.rotation;
            
            if (player.TryGetComponent<PlayerHealth>(out var health))
            {
                dynamicData.playerHealth = health.currentHealth;
            }
        }
        
        if (savePerformanceData)
        {
            dynamicData.averageFPS = CalculateAverageFPS();
        }
        
        return dynamicData;
    }
    
    private SceneDynamicData[] CollectSceneDynamicData()
    {
        var sceneDataList = new List<SceneDynamicData>();
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        var sceneData = new SceneDynamicData
        {
            sceneName = currentSceneName,
            timeSpentInScene = Time.time,
            sceneDataJson = CollectSceneObjectStates()
        };
        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            sceneData.lastPlayerPosition = player.transform.position;
        }
        
        sceneDataList.Add(sceneData);
        return sceneDataList.ToArray();
    }
    
    private string CollectSceneObjectStates()
    {
        var sceneObjects = new Dictionary<string, object>();
        
        var saveables = FindObjectsOfType<MonoBehaviour>().OfType<IDynamicSaveable>()
                       .OrderBy(s => s.GetSavePriority());
        
        foreach (var saveable in saveables)
        {
            try
            {
                sceneObjects[saveable.GetDynamicSaveKey()] = saveable.GetDynamicData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to save dynamic data for {saveable.GetDynamicSaveKey()}: {e.Message}");
            }
        }
        
        return sceneObjects.Count > 0 ? JsonUtility.ToJson(new SceneObjectData { objects = sceneObjects }) : "";
    }
    
    private ConfigStateData CollectConfigState()
    {
        var configState = new ConfigStateData();
        
#if UNITY_EDITOR
        if (GameManager.HasInstance)
        {
            if (GameManager.Instance.sceneSettings != null)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(GameManager.Instance.sceneSettings);
                configState.activeSceneSettingsGUID = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            }
        }
#endif
        
        return configState;
    }
    
    private void ApplyDynamicData(DynamicGameData dynamicData)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = dynamicData.playerPosition;
            player.transform.rotation = dynamicData.playerRotation;
            
            if (player.TryGetComponent<PlayerHealth>(out var health))
            {
                health.currentHealth = dynamicData.playerHealth;
            }
        }
        
        if (AudioManager.HasInstance)
        {
            AudioListener.volume = dynamicData.masterVolume;
        }
    }
    
    private void ApplySceneDynamicData(SceneDynamicData[] sceneDynamicData)
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        var currentSceneData = System.Array.Find(sceneDynamicData, s => s.sceneName == currentSceneName);
        if (currentSceneData != null && !string.IsNullOrEmpty(currentSceneData.sceneDataJson))
        {
            ApplySceneObjectStates(currentSceneData.sceneDataJson);
        }
    }
    
    private void ApplySceneObjectStates(string sceneDataJson)
    {
        try
        {
            var sceneObjectData = JsonUtility.FromJson<SceneObjectData>(sceneDataJson);
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<IDynamicSaveable>();
            
            foreach (var saveable in saveables)
            {
                string key = saveable.GetDynamicSaveKey();
                if (sceneObjectData.objects.TryGetValue(key, out object data))
                {
                    saveable.LoadDynamicData(data);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to apply scene object states: {e.Message}");
        }
    }
    
    private void ApplyConfigState(ConfigStateData configState)
    {
        foreach (var kvp in configState.runtimeConfigOverrides)
        {
            // 应用运行时修改的配置值
        }
    }
    
    private float CalculateTotalExplorationTime()
    {
        return 0f; // 实现具体逻辑
    }
    
    private float CalculateAverageFPS()
    {
        return 1.0f / Time.deltaTime;
    }
    
    private byte[] CaptureScreenshot()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SaveManager] Cannot capture screenshot outside of play mode");
            return null;
        }
        
        try
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
            {
                Debug.LogWarning("[SaveManager] Screenshot texture is null");
                return null;
            }
            
            var bytes = texture.EncodeToPNG();
            DestroyImmediate(texture);
            return bytes;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to capture screenshot: {e.Message}");
            return null;
        }
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
        if (!isQuitting)
        {
            SaveGame(maxSaveSlots - 1, "自动保存");
        }
    }
    
    public void OnDayChanged(int newDay)
    {
        if (autoSave && !isQuitting)
        {
            AutoSave();
        }
    }
    
   
    
    // 应用退出时的清理
    protected override void OnSingletonApplicationQuit()
    {
        isQuitting = true;
        
        // 停止自动保存
        autoSave = false;
        
        // 执行最后一次简化保存
        try
        {
            if (GameManager.HasInstance)
            {
                PlayerPrefs.SetInt("LastDay", GameManager.Instance.CurrentDay);
                PlayerPrefs.SetString("LastPhase", GameManager.Instance.CurrentPhase.ToString());
                PlayerPrefs.Save();
                Debug.Log("[SaveManager] Emergency save completed");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Emergency save failed: {e.Message}");
        }
        
        Debug.Log("[SaveManager] Application quit cleanup completed");
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
public interface ISimpleSaveable
{
    string SaveKey { get; }
    string GetSaveData();
    void LoadSaveData(string data);
}
public partial class SaveManager
{
    [Header("自动收集设置")] 
    public bool enableAutoCollection = true;
    
    // 扩展现有CollectSaveData方法
   
    /*GameSaveData CollectSaveData()
    {
        var saveData = new GameSaveData();
        
        // 现有的静态数据收集保持不变...
        if (GameManager.Instance)
        {
            saveData.currentDay = GameManager.Instance.CurrentDay;
            saveData.currentPhase = GameManager.Instance.CurrentPhase;
            saveData.gameProgress = (float)saveData.currentDay / 5f;
        }
        
        if (FamilyManager.Instance)
        {
            saveData.food = FamilyManager.Instance.Food;
            saveData.water = FamilyManager.Instance.Water;
            saveData.medicine = FamilyManager.Instance.Medicine;
            saveData.familyMembers = FamilyManager.Instance.FamilyMembers
                .Select(ConvertFamilyMemberToSaveData).ToArray();
        }
        
        // 现有的其他数据收集...
        
        // 新增：动态数据收集
       
        
        return saveData;
    }*/
    // 新增场景数据收集方法
    private void CollectSceneData(GameSaveData saveData)
    {
        var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISimpleSaveable>();
        var sceneData = new Dictionary<string, string>();
        
        foreach (var saveable in saveables)
        {
            try
            {
                sceneData[saveable.SaveKey] = saveable.GetSaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to save {saveable.SaveKey}: {e.Message}");
            }
        }
        
        // 将场景数据序列化为JSON字符串
        if (sceneData.Count > 0)
        {
            saveData.sceneDataJson = JsonUtility.ToJson(new SceneDataWrapper { data = sceneData });
        }
    }
    
    
    
    // 新增场景数据应用方法
    private void ApplySceneData(string sceneDataJson)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<SceneDataWrapper>(sceneDataJson);
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISimpleSaveable>();
            
            foreach (var saveable in saveables)
            {
                if (wrapper.data.TryGetValue(saveable.SaveKey, out string data))
                {
                    saveable.LoadSaveData(data);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load scene data: {e.Message}");
        }
    }
}

public partial class GameSaveData
{
    [Header("场景数据")] // 添加到现有字段后
    public string sceneDataJson; // 场景中可保存组件的数据
    
    [Header("元数据")]
    public string screenshotPath; // 存档截图路径
    public float totalPlayTime;   // 总游戏时间
}

// 4. 场景数据包装类
[System.Serializable]
public class SceneDataWrapper
{
    public Dictionary<string, string> data = new();
}

// 5. 简单可保存组件
public class SaveableTransform : MonoBehaviour, ISimpleSaveable
{
    public string SaveKey => $"Transform_{gameObject.name}_{GetInstanceID()}";
    
    public string GetSaveData()
    {
        var data = new TransformData
        {
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale
        };
        return JsonUtility.ToJson(data);
    }
    
    public void LoadSaveData(string data)
    {
        try
        {
            var transformData = JsonUtility.FromJson<TransformData>(data);
            transform.position = transformData.position;
            transform.rotation = transformData.rotation;
            transform.localScale = transformData.scale;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load transform data: {e.Message}");
        }
    }
    
    [System.Serializable]
    private class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
}

// 6. 改进现有存档槽位信息显示
public partial class SaveManager
{
    // 扩展现有GetSaveSlotInfo方法
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
            
            var slotInfo = new SaveSlotInfo
            {
                slotIndex = slotIndex,
                isEmpty = false,
                saveName = saveData.saveName,
                saveTime = saveData.saveTime,
                currentDay = saveData.currentDay,
                gameProgress = saveData.gameProgress
            };
            
            // 新增：显示更多信息
            if (saveData.familyMembers != null)
            {
                int aliveCount = saveData.familyMembers.Count(m => m.health > 0);
                slotInfo.saveName += $" ({aliveCount}/3人存活)";
            }
            
            return slotInfo;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to read save slot {slotIndex}: {e.Message}");
            return new SaveSlotInfo { slotIndex = slotIndex, isEmpty = true, isCorrupted = true };
        }
    }
    
    // 新增：存档验证方法
    public bool ValidateSaveData(GameSaveData saveData)
    {
        if (saveData == null) return false;
        if (saveData.currentDay < 1 || saveData.currentDay > 5) return false;
        if (saveData.familyMembers == null || saveData.familyMembers.Length == 0) return false;
        
        return true;
    }
}
// 1. 扩展现有GameSaveData - 添加动态数据字段
public partial class GameSaveData
{
    [Header("动态运行时数据")] // 添加到现有字段后
    public DynamicGameData dynamicData;
    
    [Header("场景动态数据")]
    public SceneDynamicData[] sceneDynamicData;
    
    [Header("配置状态")]
    public ConfigStateData configState;
    
    [Header("存档元数据")]
    public SaveMetadata metadata;
}

// 2. 动态游戏数据结构
[System.Serializable]
public class DynamicGameData
{
    [Header("游戏进度动态数据")]
    public float currentPhaseElapsedTime; // 当前阶段已用时间
    public bool[] dayCompletionStatus = new bool[6]; // 每天完成状态
    public float totalExplorationTime; // 总探索时间
    
    [Header("UI状态数据")]
    public bool inventoryWasOpen;
    public bool journalWasOpen;
    public string lastOpenedJournalPage;
    public Vector2 lastUIScrollPosition;
    
    [Header("音频状态")]
    public string currentMusicTrack;
    public float currentMusicTime;
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    
    [Header("玩家状态")]
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public float playerHealth = 100f;
    public string currentWeapon = "Pistol";
    public int currentAmmo = 30;
    
    [Header("探索历史")]
    public List<string> visitedMaps = new();
    public Dictionary<string, int> mapVisitCount = new();
    public Dictionary<string, float> mapExplorationTime = new();
    
    [Header("性能数据")]
    public float averageFPS;
    public int totalLoadingCount;
    public float totalLoadingTime;
}

// 3. 场景动态数据
[System.Serializable]
public class SceneDynamicData
{
    public string sceneName;
    public string sceneDataJson; // 场景中的动态对象状态
    public float timeSpentInScene;
    public Vector3 lastPlayerPosition;
    public bool[] objectStates; // 可交互对象的状态（已拾取、已激活等）
}

// 4. 配置状态数据
[System.Serializable]
public class ConfigStateData
{
    public string activeSceneSettingsGUID;
    public string activeInputSettingsGUID;
    public string activeUITextSettingsGUID;
    public string activeGameValuesGUID;
    public string activeResourcePathsGUID;
    
    // 运行时修改的配置值
    public Dictionary<string, object> runtimeConfigOverrides = new();
}

// 5. 存档元数据
[System.Serializable]
public class SaveMetadata
{
    public string saveVersion = "1.0";
    public string gameVersion;
    public string unityVersion;
    public long saveTimestamp;
    public float totalPlayTime;
    public string lastSceneName;
    public byte[] screenshotData; // 存档截图
    public string platform;
    public bool isAutoSave;
    
    public static SaveMetadata CreateCurrent(bool isAutoSave = false)
    {
        return new SaveMetadata
        {
            gameVersion = Application.version,
            unityVersion = Application.unityVersion,
            saveTimestamp = System.DateTimeOffset.Now.ToUnixTimeSeconds(),
            totalPlayTime = Time.time,
            lastSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            platform = Application.platform.ToString(),
            isAutoSave = isAutoSave
        };
    }
}

// 6. 动态数据收集器接口
public interface IDynamicSaveable
{
    string GetDynamicSaveKey();
    object GetDynamicData();
    void LoadDynamicData(object data);
    int GetSavePriority(); // 0=最高优先级
}

// 7. 扩展现有SaveManager - 添加动态数据收集


// 8. 场景对象数据包装类
[System.Serializable]
public class SceneObjectData
{
    public Dictionary<string, object> objects = new();
}

// 9. 动态数据保存组件示例
public class DynamicPickupItem : PickupItem, IDynamicSaveable
{
    [Header("动态保存设置")]
    public bool saveWhenPickedUp = true;
    
    private bool wasPickedUp = false;
    
    public string GetDynamicSaveKey()
    {
        return $"PickupItem_{gameObject.name}_{transform.position.GetHashCode()}";
    }
    
    public object GetDynamicData()
    {
        return new PickupItemDynamicData
        {
            wasPickedUp = wasPickedUp,
            remainingQuantity = quantity,
            position = transform.position
        };
    }
    
    public void LoadDynamicData(object data)
    {
        if (data is PickupItemDynamicData pickupData)
        {
            wasPickedUp = pickupData.wasPickedUp;
            quantity = pickupData.remainingQuantity;
            
            if (wasPickedUp)
            {
                gameObject.SetActive(false);
            }
        }
    }
    
    public int GetSavePriority()
    {
        return 100; // 低优先级
    }
    
    [System.Serializable]
    private class PickupItemDynamicData
    {
        public bool wasPickedUp;
        public int remainingQuantity;
        public Vector3 position;
    }
}
