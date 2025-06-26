using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OptimizedWeaponController : MonoBehaviour
{
    [Header("武器配置")]
    public WeaponConfig weaponConfig;
    public Transform firePoint;
    public LayerMask enemyLayer = -1;
    
    [Header("UI引用")]
    public TextMeshProUGUI ammoDisplay;
    public GameObject crosshair;
    
    [Header("音频")]
    public AudioSource audioSource;
    
    [Header("事件")]
    public GameEvent onWeaponFired;
    public GameEvent onReloadComplete;
    public GameEvent onAmmoEmpty;
    
    // 当前武器状态
    private WeaponData currentWeapon;
    private int currentAmmo;
    private float lastFireTime;
    private bool isReloading = false;
    private bool hasRifle = false;
    
    // 输入系统
    private readonly Dictionary<KeyCode, System.Action> inputActions = new();
    private Camera playerCamera;
    
    // 对象池
    private ObjectPool<BulletTrail> bulletTrailPool;
    private ObjectPool<MuzzleFlash> muzzleFlashPool;
    
    void Start()
    {
        InitializeComponents();
        InitializeInputSystem();
        InitializeObjectPools();
        
        // 设置初始武器
        if (weaponConfig?.pistol != null)
            SwitchWeapon(weaponConfig.pistol);
    }
    
    void InitializeComponents()
    {
        playerCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    
    void InitializeInputSystem()
    {
        inputActions[KeyCode.Alpha1] = () => SwitchWeapon(weaponConfig.pistol);
        inputActions[KeyCode.Alpha2] = () => { if (hasRifle) SwitchWeapon(weaponConfig.rifle); };
        inputActions[KeyCode.R] = TryReload;
    }
    
    void InitializeObjectPools()
    {
        bulletTrailPool = new ObjectPool<BulletTrail>(CreateBulletTrail, 20);
        muzzleFlashPool = new ObjectPool<MuzzleFlash>(CreateMuzzleFlash, 10);
    }
    
    BulletTrail CreateBulletTrail()
    {
        // 创建子弹轨迹预制体
        var trail = new GameObject("BulletTrail").AddComponent<BulletTrail>();
        return trail;
    }
    
    MuzzleFlash CreateMuzzleFlash()
    {
        // 创建枪口闪光预制体
        var flash = new GameObject("MuzzleFlash").AddComponent<MuzzleFlash>();
        return flash;
    }
    
    void Update()
    {
        if (isReloading) return;
        
        HandleInputs();
        HandleShooting();
    }
    
    void HandleInputs()
    {
        foreach (var kvp in inputActions)
        {
            if (Input.GetKeyDown(kvp.Key))
                kvp.Value?.Invoke();
        }
    }
    
    void HandleShooting()
    {
        if (currentWeapon == null || currentAmmo <= 0) return;
        
        bool canFire = Time.time - lastFireTime >= currentWeapon.fireRate;
        if (!canFire) return;
        
        bool shouldFire = currentWeapon.isAutomatic ? 
            Input.GetMouseButton(0) : 
            Input.GetMouseButtonDown(0);
        
        if (shouldFire)
        {
            Shoot();
        }
    }
    
    void Shoot()
    {
        lastFireTime = Time.time;
        currentAmmo--;
        
        // 射线检测
        PerformRaycast();
        
        // 视觉效果
        ShowMuzzleFlash();
        CreateBulletTrail();
        
        // 音频效果
        PlayFireSound();
        
        // 敌人警报
        AlertNearbyEnemies();
        
        // 更新UI
        UpdateAmmoDisplay();
        
        // 触发事件
        onWeaponFired?.Raise();
        
        // 检查弹药耗尽
        if (currentAmmo <= 0)
        {
            onAmmoEmpty?.Raise();
        }
    }
    
    void PerformRaycast()
    {
        if (playerCamera == null) return;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range, enemyLayer))
        {
            // 检查命中目标
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(currentWeapon.damage);
                
                // 创建命中效果
                CreateHitEffect(hit.point, hit.normal);
            }
        }
    }
    
    void ShowMuzzleFlash()
    {
        if (firePoint == null) return;
        
        var flash = muzzleFlashPool.Get();
        flash.transform.position = firePoint.position;
        flash.transform.rotation = firePoint.rotation;
        flash.PlayEffect();
        
        // 延迟返回对象池
        StartCoroutine(ReturnMuzzleFlashAfterDelay(flash, 0.1f));
    }
    
    void CreateBulletTrail()
    {
        if (firePoint == null || playerCamera == null) return;
        
        var trail = bulletTrailPool.Get();
        trail.InitializeTrail(firePoint.position, 
            firePoint.position + playerCamera.transform.forward * currentWeapon.range);
        
        StartCoroutine(ReturnBulletTrailAfterDelay(trail, 1f));
    }
    
    void CreateHitEffect(Vector3 position, Vector3 normal)
    {
        // 创建命中粒子效果
        // 可以根据命中材质创建不同效果
    }
    
    void PlayFireSound()
    {
        if (audioSource && currentWeapon.fireSound)
        {
            audioSource.PlayOneShot(currentWeapon.fireSound);
        }
        else if (weaponConfig)
        {
            AudioClip randomSound = weaponConfig.GetRandomGunSound();
            if (randomSound && audioSource)
                audioSource.PlayOneShot(randomSound);
        }
    }
    
    void AlertNearbyEnemies()
    {
        if (SoundManager.Instance)
        {
            SoundManager.Instance.AlertEnemies(transform.position, currentWeapon.noiseRadius);
        }
    }
    
    void TryReload()
    {
        if (isReloading || currentAmmo >= currentWeapon.maxAmmo) return;
        
        // 检查弹药库存
        if (InventoryManager.Instance?.HasItem(currentWeapon.ammoType) == true)
        {
            StartCoroutine(ReloadCoroutine());
        }
        else
        {
            // 播放空仓音效
            if (weaponConfig?.emptyclipSound && audioSource)
                audioSource.PlayOneShot(weaponConfig.emptyclipSound);
        }
    }
    
    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        
        // 播放装弹音效
        if (weaponConfig?.reloadSound && audioSource)
            audioSource.PlayOneShot(weaponConfig.reloadSound);
        
        // 等待装弹时间
        yield return new WaitForSeconds(2f);
        
        // 消耗弹药道具
        if (InventoryManager.Instance?.RemoveItem(currentWeapon.ammoType, 1) == true)
        {
            currentAmmo = currentWeapon.maxAmmo;
            UpdateAmmoDisplay();
            onReloadComplete?.Raise();
        }
        
        isReloading = false;
    }
    
    void SwitchWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) return;
        
        currentWeapon = newWeapon;
        currentAmmo = newWeapon.maxAmmo;
        isReloading = false;
        
        UpdateAmmoDisplay();
        Debug.Log($"[WeaponController] Switched to: {newWeapon.weaponName}");
    }
    
    public void UnlockRifle()
    {
        hasRifle = true;
        DualModeUIManager.Instance?.ShowMessage("获得自动步枪!", 3f);
    }
    
    void UpdateAmmoDisplay()
    {
        if (ammoDisplay && currentWeapon)
        {
            ammoDisplay.text = $"{currentAmmo} / {currentWeapon.maxAmmo}";
        }
        
        // 更新UI管理器中的弹药显示
        if (UIManager.Instance)
        {
            UIManager.Instance.UpdateAmmoDisplay(currentAmmo, currentWeapon.maxAmmo);
        }
    }
    
    IEnumerator ReturnMuzzleFlashAfterDelay(MuzzleFlash flash, float delay)
    {
        yield return new WaitForSeconds(delay);
        muzzleFlashPool.Return(flash);
    }
    
    IEnumerator ReturnBulletTrailAfterDelay(BulletTrail trail, float delay)
    {
        yield return new WaitForSeconds(delay);
        bulletTrailPool.Return(trail);
    }
    
    // 获取武器状态信息
    public WeaponStatus GetWeaponStatus()
    {
        return new WeaponStatus
        {
            currentWeapon = currentWeapon?.weaponName ?? "无",
            currentAmmo = currentAmmo,
            maxAmmo = currentWeapon?.maxAmmo ?? 0,
            isReloading = isReloading,
            hasRifle = hasRifle
        };
    }
}

[System.Serializable]
public class WeaponStatus
{
    public string currentWeapon;
    public int currentAmmo;
    public int maxAmmo;
    public bool isReloading;
    public bool hasRifle;
}
