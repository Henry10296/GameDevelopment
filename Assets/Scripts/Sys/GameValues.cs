using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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