using UnityEngine;
using System.Collections;

public class PistolController : WeaponController
{
    [Header("手枪专用设置")]
    public float semiAutoFireRate = 0.15f;
    public float quickDrawBonus = 0.8f;
    public float accuracyBonus = 0.9f;
    
    [Header("后坐力设置")]
    public float pistolRecoilMultiplier = 0.7f;
    public float recoilRecoverySpeed = 8f;
    
    [Header("瞄准设置")]
    public float pistolAimSpeed = 12f;
    public float aimAccuracyBonus = 0.5f;
    
    private float lastShotTime = 0f;
    
    public override void Initialize(WeaponManager manager)
    {
        base.Initialize(manager);
        
        // 手枪特殊设置
        weaponType = WeaponType.Pistol;
        isAutomatic = false; // 手枪是半自动的
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
    
    // 重写射击方法以确保半自动逻辑
    public override void TryShoot()
    {
        if (Time.time < nextFireTime || isReloading)
            return;
        
        // 手枪单发射击检查 - 只响应按键按下
        if (Input.GetMouseButtonDown(0))
        {
            if (currentAmmo > 0)
            {
                ExecutePistolShot();
            }
            else
            {
                CheckBackpackAmmo();
            }
        }
    }
    
    protected virtual void ExecutePistolShot()
    {
        nextFireTime = Time.time + fireRate;
        lastShotTime = Time.time;
        
        if (!infiniteAmmo)
            currentAmmo--;
        
        // 播放射击音效
        PlayShootSound();
        
        // 执行手枪射击
        PerformPistolShot();
        
        // 手枪后坐力
        ApplyPistolRecoil();
        
        // 通知系统
        NotifyAmmoChanged();
        
        // 通知音响系统
        if (SoundManager.Instance && weaponData != null)
        {
            SoundManager.Instance.AlertEnemies(transform.position, weaponData.noiseRadius);
        }
        
        Debug.Log($"[PistolController] 手枪射击! 弹药: {currentAmmo}/{maxAmmo}");
    }
    
    protected virtual void PerformPistolShot()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        Vector3 shootOrigin = cam.transform.position;
        Vector3 direction = GetPistolShootDirection(cam);
        
        // 射线检测
        if (Physics.Raycast(shootOrigin, direction, out RaycastHit hit, range))
        {
            ProcessHit(hit);
            CreateBulletTrail(GetMuzzlePosition(), hit.point);
        }
        else
        {
            Vector3 endPoint = shootOrigin + direction * range;
            CreateBulletTrail(GetMuzzlePosition(), endPoint);
        }
        
        // 枪口火焰
        ShowMuzzleFlash();
    }
    
    protected virtual Vector3 GetPistolShootDirection(Camera cam)
    {
        Vector3 direction = cam.transform.forward;
        
        // 手枪精度较高
        float currentAccuracy = GetCurrentAccuracy();
        direction += GetSpreadDirection(currentAccuracy, cam);
        
        return direction.normalized;
    }
    
    protected virtual float GetCurrentAccuracy()
    {
        float accuracy = baseSpread;
        
        // 瞄准时精度提升
        if (isAiming)
        {
            accuracy *= aimSpreadMultiplier;
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
    
    protected virtual Vector3 GetSpreadDirection(float spreadAmount, Camera cam)
    {
        return new Vector3(
            Random.Range(-spreadAmount, spreadAmount),
            Random.Range(-spreadAmount, spreadAmount),
            0f
        );
    }
    
    protected override void ApplyRecoil()
    {
        ApplyPistolRecoil();
    }
    
    protected virtual void ApplyPistolRecoil()
    {
        Vector2 recoilForce = new Vector2(
            Random.Range(-recoilAmount.x, recoilAmount.x) * pistolRecoilMultiplier,
            Random.Range(recoilAmount.y * 0.8f, recoilAmount.y) * pistolRecoilMultiplier
        );
        
        currentRecoil += recoilForce;
        
        // 视觉后坐力
        StartCoroutine(VisualRecoilEffect());
    }
    
    protected virtual IEnumerator VisualRecoilEffect()
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
    
    public override void SetAiming(bool aiming)
    {
        base.SetAiming(aiming);
        
        // 手枪瞄准速度更快
        if (aimPosition != null)
        {
            aimSpeed = aiming ? pistolAimSpeed : pistolAimSpeed * 0.8f;
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