using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState
{
    public abstract void Enter(EnemyAI enemy);
    public abstract void Update(EnemyAI enemy);
    public abstract void Exit(EnemyAI enemy);
    public abstract AIState CheckTransitions(EnemyAI enemy);
    
    protected void LookAtTarget(Transform self, Transform target, float rotationSpeed = 5f)
    {
        Vector3 direction = (target.position - self.position).normalized;
        direction.y = 0; // 保持水平
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            self.rotation = Quaternion.Slerp(self.rotation, targetRotation, 
                rotationSpeed * Time.deltaTime);
        }
    }
}

public class PatrolState : AIState
{
    private int currentPatrolIndex;
    private float waitTimer;
    private bool isWaiting;
    
    public override void Enter(EnemyAI enemy)
    {
        enemy.Agent.speed = enemy.Config.patrolSpeed;
        enemy.Agent.isStopped = false;
        
        if (enemy.PatrolPoints.Length > 0)
        {
            enemy.Agent.SetDestination(enemy.PatrolPoints[currentPatrolIndex].position);
        }
        
        if (enemy.Animator)
            enemy.Animator.SetBool("IsWalking", true);
    }
    
    public override void Update(EnemyAI enemy)
    {
        if (enemy.PatrolPoints.Length == 0) return;
        
        if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.5f)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = 0f;
                
                if (enemy.Animator)
                    enemy.Animator.SetBool("IsWalking", false);
            }
            
            waitTimer += Time.deltaTime;
            
            if (waitTimer >= enemy.WaitTime)
            {
                MoveToNextPatrolPoint(enemy);
            }
        }
    }
    
    public override void Exit(EnemyAI enemy)
    {
        isWaiting = false;
        waitTimer = 0f;
    }
    
    public override AIState CheckTransitions(EnemyAI enemy)
    {
        if (enemy.CanSeePlayer())
            return new ChaseState();
        
        if (enemy.HeardSound())
            return new InvestigateState();
        
        return null; // 保持当前状态
    }
    
    void MoveToNextPatrolPoint(EnemyAI enemy)
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % enemy.PatrolPoints.Length;
        enemy.Agent.SetDestination(enemy.PatrolPoints[currentPatrolIndex].position);
        isWaiting = false;
        
        if (enemy.Animator)
            enemy.Animator.SetBool("IsWalking", true);
    }
}

public class ChaseState : AIState
{
    private float lastSeenTime;
    private Vector3 lastKnownPosition;
    
    public override void Enter(EnemyAI enemy)
    {
        enemy.Agent.speed = enemy.Config.chaseSpeed;
        enemy.Agent.isStopped = false;
        lastSeenTime = Time.time;
        
        if (enemy.Animator)
        {
            enemy.Animator.SetBool("IsWalking", false);
            enemy.Animator.SetBool("IsRunning", true);
        }
        
        // 播放发现玩家音效
        enemy.PlaySound(enemy.Config.GetRandomAttackSound());
    }
    
    public override void Update(EnemyAI enemy)
    {
        if (enemy.CanSeePlayer())
        {
            lastSeenTime = Time.time;
            lastKnownPosition = enemy.Player.position;
            enemy.Agent.SetDestination(enemy.Player.position);
        }
        else
        {
            // 继续前往最后已知位置
            enemy.Agent.SetDestination(lastKnownPosition);
        }
    }
    
    public override void Exit(EnemyAI enemy)
    {
        if (enemy.Animator)
        {
            enemy.Animator.SetBool("IsRunning", false);
        }
    }
    
    public override AIState CheckTransitions(EnemyAI enemy)
    {
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Player.position);
        
        // 进入攻击状态
        if (enemy.CanSeePlayer())
        {
            if (enemy.EnemyType == EnemyType.Zombie && distanceToPlayer <= enemy.Config.attackRange)
                return new MeleeAttackState();
            
            if (enemy.EnemyType == EnemyType.Shooter && distanceToPlayer <= enemy.Config.shootRange)
                return new RangedAttackState();
        }
        
        // 失去目标太久，进入调查状态
        if (Time.time - lastSeenTime > 5f)
            return new InvestigateState();
        
        return null;
    }
}

public class MeleeAttackState : AIState
{
    private float lastAttackTime;
    
    public override void Enter(EnemyAI enemy)
    {
        enemy.Agent.isStopped = true;
        
        if (enemy.Animator)
        {
            enemy.Animator.SetBool("IsRunning", false);
            enemy.Animator.SetBool("IsAttacking", true);
        }
    }
    
    public override void Update(EnemyAI enemy)
    {
        // 面向玩家
        LookAtTarget(enemy.transform, enemy.Player, 10f);
        
        // 执行攻击
        if (Time.time - lastAttackTime >= enemy.Config.attackCooldown)
        {
            PerformMeleeAttack(enemy);
            lastAttackTime = Time.time;
        }
    }
    
    public override void Exit(EnemyAI enemy)
    {
        enemy.Agent.isStopped = false;
        
        if (enemy.Animator)
            enemy.Animator.SetBool("IsAttacking", false);
    }
    
    public override AIState CheckTransitions(EnemyAI enemy)
    {
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Player.position);
        
        if (!enemy.CanSeePlayer() || distanceToPlayer > enemy.Config.attackRange)
            return new ChaseState();
        
        return null;
    }
    
    void PerformMeleeAttack(EnemyAI enemy)
    {
        if (enemy.Animator)
            enemy.Animator.SetTrigger("Attack");
        
        // 造成伤害
        if (enemy.Player.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(enemy.Config.attackDamage);
        }
        
        // 播放攻击音效
        enemy.PlaySound(enemy.Config.GetRandomAttackSound());
    }
}

public class RangedAttackState : AIState
{
    private float lastShootTime;
    private int shotsInBurst = 0;
    private const int maxShotsInBurst = 3;
    
    public override void Enter(EnemyAI enemy)
    {
        enemy.Agent.isStopped = true;
        shotsInBurst = 0;
        
        if (enemy.Animator)
        {
            enemy.Animator.SetBool("IsRunning", false);
            enemy.Animator.SetBool("IsShooting", true);
        }
    }
    
    public override void Update(EnemyAI enemy)
    {
        // 面向玩家
        LookAtTarget(enemy.transform, enemy.Player, 8f);
        
        // 执行射击
        if (Time.time - lastShootTime >= enemy.Config.shootInterval)
        {
            PerformRangedAttack(enemy);
            lastShootTime = Time.time;
            shotsInBurst++;
        }
    }
    
    public override void Exit(EnemyAI enemy)
    {
        enemy.Agent.isStopped = false;
        shotsInBurst = 0;
        
        if (enemy.Animator)
            enemy.Animator.SetBool("IsShooting", false);
    }
    
    public override AIState CheckTransitions(EnemyAI enemy)
    {
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Player.position);
        
        if (!enemy.CanSeePlayer() || distanceToPlayer > enemy.Config.shootRange)
            return new ChaseState();
        
        // 射击一定次数后暂停或移动
        if (shotsInBurst >= maxShotsInBurst)
            return new ChaseState();
        
        return null;
    }
    
    void PerformRangedAttack(EnemyAI enemy)
    {
        if (enemy.Animator)
            enemy.Animator.SetTrigger("Shoot");
        
        Vector3 shootDirection = (enemy.Player.position - enemy.FirePoint.position).normalized;
        
        // 添加精度偏差
        float accuracy = enemy.Config.shootAccuracy;
        float randomOffset = (1f - accuracy) * 2f;
        shootDirection += new Vector3(
            Random.Range(-randomOffset, randomOffset),
            Random.Range(-randomOffset, randomOffset),
            Random.Range(-randomOffset, randomOffset)
        );
        
        // 创建子弹或直接命中
        if (enemy.BulletPrefab)
        {
            CreateBullet(enemy, shootDirection);
        }
        else
        {
            // 直接射线检测
            PerformHitscanAttack(enemy, shootDirection);
        }
        
        // 播放射击音效
        enemy.PlaySound(enemy.Config.GetRandomAttackSound());
    }
    
    void CreateBullet(EnemyAI enemy, Vector3 direction)
    {
        GameObject bullet = Object.Instantiate(enemy.BulletPrefab, 
            enemy.FirePoint.position, 
            Quaternion.LookRotation(direction));
        
        if (bullet.TryGetComponent<EnemyBullet>(out var bulletScript))
        {
            bulletScript.damage = enemy.Config.attackDamage;
        }
    }
    
    void PerformHitscanAttack(EnemyAI enemy, Vector3 direction)
    {
        if (Physics.Raycast(enemy.FirePoint.position, direction, out RaycastHit hit, enemy.Config.shootRange))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(enemy.Config.attackDamage);
            }
        }
    }
}

public class InvestigateState : AIState
{
    private Vector3 investigationPoint;
    private float investigationTime = 5f;
    private float currentInvestigationTime;
    
    public override void Enter(EnemyAI enemy)
    {
        enemy.Agent.speed = enemy.Config.patrolSpeed;
        enemy.Agent.isStopped = false;
        investigationPoint = enemy.LastKnownPlayerPosition;
        currentInvestigationTime = 0f;
        
        enemy.Agent.SetDestination(investigationPoint);
        
        if (enemy.Animator)
        {
            enemy.Animator.SetBool("IsWalking", true);
            enemy.Animator.SetBool("IsInvestigating", true);
        }
    }
    
    public override void Update(EnemyAI enemy)
    {
        currentInvestigationTime += Time.deltaTime;
        
        // 到达调查点后，环顾四周
        if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 1f)
        {
            // 缓慢旋转，寻找玩家
            enemy.transform.Rotate(0, 30f * Time.deltaTime, 0);
        }
    }
    
    public override void Exit(EnemyAI enemy)
    {
        if (enemy.Animator)
        {
            enemy.Animator.SetBool("IsInvestigating", false);
        }
    }
    
    public override AIState CheckTransitions(EnemyAI enemy)
    {
        // 如果看到玩家，立即追击
        if (enemy.CanSeePlayer())
            return new ChaseState();
        
        // 调查时间结束，返回巡逻
        if (currentInvestigationTime >= investigationTime)
            return new PatrolState();
        
        return null;
    }
}

// 状态机管理器
public class AIStateMachine
{
    private AIState currentState;
    private EnemyAI enemy;
    
    public AIStateMachine(EnemyAI enemyAI)
    {
        enemy = enemyAI;
        ChangeState(new PatrolState());
    }
    
    public void Update()
    {
        if (currentState == null) return;
        
        // 检查状态转换
        AIState newState = currentState.CheckTransitions(enemy);
        if (newState != null)
        {
            ChangeState(newState);
        }
        
        // 更新当前状态
        currentState.Update(enemy);
    }
    
    void ChangeState(AIState newState)
    {
        currentState?.Exit(enemy);
        currentState = newState;
        currentState?.Enter(enemy);
        
        Debug.Log($"[AIStateMachine] {enemy.name} changed to {newState.GetType().Name}");
    }
    
    public string GetCurrentStateName()
    {
        return currentState?.GetType().Name ?? "None";
    }
}
