using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class WeaponSpriteSet
{
    [Header("武器信息")]
    public WeaponType weaponType;
    public bool isMeleeWeapon = false;
    
    [Header("基础动画")]
    public Sprite[] idleFrames;
    public Sprite[] raiseFrames;
    public Sprite[] lowerFrames;
    
    [Header("射击动画")]
    public Sprite[] fireFrames;
    public Sprite[] reloadFrames;
    public Sprite aimSprite;
    
    [Header("近战动画")]
    public Sprite[] meleeFrames;
    public Sprite[] meleeChargeFrames;
}

[System.Serializable]
public class HandSpriteSet
{
    [Header("空手动画")]
    public Sprite[] emptyHandsIdle;
    public Sprite[] emptyHandsLower;
    public Sprite[] emptyHandsRaise;
    public Sprite[] interactionFrames;
    public Sprite[] runningFrames;
}

public class WeaponDisplay : MonoBehaviour
{
    [Header("UI组件")]
    public Image weaponImage;
    public RectTransform weaponRect;
    
    [Header("武器精灵配置")]
    public WeaponSpriteSet[] weaponSprites;
    
    [Header("空手配置")]
    public HandSpriteSet handSprites;
    
    [Header("动画设置")]
    public float idleFrameRate = 8f;
    public float fireFrameRate = 20f;
    public float reloadFrameRate = 10f;
    public float switchFrameRate = 15f;
    public float meleeFrameRate = 25f;
    public float interactionFrameRate = 15f;
    
    [Header("切换动画设置")]
    public float switchLowerTime = 0.3f;
    public float switchRaiseTime = 0.4f;
    public float switchHoldTime = 0.1f;
    
    [Header("摇摆设置")]
    public float mouseSwayAmount = 15f;
    public float breathSwayAmount = 8f;
    public float walkBobAmount = 12f;
    public float runBobMultiplier = 1.5f;
    public float recoilKickback = 25f;
    
    [Header("近战设置")]
    public float meleeReachDistance = 2f;
    public float meleeSwingAmount = 40f;
    
    [Header("摇摆优化")]
    public bool enableRunSmoothing = true;
    public float runSmoothingFactor = 0.3f;
    public float maxRunBob = 20f;
    
    [Header("调试")]
    public bool enableDebugLog = true;
    
    [Header("参考风格")]
    [Range(0f, 1f)] public float mouseStyle = 0.7f;
    
    // 私有变量
    private Vector2 originalPosition;
    private WeaponController currentWeapon;
    private WeaponManager weaponManager;
    private Coroutine currentAnimation;
    private Coroutine switchAnimation;
    private Coroutine weaponMonitorCoroutine;
    private Vector2 lastMousePosition;
    private float swayTimer;
    
    // 状态变量
    private bool isAnimating = false;
    private bool isSwitching = false;
    private bool isEmptyHands = true;
    private bool isMeleeWeapon = false;
    private WeaponType currentWeaponType = (WeaponType)(-1);
    
    // 状态跟踪变量
    private bool wasReloading = false;
    private bool wasAiming = false;
    private bool wasRunning = false;
    private int lastAmmo = -1;
    private Vector2 smoothedWalkBob = Vector2.zero;
    
    
    
    private bool lastAimingState = false;
    void Start()
    {
        Initialize();
        StartWeaponMonitoring();
    }
    
    void Initialize()
    {
        if (weaponRect != null)
        {
            originalPosition = weaponRect.anchoredPosition;
        }
        else
        {
            LogWarning("weaponRect is null!");
        }
        
        // 查找武器管理器
        weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            LogWarning("WeaponManager not found!");
        }
        
        // 设置初始鼠标位置
        lastMousePosition = Input.mousePosition;
        
        LogDebug("Initialized successfully");
    }
    
    void StartWeaponMonitoring()
    {
        if (weaponMonitorCoroutine != null)
        {
            StopCoroutine(weaponMonitorCoroutine);
        }
        weaponMonitorCoroutine = StartCoroutine(MonitorWeaponState());
    }
    
    IEnumerator MonitorWeaponState()
    {
        while (this != null)
        {
            // 检查weaponManager是否有效
            if (weaponManager != null)
            {
                WeaponController newWeapon = SafeGetCurrentWeapon();
                bool newEmptyHands = SafeIsEmptyHands();
                
                // 检测武器变化
                if (newWeapon != currentWeapon || newEmptyHands != isEmptyHands)
                {
                    HandleWeaponChange(newWeapon, newEmptyHands);
                }
                
                // 检测射击和换弹
                if (currentWeapon != null && !isEmptyHands)
                {
                    CheckForShooting();
                    CheckForReloading();
                }
            }
            
            yield return new WaitForSeconds(0.05f); // 20FPS检测频率
        }
    }
    
    WeaponController SafeGetCurrentWeapon()
    {
        if (weaponManager == null) return null;
        
        try
        {
            return weaponManager.GetCurrentWeapon();
        }
        catch (System.Exception e)
        {
            LogWarning($"SafeGetCurrentWeapon error: {e.Message}");
            return null;
        }
    }
    
    bool SafeIsEmptyHands()
    {
        if (weaponManager == null) return true;
        
        try
        {
            return weaponManager.IsEmptyHands();
        }
        catch (System.Exception e)
        {
            LogWarning($"SafeIsEmptyHands error: {e.Message}");
            return true;
        }
    }
    
    void HandleWeaponChange(WeaponController newWeapon, bool newEmptyHands)
    {
        currentWeapon = newWeapon;
        isEmptyHands = newEmptyHands;
        
        LogDebug($"Weapon changed: {(newWeapon ? newWeapon.weaponName : "Empty Hands")}");
        
        if (isEmptyHands)
        {
            SwitchToEmptyHands();
        }
        else if (currentWeapon != null)
        {
            currentWeaponType = currentWeapon.weaponType;
            SwitchToWeapon(currentWeaponType);
        }
    }
    
    void Update()
    {
        // 修复：限制更新频率
        if (Time.time - lastUpdateTime < 0.016f) return; // 60FPS限制
        lastUpdateTime = Time.time;
        
        UpdateWeaponSway();
        CheckInteractionInput();
        CheckMeleeInput();
    }
    private float lastUpdateTime = 0f;
    
    void UpdateWeaponSway()
    {
        if (weaponRect == null || isSwitching) return;
        
        Vector2 targetPos = originalPosition;
        
        // 鼠标摇摆
        Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
        Vector2 mouseSway = mouseDelta * mouseSwayAmount * 0.01f;
        float smoothness = Mathf.Lerp(0.1f, 0.3f, mouseStyle);
        mouseSway *= smoothness;
        mouseSway.x = -mouseSway.x;
        
        // 呼吸摇摆
        swayTimer += Time.deltaTime;
        Vector2 breathSway = new Vector2(
            Mathf.Sin(swayTimer * 1.5f) * breathSwayAmount,
            Mathf.Cos(swayTimer * 0.8f) * breathSwayAmount * 0.5f
        );
        
        // 行走摇摆
        Vector2 walkSway = CalculateWalkSway();
        
        // 合并摇摆
        targetPos += mouseSway + breathSway + walkSway;
        weaponRect.anchoredPosition = Vector2.Lerp(weaponRect.anchoredPosition, targetPos, Time.deltaTime * 8f);
        lastMousePosition = Input.mousePosition;
    }
    
    Vector2 CalculateWalkSway()
    {
        Vector2 walkSway = Vector2.zero;
        
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && player.GetCurrentSpeed() > 0.1f)
        {
            float walkTimer = Time.time * 6f;
            bool isRunning = player.IsRunning();
            
            Vector2 targetWalkBob = new Vector2(
                Mathf.Sin(walkTimer * 2f) * walkBobAmount * 0.6f,
                Mathf.Abs(Mathf.Cos(walkTimer)) * walkBobAmount
            );
            
            if (isRunning)
            {
                targetWalkBob *= runBobMultiplier;
                targetWalkBob = Vector2.ClampMagnitude(targetWalkBob, maxRunBob);
                
                if (isRunning != wasRunning && isEmptyHands)
                {
                    PlayRunningAnimation();
                }
            }
            
            if (enableRunSmoothing)
            {
                smoothedWalkBob = Vector2.Lerp(smoothedWalkBob, targetWalkBob, 
                    Time.deltaTime * (isRunning ? runSmoothingFactor * 10f : 15f));
                walkSway = smoothedWalkBob;
            }
            else
            {
                walkSway = targetWalkBob;
            }
            
            wasRunning = isRunning;
        }
        else
        {
            smoothedWalkBob = Vector2.Lerp(smoothedWalkBob, Vector2.zero, Time.deltaTime * 10f);
            walkSway = smoothedWalkBob;
            wasRunning = false;
        }
        
        return walkSway;
    }
    
    void CheckForShooting()
    {
        if (currentWeapon == null || isAnimating) return;
        
        // 方法1：检测鼠标输入
        if (Input.GetMouseButtonDown(0))
        {
            if (SafeGetCurrentAmmo() > 0)
            {
                LogDebug("Detected shooting input, playing fire animation");
                PlayFireAnimation();
            }
        }
        
        // 方法2：检测弹药变化（备用方案）
        int currentAmmoCount = SafeGetCurrentAmmo();
        if (lastAmmo != -1 && currentAmmoCount < lastAmmo && !SafeIsReloading())
        {
            LogDebug("Detected ammo decrease, playing fire animation");
            PlayFireAnimation();
        }
        lastAmmo = currentAmmoCount;
    }
    
    void CheckForReloading()
    {
        if (currentWeapon == null) return;
        
        bool isReloading = SafeIsReloading();
        
        if (isReloading && !wasReloading)
        {
            LogDebug("Detected reload start, playing reload animation");
            PlayReloadAnimation();
        }
        
        wasReloading = isReloading;
    }
    
    int SafeGetCurrentAmmo()
    {
        if (currentWeapon == null) return 0;
        try
        {
            return currentWeapon.CurrentAmmo;
        }
        catch
        {
            return 0;
        }
    }
    
    bool SafeIsReloading()
    {
        if (currentWeapon == null) return false;
        try
        {
            return currentWeapon.IsReloading;
        }
        catch
        {
            return false;
        }
    }
    
    void CheckInteractionInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isEmptyHands)
            {
                PlayInteractionAnimation();
            }
        }
    }
    
    void CheckMeleeInput()
    {
        if (isMeleeWeapon && !isSwitching && !isAnimating)
        {
            if (Input.GetMouseButtonDown(0))
            {
                PlayMeleeAttack();
            }
            else if (Input.GetMouseButton(0))
            {
                PlayMeleeCharge();
            }
        }
    }
    
    // ==== 武器切换系统 ====
    public void SwitchToWeapon(WeaponType weaponType)
    {
        if (isSwitching) return;
        
        WeaponSpriteSet newWeaponSet = GetSpriteSet(weaponType);
        if (newWeaponSet == null) return;
        
        isMeleeWeapon = newWeaponSet.isMeleeWeapon;
        currentWeaponType = weaponType;
        isEmptyHands = false;
        
        if (switchAnimation != null)
            StopCoroutine(switchAnimation);
        switchAnimation = StartCoroutine(WeaponSwitchCoroutine(newWeaponSet));
    }
    
    public void SwitchToEmptyHands()
    {
        if (isSwitching) return;
        
        isEmptyHands = true;
        isMeleeWeapon = false;
        currentWeapon = null;
        currentWeaponType = (WeaponType)(-1);
        
        if (switchAnimation != null)
            StopCoroutine(switchAnimation);
        switchAnimation = StartCoroutine(EmptyHandsSwitchCoroutine());
    }
    
    IEnumerator WeaponSwitchCoroutine(WeaponSpriteSet newWeaponSet)
    {
        isSwitching = true;
        
        // 第一阶段：当前武器下降
        if (!isEmptyHands)
        {
            yield return StartCoroutine(WeaponSlideDown());
        }
        
        yield return new WaitForSeconds(switchHoldTime);
        
        // 第二阶段：新武器滑入
        yield return StartCoroutine(WeaponSlideUp(newWeaponSet));
        
        isSwitching = false;
        StartWeaponIdle(newWeaponSet);
    }
    
    IEnumerator WeaponSlideDown()
    {
        if (weaponRect == null) yield break;
        
        Vector2 startPos = weaponRect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.down * 200f;
        
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            weaponRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
    }
    
    IEnumerator WeaponSlideUp(WeaponSpriteSet weaponSet)
    {
        if (weaponRect == null || weaponImage == null) yield break;
        
        Vector2 endPos = originalPosition;
        Vector2 startPos = endPos + Vector2.down * 200f;
        
        weaponRect.anchoredPosition = startPos;
        
        // 设置新武器图片
        if (weaponSet.idleFrames.Length > 0)
        {
            weaponImage.sprite = weaponSet.idleFrames[0];
        }
        
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            weaponRect.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            yield return null;
        }
        
        weaponRect.anchoredPosition = endPos;
    }
    
    IEnumerator EmptyHandsSwitchCoroutine()
    {
        isSwitching = true;
        
        // 收起当前武器
        if (!isEmptyHands)
        {
            WeaponSpriteSet currentSet = GetCurrentSpriteSet();
            if (currentSet != null && currentSet.lowerFrames.Length > 0)
            {
                yield return StartCoroutine(PlayAnimationCoroutine(currentSet.lowerFrames, switchFrameRate, false));
            }
        }
        
        yield return new WaitForSeconds(switchHoldTime);
        
        // 抬起空手
        if (handSprites.emptyHandsRaise.Length > 0)
        {
            yield return StartCoroutine(PlayAnimationCoroutine(handSprites.emptyHandsRaise, switchFrameRate, false));
        }
        
        isSwitching = false;
        StartEmptyHandsIdle();
    }
    
    // ==== 动画播放系统 ====
    void StartWeaponIdle(WeaponSpriteSet weaponSet)
    {
        if (weaponSet != null && weaponSet.idleFrames.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(weaponSet.idleFrames, idleFrameRate, true));
        }
        else if (weaponSet != null && weaponSet.idleFrames.Length > 0 && weaponImage != null)
        {
            weaponImage.sprite = weaponSet.idleFrames[0];
        }
    }
    
    void StartEmptyHandsIdle()
    {
        if (handSprites.emptyHandsIdle.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(handSprites.emptyHandsIdle, idleFrameRate, true));
        }
    }
    
    public void PlayFireAnimation()
    {
        if (isEmptyHands || currentWeapon == null)
        {
            LogWarning("Cannot play fire animation - no weapon equipped");
            return;
        }
        
        WeaponSpriteSet spriteSet = GetSpriteSet(currentWeaponType);
        if (spriteSet != null && spriteSet.fireFrames.Length > 0)
        {
            LogDebug($"Playing fire animation for {currentWeaponType}");
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(spriteSet.fireFrames, fireFrameRate, false));
            
            // 添加后坐力效果
            StartCoroutine(RecoilEffect());
        }
        else
        {
            LogWarning($"No fire frames found for {currentWeaponType}");
        }
    }
    
    public void PlayReloadAnimation()
    {
        if (isEmptyHands || currentWeapon == null) return;
        
        WeaponSpriteSet spriteSet = GetSpriteSet(currentWeaponType);
        if (spriteSet != null && spriteSet.reloadFrames.Length > 0)
        {
            LogDebug($"Playing reload animation for {currentWeaponType}");
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(spriteSet.reloadFrames, reloadFrameRate, false));
        }
    }
    
    public void PlayMeleeAttack()
    {
        if (!isMeleeWeapon) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null && spriteSet.meleeFrames.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(spriteSet.meleeFrames, meleeFrameRate, false));
            
            StartCoroutine(MeleeAttackCoroutine());
        }
    }
    
    public void PlayMeleeCharge()
    {
        if (!isMeleeWeapon || isAnimating) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null && spriteSet.meleeChargeFrames.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(spriteSet.meleeChargeFrames, meleeFrameRate * 0.5f, true));
        }
    }
    
    public void PlayInteractionAnimation()
    {
        if (!isEmptyHands) return;
        
        if (handSprites.interactionFrames.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(handSprites.interactionFrames, interactionFrameRate, false));
        }
    }
    
    void PlayRunningAnimation()
    {
        if (!isEmptyHands) return;
        
        if (handSprites.runningFrames.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(handSprites.runningFrames, idleFrameRate * 1.5f, true));
        }
    }
    
    public void SetAiming(bool aiming)
    {
        // 修复：避免重复设置相同状态
        if (lastAimingState == aiming) return;
        
        lastAimingState = aiming;
        
        if (isEmptyHands || isMeleeWeapon) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null)
        {
            if (aiming && spriteSet.aimSprite != null)
            {
                isAnimating = false;
                if (currentAnimation != null)
                    StopCoroutine(currentAnimation);
                weaponImage.sprite = spriteSet.aimSprite;
            }
            else if (!aiming)
            {
                StartWeaponIdle(spriteSet);
            }
        }
    }
    
    IEnumerator PlayAnimationCoroutine(Sprite[] frames, float frameRate, bool loop)
    {
        if (weaponImage == null || frames.Length == 0)
        {
            LogWarning("Cannot play animation - missing weaponImage or frames");
            yield break;
        }
        
        isAnimating = true;
        float frameTime = 1f / frameRate;
        
        LogDebug($"Starting animation with {frames.Length} frames at {frameRate} FPS");
        
        do
        {
            for (int i = 0; i < frames.Length && isAnimating; i++)
            {
                if (weaponImage != null && frames[i] != null)
                {
                    weaponImage.sprite = frames[i];
                }
                yield return new WaitForSeconds(frameTime);
            }
        } while (loop && isAnimating);
        
        // 动画结束处理
        if (!loop)
        {
            LogDebug("Animation finished, returning to idle");
            
            if (isEmptyHands)
            {
                StartEmptyHandsIdle();
            }
            else
            {
                WeaponSpriteSet currentSet = GetSpriteSet(currentWeaponType);
                if (currentSet != null)
                {
                    StartWeaponIdle(currentSet);
                }
            }
        }
        
        isAnimating = false;
    }
    
    // ==== 近战攻击逻辑 ====
    IEnumerator MeleeAttackCoroutine()
    {
        yield return new WaitForSeconds(0.1f);
        
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Ray ray = cam.ScreenPointToRay(screenCenter);
            
            RaycastHit[] hits = Physics.RaycastAll(ray, meleeReachDistance);
            
            foreach (var hit in hits)
            {
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float meleeDamage = currentWeapon ? currentWeapon.damage : 50f;
                    damageable.TakeDamage(meleeDamage);
                    
                    CreateMeleeHitEffect(hit.point, hit.normal);
                    break;
                }
            }
        }
        
        StartCoroutine(MeleeSwingEffect());
    }
    
    IEnumerator MeleeSwingEffect()
    {
        if (weaponRect == null) yield break;
        
        Vector2 startPos = weaponRect.anchoredPosition;
        Vector2 swingPos = startPos + Vector2.right * meleeSwingAmount;
        
        // 快速挥舞
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            weaponRect.anchoredPosition = Vector2.Lerp(startPos, swingPos, elapsed / 0.15f);
            yield return null;
        }
        
        // 回归原位
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            weaponRect.anchoredPosition = Vector2.Lerp(swingPos, startPos, elapsed / 0.1f);
            yield return null;
        }
    }
    
    void CreateMeleeHitEffect(Vector3 position, Vector3 normal)
    {
        Debug.Log($"近战击中！位置：{position}");
    }
    
    // ==== 后坐力效果 ====
    IEnumerator RecoilEffect()
    {
        if (weaponRect == null) yield break;
        
        Vector2 startPos = weaponRect.anchoredPosition;
        Vector2 recoilPos = startPos + Vector2.down * recoilKickback;
        
        // 快速后退
        float elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.deltaTime;
            weaponRect.anchoredPosition = Vector2.Lerp(startPos, recoilPos, elapsed / 0.08f);
            yield return null;
        }
        
        // 缓慢恢复
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            weaponRect.anchoredPosition = Vector2.Lerp(recoilPos, startPos, elapsed / 0.15f);
            yield return null;
        }
        
        weaponRect.anchoredPosition = startPos;
    }
    
    // ==== 辅助方法 ====
    WeaponSpriteSet GetSpriteSet(WeaponType weaponType)
    {
        foreach (var spriteSet in weaponSprites)
        {
            if (spriteSet.weaponType == weaponType)
                return spriteSet;
        }
        
        LogWarning($"No sprite set found for weapon type: {weaponType}");
        return null;
    }
    
    WeaponSpriteSet GetCurrentSpriteSet()
    {
        if (currentWeapon == null) return null;
        return GetSpriteSet(currentWeapon.weaponType);
    }
    
    // ==== 日志方法 ====
    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[WeaponDisplay] {message}");
        }
    }
    
    void LogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning($"[WeaponDisplay] {message}");
        }
    }
    
    // ==== 公共方法供外部调用 ====
    public void OnWeaponSwitch(WeaponType newType)
    {
        LogDebug($"OnWeaponSwitch called: {newType}");
        SwitchToWeapon(newType);
    }
    
    public void OnGoEmptyHands()
    {
        SwitchToEmptyHands();
    }
    
    public void OnWeaponFired()
    {
        LogDebug("OnWeaponFired called externally");
        PlayFireAnimation();
    }
    
    public void OnWeaponReload()
    {
        LogDebug("OnWeaponReload called externally");
        PlayReloadAnimation();
    }
    
    public void OnInteraction()
    {
        PlayInteractionAnimation();
    }
    
    public void OnMeleeAttack()
    {
        PlayMeleeAttack();
    }
    
    // 调试信息显示
    void OnGUI()
    {
        if (!enableDebugLog || !Debug.isDebugBuild) return;
        
        GUILayout.BeginArea(new Rect(10, 100, 300, 200));
        GUILayout.Label("=== WeaponDisplay Debug ===");
        GUILayout.Label($"Current Weapon: {(currentWeapon ? currentWeapon.weaponName : "None")}");
        GUILayout.Label($"Empty Hands: {isEmptyHands}");
        GUILayout.Label($"Is Animating: {isAnimating}");
        GUILayout.Label($"Weapon Type: {currentWeaponType}");
        
        if (currentWeapon != null)
        {
            GUILayout.Label($"Ammo: {SafeGetCurrentAmmo()}/{(currentWeapon.MaxAmmo)}");
            GUILayout.Label($"Is Reloading: {SafeIsReloading()}");
        }
        
        GUILayout.EndArea();
    }
    
    void OnDestroy()
    {
        // 清理协程
        if (weaponMonitorCoroutine != null)
        {
            StopCoroutine(weaponMonitorCoroutine);
        }
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        if (switchAnimation != null)
        {
            StopCoroutine(switchAnimation);
        }
    }
}