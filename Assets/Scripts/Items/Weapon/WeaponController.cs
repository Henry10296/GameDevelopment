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
    [Header("子弹系统")]
    public GameObject bulletPrefab; // 子弹预制体
    public bool usePhysicalBullets = false; // 是否使用物理子弹
    
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
        
        Debug.Log($"[WeaponController] {weaponName} initialized with {currentAmmo}/{maxAmmo} ammo");
    }
    
    public virtual void TryShoot()
    {
        if (Time.time < nextFireTime || isReloading)
            return;
        
        // 检查武器弹药
        if (currentAmmo <= 0)
        {
            CheckBackpackAmmo();
            return;
        }
        
        // 单发或自动射击
        if (isAutomatic)
        {
            if (Input.GetMouseButton(0))
            {
                ExecuteShot();
                isShooting = true;
            }
            else
            {
                isShooting = false;
            }
        }
        else
        {
            // 半自动 - 只响应按键按下
            if (Input.GetMouseButtonDown(0))
            {
                ExecuteShot();
            }
        }
    }
    
    // 统一的射击执行方法
    protected virtual void ExecuteShot()
    {
        nextFireTime = Time.time + fireRate;
        
        if (!infiniteAmmo)
            currentAmmo--;
        
        // 播放音效
        PlayShootSound();
        
        // 枪口火焰
        ShowMuzzleFlash();
        
        // 选择射击方式
        if (usePhysicalBullets && bulletPrefab != null)
        {
            CreatePhysicalBullet();
        }
        else
        {
            PerformShot(); // 原有的射线检测
        }
        
        // 其他效果
        ApplyRecoil();
        
        // 通知WeaponDisplay播放射击动画
        NotifyWeaponDisplay("fire");
        
        // 通知UI更新
        NotifyAmmoChanged();
        
        // 通知音响系统
        if (SoundManager.Instance && weaponData != null)
        {
            SoundManager.Instance.AlertEnemies(transform.position, weaponData.noiseRadius);
        }
        
        Debug.Log($"[WeaponController] Shot fired! Ammo: {currentAmmo}/{maxAmmo}");
    }
    
    protected virtual void CreatePhysicalBullet()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        Vector3 shootOrigin = GetMuzzlePosition();
        Vector3 shootDirection = GetShootDirection(cam);
        
        // 创建子弹
        GameObject bulletObj = Instantiate(bulletPrefab, shootOrigin, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        
        if (bullet == null)
        {
            bullet = bulletObj.AddComponent<Bullet>();
        }
        
        bullet.Initialize(shootOrigin, shootDirection, damage);
        
        Debug.Log($"发射物理子弹: {bulletObj.name}");
    }

    protected virtual void PerformShot()
    {
        Camera cam = Camera.main;
        if (cam == null) 
        {
            Debug.LogError("[WeaponController] No main camera found!");
            return;
        }
        
        // 修复：确保使用正确的射击起点和方向
        Vector3 shootOrigin = cam.transform.position;
        Vector3 shootDirection = GetShootDirection(cam);
        
        Debug.Log($"[WeaponController] Shooting from {shootOrigin} in direction {shootDirection}");
        
        // 执行射线检测
        if (Physics.Raycast(shootOrigin, shootDirection, out RaycastHit hit, range))
        {
            Debug.Log($"[WeaponController] Hit {hit.collider.name} at {hit.point}");
            ProcessHit(hit);
            CreateBulletTrail(shootOrigin, hit.point);
        }
        else
        {
            Vector3 endPoint = shootOrigin + shootDirection * range;
            CreateBulletTrail(shootOrigin, endPoint);
            Debug.Log($"[WeaponController] Shot missed, end point: {endPoint}");
        }
    }
    
    protected virtual Vector3 GetShootDirection(Camera cam)
    {
        // 修复：使用屏幕中心点进行射线投射，确保方向正确
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        Ray centerRay = cam.ScreenPointToRay(screenCenter);
        Vector3 direction = centerRay.direction;
        
        // 应用散布
        float spread = isAiming ? currentSpread * aimSpreadMultiplier : currentSpread;
        
        if (spread > 0)
        {
            // 使用摄像机坐标系计算散布
            direction += cam.transform.right * Random.Range(-spread, spread);
            direction += cam.transform.up * Random.Range(-spread, spread);
        }
        
        return direction.normalized;
    }
    
    protected virtual Vector3 GetMuzzlePosition()
    {
        if (muzzlePoint != null)
            return muzzlePoint.position;
        
        // 如果没有枪口点，使用摄像机位置稍微向前
        Camera cam = Camera.main;
        if (cam != null)
            return cam.transform.position + cam.transform.forward * 0.5f;
            
        return transform.position;
    }
    
    protected virtual void ProcessHit(RaycastHit hit)
    {
        // 伤害处理
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float finalDamage = CalculateDamage(hit);
            damageable.TakeDamage(finalDamage);
            
            Debug.Log($"[WeaponController] Hit {hit.collider.name} for {finalDamage} damage");
            
            // 通知准星击中
            NotifyCrosshairHit();
        }
        
        // 击中效果
        CreateHitEffect(hit.point, hit.normal);
    }
    
    protected virtual float CalculateDamage(RaycastHit hit)
    {
        float baseDamage = damage;
        
        // 距离伤害衰减
        float distance = hit.distance;
        float damageMultiplier = 1f;
        
        if (distance > range * 0.7f)
        {
            damageMultiplier = Mathf.Lerp(1f, 0.6f, (distance - range * 0.7f) / (range * 0.3f));
        }
        
        // 部位伤害倍数
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
    
    protected virtual void CreateHitEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffect)
        {
            GameObject effect = Instantiate(impactEffect, position, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
    }
    
    protected virtual void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        // 改进的子弹轨迹创建
        GameObject trailObj = new GameObject("BulletTrail");
        LineRenderer line = trailObj.AddComponent<LineRenderer>();

        // 基本设置
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.yellow;
        line.endColor = Color.yellow;
        line.startWidth = 0.02f;
        line.endWidth = 0.01f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.sortingOrder = 100;

        // 设置位置
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        // 开始淡出协程
        StartCoroutine(FadeOutTrail(line, trailObj));

        Debug.Log($"子弹轨迹: {start} -> {end}");
    }
    
    IEnumerator FadeOutTrail(LineRenderer line, GameObject trailObj)
    {
        // 获取初始颜色
        Color startColor = line.startColor;
        float fadeTime = 0.1f;
        float elapsed = 0f;

        while (elapsed < fadeTime && line != null)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeTime);
            Color fadedColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            line.startColor = fadedColor;
            line.endColor = fadedColor;
            yield return null;
        }

        // 销毁轨迹对象
        if (trailObj != null)
            GameObject.Destroy(trailObj);
    }

    protected virtual void ShowMuzzleFlash()
    {
        if (muzzleFlash && muzzlePoint)
        {
            GameObject flash = Instantiate(muzzleFlash, muzzlePoint.position, muzzlePoint.rotation);
            MuzzleFlash flashComponent = flash.GetComponent<MuzzleFlash>();
            if (flashComponent)
            {
                flashComponent.PlayEffect();
            }
            Destroy(flash, 0.2f);
        }
    }
    
    protected virtual void PlayShootSound()
    {
        if (shootSound && audioSource)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(shootSound);
        }
    }
    
    protected virtual void NotifyCrosshairHit()
    {
        SimpleMouseCrosshair crosshair = FindObjectOfType<SimpleMouseCrosshair>();
        if (crosshair)
        {
            crosshair.OnTargetHit();
        }
        
        PlayerUI playerUI = FindObjectOfType<PlayerUI>();
        if (playerUI)
        {
            playerUI.OnWeaponHit();
        }
    }
    
    // 修复：通知WeaponDisplay播放动画
    protected virtual void NotifyWeaponDisplay(string animationType)
    {
        WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
        if (weaponDisplay != null)
        {
            switch (animationType)
            {
                case "fire":
                    weaponDisplay.OnWeaponFired();
                    break;
                case "reload":
                    weaponDisplay.OnWeaponReload();
                    break;
                case "switch":
                    weaponDisplay.OnWeaponSwitch(weaponType);
                    break;
            }
        }
    }
    
    public virtual void StopShooting()
    {
        isShooting = false;
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
        Debug.Log($"[WeaponController] Started reloading {weaponName} with {ammoType}");
    
        if (reloadSound)
            audioSource.PlayOneShot(reloadSound);
        
        // 通知WeaponDisplay播放换弹动画
        NotifyWeaponDisplay("reload");
    
        yield return PlayReloadAnimation();
    
        // 计算需要的弹药数量
        int needAmmo = maxAmmo - currentAmmo;
        int availableAmmo = InventoryManager.Instance.GetAmmoCount(ammoType);
        int actualReload = Mathf.Min(needAmmo, availableAmmo);
    
        Debug.Log($"[WeaponController] Reload calculation: need={needAmmo}, available={availableAmmo}, actual={actualReload}");
    
        // 从背包消耗弹药并装入武器
        if (actualReload > 0 && InventoryManager.Instance.ConsumeAmmo(ammoType, actualReload))
        {
            currentAmmo += actualReload;
        
            if (UIManager.Instance)
            {
                UIManager.Instance.ShowMessage($"{weaponName} 换弹完成 {currentAmmo}/{maxAmmo}", 2f);
            }
        
            Debug.Log($"[WeaponController] Reload complete: {currentAmmo}/{maxAmmo}");
        }
        else
        {
            Debug.LogWarning($"[WeaponController] Reload failed - no ammo consumed");
        }
    
        isReloading = false;
        NotifyAmmoChanged();
    }
    
    protected virtual IEnumerator PlayReloadAnimation()
    {
        Vector3 startPos = transform.localPosition;
        Vector3 downPos = startPos - Vector3.up * 0.3f;
        
        float timer = 0f;
        while (timer < reloadTime * 0.3f)
        {
            timer += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(startPos, downPos, timer / (reloadTime * 0.3f));
            yield return null;
        }
        
        yield return new WaitForSeconds(reloadTime * 0.4f);
        
        timer = 0f;
        while (timer < reloadTime * 0.3f)
        {
            timer += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(downPos, startPos, timer / (reloadTime * 0.3f));
            yield return null;
        }
        
        transform.localPosition = startPos;
    }
    
    #region 弹药系统
    protected virtual string GetAmmoType()
    {
        // 优先从weaponData获取
        if (weaponData != null && !string.IsNullOrEmpty(weaponData.ammoType))
        {
            return weaponData.ammoType;
        }
    
        // 根据武器类型返回默认值
        return weaponType switch
        {
            WeaponType.Pistol => "9mm_Ammo",  
            WeaponType.Rifle => "5.56mm_Ammo",
            _ => "9mm_Ammo"
        };
    }
    
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
            ShowReloadPrompt();
        }
        else
        {
            ShowNoAmmoMessage();
        }
        
        PlayEmptySound();
    }
    
    protected virtual void ShowReloadPrompt()
    {
        if (UIManager.Instance)
        {
            UIManager.Instance.ShowMessage("按 R 键换弹", 2f);
        }
    }
    
    protected virtual void ShowNoAmmoMessage()
    {
        string ammoType = GetAmmoType();
        if (UIManager.Instance)
        {
            string displayName = InventoryManager.Instance?.GetAmmoDisplayName(ammoType) ?? ammoType;
            UIManager.Instance.ShowMessage($"没有{displayName}!", 3f);
        }
    }
    
    protected virtual void NotifyAmmoChanged()
    {
        if (UIManager.Instance)
        {
            UIManager.Instance.UpdateAmmoDisplay(currentAmmo, maxAmmo);
        }
    }
    
    protected virtual void PlayEmptySound()
    {
        if (emptySound && audioSource && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(emptySound);
        }
    }
    #endregion
    
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

public interface IDamageable
{ 
    void TakeDamage(float damage);
    float GetCurrentHealth();
    float GetMaxHealth();
    bool IsAlive();
}