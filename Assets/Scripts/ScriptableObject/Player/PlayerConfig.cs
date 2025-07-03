using UnityEngine;

[CreateAssetMenu(fileName = "EnhancedPlayerConfig", menuName = "Game/Enhanced Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("移动设置")]
    [Range(1f, 10f)] public float walkSpeed = 3.5f;
    [Range(5f, 20f)] public float runSpeed = 7f;
    [Range(0.5f, 3f)] public float crouchSpeed = 1.5f;
    [Range(1f, 5f)] public float aimSpeed = 2f;
    [Range(5f, 20f)] public float acceleration = 10f;
    [Range(5f, 20f)] public float deceleration = 10f;
    [Range(0f, 1f)] public float airControl = 0.3f;
    [Range(0f, 1f)] public float slopeLimit = 45f;
    
    [Header("跳跃设置")]
    [Range(0.5f, 5f)] public float jumpHeight = 1.5f;
    [Range(5f, 50f)] public float gravity = 20f;
    [Range(0.01f, 0.5f)] public float groundCheckDistance = 0.1f;
    public bool allowDoubleJump = false;
    public bool allowWallJump = false;
    
    [Header("鼠标设置")]
    [Range(0.1f, 10f)] public float mouseSensitivity = 2f;
    [Range(0.1f, 10f)] public float aimSensitivity = 1f;
    [Range(30f, 90f)] public float maxLookAngle = 80f;
    public bool invertMouseY = false;
    public bool smoothMouseLook = true;
    [Range(1f, 20f)] public float mouseSmoothTime = 10f;
    
    [Header("下蹲设置")]
    [Range(0.5f, 1.5f)] public float crouchHeight = 1f;
    [Range(1.5f, 3f)] public float standingHeight = 2f;
    [Range(1f, 20f)] public float crouchTransitionSpeed = 10f;
    public bool holdToCrouch = false;
    public bool canCrouchInAir = false;
    
    [Header("倾斜设置")]
    [Range(5f, 45f)] public float leanAngle = 15f;
    [Range(1f, 10f)] public float leanSpeed = 5f;
    [Range(0.1f, 1f)] public float leanOffset = 0.3f;
    public bool canLeanWhileMoving = true;
    
    [Header("瞄准设置")]
    [Range(30f, 60f)] public float aimFOV = 40f;
    [Range(50f, 90f)] public float normalFOV = 60f;
    [Range(1f, 20f)] public float aimTransitionSpeed = 10f;
    [Range(0f, 0.5f)] public float aimSwayAmount = 0.1f;
    [Range(0.5f, 5f)] public float aimSwaySpeed = 2f;
    public bool holdToAim = true;
    
    [Header("相机摇晃设置")]
    [Range(1f, 20f)] public float walkBobSpeed = 8f;
    [Range(0f, 0.2f)] public float walkBobAmount = 0.05f;
    [Range(5f, 30f)] public float runBobSpeed = 12f;
    [Range(0f, 0.5f)] public float runBobAmount = 0.1f;
    [Range(0f, 1f)] public float crouchBobMultiplier = 0.5f;
    public AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("手部摇晃设置")]
    [Range(0f, 0.1f)] public float idleSwayAmount = 0.02f;
    [Range(0.5f, 3f)] public float idleSwaySpeed = 1f;
    [Range(0f, 0.2f)] public float moveSwayAmount = 0.05f;
    [Range(1f, 5f)] public float moveSwaySpeed = 2f;
    public bool enableBreathingSway = true;
    
    [Header("武器设置")]
    [Range(1f, 10f)] public float weaponSwitchSpeed = 5f;
    [Range(0.1f, 1f)] public float weaponDropHeight = 0.5f;
    public bool autoReload = true;
    public bool infiniteAmmo = false;
    
    [Header("交互设置")]
    [Range(1f, 10f)] public float interactionRange = 3f;
    [Range(0.1f, 1f)] public float interactionDot = 0.8f; // 需要多准确地看着物体
    public LayerMask interactableLayers = -1;
    
    [Header("音效设置")]
    public AudioClip[] footstepSounds;
    public AudioClip[] footstepSoundsRun;
    public AudioClip[] footstepSoundsCrouch;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip landHardSound; // 重着陆
    [Range(0.1f, 1f)] public float footstepInterval = 0.5f;
    [Range(0f, 1f)] public float footstepVolume = 0.7f;
    
    [Header("生命值设置")]
    [Range(50f, 200f)] public float maxHealth = 100f;
    [Range(0f, 100f)] public float maxArmor = 100f;
    [Range(0f, 10f)] public float healthRegenRate = 0f;
    [Range(0f, 30f)] public float healthRegenDelay = 5f;
    [Range(0f, 1f)] public float armorAbsorption = 0.5f;
    
    [Header("伤害反馈")]
    public bool enableDamageShake = true;
    public bool enableDamageVignette = true;
    public bool enableDamageIndicators = true;
    [Range(0f, 1f)] public float damageShakeIntensity = 0.5f;
    public Color damageVignetteColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("控制设置")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode leanLeftKey = KeyCode.Q;
    public KeyCode leanRightKey = KeyCode.E;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode reloadKey = KeyCode.R;
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode weapon1Key = KeyCode.Alpha1;
    public KeyCode weapon2Key = KeyCode.Alpha2;
    
    [Header("高级设置")]
    public bool enableBunnyHopping = false;
    public bool enableSliding = false;
    public bool enableWallRunning = false;
    public bool enableMantling = false;
    [Range(0f, 2f)] public float slideFriction = 0.3f;
    [Range(1f, 5f)] public float wallRunSpeed = 5f;
    
    [Header("调试设置")]
    public bool showDebugInfo = false;
    public bool showMovementGizmos = false;
    public bool enableCheats = false;
    public bool enableDoubleJump;

    // 验证数据
    void OnValidate()
    {
        // 确保值的合理性
        crouchHeight = Mathf.Min(crouchHeight, standingHeight - 0.5f);
        walkSpeed = Mathf.Min(walkSpeed, runSpeed);
        crouchSpeed = Mathf.Min(crouchSpeed, walkSpeed);
        aimSpeed = Mathf.Min(aimSpeed, walkSpeed);
        
        // FOV限制
        aimFOV = Mathf.Min(aimFOV, normalFOV);
    }
    
    // 便捷方法
    public float GetSpeedForState(bool isRunning, bool isCrouching, bool isAiming)
    {
        if (isAiming) return aimSpeed;
        if (isCrouching) return crouchSpeed;
        if (isRunning) return runSpeed;
        return walkSpeed;
    }
    
    public AudioClip GetFootstepSound(bool isRunning, bool isCrouching)
    {
        AudioClip[] clips = footstepSounds;
        
        if (isRunning && footstepSoundsRun.Length > 0)
            clips = footstepSoundsRun;
        else if (isCrouching && footstepSoundsCrouch.Length > 0)
            clips = footstepSoundsCrouch;
        
        if (clips.Length > 0)
            return clips[Random.Range(0, clips.Length)];
        
        return null;
    }
}

// 预设配置
public static class PlayerConfigPresets
{
    public static void ApplyRealisticPreset(PlayerConfig config)
    {
        config.walkSpeed = 3.5f;
        config.runSpeed = 6f;
        config.crouchSpeed = 1.5f;
        config.jumpHeight = 1.2f;
        config.enableBunnyHopping = false;
        config.enableDoubleJump = false;
        config.healthRegenRate = 0f;
    }
    
    public static void ApplyArcadePreset(PlayerConfig config)
    {
        config.walkSpeed = 5f;
        config.runSpeed = 10f;
        config.crouchSpeed = 3f;
        config.jumpHeight = 2f;
        config.enableBunnyHopping = true;
        config.enableDoubleJump = true;
        config.healthRegenRate = 5f;
    }
    
    public static void ApplyDoomPreset(PlayerConfig config)
    {
        config.walkSpeed = 4.5f;
        config.runSpeed = 9f;
        config.crouchSpeed = 2f;
        config.jumpHeight = 1.5f;
        config.acceleration = 15f;
        config.deceleration = 15f;
        config.enableBunnyHopping = true;
        config.mouseSensitivity = 3f;
        config.walkBobAmount = 0.03f;
        config.runBobAmount = 0.06f;
    }
}
