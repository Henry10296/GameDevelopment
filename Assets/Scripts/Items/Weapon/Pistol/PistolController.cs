using UnityEngine;
using System.Collections;

public class PistolController : WeaponController
{
    [Header("手枪专用设置")]
    public float semiAutoFireRate = 0.15f;    // 半自动射击间隔
    public float quickDrawBonus = 0.8f;       // 快速拔枪加成
    public float accuracyBonus = 0.9f;        // 精度加成
    
    [Header("射击模式")]
    public bool enableRapidFire = false;      // 是否允许快速点击
    public float rapidFireThreshold = 0.1f;   // 快速点击阈值
    
    [Header("后坐力设置")]
    public float pistolRecoilMultiplier = 0.7f;
    public float recoilRecoverySpeed = 8f;
    
    [Header("瞄准设置")]
    public float pistolAimSpeed = 12f;
    public float aimAccuracyBonus = 0.5f;
    
    private bool lastFrameInput = false;
    private float lastShotTime = 0f;
    private int rapidFireCount = 0;
    private float rapidFireWindow = 1f;
    
    public override void Initialize(WeaponManager manager)
    {
        base.Initialize(manager);
        
        // 手枪特殊设置
        weaponType = WeaponType.Pistol;
        isAutomatic = false;
        fireRate = semiAutoFireRate;
        
        // 从配置读取数据
        if (weaponData != null)
        {
            damage = weaponData.damage;
            range = weaponData.range;
            maxAmmo = weaponData.maxAmmo;
            currentAmmo = maxAmmo;
            
            // 手枪特有属性
            baseSpread *= accuracyBonus;
            aimSpreadMultiplier = aimAccuracyBonus;
        }
        
        Debug.Log($"[PistolController] Initialized: {weaponName}");
    }
    protected override string GetAmmoType()
    {
        return weaponData?.ammoType ?? "9mm";
    }
    /*public override void TryShoot()
    {
        // 检查基本射击条件
        if (Time.time < nextFireTime || isReloading)
            return;
        
        if (currentAmmo <= 0)
        {
            PlayEmptySound();
            return;
        }
        
        // 半自动射击检测
        bool canShoot = false;
        
        if (enableRapidFire)
        {
            // 允许快速点击模式
            canShoot = Input.GetMouseButtonDown(0) || 
                      (Input.GetMouseButton(0) && Time.time - lastShotTime > rapidFireThreshold);
        }
        else
        {
            // 标准半自动模式 - 只响应按键按下
            canShoot = Input.GetMouseButtonDown(0);
        }
        
        if (canShoot)
        {
            ExecutePistolShot();
        }
    }*/
    public override void TryShoot()
    {
        if (Time.time < nextFireTime || isReloading)
            return;
    
        // 手枪单发射击检查
        if (Input.GetMouseButtonDown(0)) // 只响应按键按下，不是持续按住
        {
            if (currentAmmo > 0)
            {
                ExecutePistolShot();
            }
            else
            {
                // 检查背包弹药
                CheckBackpackAmmo();
            }
        }
    }

    void CheckAmmoStatus()
    {
        if (!InventoryManager.Instance) return;
        
        string ammoType = GetAmmoType();
        if (InventoryManager.Instance.HasAmmo(ammoType, 1))
        {
            ShowReloadPrompt();
        }
        else
        {
            ShowNoAmmoMessage();
        }
        
        PlayEmptySound();
    }
    void ExecutePistolShot()
    {
        nextFireTime = Time.time + fireRate;
        lastShotTime = Time.time;
    
        if (!infiniteAmmo)
            currentAmmo--;
    
        // 播放射击音效
        PlayShootSound();
    
        // 执行射击
        PerformPistolRaycast();
    
        // 后坐力效果
        ApplyPistolRecoil();
    
        // 通知其他系统
        NotifyAmmoChanged();
    
        Debug.Log($"[PistolController] 手枪射击! 弹药: {currentAmmo}/{maxAmmo}");
    }
    void PerformPistolRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
    
        Vector3 direction = cam.transform.forward;
    
        // 手枪精度较高
        float currentAccuracy = GetCurrentAccuracy();
        direction += GetSpreadDirection(currentAccuracy);
    
        // 射线检测
        if (Physics.Raycast(cam.transform.position, direction, out RaycastHit hit, range))
        {
            ProcessHit(hit);
            CreateBulletTrail(GetMuzzlePosition(), hit.point);
        }
        else
        {
            Vector3 endPoint = cam.transform.position + direction * range;
            CreateBulletTrail(GetMuzzlePosition(), endPoint);
        }
    }
    
    float GetCurrentAccuracy()
    {
        float accuracy = baseSpread;
    
        // 瞄准时精度提升
        if (isAiming)
        {
            accuracy *= aimSpreadMultiplier;
        }
    
        return Mathf.Clamp(accuracy, baseSpread, maxSpread);
    }

    Vector3 GetSpreadDirection(float spreadAmount)
    {
        return new Vector3(
            Random.Range(-spreadAmount, spreadAmount),
            Random.Range(-spreadAmount, spreadAmount),
            0f
        );
    }

    Vector3 GetMuzzlePosition()
    {
        return muzzlePoint != null ? muzzlePoint.position : transform.position;
    }

    void PlayShootSound()
    {
        if (shootSound && audioSource)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f); // 轻微变调
            audioSource.PlayOneShot(shootSound);
        }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /*
    float GetCurrentAccuracy()
    {
        float accuracy = baseSpread;
        
        // 瞄准时精度提升
        if (isAiming)
        {
            accuracy *= aimSpreadMultiplier;
        }
        
        // 快速射击精度降低
        if (enableRapidFire && rapidFireCount > 3)
        {
            accuracy *= (1f + rapidFireCount * 0.1f);
        }
        
        // 移动时精度降低
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && player.GetCurrentSpeed() > 0.1f)
        {
            accuracy *= 1.3f;
            
            if (player.IsRunning())
            {
                accuracy *= 1.5f;
            }
        }
        
        return Mathf.Clamp(accuracy, baseSpread, maxSpread);
    }
    
    Vector3 GetSpreadDirection(float spreadAmount)
    {
        Camera cam = Camera.main;
        return new Vector3(
            Random.Range(-spreadAmount, spreadAmount),
            Random.Range(-spreadAmount, spreadAmount),
            0f
        );
    }*/
    
    void ProcessHit(RaycastHit hit)
    {
        // 伤害处理
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float finalDamage = CalculateDamage(hit);
            damageable.TakeDamage(finalDamage);
            
            // 通知准星击中
            NotifyCrosshairHit();
        }
        
        // 击中效果
        CreateHitEffect(hit.point, hit.normal);
        
        // 弹孔
        CreateBulletHole(hit.point, hit.normal, hit.collider);
    }
    
    float CalculateDamage(RaycastHit hit)
    {
        float baseDamage = damage;
        
        // 距离损伤计算
        float distance = hit.distance;
        float damageMultiplier = 1f;
        
        if (distance > range * 0.7f)
        {
            // 远距离伤害衰减
            damageMultiplier = Mathf.Lerp(1f, 0.6f, (distance - range * 0.7f) / (range * 0.3f));
        }
        
        // 暴击检测（头部等）
        if (hit.collider.CompareTag("Head"))
        {
            damageMultiplier *= 2f;
        }
        else if (hit.collider.CompareTag("Torso"))
        {
            damageMultiplier *= 1.2f;
        }
        
        return baseDamage * damageMultiplier;
    }
    
    void ApplyPistolRecoil()
    {
        Vector2 recoilForce = new Vector2(
            Random.Range(-recoilAmount.x, recoilAmount.x) * pistolRecoilMultiplier,
            Random.Range(recoilAmount.y * 0.8f, recoilAmount.y) * pistolRecoilMultiplier
        );
        
        currentRecoil += recoilForce;
        
        // 视觉后坐力
        StartCoroutine(VisualRecoilEffect());
    }
    
    IEnumerator VisualRecoilEffect()
    {
        Vector3 originalPos = transform.localPosition;
        Vector3 recoilPos = originalPos - Vector3.forward * 0.05f;
        
        // 快速后退
        float elapsed = 0f;
        while (elapsed < 0.05f)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(originalPos, recoilPos, elapsed / 0.05f);
            yield return null;
        }
        
        // 缓慢恢复
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(recoilPos, originalPos, elapsed / 0.15f);
            yield return null;
        }
        
        transform.localPosition = originalPos;
    }
    
    IEnumerator ResetRapidFireCount()
    {
        yield return new WaitForSeconds(rapidFireWindow);
        rapidFireCount = Mathf.Max(0, rapidFireCount - 1);
    }
    
    public override void SetAiming(bool aiming)
    {
        base.SetAiming(aiming);
        
        // 手枪瞄准速度更快
        if (aimPosition != null)
        {
            float targetAimSpeed = aiming ? pistolAimSpeed : pistolAimSpeed * 0.8f;
            // 这里可以调整瞄准动画速度
        }
    }
    
    /*Vector3 GetMuzzlePosition()
    {
        return muzzlePoint != null ? muzzlePoint.position : transform.position;
    }
    
    void PlayShootSound()
    {
        if (shootSound && audioSource)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f); // 轻微变调
            audioSource.PlayOneShot(shootSound);
        }
    }*/
    
    void PlayEmptySound()
    {
        if (emptySound && audioSource && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(emptySound);
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
    
    void CreateHitEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffect)
        {
            GameObject effect = Instantiate(impactEffect, position, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
    }
    
    void CreateBulletHole(Vector3 position, Vector3 normal, Collider hitCollider)
    {
        // 只在非敌人表面创建弹孔
        if (hitCollider.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            Vector3 holePosition = position + normal * 0.01f;
            // 这里可以创建弹孔预制体
        }
    }
    
    void NotifyWeaponFired()
    {
        // 通知武器显示系统
        WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
        if (weaponDisplay)
        {
            weaponDisplay.OnWeaponFired();
        }
        
        // 通知UI更新弹药显示
        UIManager.Instance?.UpdateAmmoDisplay(currentAmmo, maxAmmo);
    }
    
    void NotifyCrosshairHit()
    {
        // 通知准星击中反馈
        SimpleMouseCrosshair crosshair = FindObjectOfType<SimpleMouseCrosshair>();
        if (crosshair)
        {
            crosshair.OnTargetHit();
        }
    }
    
    public override float GetCurrentSpread()
    {
        return GetCurrentAccuracy();
    }
    
    // 调试信息
    void OnDrawGizmosSelected()
    {
        if (muzzlePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * range);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(muzzlePoint.position, 0.1f);
        }
    }
}