using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExplorationManager : Singleton<ExplorationManager>
{
    [Header("地图配置")]
    public MapData[] availableMaps;
    public MapData currentMap;
    
    [Header("生成配置")]
    public Transform[] lootSpawnPoints;
    public Transform[] enemySpawnPoints;
    public Transform playerSpawnPoint;
    public Transform exitPoint;
    
    [Header("预制体")]//TODO:随机生成的敌人和物品，需要特定的生成和做好的敌人预制体和物品
    public GameObject lootItemPrefab;
    public GameObject[] enemyPrefabs;
    
    [Header("事件")]
    public GameEvent onMapLoaded;
    public GameEvent onExplorationStarted;
    public GameEvent onPlayerReachedExit;
    
    private List<GameObject> spawnedObjects = new();
    private bool explorationActive = false;
    private int selectedMapIndex = -1;//TODO:绑定到地图按钮
    
    // 属性访问器
    public MapData CurrentMap => currentMap;
    public bool ExplorationActive => explorationActive;
    public Vector3 ExitPosition => exitPoint ? exitPoint.position : Vector3.zero;
    
    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
    }
    
    void Start()
    {
        SubscribeToEvents();
    }
    protected override void OnSingletonApplicationQuit()
    {
        // 结束探索
        explorationActive = false;
    
        // 清理生成的对象列表（不销毁对象，让Unity处理）
        spawnedObjects?.Clear();
    
        // 清理事件
        onMapLoaded = null;
        onExplorationStarted = null;
        onPlayerReachedExit = null;
    
        Debug.Log("[ExplorationManager] Application quit cleanup completed");
    }
    void SubscribeToEvents()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.onPhaseChanged.RegisterListener(
                GetComponent<GameEventListener>());
        }
    }
    
    void ValidateConfiguration()//验证配置的
    {
        if (availableMaps.Length == 0)
        {
            Debug.LogError("[ExplorationManager] No maps configured!");
        }
        
        if (lootSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[ExplorationManager] No loot spawn points configured!");
        }
        
        if (enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[ExplorationManager] No enemy spawn points configured!");
        }
    }
    
    public void SetSelectedMap(int mapIndex)//选中地图
    {
        if (mapIndex >= 0 && mapIndex < availableMaps.Length)
        {
            selectedMapIndex = mapIndex;
            currentMap = availableMaps[mapIndex];
            
            Debug.Log($"[ExplorationManager] Selected map: {currentMap.mapName}");
        }
        else
        {
            Debug.LogError($"[ExplorationManager] Invalid map index: {mapIndex}");
        }
    }
    
    public void InitializeExploration()//TODO：加载界面，生成随即物品和敌人
    {
        if (currentMap == null)
        {
            Debug.LogError("[ExplorationManager] No map selected for exploration!");
            return;
        }
        
        explorationActive = true;
        ClearPreviousSpawns();
        
        SpawnLoot();
        SpawnEnemies();
        SpawnPlayer();
        SetupExit();
        
        onMapLoaded?.Raise();
        onExplorationStarted?.Raise();
        
        // 记录探索开始
        JournalManager.Instance?.AddEntry($"探索{currentMap.mapName}", 
            $"开始探索{currentMap.mapName}。{currentMap.explorationTips}");
        
        Debug.Log($"[ExplorationManager] Exploration initialized for {currentMap.mapName}");
    }
    
    void ClearPreviousSpawns()//只清楚原来的
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }
    
    void SpawnLoot()//TODO:每个不一样
    {
        SpawnLootFromTable(currentMap.commonLoot, "普通");
        SpawnLootFromTable(currentMap.rareLoot, "稀有");
        SpawnLootFromTable(currentMap.specialLoot, "特殊");
        
        // 特殊物品生成
        if (currentMap.containsAutoRifle)
        {
            SpawnSpecialItem("AutoRifle");
        }
        
        if (currentMap.containsRadioClue)
        {
            SpawnSpecialItem("RadioClue");
        }
        
        if (currentMap.containsRadioStation)
        {
            SpawnSpecialItem("RadioStation");
        }
    }
    
    void SpawnLootFromTable(LootTable[] lootTable, string rarity)
    {
        List<Transform> availablePoints = new List<Transform>(lootSpawnPoints);
        
        foreach (var loot in lootTable)
        {
            if (availablePoints.Count == 0) break;
            
            bool shouldSpawn = loot.guaranteedSpawn || Random.Range(0f, 1f) < loot.spawnChance;
            if (!shouldSpawn) continue;
            
            Transform spawnPoint = availablePoints[Random.Range(0, availablePoints.Count)];
            availablePoints.Remove(spawnPoint);
            
            int quantity = Random.Range(loot.minQuantity, loot.maxQuantity + 1);
            GameObject lootObj = CreateLootObject(loot.item, quantity, spawnPoint.position);
            
            if (lootObj != null)
            {
                spawnedObjects.Add(lootObj);
                Debug.Log($"[ExplorationManager] Spawned {rarity} loot: {loot.item.itemName} x{quantity}");
            }
        }
    }
    
    GameObject CreateLootObject(ItemData itemData, int quantity, Vector3 position)
    {
        if (lootItemPrefab == null)
        {
            Debug.LogError("[ExplorationManager] Loot item prefab not assigned!");
            return null;
        }
        
        GameObject lootObj = Instantiate(lootItemPrefab, position, Quaternion.identity);
        
        if (lootObj.TryGetComponent<PickupItem>(out var pickup))
        {
            pickup.itemData = itemData;
            pickup.quantity = quantity;
        }
        
        return lootObj;
    }
    
    void SpawnSpecialItem(string itemType)//TODO:
    {
        if (lootSpawnPoints.Length == 0) return;
        
        Transform spawnPoint = lootSpawnPoints[Random.Range(0, lootSpawnPoints.Length)];
        
        switch (itemType)
        {
            case "AutoRifle":
                // 生成自动步枪
                break;
            case "RadioClue":
                // 生成无线电线索
                break;
            case "RadioStation":
                // 生成无线电台
                break;
        }
    }
    
    void SpawnEnemies()
    {
        List<Transform> availablePoints = new List<Transform>(enemySpawnPoints);
        
        foreach (var enemyConfig in currentMap.enemySpawns)
        {
            if (availablePoints.Count == 0) break;
            
            if (Random.Range(0f, 1f) > enemyConfig.spawnChance) continue;
            
            int enemyCount = Random.Range(enemyConfig.minCount, enemyConfig.maxCount + 1);
            
            for (int i = 0; i < enemyCount && availablePoints.Count > 0; i++)
            {
                Transform spawnPoint = availablePoints[Random.Range(0, availablePoints.Count)];
                availablePoints.Remove(spawnPoint);
                
                GameObject enemy = Instantiate(enemyConfig.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedObjects.Add(enemy);
                
                Debug.Log($"[ExplorationManager] Spawned enemy: {enemyConfig.enemyPrefab.name}");
            }
        }
    }
    
    void SpawnPlayer()
    {
        if (playerSpawnPoint == null)
        {
            Debug.LogError("[ExplorationManager] Player spawn point not assigned!");
            return;
        }
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerSpawnPoint.position;
            player.transform.rotation = playerSpawnPoint.rotation;
            
            // 重置玩家状态
            if (player.TryGetComponent<PlayerController>(out var controller))
            {
                // 重置控制器状态
            }
        }
        else
        {
            Debug.LogWarning("[ExplorationManager] Player not found in scene!");
        }
    }
    
    void SetupExit()//退出功能
    {
        if (exitPoint == null)
        {
            Debug.LogError("[ExplorationManager] Exit point not assigned!");
            return;
        }
        
        // 可以在出口点添加视觉指示器
        // 例如发光效果、UI指示器等
    }
    
    public void OnPlayerReachedExit()//返回
    {
        if (!explorationActive) return;
        
        explorationActive = false;
        onPlayerReachedExit?.Raise();
        
        // 让GameStateManager处理返回家庭
        if (GameManager.Instance)
            GameManager.Instance.ReturnHomeFromExploration();
        
        Debug.Log("[ExplorationManager] Player reached exit, returning home");
    }
    
    public void EndExploration()//刷新状态
    {
        explorationActive = false;
        currentMap = null;
        selectedMapIndex = -1;
        
        Debug.Log("[ExplorationManager] Exploration ended");
    }
    
    // 获取探索统计
    public ExplorationStats GetExplorationStats()
    {
        return new ExplorationStats
        {
            mapName = currentMap?.mapName ?? "未知",
            itemsCollected = InventoryManager.Instance?.GetItems().Sum(i => i.quantity) ?? 0,
            //timeRemaining = GameManager.Instance?.RemainingTime ?? 0f,
            enemiesEncountered = GetEnemiesInArea()
        };
    }
    
    int GetEnemiesInArea()
    {
        return spawnedObjects.Count(obj => obj != null && obj.GetComponent<EnemyAI>() != null);
    }
    
}

[System.Serializable]
public class ExplorationStats
{
    public string mapName;
    public int itemsCollected;
    public float timeRemaining;
    public int enemiesEncountered;
}
