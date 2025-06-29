using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("基础属性")]
    public string enemyName;
    public EnemyType enemyType;
    public float health = 100f;
    
    [Header("移动设置")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public float rotationSpeed = 3f;
    
    [Header("感知系统")]
    public float visionRange = 15f;
    public float visionAngle = 60f;
    public float hearingRange = 10f;
    
    [Header("攻击设置")]
    public float attackDamage = 25f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    
    [Header("射击敌人专用")]
    public float shootRange = 20f;
    public float shootInterval = 1f;
    public float shootAccuracy = 0.7f;
    
    [Header("掉落物品")]
    public GameObject[] dropItems;
    [Range(0f, 1f)] public float dropChance = 0.3f;
    
    [Header("音效")]
    public AudioClip[] attackSounds;
    public AudioClip[] hurtSounds;
    public AudioClip deathSound;
    public AudioClip[] idleSounds;
    
    [Header("AI行为")]
    public float alertDuration = 10f;
    public float investigationTime = 5f;
    public bool canOpenDoors = false;
    public bool canClimbStairs = true;
    
    public AudioClip GetRandomAttackSound()
    {
        if (attackSounds == null || attackSounds.Length == 0) return null;
        return attackSounds[Random.Range(0, attackSounds.Length)];
    }
    
    public AudioClip GetRandomHurtSound()
    {
        if (hurtSounds == null || hurtSounds.Length == 0) return null;
        return hurtSounds[Random.Range(0, hurtSounds.Length)];
    }
    
    public AudioClip GetRandomIdleSound()
    {
        if (idleSounds == null || idleSounds.Length == 0) return null;
        return idleSounds[Random.Range(0, idleSounds.Length)];
    }
}