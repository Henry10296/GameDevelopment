using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class WeaponSpriteSet
{
    [Header("武器信息")]
    public WeaponType weaponType;
    public bool isMeleeWeapon = false;  // 是否为近战武器
    
    [Header("基础动画")]
    public Sprite[] idleFrames;         // 待机动画
    public Sprite[] raiseFrames;        // 抬起武器动画
    public Sprite[] lowerFrames;        // 收起武器动画
    
    [Header("射击动画")]
    public Sprite[] fireFrames;         // 射击动画
    public Sprite[] reloadFrames;       // 换弹动画
    public Sprite aimSprite;            // 瞄准精灵
    
    [Header("近战动画")]
    public Sprite[] meleeFrames;        // 近战攻击动画
    public Sprite[] meleeChargeFrames;  // 近战蓄力动画
}

[System.Serializable]
public class HandSpriteSet
{
    [Header("空手动画")]
    public Sprite[] emptyHandsIdle;     // 空手待机
    public Sprite[] emptyHandsLower;    // 空手收起
    public Sprite[] emptyHandsRaise;    // 空手抬起
    public Sprite[] interactionFrames;  // 交互动画
    public Sprite[] runningFrames;      // 跑步动画
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
    public float switchFrameRate = 15f;     // 切换动画帧率
    public float meleeFrameRate = 25f;      // 近战动画帧率
    public float interactionFrameRate = 15f;
    
    [Header("切换动画设置")]
    public float switchLowerTime = 0.3f;    // 下降时间
    public float switchRaiseTime = 0.4f;    // 抬起时间
    public float switchHoldTime = 0.1f;     // 中间停顿时间
    
    [Header("摇摆设置")]
    public float mouseSwayAmount = 15f;
    public float breathSwayAmount = 8f;
    public float walkBobAmount = 12f;
    public float runBobMultiplier = 1.5f;
    public float recoilKickback = 25f;
    
    [Header("近战设置")]
    public float meleeReachDistance = 2f;   // 近战距离
    public float meleeSwingAmount = 40f;    // 近战挥舞幅度
    
    [Header("摇摆优化")]
    public bool enableRunSmoothing = true;
    public float runSmoothingFactor = 0.3f;
    public float maxRunBob = 20f;
    
    [Header("参考风格")]
    [Range(0f, 1f)] public float mouseStyle = 0.7f;
    
    // 私有变量
    private Vector2 originalPosition;
    private WeaponController currentWeapon;
    private Coroutine currentAnimation;
    private Coroutine switchAnimation;
    private Vector2 lastMousePosition;
    private float swayTimer;
    private bool isAnimating = false;
    private bool isSwitching = false;
    private bool isEmptyHands = false;
    private bool isMeleeWeapon = false;
    
    // 状态跟踪
    private WeaponType lastWeaponType = (WeaponType)(-1);
    private bool wasReloading = false;
    private bool wasAiming = false;
    private bool wasRunning = false;
    private int lastAmmo = -1;
    private Vector2 smoothedWalkBob = Vector2.zero;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        if (weaponRect != null)
        {
            originalPosition = weaponRect.anchoredPosition;
        }
        
        lastMousePosition = Input.mousePosition;
        StartListeningToWeapon();
    }
    
    void StartListeningToWeapon()
    {
        // 检测当前武器状态
        currentWeapon = FindObjectOfType<WeaponController>();
        if (currentWeapon != null)
        {
            SwitchToWeapon(currentWeapon.weaponType);
        }
        else
        {
            SwitchToEmptyHands();
        }
    }
    
    void Update()
    {
        UpdateWeaponSway();
        CheckWeaponEvents();
        CheckInteractionInput();
        CheckMeleeInput();
    }
    
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
        
        // 行走摇摆（优化）
        PlayerController player = FindObjectOfType<PlayerController>();
        Vector2 walkSway = Vector2.zero;
        
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
        
        // 合并摇摆
        targetPos += mouseSway + breathSway + walkSway;
        weaponRect.anchoredPosition = Vector2.Lerp(weaponRect.anchoredPosition, targetPos, Time.deltaTime * 8f);
        lastMousePosition = Input.mousePosition;
    }
    
    void CheckWeaponEvents()
    {
        if (currentWeapon == null || isEmptyHands || isSwitching) return;
        
        // 检测武器类型变化（切换武器）
        if (currentWeapon.weaponType != lastWeaponType)
        {
            SwitchToWeapon(currentWeapon.weaponType);
            return;
        }
        
        // 检测射击（通过弹药变化或你的射击事件）
        if (lastAmmo != -1 && currentWeapon.CurrentAmmo < lastAmmo && !currentWeapon.IsReloading)
        {
            if (isMeleeWeapon)
            {
                // 近战武器不响应射击
            }
            else
            {
                PlayFireAnimation();
                
                // 通知你的射击系统（保持你现有的逻辑）
                TriggerWeaponFire();
            }
        }
        lastAmmo = currentWeapon.CurrentAmmo;
        
        // 检测换弹
        if (currentWeapon.IsReloading && !wasReloading)
        {
            PlayReloadAnimation();
        }
        wasReloading = currentWeapon.IsReloading;
        
        // 检测瞄准
        bool isAiming = Input.GetMouseButton(1);
        if (isAiming != wasAiming)
        {
            SetAiming(isAiming);
        }
        wasAiming = isAiming;
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
            if (Input.GetMouseButtonDown(0)) // 左键近战攻击
            {
                PlayMeleeAttack();
            }
            else if (Input.GetMouseButton(0)) // 持续按住蓄力
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
        lastWeaponType = weaponType;
        isEmptyHands = false;
        
        // 开始切换动画
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
        lastWeaponType = (WeaponType)(-1);
        
        if (switchAnimation != null)
            StopCoroutine(switchAnimation);
        switchAnimation = StartCoroutine(EmptyHandsSwitchCoroutine());
    }
    
    IEnumerator WeaponSwitchCoroutine(WeaponSpriteSet newWeaponSet)
    {
        isSwitching = true;
    
        // 第一阶段：当前武器快速下降
        if (!isEmptyHands)
        {
            yield return StartCoroutine(WeaponSlideDown());
        }
    
        yield return new WaitForSeconds(switchHoldTime);
    
        // 第二阶段：新武器从下方滑入
        yield return StartCoroutine(WeaponSlideUp(newWeaponSet));
    
        isSwitching = false;
        StartWeaponIdle(newWeaponSet);
    }
    IEnumerator WeaponSlideDown()
    {
        Vector2 startPos = weaponRect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.down * 200f; // 向下200像素
    
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
            // 使用缓动曲线让动画更smooth
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
        if (weaponSet.idleFrames.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(weaponSet.idleFrames, idleFrameRate, true));
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
        if (isMeleeWeapon || isEmptyHands) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null && spriteSet.fireFrames.Length > 0)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(PlayAnimationCoroutine(spriteSet.fireFrames, fireFrameRate, false));
            StartCoroutine(RecoilEffect());
        }
    }
    
    public void PlayReloadAnimation()
    {
        if (isMeleeWeapon || isEmptyHands) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null && spriteSet.reloadFrames.Length > 0)
        {
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
            
            // 执行近战攻击逻辑
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
        if (weaponImage == null || frames.Length == 0) yield break;
        
        isAnimating = true;
        float frameTime = 1f / frameRate;
        
        do
        {
            for (int i = 0; i < frames.Length && isAnimating; i++)
            {
                weaponImage.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
        } while (loop && isAnimating && !isSwitching);
        
        // 动画结束处理
        if (!loop && !isSwitching)
        {
            if (isEmptyHands)
            {
                StartEmptyHandsIdle();
            }
            else
            {
                WeaponSpriteSet currentSet = GetCurrentSpriteSet();
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
        yield return new WaitForSeconds(0.1f); // 攻击前摇
        
        // 检测近战范围内的敌人
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
                    // 近战伤害
                    float meleeDamage = currentWeapon ? currentWeapon.damage : 50f;
                    damageable.TakeDamage(meleeDamage);
                    
                    // 近战击中效果
                    CreateMeleeHitEffect(hit.point, hit.normal);
                    break; // 只击中第一个目标
                }
            }
        }
        
        // 武器摇摆效果
        StartCoroutine(MeleeSwingEffect());
    }
    
    IEnumerator MeleeSwingEffect()
    {
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
        // 这里可以创建近战击中特效
        Debug.Log($"近战击中！位置：{position}");
        
        // 可以播放击中音效
        // AudioSource.PlayClipAtPoint(meleeHitSound, position);
    }
    
    // ==== 与你现有射击系统的集成 ====
    void TriggerWeaponFire()
    {
        // 这里调用你现有的射击逻辑
        // 比如生成子弹、播放音效等
        
        if (currentWeapon != null)
        {
            // 如果你的WeaponController有公共的Fire方法，可以这样调用：
            // currentWeapon.Fire();
            
            // 或者通过事件系统通知
            Debug.Log("触发武器射击！");
        }
    }
    
    // ==== 后坐力效果 ====
    IEnumerator RecoilEffect()
    {
        if (weaponRect == null) yield break;
        
        Vector2 startPos = weaponRect.anchoredPosition;
        Vector2 recoilPos = startPos + Vector2.down * recoilKickback;
        
        float elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.deltaTime;
            weaponRect.anchoredPosition = Vector2.Lerp(startPos, recoilPos, elapsed / 0.08f);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            weaponRect.anchoredPosition = Vector2.Lerp(recoilPos, startPos, elapsed / 0.15f);
            yield return null;
        }
    }
    
    // ==== 辅助方法 ====
    WeaponSpriteSet GetSpriteSet(WeaponType weaponType)
    {
        foreach (var spriteSet in weaponSprites)
        {
            if (spriteSet.weaponType == weaponType)
                return spriteSet;
        }
        return null;
    }
    
    WeaponSpriteSet GetCurrentSpriteSet()
    {
        if (currentWeapon == null) return null;
        return GetSpriteSet(currentWeapon.weaponType);
    }
    
    // ==== 公共方法供外部调用 ====
    public void OnWeaponSwitch(WeaponType newType)
    {
        SwitchToWeapon(newType);
    }
    
    public void OnGoEmptyHands()
    {
        SwitchToEmptyHands();
    }
    
    public void OnWeaponFired()
    {
        PlayFireAnimation();
    }
    
    public void OnWeaponReload()
    {
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
}