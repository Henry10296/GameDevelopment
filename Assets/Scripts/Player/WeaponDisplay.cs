using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class WeaponSpriteSet
{
    public WeaponType weaponType;
    public Sprite[] idleFrames;
    public Sprite[] fireFrames;
    public Sprite[] reloadFrames;
    public Sprite aimSprite;
}

[System.Serializable]
public class HandSpriteSet
{
    [Header("空手精灵")]
    public Sprite[] emptyHandsIdle;     // 空手待机
    public Sprite[] interactionFrames;  // 交互动画（按E时）
    public Sprite[] runningFrames;      // 跑步时的手部动画
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
    public float interactionFrameRate = 15f;
    
    [Header("摇摆设置 (像素)")]
    public float mouseSwayAmount = 15f;
    public float breathSwayAmount = 8f;
    public float walkBobAmount = 12f;
    public float runBobMultiplier = 1.5f;  // 跑步时摇摆倍数
    public float recoilKickback = 25f;
    
    [Header("跑动抖动优化")]
    public bool enableRunSmoothing = true;
    public float runSmoothingFactor = 0.3f;  // 跑步平滑因子
    public float maxRunBob = 20f;            // 最大跑步摇摆
    
    [Header("参考游戏风格")]
    [Range(0f, 1f)] public float mouseStyle = 0.7f;
    
    // 私有变量
    private Vector2 originalPosition;
    private WeaponController currentWeapon;
    private Coroutine animationCoroutine;
    private Vector2 lastMousePosition;
    private float swayTimer;
    private bool isAnimating = false;
    private bool isEmptyHands = false;
    private bool isInteracting = false;
    
    // 状态跟踪
    private bool wasReloading = false;
    private bool wasAiming = false;
    private bool wasRunning = false;
    private int lastAmmo = -1;
    
    // 跑动抖动优化
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
        currentWeapon = FindObjectOfType<WeaponController>();
        if (currentWeapon != null)
        {
            SetWeaponType(currentWeapon.weaponType);
            StartIdleAnimation();
            isEmptyHands = false;
        }
        else
        {
            SetEmptyHands();
        }
    }
    
    void Update()
    {
        UpdateWeaponSway();
        CheckWeaponEvents();
        CheckInteractionInput();
    }
    
    void UpdateWeaponSway()
    {
        if (weaponRect == null) return;
        
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
        
        // 行走/跑步摇摆（优化抖动）
        PlayerController player = FindObjectOfType<PlayerController>();
        Vector2 walkSway = Vector2.zero;
        
        if (player != null && player.GetCurrentSpeed() > 0.1f)
        {
            float walkTimer = Time.time * 6f;
            bool isRunning = player.IsRunning();
            
            // 计算目标摇摆
            Vector2 targetWalkBob = new Vector2(
                Mathf.Sin(walkTimer * 2f) * walkBobAmount * 0.6f,
                Mathf.Abs(Mathf.Cos(walkTimer)) * walkBobAmount
            );
            
            // 跑步时增加摇摆
            if (isRunning)
            {
                targetWalkBob *= runBobMultiplier;
                targetWalkBob = Vector2.ClampMagnitude(targetWalkBob, maxRunBob);
                
                // 跑步状态变化时播放动画
                if (isRunning != wasRunning && isEmptyHands)
                {
                    PlayRunningAnimation();
                }
            }
            
            // 平滑跑步摇摆（减少抖动）
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
            // 停止移动时平滑回到零
            smoothedWalkBob = Vector2.Lerp(smoothedWalkBob, Vector2.zero, Time.deltaTime * 10f);
            walkSway = smoothedWalkBob;
            wasRunning = false;
        }
        
        // 合并所有摇摆
        targetPos += mouseSway + breathSway + walkSway;
        
        // 平滑移动
        weaponRect.anchoredPosition = Vector2.Lerp(weaponRect.anchoredPosition, targetPos, Time.deltaTime * 8f);
        lastMousePosition = Input.mousePosition;
    }
    
    void CheckWeaponEvents()
    {
        if (currentWeapon == null || isEmptyHands) return;
        
        // 检测开火
        if (lastAmmo != -1 && currentWeapon.CurrentAmmo < lastAmmo && !currentWeapon.IsReloading)
        {
            PlayFireAnimation();
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
        // 检测交互输入（E键或F键）
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
        {
            PlayInteractionAnimation();
        }
    }
    
    public void SetEmptyHands()
    {
        isEmptyHands = true;
        currentWeapon = null;
        
        if (weaponImage != null && handSprites.emptyHandsIdle.Length > 0)
        {
            weaponImage.sprite = handSprites.emptyHandsIdle[0];
            StartEmptyHandsIdle();
        }
    }
    
    public void SetWeaponType(WeaponType weaponType)
    {
        isEmptyHands = false;
        WeaponSpriteSet spriteSet = GetSpriteSet(weaponType);
        if (spriteSet != null && weaponImage != null && spriteSet.idleFrames.Length > 0)
        {
            weaponImage.sprite = spriteSet.idleFrames[0];
            StartIdleAnimation();
        }
    }
    
    WeaponSpriteSet GetSpriteSet(WeaponType weaponType)
    {
        for (int i = 0; i < weaponSprites.Length; i++)
        {
            if (weaponSprites[i].weaponType == weaponType)
            {
                return weaponSprites[i];
            }
        }
        return null;
    }
    
    // 动画方法
    public void PlayFireAnimation()
    {
        if (isEmptyHands) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null && spriteSet.fireFrames.Length > 0)
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
                
            animationCoroutine = StartCoroutine(PlayAnimation(spriteSet.fireFrames, fireFrameRate, false));
            StartCoroutine(RecoilEffect());
        }
    }
    
    public void PlayReloadAnimation()
    {
        if (isEmptyHands) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null && spriteSet.reloadFrames.Length > 0)
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
                
            animationCoroutine = StartCoroutine(PlayAnimation(spriteSet.reloadFrames, reloadFrameRate, false));
        }
    }
    
    public void PlayInteractionAnimation()
    {
        if (isEmptyHands && handSprites.interactionFrames.Length > 0)
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
                
            isInteracting = true;
            animationCoroutine = StartCoroutine(PlayAnimation(handSprites.interactionFrames, interactionFrameRate, false));
        }
    }
    
    void PlayRunningAnimation()
    {
        if (isEmptyHands && handSprites.runningFrames.Length > 0)
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
                
            animationCoroutine = StartCoroutine(PlayAnimation(handSprites.runningFrames, idleFrameRate * 1.5f, true));
        }
    }
    
    void StartIdleAnimation()
    {
        if (isEmptyHands)
        {
            StartEmptyHandsIdle();
            return;
        }
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null && spriteSet.idleFrames.Length > 0)
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
                
            animationCoroutine = StartCoroutine(PlayAnimation(spriteSet.idleFrames, idleFrameRate, true));
        }
    }
    
    void StartEmptyHandsIdle()
    {
        if (handSprites.emptyHandsIdle.Length > 0)
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
                
            animationCoroutine = StartCoroutine(PlayAnimation(handSprites.emptyHandsIdle, idleFrameRate, true));
        }
    }
    
    public void SetAiming(bool aiming)
    {
        if (isEmptyHands) return;
        
        WeaponSpriteSet spriteSet = GetCurrentSpriteSet();
        if (spriteSet != null)
        {
            if (aiming && spriteSet.aimSprite != null)
            {
                isAnimating = false;
                if (animationCoroutine != null)
                    StopCoroutine(animationCoroutine);
                weaponImage.sprite = spriteSet.aimSprite;
            }
            else if (!aiming)
            {
                StartIdleAnimation();
            }
        }
    }
    
    WeaponSpriteSet GetCurrentSpriteSet()
    {
        if (currentWeapon == null) return null;
        return GetSpriteSet(currentWeapon.weaponType);
    }
    
    IEnumerator PlayAnimation(Sprite[] frames, float frameRate, bool loop)
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
        } while (loop && isAnimating);
        
        // 动画结束处理
        if (!loop)
        {
            if (isInteracting)
            {
                isInteracting = false;
                StartEmptyHandsIdle();
            }
            else if (currentWeapon != null && !currentWeapon.IsReloading)
            {
                StartIdleAnimation();
            }
            else if (isEmptyHands)
            {
                StartEmptyHandsIdle();
            }
        }
    }
    
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
    
    // 公共方法
    public void OnWeaponFired() => PlayFireAnimation();
    public void OnWeaponReload() => PlayReloadAnimation();
    public void OnWeaponSwitch(WeaponType newType) => SetWeaponType(newType);
    public void OnInteraction() => PlayInteractionAnimation();
}