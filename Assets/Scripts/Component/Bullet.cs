using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子弹设置")]
    public float speed = 100f;
    public float damage = 25f;
    public float lifetime = 3f;
    
    [Header("视觉")]
    public LineRenderer trail;
    public GameObject hitEffect;
    
    private Vector3 direction;
    private Vector3 startPosition;
    private float travelTime = 0f;
    
    void Awake()
    {
        // 自动创建拖尾
        if (trail == null)
        {
            trail = gameObject.AddComponent<LineRenderer>();
            SetupTrail();
        }
        
        // 自动销毁
        Destroy(gameObject, lifetime);
    }
    
    void SetupTrail()
    {
        trail.material = new Material(Shader.Find("Sprites/Default"));

        Color color = Color.yellow;
        trail.startColor = color;
        trail.endColor = color;

        trail.startWidth = 0.02f;
        trail.endWidth = 0.01f;

        trail.positionCount = 2;
        trail.useWorldSpace = true;
    }

    
    public void Initialize(Vector3 origin, Vector3 shootDirection, float bulletDamage = 25f)
    {
        startPosition = origin;
        transform.position = origin;
        direction = shootDirection.normalized;
        damage = bulletDamage;
        travelTime = 0f;
        
        // 设置初始拖尾
        if (trail)
        {
            trail.SetPosition(0, origin);
            trail.SetPosition(1, origin);
        }
        
        Debug.Log($"子弹初始化: 位置{origin}, 方向{direction}, 伤害{damage}");
    }
    
    void Update()
    {
        MoveBullet();
        UpdateTrail();
    }
    
    void MoveBullet()
    {
        float deltaTime = Time.deltaTime;
        float moveDistance = speed * deltaTime;
        Vector3 newPosition = transform.position + direction * moveDistance;
        
        // 射线检测碰撞
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, moveDistance))
        {
            // 击中目标
            ProcessHit(hit);
            CreateHitEffect(hit.point, hit.normal);
            DestroyBullet();
            return;
        }
        
        // 移动子弹
        transform.position = newPosition;
        travelTime += deltaTime;
    }
    
    void UpdateTrail()
    {
        if (trail)
        {
            trail.SetPosition(0, startPosition);
            trail.SetPosition(1, transform.position);
        
            // 拖尾淡出（根据 travelTime 和 lifetime 插值计算 alpha）
            float alpha = Mathf.Lerp(1f, 0.2f, travelTime / lifetime);
        
            Color baseColor = trail.startColor; // 通常 startColor 和 endColor 是一样的
            Color fadedColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        
            trail.startColor = fadedColor;
            trail.endColor = fadedColor;
        }
    }

    
    void ProcessHit(RaycastHit hit)
    {
        Debug.Log($"[Bullet] 子弹击中: {hit.collider.name} 在位置 {hit.point}, tag: {hit.collider.tag}");
        
        // 伤害处理
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log($"[Bullet] 找到IDamageable组件，对 {hit.collider.name} 造成 {damage} 伤害");
            damageable.TakeDamage(damage);
            
            // 通知准星击中
            SimpleMouseCrosshair crosshair = FindObjectOfType<SimpleMouseCrosshair>();
            if (crosshair) crosshair.OnTargetHit();
        }
        else
        {
            Debug.LogWarning($"[Bullet] 未找到IDamageable组件在 {hit.collider.name}");
        }
        
        // 通知音响系统
        if (SoundManager.Instance)
        {
            SoundManager.Instance.AlertEnemies(hit.point, 5f);
        }
    }
    
    void CreateHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, position, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
        
        // 简单的击中火花效果（如果没有预制体）
        CreateSimpleHitEffect(position);
    }
    
    void CreateSimpleHitEffect(Vector3 position)
    {
        // 创建简单的粒子效果
        GameObject sparkEffect = new GameObject("BulletHit");
        sparkEffect.transform.position = position;

        ParticleSystem particles = sparkEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 0.2f;
        main.startSpeed = 3f;
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.5f, 0f); // 橙色
        main.maxParticles = 10;

        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 10)
        });

        Destroy(sparkEffect, 1f);
    }

    
    void DestroyBullet()
    {
        Destroy(gameObject);
    }
}


