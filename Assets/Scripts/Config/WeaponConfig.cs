using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Game/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    [Header("武器数据")]
    public WeaponData pistol;
    public WeaponData rifle;
    
    [Header("通用音效")]
    public AudioClip reloadSound;
    public AudioClip emptyclipSound;
    public AudioClip[] gunSounds;
    
    [Header("UI设置")]
    public Sprite crosshairDefault;
    public Sprite crosshairAiming;
    public Color crosshairColor = Color.white;
    
    [Header("射击效果")]
    public GameObject muzzleFlashPrefab;
    public GameObject bulletHolePrefab;
    public GameObject bulletTrailPrefab;
    
    [Header("瞄准设置")]
    public float aimSensitivity = 0.5f;
    public float aimFOV = 40f;
    public float normalFOV = 60f;
    
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
}

