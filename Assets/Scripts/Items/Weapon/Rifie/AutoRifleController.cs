// 在你现有的WeaponController基础上创建新武器
// 例如：自动步枪

using UnityEngine;

public class AutoRifleController : WeaponController
{
    [Header("自动步枪专用设置")]
    public float burstFireRate = 0.08f;  // 连发射速
    public int burstCount = 3;           // 连发数量
    public float burstDelay = 0.4f;      // 连发间隔
    
    [Header("后坐力")]
    public float recoilIncrease = 0.01f; // 连射后坐力递增
    public float maxRecoil = 0.15f;      // 最大后坐力
    
    private bool isBursting = false;
    private int currentBurstCount = 0;
    private float currentRecoil = 0f;
    
    public override void Initialize(WeaponManager manager)
    {
        base.Initialize(manager);
        
        // 自动步枪特殊设置
        weaponName = "自动步枪";
        weaponType = WeaponType.Rifle;
        isAutomatic = true;
        fireRate = 0.12f;
        damage = 35f;
        maxAmmo = 30;
        currentAmmo = 30;
    }
    
    public override void TryShoot()
    {
        if (Time.time < nextFireTime || isReloading || isBursting)
            return;
        
        if (currentAmmo <= 0)
        {
            PlayEmptySound();
            return;
        }
        
        // 开始连发
        StartCoroutine(BurstFire());
    }
    
    System.Collections.IEnumerator BurstFire()
    {
        isBursting = true;
        currentBurstCount = 0;
        
        while (currentBurstCount < burstCount && currentAmmo > 0 && isShooting)
        {
            // 执行单发射击
            ExecuteSingleShot();
            currentBurstCount++;
            
            if (currentBurstCount < burstCount)
            {
                yield return new WaitForSeconds(burstFireRate);
            }
        }
        
        // 连发完成，等待下次射击
        nextFireTime = Time.time + burstDelay;
        isBursting = false;
        
        // 后坐力恢复
        StartCoroutine(RecoverRecoil());
    }
    
    void ExecuteSingleShot()
    {
        if (!infiniteAmmo)
            currentAmmo--;
        
        // 增加后坐力
        currentRecoil = Mathf.Min(currentRecoil + recoilIncrease, maxRecoil);
        
        // 播放射击音效
        if (shootSound)
            audioSource.PlayOneShot(shootSound);
        
        // 枪口火焰
        ShowMuzzleFlash();
        
        // 执行射击
        PerformAdvancedRaycast();
        
        // 通知WeaponDisplay
        NotifyWeaponDisplay();
    }
    
    void PerformAdvancedRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        Vector3 direction = cam.transform.forward;
        
        // 应用当前后坐力
        float totalSpread = baseSpread + currentRecoil;
        direction += cam.transform.right * Random.Range(-totalSpread, totalSpread);
        direction += cam.transform.up * Random.Range(-totalSpread, totalSpread);
        
        // 射线检测
        if (Physics.Raycast(cam.transform.position, direction, out RaycastHit hit, range))
        {
            // 处理击中
            ProcessHit(hit);
        }
    }
    
    void ProcessHit(RaycastHit hit)
    {
        // 伤害处理
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        // 击中效果
        if (impactEffect)
        {
            GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f);
        }
        
        // 子弹轨迹
        if (muzzlePoint)
        {
            CreateBulletTrail(muzzlePoint.position, hit.point);
        }
    }
    
    System.Collections.IEnumerator RecoverRecoil()
    {
        while (currentRecoil > baseSpread)
        {
            currentRecoil = Mathf.Max(baseSpread, currentRecoil - recoilRecovery * Time.deltaTime);
            yield return null;
        }
    }
    
    void ShowMuzzleFlash()
    {
        if (muzzleFlash && muzzlePoint)
        {
            GameObject flash = Instantiate(muzzleFlash, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 0.05f);
        }
    }
    
    void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrail)
        {
            GameObject trail = Instantiate(bulletTrail);
            BulletTrail trailComponent = trail.GetComponent<BulletTrail>();
            if (trailComponent)
            {
                trailComponent.InitializeTrail(start, end);
            }
        }
    }
    
    void PlayEmptySound()
    {
        if (emptySound && !audioSource.isPlaying)
            audioSource.PlayOneShot(emptySound);
    }
    
    void NotifyWeaponDisplay()
    {
        // 通知2D武器显示系统
        WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
        if (weaponDisplay)
        {
            weaponDisplay.OnWeaponFired();
        }
    }
    
    public override float GetCurrentSpread()
    {
        return currentRecoil;
    }
    
    public override void StopShooting()
    {
        base.StopShooting();
        isBursting = false;
    }
}