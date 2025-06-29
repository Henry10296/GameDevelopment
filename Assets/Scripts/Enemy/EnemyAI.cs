using UnityEngine;
using UnityEngine.AI;

public enum EnemyType
{
    Zombie = 0,
    Shooter=1,
    Snipers = 2,
    
}
public class EnemyAI : MonoBehaviour
{
    [Header("配置")]
    public EnemyType enemyType = EnemyType.Zombie;
    public EnemyConfig enemyConfig;
    
    [Header("射击敌人专用")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    
    [Header("巡逻设置")]
    public Transform[] patrolPoints;
    public float waitTime = 2f;
    
    // 组件引用
    private NavMeshAgent agent;
    private Transform player;
    private EnemyHealth enemyHealth;
    private Animator animator;
    private AudioSource audioSource;
    
    // 状态机系统 - 使用新的状态机
    private AIStateMachine stateMachine;
    private Vector3 lastKnownPlayerPosition;
    
    // 属性访问器 - 供状态机使用
    public NavMeshAgent Agent => agent;
    public Transform Player => player;
    public EnemyConfig Config => enemyConfig;
    public Animator Animator => animator;
    public Transform[] PatrolPoints => patrolPoints;
    public float WaitTime => waitTime;
    public GameObject BulletPrefab => bulletPrefab;
    public Transform FirePoint => firePoint;
    public EnemyType EnemyType => enemyType;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    
    void Start()
    {
        ValidateConfiguration();
        InitializeComponents();
        SetupPatrolling();
        SubscribeToEvents();
        
        // 初始化状态机
        stateMachine = new AIStateMachine(this);
    }
    
    void ValidateConfiguration()
    {
        if (enemyConfig == null)
        {
            Debug.LogError($"[EnemyAI] {gameObject.name} - EnemyConfig not assigned!");
            enabled = false;
            return;
        }
        
        if (enemyType == EnemyType.Shooter && (bulletPrefab == null || firePoint == null))
        {
            Debug.LogWarning($"[EnemyAI] {gameObject.name} - Shooter enemy missing bullet prefab or fire point!");
        }
    }
    
    void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        // 找到玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("[EnemyAI] Player not found!");
        
        // 应用配置参数
        if (enemyHealth != null)
        {
            enemyHealth.Initialize(enemyConfig);
        }
    }
    
    void SetupPatrolling()
    {
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
        else
        {
            // 如果没有设置巡逻点，创建默认的
            CreateDefaultPatrolPoints();
        }
    }
    
    void CreateDefaultPatrolPoints()
    {
        GameObject patrolParent = new GameObject($"PatrolPoints_{gameObject.name}");
        patrolPoints = new Transform[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject point = new GameObject($"PatrolPoint_{i}");
            point.transform.parent = patrolParent.transform;
            
            Vector3 randomOffset = new Vector3(
                Random.Range(-10f, 10f),
                0,
                Random.Range(-10f, 10f)
            );
            
            point.transform.position = transform.position + randomOffset;
            patrolPoints[i] = point.transform;
        }
        
        Debug.Log($"[EnemyAI] Created default patrol points for {gameObject.name}");
    }
    
    void SubscribeToEvents()
    {
        if (SoundManager.Instance)
        {
            SoundManager.Instance.OnSoundHeard += OnSoundHeard;
        }
    }
    
    void Update()
    {
        if (enemyHealth?.IsDead() == true) return;
        if (player == null) return;
        
        // 使用新状态机更新
        stateMachine?.Update();
    }
    
    public bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        
        if (angle > enemyConfig.visionAngle / 2) return false;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, enemyConfig.visionRange))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }
    
    public bool HeardSound()
    {
        // 这里可以实现更复杂的声音检测逻辑
        return false;
    }
    
    public void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void OnSoundHeard(Vector3 soundPosition, float soundRadius)
    {
        float distanceToSound = Vector3.Distance(transform.position, soundPosition);
        
        if (distanceToSound <= enemyConfig.hearingRange)
        {
            lastKnownPlayerPosition = soundPosition;
            Debug.Log($"[EnemyAI] {gameObject.name} investigating sound at {soundPosition}");
        }
    }
    
    void OnDestroy()
    {
        if (SoundManager.Instance)
        {
            SoundManager.Instance.OnSoundHeard -= OnSoundHeard;
        }
    }
    
    // 调试方法
    [ContextMenu("Reset to Patrol")]
    public void DebugResetToPatrol()
    {
        agent.isStopped = false;
        // 状态机会自动处理状态重置
    }
    
    // 编辑器可视化
    void OnDrawGizmosSelected()
    {
        if (enemyConfig == null) return;
        
        // 视野范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyConfig.visionRange);
        
        // 听觉范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, enemyConfig.hearingRange);
        
        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyConfig.attackRange);
        
        // 视野角度
        Vector3 leftBoundary = Quaternion.Euler(0, -enemyConfig.visionAngle / 2, 0) * transform.forward * enemyConfig.visionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, enemyConfig.visionAngle / 2, 0) * transform.forward * enemyConfig.visionRange;
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        
        // 巡逻路径
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 1f);
                    
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }
        }
    }
}