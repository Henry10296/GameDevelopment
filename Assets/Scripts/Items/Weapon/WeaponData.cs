using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("基础信息")]
    public string weaponName;
    public Sprite weaponIcon;

    [Header("伤害设置")]
    public int damage = 25;
    public float range = 100f;

    [Header("射击设置")]
    public float fireRate = 0.5f; // 射击间隔
    public bool isAutomatic = false;
    public int maxAmmo = 30;
    public string ammoType = "Bullet";

    [Header("声音设置")]
    public float noiseRadius = 20f; // 吸引敌人的范围
    public AudioClip fireSound;

    [Header("描述")]
    [TextArea(3, 5)]
    public string description;
    
    [Header("视觉配置")]
    public WeaponVisualConfig visualConfig ;
    
    // 便捷方法
    public Sprite GetWorldSprite() => visualConfig.worldSprite ?? weaponIcon;
    public Sprite GetInventoryIcon() => visualConfig.inventoryIcon ?? weaponIcon;
    
    
    
}

[System.Serializable]
public class WeaponVisualConfig
{
    [Header("2D显示配置")]
    public Sprite worldSprite;      // 世界中的2D贴图
    public Sprite inventoryIcon;    // 背包中的图标
    public Sprite weaponIcon;       // UI中的武器图标
    
    [Header("颜色配置")]
    public Color worldTint = Color.white;
    public bool glowEffect = false;
    public Color glowColor = Color.yellow;
    
    [Header("动画配置")]
    public bool rotateInWorld = false;
    public float rotationSpeed = 45f;
    public bool floatAnimation = true;
    public float floatAmplitude = 0.1f;
}