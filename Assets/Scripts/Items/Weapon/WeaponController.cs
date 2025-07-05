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
        
        originalPosition = transform.localPosition;
        currentAmmo = maxAmmo;
        currentSpread = baseSpread;
    }
    
    public virtual void TryShoot()
    {
        if (Time.time < nextFireTime || isReloading)
            return;
        
        if (currentAmmo <= 0)
        {
            if (emptySound && !audioSource.isPlaying)
                audioSource.PlayOneShot(emptySound);
            return;
        }
        
        // 单发或自动射击
        if (!isShooting || isAutomatic)
        {
            Shoot();
            isShooting = true;
        }
    }
    
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
    }
    
    protected virtual void PerformRaycast()
    {
        Camera cam = Camera.main;
        Vector3 direction = cam.transform.forward;
        
        // 应用散布
        float spread = isAiming ? currentSpread * aimSpreadMultiplier : currentSpread;
        direction += cam.transform.right * Random.Range(-spread, spread);
        direction += cam.transform.up * Random.Range(-spread, spread);
        
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, direction, out hit, range))
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
            if (bulletTrail && muzzlePoint)
            {
                CreateBulletTrail(muzzlePoint.position, hit.point);
            }
        }
    }
    
    protected virtual void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        GameObject trail = Instantiate(bulletTrail, start, Quaternion.identity);
        LineRenderer line = trail.GetComponent<LineRenderer>();
        if (line)
        {
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
        Destroy(trail, 0.1f);
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
        
        StartCoroutine(ReloadCoroutine());
    }
    
    protected virtual IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        
        if (reloadSound)
            audioSource.PlayOneShot(reloadSound);
        
        // 播放换弹动画
        yield return PlayReloadAnimation();
        
        currentAmmo = maxAmmo;
        isReloading = false;
    }
    
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
    public float GetCurrentSpread() => currentSpread;
    public Vector2 GetRecoil() => currentRecoil;
}
internal interface IDamageable
{ 
    void TakeDamage(float damage);
    float GetCurrentHealth(); // 新增
    float GetMaxHealth();     // 新增
    bool IsAlive();          // 新增
    
}


