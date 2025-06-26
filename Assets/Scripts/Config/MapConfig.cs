using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    [Header("基础信息")]
    public string mapName;
    public string sceneName;
    public Sprite mapIcon;
    public Sprite previewImage;
    
    [Header("风险评估")]
    [Range(1, 5)] public int riskLevel = 1;
    public Color riskColor = Color.yellow;
    public string riskDescription;
    
    [Header("地图特色")]
    public MapFeature[] features;
    
    [Header("战利品配置")]
    public LootTable[] commonLoot;
    public LootTable[] rareLoot;
    public LootTable[] specialLoot;
    
    [Header("敌人配置")]
    public EnemySpawnConfig[] enemySpawns;
    public bool hasShooterEnemies;
    public bool hasBossEnemy;
    
    [Header("特殊物品")]
    public bool containsAutoRifle;     // 医院特有
    public bool containsRadioClue;     // 学校特有  
    public bool containsRadioStation;  // 公园特有
    public bool hasAbundantFood;       // 超市特有
    
    [Header("描述文本")]
    [TextArea(3, 5)] public string description;
    [TextArea(2, 3)] public string explorationTips;
    
    public string GetRiskLevelText()
    {
        return riskLevel switch
        {
            1 => "相对安全",
            2 => "轻微危险", 
            3 => "中等风险",
            4 => "高度危险",
            5 => "极度危险",
            _ => "未知风险"
        };
    }
    
    public Color GetRiskLevelColor()
    {
        return riskLevel switch
        {
            1 => Color.green,
            2 => Color.yellow,
            3 => new Color(1f, 0.5f, 0f), // 橙色
            4 => Color.red,
            5 => Color.magenta,
            _ => Color.gray
        };
    }
}

[System.Serializable]
public class MapFeature
{
    public string featureName;
    public string description;
    public Sprite icon;
}

[System.Serializable] 
public class LootTable
{
    public ItemData item;
    public int minQuantity = 1;
    public int maxQuantity = 3;
    [Range(0f, 1f)] public float spawnChance = 0.5f;
    public bool guaranteedSpawn = false;
}

[System.Serializable]
public class EnemySpawnConfig
{
    public GameObject enemyPrefab;
    public int minCount = 1;
    public int maxCount = 3;
    [Range(0f, 1f)] public float spawnChance = 0.8f;
}