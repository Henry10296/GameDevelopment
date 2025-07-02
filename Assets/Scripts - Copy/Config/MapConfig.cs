using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    [Header("基础信息")]
    public string mapName;
    public string sceneName;
    public string description;
    public Sprite mapImage;
    
    [Header("风险评估")]
    [Range(1, 5)] public int riskLevel = 1;
    public bool isUnlocked = true;
    public int unlockDay = 1;
    
    [Header("战利品配置")]
    public LootTable[] commonLoot;
    public LootTable[] rareLoot;
    public LootTable[] specialLoot;
    
    [Header("敌人生成")]
    public EnemySpawnConfig[] enemySpawns;
    
    [Header("特殊物品")]
    public bool containsRadioStation = false;
    public bool containsRadioClue = false;
    public bool containsAutoRifle = false;
    
    [Header("探索提示")]
    [TextArea(2, 4)] public string explorationTips;
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
    [Range(0f, 1f)] public float spawnChance = 0.7f;
}