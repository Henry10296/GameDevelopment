using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "StoryData", menuName = "Game/Story Data")]
public class StoryData : ScriptableObject
{
    [Header("故事基础信息")]
    public string storyTitle = "核战争爆发";
    public Sprite backgroundImage;
    
    [Header("故事段落")]
    public StorySegment[] segments;
    
    [Header("音效")]
    public AudioClip narratorVoice;
    public AudioClip backgroundMusic;
}

[System.Serializable]
public class StorySegment
{
    [Header("文本内容")]
    [TextArea(3, 6)]
    public string storyText;
    
    [Header("显示设置")]
    public float textSpeed = 50f; // 字符/秒
    public float pauseAfterText = 2f;
    public bool waitForInput = true;
    
    [Header("选择分支")]
    public StoryChoice[] choices;
    
    [Header("视觉效果")]
    public Sprite segmentImage;
    public Color textColor = Color.white;
    public AnimationCurve fadeInCurve;
}

[System.Serializable]
public class StoryChoice
{
    [Header("选择文本")]
    public string choiceText;
    [TextArea(2, 3)]
    public string description;
    
    [Header("游戏效果")]
    public int foodBonus;
    public int waterBonus; 
    public int medicineBonus;
    public bool grantWeapon;
    public WeaponType weaponType = WeaponType.Pistol;
    
    [Header("后续内容")]
    [TextArea(2, 4)]
    public string resultText;
    public float resultDisplayTime = 3f;
}