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
}