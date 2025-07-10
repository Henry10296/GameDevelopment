using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 修改后的WeaponConfig.cs - 移除重复，整合功能
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Game/Weapon System Config")]
public class WeaponConfig : BaseGameConfig
{
    [Header("武器数据")]
    public WeaponData pistol;
    public WeaponData rifle;
    
    [Header("通用设置")]
    public float reloadTime = 2f;
    public float weaponSwitchTime = 1f;
    public float aimSensitivity = 0.5f;
    public float aimFOV = 40f;
    public float normalFOV = 60f;
    
    [Header("音效配置")]
    public AudioClip reloadSound;
    public AudioClip emptyclipSound;
    public AudioClip weaponSwitchSound;
    public AudioClip[] gunSounds;
    
    [Header("视觉效果")]
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTrailPrefab;
    public GameObject hitEffectPrefab;
    public GameObject bulletHolePrefab;
    
    [Header("UI设置")]
    public Sprite crosshairDefault;
    public Sprite crosshairAiming;
    public Color crosshairColor = Color.white;
    
    public override bool ValidateConfig()
    {
        bool isValid = base.ValidateConfig();
        
        if (pistol == null)
        {
            Debug.LogError("[WeaponSystemConfig] Pistol data is missing!");
            isValid = false;
        }
        
        if (aimFOV >= normalFOV)
        {
            Debug.LogWarning("[WeaponSystemConfig] aimFOV should be less than normalFOV");
            aimFOV = normalFOV * 0.7f;
        }
        
        return isValid;
    }
    
    // 整合后的便捷方法
    public AudioClip GetRandomGunSound()
    {
        if (gunSounds == null || gunSounds.Length == 0) return null;
        return gunSounds[Random.Range(0, gunSounds.Length)];
    }
    
    public WeaponData GetWeaponByType(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Pistol => pistol,
            WeaponType.Rifle => rifle,
            _ => pistol
        };
    }
    
    public override string GetConfigSummary()
    {
        return $"{base.GetConfigSummary()} - {(pistol ? "Pistol ✓" : "Pistol ✗")} {(rifle ? "Rifle ✓" : "Rifle ✗")}";
    }
}
