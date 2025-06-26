using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Game/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    [Header("武器数据")]
    public WeaponData pistol;
    public WeaponData rifle;
    
    [Header("通用设置")]
    public LayerMask enemyLayer = -1;
    public float defaultRange = 100f;
    
    [Header("音效")]
    public AudioClip[] gunShotSounds;
    [FormerlySerializedAs("ReloadSound")] public AudioClip reloadSound;
    public AudioClip emptyclipSound;
    
    public AudioClip GetRandomGunSound()
    {
        if (gunShotSounds.Length > 0)
            return gunShotSounds[Random.Range(0, gunShotSounds.Length)];
        return null;
    }
    
    void OnValidate()
    {
        if (pistol != null && pistol.range <= 0) pistol.range = defaultRange;
        if (rifle != null && rifle.range <= 0) rifle.range = defaultRange;
    }
}