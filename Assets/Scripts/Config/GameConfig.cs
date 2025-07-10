using UnityEngine;
using System.Collections.Generic;


#region 动态数据



#endregion






#region 静态数据

#endregion


/// <summary>
/// 游戏配置：
/// </summary>
public enum DifficultyLevel{
    Easy = 0,
    Normal = 1,
    Difficult = 2,

}
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("核心游戏设置")]
    public int maxDays = 5;
    public float explorationTimeLimit = 900f;
    public float timeWarningThreshold = 150f;
    public DifficultyLevel currentDifficulty = DifficultyLevel.Normal;
    
    [Header("配置引用 - 主要配置文件")]
    [SerializeField] private BaseGameConfig[] configModules; // 所有配置模块
    
    private Dictionary<System.Type, BaseGameConfig> configRegistry;
    
    protected virtual void OnEnable()
    {
        BuildConfigRegistry();
        ValidateConfigs();
    }
    
    void BuildConfigRegistry()
    {
        configRegistry = new Dictionary<System.Type, BaseGameConfig>();
        
        // 自动收集配置模块
        if (configModules != null)
        {
            foreach (var config in configModules)
            {
                if (config != null)
                {
                    configRegistry[config.GetType()] = config;
                }
            }
        }
        
        // 从Resources自动加载缺失的配置
        AutoLoadMissingConfigs();
    }
    
    void AutoLoadMissingConfigs()
    {
        // 自动加载标准配置类型
        TryLoadConfig<DifficultyConfig>("Configs/DifficultyConfig");
        TryLoadConfig<FamilyConfig>("Configs/FamilyConfig");
        TryLoadConfig<WeaponConfig>("Configs/WeaponConfig");
        TryLoadConfig<ItemSystemConfig>("Configs/ItemConfig");
        TryLoadConfig<AudioSystemConfig>("Configs/AudioConfig");
        TryLoadConfig<UISystemConfig>("Configs/UIConfig");
    }
    
    void TryLoadConfig<T>(string resourcePath) where T : BaseGameConfig
    {
        if (!configRegistry.ContainsKey(typeof(T)))
        {
            var config = Resources.Load<T>(resourcePath);
            if (config != null)
            {
                configRegistry[typeof(T)] = config;
            }
        }
    }
    
    // 泛型配置获取方法
    public T GetConfig<T>() where T : BaseGameConfig
    {
        configRegistry.TryGetValue(typeof(T), out BaseGameConfig config);
        return config as T;
    }
    
    // 兼容性方法 - 保持现有代码工作
    public DifficultyConfig GetCurrentDifficulty() => GetConfig<DifficultyConfig>();
    public FamilyConfig Family => GetConfig<FamilyConfig>();
    public WeaponConfig Weapon => GetConfig<WeaponConfig>();
    public ItemSystemConfig Item => GetConfig<ItemSystemConfig>();
    public AudioSystemConfig Audio => GetConfig<AudioSystemConfig>();
    public UISystemConfig UI => GetConfig<UISystemConfig>();
    
    void ValidateConfigs()
    {
        foreach (var config in configRegistry.Values)
        {
            if (config != null)
            {
                config.ValidateConfig();
            }
        }
    }
    
    // 设置配置
    public void SetConfig<T>(T config) where T : BaseGameConfig
    {
        if (config != null)
        {
            configRegistry[typeof(T)] = config;
        }
    }
}

[System.Serializable]
public class DifficultyConfig: BaseGameConfig
{
    [Header("难度信息")]
    public DifficultyLevel difficulty;
    public string difficultyName;
    public string description;
    
    [Header("家庭消耗")]
    public float hungerMultiplier = 1f;
    public float thirstMultiplier = 1f;
    public float sicknessProbability = 0.1f;
    
    [Header("敌人强度")]
    public float enemyHealthMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;
    public float enemySpawnChanceMultiplier = 1f;
    
    [Header("资源稀缺度")]
    public float lootSpawnMultiplier = 1f;
    public float resourceConsumptionMultiplier = 1f;
    
    [Header("时间限制")]
    public float explorationTimeMultiplier = 1f;
}

[System.Serializable]
public class FamilyConfig: BaseGameConfig
{
    [Header("基础消耗")]
    public int dailyFoodConsumption = 3;
    public int dailyWaterConsumption = 3;
    
    [Header("惩罚设置")]
    public float hungerDamageRate = 30f;
    public float thirstDamageRate = 30f;
    
    [Header("疾病系统")]
    public float sicknessProbability = 0.1f;
    public int maxSicknessDays = 3;
    [Header("初始资源")]
    public int initialFood = 15;
    public int initialWater = 15;
    public int initialMedicine = 2;
    //[Header("初始道具")]

}

public enum WeaponType
{
    Pistol=1,
    Rifle=2,
    Knife
}
/*[System.Serializable]
public class WeaponSystemConfig
{
    [Header("武器数据")]
    public WeaponData pistol;
    public WeaponData rifle;
    
    [Header("通用设置")]
    public float reloadTime = 2f;
    public float weaponSwitchTime = 1f;
    
    [Header("音效")]
    public AudioClip reloadSound;
    public AudioClip emptyclipSound;
    public AudioClip weaponSwitchSound;
    public AudioClip[] gunSounds;
    
    [Header("视觉效果")]
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTrailPrefab;
    public GameObject hitEffectPrefab;//击中效果
    
    [Header("UI设置")]
    public Sprite crosshairDefault;
    public Sprite crosshairAiming;
    public Color crosshairColor = Color.white;
    
    public AudioClip GetRandomGunSound()
    {
        if (gunSounds == null || gunSounds.Length == 0) return null;
        return gunSounds[Random.Range(0, gunSounds.Length)];
    }
}*/

[System.Serializable]
public class ItemSystemConfig: BaseGameConfig  
{
    [Header("物品数据")]
    public ItemData[] allItems;
    
    [Header("背包设置")]
    public int maxInventorySlots = 9;
    public int maxStackSize = 99;
    
    [Header("拾取设置")]
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.F;
    public AudioClip pickupSound;
    
    [Header("使用设置")]
    public float useDelay = 0.5f;
    public AudioClip useSound;
    
    [Header("特殊物品")]
    public ItemData radioItem;
    public ItemData[] keyItems;
    
    public ItemData GetItemByName(string itemName)
    {
        foreach (var item in allItems)
        {
            if (item.itemName == itemName)
                return item;
        }
        return null;
    }
    
    public ItemData[] GetItemsByType(ItemType itemType)
    {
        List<ItemData> items = new List<ItemData>();
        foreach (var item in allItems)
        {
            if (item.itemType == itemType)
                items.Add(item);
        }
        return items.ToArray();
    }
}

[System.Serializable]
public class MapSystemConfig
{
    [Header("可用地图")]
    public MapData[] availableMaps;
    
    [Header("地图生成设置")]//给探索用的
    public int minLootSpawns = 30;
    public int maxLootSpawns = 90;
    public int minEnemySpawns = 1;
    public int maxEnemySpawns = 5;
    
    [Header("解锁系统")]
    public bool useMapUnlockSystem = true;
    public MapUnlockRule[] unlockRules;
    
    [Header("特殊地图")]
    public MapData tutorialMap;//教程
    public MapData finalMap;//最终
    
    public MapData[] GetUnlockedMaps(int currentDay)//解锁
    {
        List<MapData> unlockedMaps = new List<MapData>();
        
        foreach (var map in availableMaps)
        {
            if (IsMapUnlocked(map, currentDay))
            {
                unlockedMaps.Add(map);
            }
        }
        
        return unlockedMaps.ToArray();
    }
    
    bool IsMapUnlocked(MapData map, int currentDay)//是否解锁
    {
        if (!useMapUnlockSystem) return true;
        
        return currentDay >= map.unlockDay;
    }
}

[System.Serializable]
public class MapUnlockRule//地图
{
    public string mapName;
    //条件
    public int requiredDay;
    public string[] requiredItems;
    public bool[] requiredEvents;
}

[System.Serializable]
public class EventSystemConfig//事件
{
    [Header("事件数据")]
    public RandomEvent[] allEvents;
    
    [Header("事件生成设置")]
    public float baseEventChance = 0.3f;
    public int maxEventsPerDay = 2;
    public bool allowEventChaining = true;
    
    [Header("事件类型权重")]
    public EventTypeWeight[] eventTypeWeights;
    
    [Header("UI设置")]
    public float eventDisplayDuration = 0.5f;
    public AudioClip eventSound;
    public Color[] priorityColors;
    
    public RandomEvent[] GetEventsForDay(int day)
    {
        List<RandomEvent> dayEvents = new List<RandomEvent>();
        
        foreach (var eventData in allEvents)
        {
            if (eventData.minDay <= day && day <= eventData.maxDay)
            {
                dayEvents.Add(eventData);
            }
        }
        
        return dayEvents.ToArray();
    }
    
    public RandomEvent GetEventByName(string eventName)
    {
        foreach (var eventData in allEvents)
        {
            if (eventData.eventName == eventName)
                return eventData;
        }
        return null;
    }
}

[System.Serializable]
public class EventTypeWeight//事件权重
{
    public EventType eventType;
    public float weight = 1f;
}

[System.Serializable]
public class AudioSystemConfig:BaseGameConfig//TODO:音乐配置的话可以读取
{
    [Header("主音乐")]
    public AudioClip menuMusic;
    public AudioClip homeMusic;
    public AudioClip explorationMusic;
    public AudioClip tensionMusic;
    public AudioClip endingMusic;
    
    [Header("环境音效")]
    public AudioClip[] ambientSounds;
    public AudioClip[] weatherSounds;
    
    [Header("UI音效")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    public AudioClip errorSound;
    public AudioClip successSound;
    
    [Header("游戏音效")]
    public AudioClip[] footstepSounds;
    public AudioClip[] doorSounds;
    public AudioClip[] pickupSounds;
    
    [Header("音频设置")]
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    public float voiceVolume = 1f;
    public float masterVolume = 1f;
    
    [Header("音频切换")]
    public float musicFadeTime = 2f;
    public bool enableDynamicMusic = true;
}

[System.Serializable]
public class UISystemConfig:BaseGameConfig//UI
{
    [Header("界面切换")]
    public float fadeSpeed = 2f;//消失时间
    public float transitionDuration = 0.5f;//切换
    
    [Header("消息系统")]
    public float defaultMessageDuration = 3f;
    public Color messageColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;
    
    [Header("交互提示")]
    public Color interactionColor = Color.cyan;
    public float interactionDistance = 3f;
    public string interactionPromptFormat = "按 {0} 交互";
    
    [Header("健康显示")]//血条的颜色
    public Color healthyColor = Color.green;
    public Color warningHealthColor = Color.yellow;
    public Color criticalHealthColor = Color.red;
    public float healthWarningThreshold = 0.3f;
    public float healthCriticalThreshold = 0.15f;
    
    [Header("资源显示")]
    public Color abundantResourceColor = Color.green;
    public Color normalResourceColor = Color.white;
    public Color lowResourceColor = Color.yellow;
    public Color criticalResourceColor = Color.red;
    public int resourceWarningThreshold = 5;
    public int resourceCriticalThreshold = 2;
    
    [Header("时间显示")]
    public Color normalTimeColor = Color.white;
    public Color warningTimeColor = Color.yellow;
    public Color criticalTimeColor = Color.red;
    public float timeWarningRatio = 0.3f; // 剩余30%时警告
    public float timeCriticalRatio = 0.1f; // 剩余10%时危险
}
