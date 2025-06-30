using UnityEngine;
using System.Collections.Generic;

public enum DifficultyLevel{
    Easy = 0,
    Normal = 1,
    Difficult = 2,

}
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("游戏基础设置")]
    public int maxDays = 5;
    public float explorationTimeLimit = 900f; // 15分钟
    public float timeWarningThreshold = 150f;  // 5分钟警告
    
    [Header("难度设置")]
    public DifficultyLevel currentDifficulty = DifficultyLevel.Normal;
    public DifficultyConfig[] difficultyConfigs;
    
    [Header("家庭系统配置")]
    public FamilyConfig familyConfig;
    
    [Header("敌人配置")]
    public EnemyConfig[] enemyConfigs;
    
    [Header("武器配置")]
    public WeaponSystemConfig weaponConfig;
    
    [Header("物品配置")]
    public ItemSystemConfig itemConfig;
    
    [Header("地图配置")]
    public MapSystemConfig mapConfig;
    
    [Header("事件配置")]
    public EventSystemConfig eventConfig;
    
    [Header("音频配置")]
    public AudioSystemConfig audioConfig;
    
    [Header("UI配置")]
    public UISystemConfig uiConfig;
    
    // 便捷访问方法
    public DifficultyConfig GetCurrentDifficulty()
    {
        foreach (var config in difficultyConfigs)
        {
            if (config.difficulty == currentDifficulty)
                return config;
        }
        return difficultyConfigs[0]; // 默认返回第一个
    }
    
    public EnemyConfig GetEnemyConfig(EnemyType enemyType)
    {
        foreach (var config in enemyConfigs)
        {
            if (config.enemyType == enemyType)
                return config;
        }
        return null;
    }
    
    public WeaponData GetWeaponData(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Pistol => weaponConfig.pistol,
            WeaponType.Rifle => weaponConfig.rifle,
            _ => weaponConfig.pistol
        };
    }
    
    public MapData GetMapData(string mapName)
    {
        foreach (var map in mapConfig.availableMaps)
        {
            if (map.mapName == mapName)
                return map;
        }
        return null;
    }
}

[System.Serializable]
public class DifficultyConfig
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
public class FamilyConfig
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
    
    /*
    [Header("心情系统")]
    public float moodDecayRate = 5f;
    public float moodRecoveryRate = 3f;
    
    */
    [Header("初始资源")]
    public int initialFood = 15;
    public int initialWater = 15;
    public int initialMedicine = 2;
    /*[Header("初始道具")]
    public int initialResource = 20;*/
}

public enum WeaponType
{
    Pistol=1,
    Rifle=2,
}
[System.Serializable]
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
}

[System.Serializable]
public class ItemSystemConfig
{
    [Header("物品数据")]
    public ItemData[] allItems;
    
    [Header("背包设置")]
    public int maxInventorySlots = 9;
    public int maxStackSize = 99;
    
    [Header("拾取设置")]
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;
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
    
    [Header("地图生成设置")]
    public int minLootSpawns = 30;
    public int maxLootSpawns = 200;
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
public class MapUnlockRule
{
    public string mapName;
    //条件
    public int requiredDay;
    public string[] requiredItems;
    public bool[] requiredEvents;
}

[System.Serializable]
public class EventSystemConfig
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
public class EventTypeWeight
{
    public EventType eventType;
    public float weight = 1f;
}

[System.Serializable]
public class AudioSystemConfig
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
public class UISystemConfig
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
// 1. 场景配置SO - 替代GameManager中的硬编码数组
[CreateAssetMenu(fileName = "SceneSettings", menuName = "Game/Scene Settings")]
public class SceneSettings : ScriptableObject
{
    [System.Serializable]
    public class SceneData
    {
        public GamePhase gamePhase;
        public string sceneName;
        public string displayName;
        public Sprite scenePreview;
    }
    
    [Header("游戏场景配置")]
    public SceneData[] gameScenes = new SceneData[]
    {
        new() { gamePhase = GamePhase.MainMenu, sceneName = "0_MainMenu", displayName = "主菜单" },
        new() { gamePhase = GamePhase.Home, sceneName = "1_Home", displayName = "家庭" },
        new() { gamePhase = GamePhase.MapSelection, sceneName = "1_Home", displayName = "地图选择" }
    };
    
    [Header("探索场景配置")]
    public string[] explorationScenes = new string[]
    {
        "2_Hospital",
        "3_School", 
        "4_Supermarket",
        "5_Park"
    };
    
    public string GetSceneName(GamePhase phase)
    {
        var scene = System.Array.Find(gameScenes, s => s.gamePhase == phase);
        return scene?.sceneName ?? "";
    }
    
    public string GetExplorationScene(int index)
    {
        return index >= 0 && index < explorationScenes.Length ? explorationScenes[index] : "";
    }
}

// 2. 输入配置SO - 替代代码中的硬编码按键
[CreateAssetMenu(fileName = "InputSettings", menuName = "Game/Input Settings")]
public class InputSettings : ScriptableObject
{
    [Header("交互按键")]
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode useItemKey = KeyCode.E;
    
    [Header("UI按键")]
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode journalKey = KeyCode.J;
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Header("武器按键")]
    public KeyCode reloadKey = KeyCode.R;
    public KeyCode weapon1Key = KeyCode.Alpha1;
    public KeyCode weapon2Key = KeyCode.Alpha2;
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode aimKey = KeyCode.Mouse1;
    
    [Header("移动按键")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode jumpKey = KeyCode.Space;
    
    [Header("调试按键")]
    public KeyCode debugNextDay = KeyCode.F1;
    public KeyCode debugEndGame = KeyCode.F2;
    public KeyCode debugAddResources = KeyCode.F3;
    public KeyCode debugFindRadio = KeyCode.F4;
}

// 3. UI文本配置SO - 替代代码中的硬编码字符串
[CreateAssetMenu(fileName = "UITextSettings", menuName = "Game/UI Text Settings")]
public class UITextSettings : ScriptableObject
{
    [System.Serializable]
    public class TextEntry
    {
        public string key;
        [TextArea(1, 3)]
        public string text;
    }
    
    [Header("交互提示文本")]
    public TextEntry[] interactionTexts = new TextEntry[]
    {
        new() { key = "PICKUP_PROMPT", text = "按 {0} 拾取" },
        new() { key = "INTERACT_PROMPT", text = "按 {0} 交互" },
        new() { key = "USE_PROMPT", text = "按 {0} 使用" }
    };
    
    [Header("系统消息文本")]
    public TextEntry[] systemMessages = new TextEntry[]
    {
        new() { key = "INVENTORY_FULL", text = "背包已满!" },
        new() { key = "TIME_WARNING", text = "时间不多了！赶紧回家！" },
        new() { key = "RADIO_FOUND", text = "找到了无线电设备!" },
        new() { key = "GAME_SAVED", text = "游戏已保存" },
        new() { key = "GAME_LOADED", text = "游戏已加载" },
        new() { key = "NO_AMMO", text = "没有弹药了!" }
    };
    
    [Header("成就文本")]
    public TextEntry[] achievementTexts = new TextEntry[]
    {
        new() { key = "RADIO_FINDER", text = "无线电专家" },
        new() { key = "COLLECTOR", text = "收集者" },
        new() { key = "SURVIVOR_3_DAYS", text = "三日生存者" }
    };
    
    private Dictionary<string, string> textDict;
    
    void OnEnable()
    {
        BuildTextDictionary();
    }
    
    void BuildTextDictionary()
    {
        textDict = new Dictionary<string, string>();
        
        AddTextsToDict(interactionTexts);
        AddTextsToDict(systemMessages);
        AddTextsToDict(achievementTexts);
    }
    
    void AddTextsToDict(TextEntry[] entries)
    {
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.key))
                textDict[entry.key] = entry.text;
        }
    }
    
    public string GetText(string key, params object[] args)
    {
        if (textDict == null) BuildTextDictionary();
        
        if (textDict.TryGetValue(key, out string text))
        {
            return args.Length > 0 ? string.Format(text, args) : text;
        }
        
        return $"[Missing: {key}]";
    }
}

// 4. 数值配置SO - 替代代码中的魔术数字
[CreateAssetMenu(fileName = "GameValues", menuName = "Game/Game Values")]
public class GameValues : ScriptableObject
{
    [Header("时间配置")]
    public float explorationTimeLimit = 900f; // 15分钟
    public float timeWarningThreshold = 300f;  // 5分钟警告
    public float autoSaveInterval = 60f;       // 自动保存间隔
    
    [Header("家庭配置数值")]
    public int dailyFoodConsumption = 3;
    public int dailyWaterConsumption = 3;
    public float hungerDamageRate = 30f;
    public float thirstDamageRate = 30f;
    public float sicknessProbability = 0.1f;
    public int maxSicknessDays = 3;
    
    [Header("背包配置")]
    public int maxInventorySlots = 9;
    public int maxStackSize = 99;
    public float pickupRange = 2f;
    
    [Header("UI配置")]
    public float defaultMessageDuration = 3f;
    public float fadeSpeed = 2f;
    public float transitionDuration = 0.5f;
    
    [Header("音频配置")]
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    public float musicFadeTime = 2f;
}

// 5. 资源路径配置SO - 替代代码中的硬编码路径
[CreateAssetMenu(fileName = "ResourcePaths", menuName = "Game/Resource Paths")]
public class ResourcePaths : ScriptableObject
{
    [Header("音频资源路径")]
    public string audioBasePath = "Audio/";
    public string musicPath = "Audio/Music/";
    public string sfxPath = "Audio/SFX/";
    public string voicePath = "Audio/Voice/";
    
    [Header("预制体路径")]
    public string uiPrefabsPath = "Prefabs/UI/";
    public string enemyPrefabsPath = "Prefabs/Enemies/";
    public string itemPrefabsPath = "Prefabs/Items/";
    public string effectsPath = "Prefabs/Effects/";
    
    [Header("材质资源路径")]
    public string materialsPath = "Materials/";
    public string texturesPath = "Textures/";
    public string spritesPath = "Sprites/";
    
    [Header("特殊资源名称")]
    public string bulletTrailMaterial = "BulletTrailMaterial";
    public string muzzleFlashPrefab = "MuzzleFlashPrefab";
    public string hitEffectPrefab = "HitEffectPrefab";
    
    public string GetFullPath(string category, string fileName)
    {
        string basePath = category.ToLower() switch
        {
            "audio" => audioBasePath,
            "music" => musicPath,
            "sfx" => sfxPath,
            "voice" => voicePath,
            "ui" => uiPrefabsPath,
            "enemy" => enemyPrefabsPath,
            "item" => itemPrefabsPath,
            "effects" => effectsPath,
            "materials" => materialsPath,
            "textures" => texturesPath,
            "sprites" => spritesPath,
            _ => ""
        };
        
        return basePath + fileName;
    }
}