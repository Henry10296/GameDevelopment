using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("基础属性")]
    public float health = 100f;
    public float attackDamage = 20f;
    public float attackCooldown = 2f;
    
    [Header("移动设置")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 8f;
    public float attackRange = 2f;
    
    [Header("感知设置")]
    public float visionRange = 10f;
    public float visionAngle = 90f;
    public float hearingRange = 15f;
    
    [Header("射击敌人专用")]
    public float shootRange = 25f;
    public float shootInterval = 3f;
    public float shootAccuracy = 0.8f;
    
    [Header("掉落设置")]
    public GameObject[] dropItems;
    public float dropChance = 0.3f;
    
    [Header("音效")]
    public AudioClip[] attackSounds;
    public AudioClip[] hurtSounds;
    public AudioClip deathSound;
    
    public AudioClip GetRandomAttackSound()
    {
        if (attackSounds.Length > 0)
            return attackSounds[Random.Range(0, attackSounds.Length)];
        return null;
    }
    
    public AudioClip GetRandomHurtSound()
    {
        if (hurtSounds.Length > 0)
            return hurtSounds[Random.Range(0, hurtSounds.Length)];
        return null;
    }
}