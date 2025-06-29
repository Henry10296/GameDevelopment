using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : Singleton<MapManager>
{
    [Header("地图数据")]
    public MapData[] availableMaps;
    
    [Header("解锁状态")]
    public List<string> unlockedMapNames = new List<string>();
    
    [Header("事件")]
    public GameEvent onMapUnlocked;
    public GameEvent onMapDataChanged;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeMaps();
    }
    
    void Start()
    {
        LoadUnlockStates();
    }
    
    void InitializeMaps()
    {
        if (availableMaps == null || availableMaps.Length == 0)
        {
            CreateDefaultMaps();
        }
        
        // 初始化解锁状态
        foreach (var map in availableMaps)
        {
            if (map.unlockDay <= 1)
            {
                UnlockMap(map.mapName);
            }
        }
    }
    
    void CreateDefaultMaps()//未创建
    {
        Debug.Log("[MapManager] Creating default map configuration");
        
        // 这里可以创建默认的地图配置
        // 实际项目中应该在Inspector中配置
    }
    
    public MapData[] GetAvailableMaps()//返回地图
    {
        List<MapData> available = new List<MapData>();
        
        foreach (var map in availableMaps)
        {
            if (IsMapUnlocked(map.mapName))
            {
                available.Add(map);
            }
        }
        
        return available.ToArray();
    }
    
    public MapData GetMapByName(string mapName)
    {
        foreach (var map in availableMaps)
        {
            if (map.mapName == mapName)
                return map;
        }
        
        Debug.LogWarning($"[MapManager] Map not found: {mapName}");
        return null;
    }
    
    public MapData GetMapBySceneName(string sceneName)
    {
        foreach (var map in availableMaps)
        {
            if (map.sceneName == sceneName)
                return map;
        }
        
        Debug.LogWarning($"[MapManager] Map not found for scene: {sceneName}");
        return null;
    }
    
    public bool IsMapUnlocked(string mapName)//判断地图解锁
    {
        return unlockedMapNames.Contains(mapName);
    }
    
    public void UnlockMap(string mapName)//解锁
    {
        if (!unlockedMapNames.Contains(mapName))
        {
            unlockedMapNames.Add(mapName);
            
            MapData map = GetMapByName(mapName);
            if (map != null)
            {
                map.isUnlocked = true;
                
                onMapUnlocked?.Raise();
                
                // 记录到日志
                if (JournalManager.Instance)
                {
                    JournalManager.Instance.AddEntry($"新区域解锁", 
                        $"现在可以探索{mapName}了。{map.description}", 
                        JournalEntryType.Important);
                }
                
                Debug.Log($"[MapManager] Unlocked map: {mapName}");
            }
        }
    }
    
    public void CheckDayUnlocks(int currentDay)
    {
        foreach (var map in availableMaps)
        {
            if (map.unlockDay == currentDay && !IsMapUnlocked(map.mapName))
            {
                UnlockMap(map.mapName);
            }
        }
    }
    
    public MapSelectionInfo[] GetMapSelectionData()
    {
        List<MapSelectionInfo> mapInfos = new List<MapSelectionInfo>();
        
        foreach (var map in availableMaps)
        {
            mapInfos.Add(new MapSelectionInfo
            {
                mapData = map,
                isUnlocked = IsMapUnlocked(map.mapName),
                riskDescription = GetRiskDescription(map.riskLevel),
                recommendedGear = GetRecommendedGear(map.riskLevel)
            });
        }
        
        return mapInfos.ToArray();
    }
    
    string GetRiskDescription(int riskLevel)
    {
        return riskLevel switch
        {
            1 => "安全 - 适合新手探索",
            2 => "低风险 - 少量敌人",
            3 => "中等风险 - 需要谨慎",
            4 => "高风险 - 建议携带武器",
            5 => "极度危险 - 做好战斗准备",
            _ => "未知风险"
        };
    }
    
    string GetRecommendedGear(int riskLevel)
    {
        return riskLevel switch
        {
            1 => "无特殊要求",
            2 => "建议携带手枪",
            3 => "建议携带武器和药品",
            4 => "建议携带自动武器",
            5 => "建议满装备出发",
            _ => ""
        };
    }
    
    public void SaveUnlockStates()//保存
    {
        string unlockedMapsJson = JsonUtility.ToJson(new SerializableStringList(unlockedMapNames));
        PlayerPrefs.SetString("UnlockedMaps", unlockedMapsJson);
        PlayerPrefs.Save();
    }
    
    public void LoadUnlockStates()
    {
        if (PlayerPrefs.HasKey("UnlockedMaps"))
        {
            string json = PlayerPrefs.GetString("UnlockedMaps");
            SerializableStringList loadedList = JsonUtility.FromJson<SerializableStringList>(json);
            
            if (loadedList?.items != null)
            {
                unlockedMapNames = new List<string>(loadedList.items);
                
                // 更新地图数据的解锁状态
                foreach (var map in availableMaps)
                {
                    map.isUnlocked = IsMapUnlocked(map.mapName);
                }
                
                onMapDataChanged?.Raise();
            }
        }
    }
    
    public void ResetMapProgress()
    {
        unlockedMapNames.Clear();
        
        // 重新解锁初始地图
        foreach (var map in availableMaps)
        {
            map.isUnlocked = map.unlockDay <= 1;
            if (map.isUnlocked)
            {
                unlockedMapNames.Add(map.mapName);
            }
        }
        
        onMapDataChanged?.Raise();
    }
    
    protected override void OnDestroy()
    {
        SaveUnlockStates();
        base.OnDestroy();
    }
}

[System.Serializable]
public class MapSelectionInfo
{
    public MapData mapData;
    public bool isUnlocked;
    public string riskDescription;
    public string recommendedGear;
}

[System.Serializable]
public class SerializableStringList
{
    public string[] items;
    
    public SerializableStringList(List<string> list)
    {
        items = list.ToArray();
    }
}
