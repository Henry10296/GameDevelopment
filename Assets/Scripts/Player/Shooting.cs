using UnityEngine;

// 在你现有的WeaponController中添加这些功能
public class Shooting : MonoBehaviour 
{
    [Header("射击增强")]
    public LayerMask targetLayers = -1;
    public float maxRange = 100f;
    public GameObject hitEffectPrefab;
    public GameObject bulletHolePrefab;
    
    [Header("音效")]
    public AudioClip[] gunShotSounds;
    public AudioClip[] hitSounds;
    public AudioClip[] ricochetSounds;
    
    [Header("枪口火焰")]
    public Transform muzzlePoint;
    public GameObject muzzleFlashPrefab;
    public BulletTrail bulletTrailPrefab;
    
    private Camera playerCamera;
    private AudioSource audioSource;
    private WeaponController weaponController;
    
    void Start()
    {
        playerCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        weaponController = GetComponent<WeaponController>();
        
        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    // 调用这个方法来执行射击
    public void PerformShoot()
    {
        if (playerCamera == null) return;
        
        // 播放射击音效
        PlayRandomSound(gunShotSounds);
        
        // 枪口火焰
        ShowMuzzleFlash();
        
        // 射线检测
        Vector3 shootDirection = GetShootDirection();
        PerformRaycast(shootDirection);
    }
    
    Vector3 GetShootDirection()
    {
        Vector3 direction = playerCamera.transform.forward;
        
        // 添加散布
        if (weaponController != null)
        {
            float spread = weaponController.GetCurrentSpread();
            direction += playerCamera.transform.right * Random.Range(-spread, spread);
            direction += playerCamera.transform.up * Random.Range(-spread, spread);
        }
        
        return direction.normalized;
    }
    
    void PerformRaycast(Vector3 direction)
    {
        Vector3 origin = playerCamera.transform.position;
        
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, targetLayers))
        {
            ProcessHit(hit);
            
            // 子弹轨迹到击中点
            if (bulletTrailPrefab && muzzlePoint)
            {
                CreateBulletTrail(muzzlePoint.position, hit.point);
            }
        }
        else
        {
            // 子弹轨迹到最远距离
            if (bulletTrailPrefab && muzzlePoint)
            {
                Vector3 endPoint = origin + direction * maxRange;
                CreateBulletTrail(muzzlePoint.position, endPoint);
            }
        }
    }
    
    void ProcessHit(RaycastHit hit)
    {
        // 伤害处理
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float damage = weaponController?.damage ?? 25f;
            damageable.TakeDamage(damage);
            
            // 敌人击中音效
            PlayRandomSound(hitSounds);
            
            // 准星击中反馈
            NotifyCrosshairHit();
        }
        else
        {
            // 环境击中音效
            PlayRandomSound(ricochetSounds);
        }
        
        // 击中特效
        CreateHitEffect(hit.point, hit.normal);
        
        // 弹孔
        CreateBulletHole(hit.point, hit.normal, hit.collider);
    }
    
    void ShowMuzzleFlash()
    {
        if (muzzleFlashPrefab && muzzlePoint)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 0.1f);
        }
    }
    
    void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrailPrefab)
        {
            BulletTrail trail = Instantiate(bulletTrailPrefab);
            trail.InitializeTrail(start, end);
        }
    }
    
    void CreateHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
    }
    
    void CreateBulletHole(Vector3 position, Vector3 normal, Collider hitCollider)
    {
        if (bulletHolePrefab && hitCollider.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            // 稍微往里一点，避免Z-fighting
            Vector3 holePosition = position + normal * 0.01f;
            GameObject hole = Instantiate(bulletHolePrefab, holePosition, Quaternion.LookRotation(-normal));
            
            // 设为击中物体的子对象
            hole.transform.SetParent(hitCollider.transform);
            
            // 一段时间后销毁
            Destroy(hole, 30f);
        }
    }
    
    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && audioSource)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    void NotifyCrosshairHit()
    {
        // 通知UI显示击中反馈
        PlayerUI playerUI = FindObjectOfType<PlayerUI>();
        if (playerUI)
        {
            playerUI.OnWeaponHit();
        }
    }
}