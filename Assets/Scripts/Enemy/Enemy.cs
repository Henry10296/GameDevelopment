using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Doo mEnemyData", menuName = "Game/Doom Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("基础信息")]
    public string enemyName;
    public EnemyType enemyType;
    public float health = 100f;
    public float armor = 0f;
    
    [Header("移动设置")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 6f;
    public float rotationSpeed = 5f;
    public bool canFly = false;
    
    [Header("AI行为")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float loseTargetTime = 5f;
    public bool alwaysHostile = true;
    
    [Header("攻击设置")]
    public float attackDamage = 25f;
    public float attackCooldown = 1.5f;
    public DoomAttackType[] attackTypes;
    
    [Header("Sprite动画")]
    public SpriteSet spriteSet;
    
    [Header("掉落物品")]
    public DoomLootDrop[] lootDrops;
    
    [Header("音效")]
    public AudioClip[] idleSounds;
    public AudioClip[] alertSounds;
    public AudioClip[] attackSounds;
    public AudioClip[] painSounds;
    public AudioClip deathSound;
    
    [Header("特殊行为")]
    public bool canOpenDoors = false;
    public bool immuneToInfighting = false;
    public float painChance = 0.3f; // 受伤时进入疼痛状态的概率
}

[System.Serializable]
public class SpriteSet
{
    [Header("Sprite动画帧")]
    public Sprite[] idleSprites = new Sprite[8]; // 8个方向的静止帧
    public Sprite[] walkSprites = new Sprite[32]; // 8方向 x 4帧动画
    public Sprite[] attackSprites = new Sprite[8]; // 8个方向的攻击帧
    public Sprite[] painSprites = new Sprite[8]; // 8个方向的受伤帧
    public Sprite[] deathSprites = new Sprite[5]; // 死亡动画序列
    
    [Header("动画设置")]
    public float walkAnimSpeed = 0.2f;
    public float attackAnimSpeed = 0.1f;
    public float deathAnimSpeed = 0.15f;
    
    public Sprite GetIdleSprite(int direction)
    {
        return GetSpriteFromArray(idleSprites, direction);
    }
    
    public Sprite GetWalkSprite(int direction, int frame)
    {
        int index = direction * 4 + frame;
        return GetSpriteFromArray(walkSprites, index);
    }
    
    public Sprite GetAttackSprite(int direction)
    {
        return GetSpriteFromArray(attackSprites, direction);
    }
    
    public Sprite GetPainSprite(int direction)
    {
        return GetSpriteFromArray(painSprites, direction);
    }
    
    public Sprite GetDeathSprite(int frame)
    {
        return GetSpriteFromArray(deathSprites, frame);
    }
    
    private Sprite GetSpriteFromArray(Sprite[] array, int index)
    {
        if (array == null || array.Length == 0) return null;
        return array[Mathf.Clamp(index, 0, array.Length - 1)];
    }
}

[System.Serializable]
public class DoomAttackType
{
    public string attackName;
    public float damage;
    public float range;
    public float cooldown;
    public bool isProjectile;
    public GameObject projectilePrefab;
    public AudioClip attackSound;
}

[System.Serializable]
public class DoomLootDrop
{
    public GameObject itemPrefab;
    [Range(0f, 1f)] public float dropChance;
    public int minQuantity = 1;
    public int maxQuantity = 1;
}

public enum DoomEnemyState
{
    Idle,
    Patrol,
    Alert,
    Chase,
    Attack,
    Pain,
    Death
}

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("配置")]
    public EnemyData enemyData;
    
    [Header("调试")]
    public bool showDebugInfo = false;
    public bool showGizmos = true;
    
    // 组件引用
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider enemyCollider;
    
    // 状态管理
    private DoomEnemyState currentState = DoomEnemyState.Idle;
    private float stateTimer = 0f;
    
    // 目标和导航
    private Transform player;
    private Vector3 lastKnownPlayerPosition;
    private float lastPlayerSightTime = 0f;
    
    // 战斗相关
    private float health;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    
    // 动画相关
    private int currentWalkFrame = 0;
    private float animationTimer = 0f;
    private int facingDirection = 0; // 0-7, 8个方向
    
    // 移动相关
    private Vector3 moveDirection;
    private Vector3 targetPosition;
    
    // 事件
    public System.Action<Enemy> OnDeath;
    public System.Action<Enemy, float> OnTakeDamage;
    
    void Start()
    {
        InitializeComponents();
        InitializeStats();
        SetState(DoomEnemyState.Idle);
    }
    
    void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        enemyCollider = GetComponent<Collider>();
        
        // 寻找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
    
    void InitializeStats()
    {
        if (enemyData != null)
        {
            health = enemyData.health;
        }
    }
    
    void Update()
    {
        if (isDead) return;
        
        stateTimer += Time.deltaTime;
        animationTimer += Time.deltaTime;
        
        UpdateState();
        UpdateAnimation();
        UpdateFacing();
        
        if (showDebugInfo)
            DisplayDebugInfo();
    }
    
    void UpdateState()
    {
        switch (currentState)
        {
            case DoomEnemyState.Idle:
                UpdateIdleState();
                break;
            case DoomEnemyState.Patrol:
                UpdatePatrolState();
                break;
            case DoomEnemyState.Alert:
                UpdateAlertState();
                break;
            case DoomEnemyState.Chase:
                UpdateChaseState();
                break;
            case DoomEnemyState.Attack:
                UpdateAttackState();
                break;
            case DoomEnemyState.Pain:
                UpdatePainState();
                break;
            case DoomEnemyState.Death:
                UpdateDeathState();
                break;
        }
    }
    
    void UpdateIdleState()
    {
        if (CanSeePlayer())
        {
            SetState(DoomEnemyState.Alert);
        }
        
        // 随机播放idle音效
        if (stateTimer > 3f && Random.Range(0f, 1f) < 0.1f)
        {
            PlayRandomSound(enemyData.idleSounds);
            stateTimer = 0f;
        }
    }
    
    void UpdatePatrolState()
    {
        // 简单的巡逻逻辑
        if (Vector3.Distance(transform.position, targetPosition) < 1f)
        {
            // 选择新的巡逻点
            targetPosition = transform.position + Random.insideUnitSphere * 10f;
            targetPosition.y = transform.position.y;
        }
        
        MoveTowards(targetPosition, enemyData.moveSpeed);
        
        if (CanSeePlayer())
        {
            SetState(DoomEnemyState.Alert);
        }
    }
    
    void UpdateAlertState()
    {
        if (stateTimer < 0.5f) return; // 短暂的反应时间
        
        if (CanSeePlayer())
        {
            SetState(DoomEnemyState.Chase);
        }
        else
        {
            SetState(DoomEnemyState.Idle);
        }
    }
    
    void UpdateChaseState()
    {
        if (player == null)
        {
            SetState(DoomEnemyState.Idle);
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;
            lastPlayerSightTime = Time.time;
            
            if (distanceToPlayer <= enemyData.attackRange)
            {
                SetState(DoomEnemyState.Attack);
                return;
            }
        }
        else if (Time.time - lastPlayerSightTime > enemyData.loseTargetTime)
        {
            SetState(DoomEnemyState.Idle);
            return;
        }
        
        // 移动向玩家
        Vector3 targetPos = CanSeePlayer() ? player.position : lastKnownPlayerPosition;
        MoveTowards(targetPos, enemyData.chaseSpeed);
    }
    
    void UpdateAttackState()
    {
        if (player == null)
        {
            SetState(DoomEnemyState.Chase);
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer > enemyData.attackRange)
        {
            SetState(DoomEnemyState.Chase);
            return;
        }
        
        if (Time.time - lastAttackTime >= enemyData.attackCooldown)
        {
            PerformAttack();
        }
    }
    
    void UpdatePainState()
    {
        if (stateTimer > 0.5f) // 疼痛状态持续时间
        {
            SetState(DoomEnemyState.Chase);
        }
    }
    
    void UpdateDeathState()
    {
        // 死亡动画播放完成后的处理
        if (stateTimer > 2f)
        {
            // 可以在这里添加尸体消失逻辑
        }
    }
    
    void SetState(DoomEnemyState newState)
    {
        if (currentState == newState) return;
        
        // 退出当前状态
        ExitState(currentState);
        
        // 进入新状态
        currentState = newState;
        stateTimer = 0f;
        EnterState(newState);
    }
    
    void EnterState(DoomEnemyState state)
    {
        switch (state)
        {
            case DoomEnemyState.Alert:
                PlayRandomSound(enemyData.alertSounds);
                break;
            case DoomEnemyState.Pain:
                PlayRandomSound(enemyData.painSounds);
                break;
            case DoomEnemyState.Death:
                PlaySound(enemyData.deathSound);
                OnDeath?.Invoke(this);
                break;
        }
    }
    
    void ExitState(DoomEnemyState state)
    {
        // 状态退出时的清理工作
    }
    
    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // 保持水平移动
        
        transform.position += direction * speed * Time.deltaTime;
        moveDirection = direction;
    }
    
    void UpdateFacing()
    {
        if (player == null) return;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;
        
        // 将角度转换为8个方向 (0-7)
        angle = (angle + 360f) % 360f;
        facingDirection = Mathf.RoundToInt(angle / 45f) % 8;
    }
    
    void UpdateAnimation()
    {
        if (enemyData?.spriteSet == null) return;
        
        Sprite currentSprite = null;
        
        switch (currentState)
        {
            case DoomEnemyState.Idle:
                currentSprite = enemyData.spriteSet.GetIdleSprite(facingDirection);
                break;
                
            case DoomEnemyState.Chase:
            case DoomEnemyState.Patrol:
                if (animationTimer >= enemyData.spriteSet.walkAnimSpeed)
                {
                    currentWalkFrame = (currentWalkFrame + 1) % 4;
                    animationTimer = 0f;
                }
                currentSprite = enemyData.spriteSet.GetWalkSprite(facingDirection, currentWalkFrame);
                break;
                
            case DoomEnemyState.Attack:
                currentSprite = enemyData.spriteSet.GetAttackSprite(facingDirection);
                break;
                
            case DoomEnemyState.Pain:
                currentSprite = enemyData.spriteSet.GetPainSprite(facingDirection);
                break;
                
            case DoomEnemyState.Death:
                int deathFrame = Mathf.FloorToInt(stateTimer / enemyData.spriteSet.deathAnimSpeed);
                currentSprite = enemyData.spriteSet.GetDeathSprite(deathFrame);
                break;
        }
        
        if (currentSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = currentSprite;
        }
    }
    
    bool CanSeePlayer()
    {
        if (player == null) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > enemyData.detectionRange) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        
        // 射线检测
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, distance))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }
    
    void PerformAttack()
    {
        lastAttackTime = Time.time;
        
        if (enemyData.attackTypes != null && enemyData.attackTypes.Length > 0)
        {
            var attackType = enemyData.attackTypes[0]; // 使用第一个攻击类型
            
            PlaySound(attackType.attackSound);
            
            if (attackType.isProjectile && attackType.projectilePrefab != null)
            {
                // 发射投射物
                FireProjectile(attackType);
            }
            else
            {
                // 近战攻击
                MeleeAttack(attackType);
            }
        }
    }
    
    void FireProjectile(DoomAttackType attackType)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        GameObject projectile = Instantiate(attackType.projectilePrefab, transform.position + Vector3.up, 
            Quaternion.LookRotation(direction));
        
        // 给投射物设置伤害和速度
        var projectileScript = projectile.GetComponent<DoomProjectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDamage(attackType.damage);
            projectileScript.SetSpeed(10f);
        }
    }
    
    void MeleeAttack(DoomAttackType attackType)
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackType.range)
        {
            // 对玩家造成伤害
            var playerHealth = player.GetComponent<IDamageable>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackType.damage);
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        health -= damage;
        OnTakeDamage?.Invoke(this, damage);
        
        Debug.Log($"[Enemy] {gameObject.name} took {damage} damage, health: {health}");
        
        if (health <= 0)
        {
            Die();
        }
        else
        {
            // 随机进入疼痛状态
            if (Random.Range(0f, 1f) < enemyData.painChance)
            {
                SetState(DoomEnemyState.Pain);
            }
            else if (currentState == DoomEnemyState.Idle)
            {
                SetState(DoomEnemyState.Alert);
            }
        }
    }
    
    public float GetCurrentHealth()
    {
        return health;
    }
    
    public float GetMaxHealth()
    {
        return enemyData != null ? enemyData.health : 100f;
    }
    
    public bool IsAlive()
    {
        return !isDead;
    }
    
    void Die()
    {
        isDead = true;
        SetState(DoomEnemyState.Death);
        
        // 禁用碰撞器
        if (enemyCollider) enemyCollider.enabled = false;
        
        // 掉落物品
        DropLoot();
        
        // 延迟销毁
        Destroy(gameObject, 5f);
    }
    
    void DropLoot()
    {
        if (enemyData.lootDrops == null) return;
        
        foreach (var loot in enemyData.lootDrops)
        {
            if (Random.Range(0f, 1f) < loot.dropChance)
            {
                int quantity = Random.Range(loot.minQuantity, loot.maxQuantity + 1);
                for (int i = 0; i < quantity; i++)
                {
                    Vector3 dropPosition = transform.position + Random.insideUnitSphere * 2f;
                    dropPosition.y = transform.position.y;
                    Instantiate(loot.itemPrefab, dropPosition, Quaternion.identity);
                }
            }
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlaySound(clip);
        }
    }
    
    void DisplayDebugInfo()
    {
        Debug.Log($"{name}: State={currentState}, Health={health}, CanSeePlayer={CanSeePlayer()}");
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos || enemyData == null) return;
        
        // 检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);
        
        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);
        
        // 视线
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position + Vector3.up, player.position);
        }
    }
}

// 投射物类
public class DoomProjectile : MonoBehaviour
{
    private float damage = 10f;
    private float speed = 15f;
    private float lifetime = 5f;
    private Vector3 direction;
    
    void Start()
    {
        direction = transform.forward;
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
    
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerHealth = other.GetComponent<IDamageable>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            // 创建撞击效果
            Destroy(gameObject);
        }
    }
}