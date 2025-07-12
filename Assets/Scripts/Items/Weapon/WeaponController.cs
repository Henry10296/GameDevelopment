using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public abstract class WeaponController : MonoBehaviour
{
    [Header("基础设置")]
    public string weaponName = "Weapon";
    public WeaponType weaponType;
    public GameObject weaponModel;
    [Header("武器数据配置")]
    public WeaponData weaponData;
    [Header("射击设置")]
    public float fireRate = 0.1f;
    public float damage = 25f;
    public float range = 100f;
    public bool isAutomatic = true;
    
    [Header("弹药设置")]
    public int maxAmmo = 30;
    public int currentAmmo = 30;
    public float reloadTime = 2f;
    public bool infiniteAmmo = false;
    
    [Header("精度设置")]
    public float baseSpread = 0.01f;
    public float maxSpread = 0.1f;
    public float spreadIncrease = 0.02f;
    public float spreadDecrease = 0.1f;
    public float aimSpreadMultiplier = 0.3f;
    
    [Header("后坐力设置")]
    public Vector2 recoilAmount = new Vector2(0.5f, 1f);
    public float recoilSpeed = 10f;
    public float recoilRecovery = 5f;
    
    [Header("瞄准设置")]
    public Transform aimPosition;
    public float aimSpeed = 10f;
    public float aimFOVMultiplier = 0.8f;
    
    [Header("效果")]
    public GameObject muzzleFlash;
    public Transform muzzlePoint;
    public GameObject bulletTrail;
    public GameObject impactEffect;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    // 内部变量
    protected WeaponManager weaponManager;
    protected AudioSource audioSource;
    protected bool isReloading = false;
    protected bool isShooting = false;
    protected bool isAiming = false;
    protected float nextFireTime = 0f;
    protected float currentSpread;
    protected Vector2 currentRecoil;
    
    [HideInInspector] public Vector3 originalPosition;
    

    
 
    public virtual void Initialize(WeaponManager manager)
    {
        weaponManager = manager;
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 从WeaponData读取配置
        if (weaponData != null)
        {
            weaponName = weaponData.weaponName;
            damage = weaponData.damage;
            range = weaponData.range;
            fireRate = weaponData.fireRate;
            maxAmmo = weaponData.maxAmmo;
            isAutomatic = weaponData.isAutomatic;
            
            // 设置音效
            shootSound = weaponData.fireSound;
        }
        
        originalPosition = transform.localPosition;
        currentAmmo = maxAmmo;
        currentSpread = baseSpread;
    }
    public virtual void TryShoot()
    {
        if (Time.time < nextFireTime || isReloading)
            return;
    
        // 检查武器弹药
        if (currentAmmo <= 0)
        {
            // 武器没弹了，检查背包
            CheckBackpackAmmo();
            return;
        }
    
        // 单发或自动射击
        if (!isShooting || isAutomatic)
        {
            Shoot();
            isShooting = true;
        }
    }
    // 获取武器弹药类型
    public virtual void StopShooting()
    {
        isShooting = false;
    }
    
    protected virtual void Shoot()
    {
        nextFireTime = Time.time + fireRate;
    
        if (!infiniteAmmo)
            currentAmmo--;
    
        // 播放音效
        if (shootSound)
            audioSource.PlayOneShot(shootSound);
    
        // 枪口火焰
        if (muzzleFlash && muzzlePoint)
        {
            GameObject flash = Instantiate(muzzleFlash, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 0.1f);
        }
    
        // 执行射击逻辑
        PerformRaycast();
    
        // 增加散布
        currentSpread = Mathf.Min(currentSpread + spreadIncrease, maxSpread);
    
        // 应用后坐力
        ApplyRecoil();
    
        // 通知弹药变化 (新增)
        NotifyAmmoChanged();
    
        // 增强射击效果
        Shooting enhancer = GetComponent<Shooting>();
        if (enhancer)
            enhancer.PerformShoot();
    }
    
    protected virtual void PerformRaycast()
    {
        Camera cam = Camera.main;
        Vector3 direction = cam.transform.forward;
    
        // 应用散布
        float spread = isAiming ? currentSpread * aimSpreadMultiplier : currentSpread;
        direction += cam.transform.right * Random.Range(-spread, spread);
        direction += cam.transform.up * Random.Range(-spread, spread);
    
        Vector3 startPos = cam.transform.position;
    
        // 发射子弹轨迹（视觉效果）
        CreateBulletTrail(startPos, direction);
    
        // 射线检测伤害
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, range))
        {
            ProcessHit(hit);
        }
    }
    protected virtual void ProcessHit(RaycastHit hit)
{
    // 伤害处理
    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
    if (damageable != null)
    {
        float finalDamage = CalculateDamage(hit);
        damageable.TakeDamage(finalDamage);
        
        Debug.Log($"[WeaponController] 击中目标: {hit.collider.name}, 伤害: {finalDamage}");
        
        // 通知准星击中
        NotifyCrosshairHit();
    }
    
    // 击中效果
    CreateHitEffect(hit.point, hit.normal);
    
    // 弹孔效果（可选）
    CreateBulletHole(hit.point, hit.normal, hit.collider);
}

/// <summary>
/// 计算伤害值
/// </summary>
protected virtual float CalculateDamage(RaycastHit hit)
{
    float baseDamage = damage;
    
    // 距离伤害衰减
    float distance = hit.distance;
    float damageMultiplier = 1f;
    
    if (distance > range * 0.7f)
    {
        // 远距离伤害衰减
        damageMultiplier = Mathf.Lerp(1f, 0.6f, (distance - range * 0.7f) / (range * 0.3f));
    }
    
    // 部位伤害倍数
    if (hit.collider.CompareTag("Head"))
    {
        damageMultiplier *= 2f; // 头部暴击
    }
    else if (hit.collider.CompareTag("Torso"))
    {
        damageMultiplier *= 1.2f; // 躯干伤害
    }
    
    return baseDamage * damageMultiplier;
}

/// <summary>
/// 创建击中特效
/// </summary>
protected virtual void CreateHitEffect(Vector3 position, Vector3 normal)
{
    if (impactEffect)
    {
        GameObject effect = Instantiate(impactEffect, position, Quaternion.LookRotation(normal));
        Destroy(effect, 2f);
    }
}

/// <summary>
/// 创建弹孔效果
/// </summary>
protected virtual void CreateBulletHole(Vector3 position, Vector3 normal, Collider hitCollider)
{
    // 只在非敌人表面创建弹孔
    if (hitCollider.gameObject.layer != LayerMask.NameToLayer("Enemy"))
    {
        Vector3 holePosition = position + normal * 0.01f;
        // 这里可以创建弹孔预制体
        Debug.Log($"[WeaponController] 创建弹孔: {holePosition}");
    }
}

/// <summary>
/// 通知准星击中目标
/// </summary>
protected virtual void NotifyCrosshairHit()
{
    // 通知准星显示击中反馈
    SimpleMouseCrosshair crosshair = FindObjectOfType<SimpleMouseCrosshair>();
    if (crosshair)
    {
        crosshair.OnTargetHit();
    }
    
    // 通知PlayerUI显示击中标记
    PlayerUI playerUI = FindObjectOfType<PlayerUI>();
    if (playerUI)
    {
        playerUI.OnWeaponHit();
    }
}

/// <summary>
/// 更新弹药显示（兼容旧方法）
/// </summary>
protected virtual void UpdateAmmoDisplay()
{
    // 这个方法和NotifyAmmoChanged()功能相同，为了兼容性保留
    NotifyAmmoChanged();
    
    // 额外更新背包弹药显示
    if (InventoryManager.Instance)
    {
        string ammoType = GetAmmoType();
        int backpackAmmo = InventoryManager.Instance.GetAmmoCount(ammoType);
        Debug.Log($"[WeaponController] 背包弹药: {ammoType} x{backpackAmmo}");
    }
}
    void CreateBulletTrail(Vector3 start, Vector3 direction)
    {
        Vector3 endPos = start + direction * range;
    
        // 如果击中了什么，调整终点
        if (Physics.Raycast(start, direction, out RaycastHit hit, range))
        {
            endPos = hit.point;
        }
    
        // 使用你现有的BulletTrail
        if (bulletTrail)
        {
            GameObject trail = Instantiate(bulletTrail);
            BulletTrail trailComponent = trail.GetComponent<BulletTrail>();
            if (trailComponent)
            {
                trailComponent.InitializeTrail(start, endPos);
            }
        }
    }
    
    protected virtual void ApplyRecoil()
    {
        float recoilX = Random.Range(-recoilAmount.x, recoilAmount.x);
        float recoilY = Random.Range(recoilAmount.y * 0.8f, recoilAmount.y);
        
        currentRecoil += new Vector2(recoilX, recoilY);
    }
    
    public virtual void Reload()
    {
        if (isReloading || currentAmmo == maxAmmo)
            return;
    
        if (!InventoryManager.Instance)
        {
            Debug.LogWarning("[WeaponController] InventoryManager not found!");
            return;
        }
    
        string ammoType = GetAmmoType();
    
        if (!InventoryManager.Instance.HasAmmo(ammoType, 1))
        {
            ShowNoAmmoMessage();
            return;
        }
    
        StartCoroutine(ReloadFromBackpack(ammoType));
    }
    
    protected virtual IEnumerator ReloadFromBackpack(string ammoType)
    {
        isReloading = true;
        Debug.Log($"[WeaponController] 开始换弹: {ammoType}");
    
        if (reloadSound)
            audioSource.PlayOneShot(reloadSound);
    
        // 播放换弹动画
        yield return PlayReloadAnimation();
    
        // 计算需要的弹药数量
        int needAmmo = maxAmmo - currentAmmo;
        int availableAmmo = InventoryManager.Instance.GetAmmoCount(ammoType);
        int actualReload = Mathf.Min(needAmmo, availableAmmo);
    
        Debug.Log($"[WeaponController] 需要弹药:{needAmmo}, 可用弹药:{availableAmmo}, 实际装弹:{actualReload}");
    
        // 从背包消耗弹药并装入武器
        if (InventoryManager.Instance.ConsumeAmmo(ammoType, actualReload))
        {
            currentAmmo += actualReload;
        
            if (UIManager.Instance)
            {
                UIManager.Instance.ShowMessage($"装弹完成 {currentAmmo}/{maxAmmo}", 2f);
            }
        
            Debug.Log($"[WeaponController] 换弹成功: {currentAmmo}/{maxAmmo}");
        }
        else
        {
            Debug.LogError("[WeaponController] 换弹失败: 无法消耗弹药");
        }
    
        isReloading = false;
    
        // 通知UI更新弹药显示
        NotifyAmmoChanged();
    }

    #region 弹药
    /// <summary>
    /// 获取武器使用的弹药类型
    /// </summary>
    protected virtual string GetAmmoType()
    {
        if (weaponData != null && !string.IsNullOrEmpty(weaponData.ammoType))
        {
            return weaponData.ammoType;
        }
        return "9mm"; // 默认弹药类型
    }

    /// <summary>
    /// 检查背包弹药状态
    /// </summary>
    protected virtual void CheckBackpackAmmo()
    {
        if (!InventoryManager.Instance)
        {
            PlayEmptySound();
            return;
        }
    
        string ammoType = GetAmmoType();
        if (InventoryManager.Instance.HasAmmo(ammoType, 1))
        {
            // 有弹药，提示换弹
            ShowReloadPrompt();
        }
        else
        {
            // 没有弹药
            ShowNoAmmoMessage();
        }
    
        PlayEmptySound();
    }

    /// <summary>
    /// 显示换弹提示
    /// </summary>
    protected virtual void ShowReloadPrompt()
    {
        if (UIManager.Instance)
        {
            UIManager.Instance.ShowMessage("按 R 键换弹", 2f);
        }
        Debug.Log("[WeaponController] 提示换弹");
    }

    /// <summary>
    /// 显示没有弹药提示
    /// </summary>
    protected virtual void ShowNoAmmoMessage()
    {
        string ammoType = GetAmmoType();
        if (UIManager.Instance)
        {
            string displayName = InventoryManager.Instance?.GetAmmoDisplayName(ammoType) ?? ammoType;
            UIManager.Instance.ShowMessage($"没有{displayName}!", 3f);
        }
        Debug.Log($"[WeaponController] 没有弹药: {ammoType}");
    }

    /// <summary>
    /// 通知弹药变化
    /// </summary>
    protected virtual void NotifyAmmoChanged()
    {
        if (UIManager.Instance)
        {
            UIManager.Instance.UpdateAmmoDisplay(currentAmmo, maxAmmo);
        }
    
        Debug.Log($"[WeaponController] 弹药变化: {currentAmmo}/{maxAmmo}");
    }

    /// <summary>
    /// 播放空弹夹音效
    /// </summary>
    protected virtual void PlayEmptySound()
    {
        if (emptySound && audioSource && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(emptySound);
        }
    }
    
    #endregion
    
    
    
  
    
    protected virtual IEnumerator PlayReloadAnimation()
    {
        // 简单的下降和上升动画
        Vector3 startPos = transform.localPosition;
        Vector3 downPos = startPos - Vector3.up * 0.3f;
        
        // 下降
        float timer = 0f;
        while (timer < reloadTime * 0.3f)
        {
            timer += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(startPos, downPos, timer / (reloadTime * 0.3f));
            yield return null;
        }
        
        // 等待
        yield return new WaitForSeconds(reloadTime * 0.4f);
        
        // 上升
        timer = 0f;
        while (timer < reloadTime * 0.3f)
        {
            timer += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(downPos, startPos, timer / (reloadTime * 0.3f));
            yield return null;
        }
        
        transform.localPosition = startPos;
    }
    
    public virtual void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }
    
    void Update()
    {
        // 减少散布
        if (!isShooting)
        {
            currentSpread = Mathf.Max(baseSpread, currentSpread - spreadDecrease * Time.deltaTime);
        }
        
        // 恢复后坐力
        currentRecoil = Vector2.Lerp(currentRecoil, Vector2.zero, recoilRecovery * Time.deltaTime);
        
        // 瞄准位置插值
        if (aimPosition != null)
        {
            Vector3 targetPos = isAiming ? aimPosition.localPosition : originalPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, aimSpeed * Time.deltaTime);
        }
    }
    
    // 公共属性
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsReloading => isReloading;
    public bool HasAmmo() => currentAmmo > 0 || infiniteAmmo;
    public virtual float GetCurrentSpread() => currentSpread;
    public Vector2 GetRecoil() => currentRecoil;
}
internal interface IDamageable
{ 
    void TakeDamage(float damage);
    float GetCurrentHealth(); // 新增
    float GetMaxHealth();     // 新增
    bool IsAlive();          // 新增
    
}


