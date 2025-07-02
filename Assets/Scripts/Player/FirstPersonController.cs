using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("配置")]
    public PlayerConfig config;
    
    [Header("调试")]
    public bool showDebugInfo = false;
    public bool enableCheats = false;
    
    // 组件引用
    private CharacterController controller;
    private Camera playerCamera;
    private AudioSource audioSource;
    
    // 移动相关
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private float verticalVelocity = 0f;
    
    // 状态变量
    private bool isGrounded = false;
    private bool isCrouching = false;
    private bool isRunning = false;
    private bool wasGrounded = false;
    private int currentAirJumps = 0;
    private float lastJumpTime = 0f;
    
    // 鼠标控制
    private float mouseX = 0f;
    private float mouseY = 0f;
    
    // 音效相关
    private float lastFootstepTime = 0f;
    private float footstepInterval = 0.5f;
    
    // Doom风格移动
    private Vector3 wishDirection = Vector3.zero;
    private float currentSpeed = 0f;
    
    public PlayerStats Stats { get; private set; }
    
    void Start()
    {
        InitializeComponents();
        InitializeSettings();
        InitializeStats();
    }
    
    void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        audioSource = GetComponent<AudioSource>();
        
        if (playerCamera == null)
        {
            GameObject cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(transform);
            playerCamera = cameraObj.AddComponent<Camera>();
        }
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void InitializeSettings()
    {
        if (config == null)
        {
            Debug.LogWarning("PlayerConfig not assigned, using default values");
            return;
        }
        
        // 设置控制器高度
        controller.height = config.standingHeight;
        controller.center = new Vector3(0, config.standingHeight / 2, 0);
        
        // 设置摄像机位置
        UpdateCameraPosition();
        
        // 锁定光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void InitializeStats()
    {
        Stats = new PlayerStats();
    }
    
    void Update()
    {
        if (config == null) return;
        
        HandleInput();
        HandleMouseLook();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleAudio();
        
        UpdateStats();
        
        if (showDebugInfo)
            DisplayDebugInfo();
    }
    
    void HandleInput()
    {
        // 基础移动输入
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Doom风格：使用原始输入，支持45度角移动
        wishDirection = (transform.right * horizontal + transform.forward * vertical);
        
        // 奔跑输入
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && isGrounded;
        
        // 调试功能
        if (enableCheats)
        {
            HandleCheatInput();
        }
    }
    
    void HandleCheatInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            // 飞行模式切换
            Physics.gravity = Physics.gravity.y == 0 ? new Vector3(0, -9.81f, 0) : Vector3.zero;
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            // 无敌模式
            var health = GetComponent<PlayerHealth>();
            if (health) health.Heal(health.GetMaxHealth());
        }
    }
    
    void HandleMouseLook()
    {
        float mouseMultiplier = config.invertMouseY ? -1f : 1f;
        
        mouseX += Input.GetAxis("Mouse X") * config.mouseSensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * config.mouseSensitivity * mouseMultiplier;
        
        mouseY = Mathf.Clamp(mouseY, -config.maxLookAngle, config.maxLookAngle);
        
        // 应用旋转
        transform.rotation = Quaternion.Euler(0, mouseX, 0);
        playerCamera.transform.localRotation = Quaternion.Euler(mouseY, 0, 0);
    }
    
    void HandleMovement()
    {
        // 检查地面状态
        wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;
        
        if (isGrounded && !wasGrounded)
        {
            OnLanded();
        }
        
        // Doom风格移动计算
        if (isGrounded)
        {
            GroundMovement();
        }
        else
        {
            AirMovement();
        }
        
        // 应用重力
        if (!isGrounded)
        {
            verticalVelocity -= config.gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = -2f; // 轻微向下的力保持贴地
        }
        
        // 最终移动
        Vector3 finalMovement = moveDirection + Vector3.up * verticalVelocity;
        controller.Move(finalMovement * Time.deltaTime);
        
        // 更新统计
        Stats.distanceTraveled += moveDirection.magnitude * Time.deltaTime;
    }
    
    void GroundMovement()
    {
        float targetSpeed = GetTargetSpeed();
        
        if (config.enableStrafing && wishDirection.magnitude > 0)
        {
            // Doom风格加速
            DoomAccelerate(wishDirection.normalized, targetSpeed, config.acceleration);
        }
        else
        {
            // 摩擦力
            ApplyFriction();
        }
        
        // 限制最大速度
        if (moveDirection.magnitude > targetSpeed)
        {
            moveDirection = moveDirection.normalized * targetSpeed;
        }
    }
    
    void AirMovement()
    {
        if (wishDirection.magnitude > 0)
        {
            float targetSpeed = config.airSpeed;
            DoomAccelerate(wishDirection.normalized, targetSpeed, config.airAcceleration);
        }
    }
    
    void DoomAccelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float currentspeed = Vector3.Dot(moveDirection, wishdir);
        float addspeed = wishspeed - currentspeed;
        
        if (addspeed <= 0) return;
        
        float accelspeed = accel * wishspeed * Time.deltaTime;
        
        if (accelspeed > addspeed)
            accelspeed = addspeed;
        
        moveDirection += wishdir * accelspeed;
    }
    
    void ApplyFriction()
    {
        float speed = moveDirection.magnitude;
        if (speed < 0.1f)
        {
            moveDirection = Vector3.zero;
            return;
        }
        
        float friction = config.friction * Time.deltaTime;
        float newSpeed = Mathf.Max(0, speed - friction);
        
        moveDirection = moveDirection.normalized * newSpeed;
    }
    
    float GetTargetSpeed()
    {
        if (isCrouching) return config.crouchSpeed;
        if (isRunning) return config.runSpeed;
        return config.walkSpeed;
    }
    
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded && Time.time - lastJumpTime > config.jumpCooldown)
            {
                Jump();
            }
            else if (config.allowAirJump && currentAirJumps < config.maxAirJumps && 
                     Time.time - lastJumpTime > config.jumpCooldown)
            {
                AirJump();
            }
        }
    }
    
    void Jump()
    {
        verticalVelocity = config.jumpForce;
        lastJumpTime = Time.time;
        currentAirJumps = 0;
        
        PlaySound(config.jumpSound);
        Stats.jumpCount++;
        
        // Bunny Hop支持
        if (config.enableBunnyHopping)
        {
            float currentHorizontalSpeed = new Vector3(moveDirection.x, 0, moveDirection.z).magnitude;
            if (currentHorizontalSpeed > config.walkSpeed)
            {
                // 保持水平速度用于连跳
                Stats.bunnyHopCount++;
            }
        }
    }
    
    void AirJump()
    {
        verticalVelocity = config.jumpForce * 0.8f; // 空中跳跃力度较小
        lastJumpTime = Time.time;
        currentAirJumps++;
        
        PlaySound(config.jumpSound);
        Stats.airJumpCount++;
    }
    
    void HandleCrouch()
    {
        bool wantsToCrouch = Input.GetKey(KeyCode.C);
        
        if (wantsToCrouch && !isCrouching)
        {
            StartCrouch();
        }
        else if (!wantsToCrouch && isCrouching)
        {
            if (CanStandUp())
            {
                StopCrouch();
            }
        }
    }
    
    void StartCrouch()
    {
        isCrouching = true;
        controller.height = config.crouchingHeight;
        controller.center = new Vector3(0, config.crouchingHeight / 2, 0);
        UpdateCameraPosition();
        
        Stats.crouchTime += Time.deltaTime;
    }
    
    void StopCrouch()
    {
        isCrouching = false;
        controller.height = config.standingHeight;
        controller.center = new Vector3(0, config.standingHeight / 2, 0);
        UpdateCameraPosition();
    }
    
    bool CanStandUp()
    {
        // 检查头顶是否有障碍
        float checkHeight = config.standingHeight - config.crouchingHeight + 0.1f;
        Vector3 checkStart = transform.position + Vector3.up * config.crouchingHeight;
        
        return !Physics.SphereCast(checkStart, controller.radius, Vector3.up, out _, checkHeight);
    }
    
    void UpdateCameraPosition()
    {
        float targetHeight = (isCrouching ? config.crouchingHeight : config.standingHeight);
        playerCamera.transform.localPosition = new Vector3(0, targetHeight - config.cameraHeightOffset, 0);
    }
    
    void HandleAudio()
    {
        if (isGrounded && moveDirection.magnitude > 0.1f)
        {
            float currentInterval = GetFootstepInterval();
            
            if (Time.time - lastFootstepTime > currentInterval)
            {
                PlayFootstepSound();
                lastFootstepTime = Time.time;
            }
        }
    }
    
    float GetFootstepInterval()
    {
        float baseInterval = 0.5f;
        float speedMultiplier = moveDirection.magnitude / config.walkSpeed;
        return baseInterval / speedMultiplier;
    }
    
    void PlayFootstepSound()
    {
        if (config.footstepSounds != null && config.footstepSounds.Length > 0)
        {
            AudioClip clip = config.footstepSounds[Random.Range(0, config.footstepSounds.Length)];
            PlaySound(clip);
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void OnLanded()
    {
        PlaySound(config.landSound);
        currentAirJumps = 0;
        Stats.landingCount++;
    }
    
    void UpdateStats()
    {
        Stats.playTime += Time.deltaTime;
        
        if (isRunning)
            Stats.runTime += Time.deltaTime;
        
        if (isCrouching)
            Stats.crouchTime += Time.deltaTime;
        
        if (!isGrounded)
            Stats.airTime += Time.deltaTime;
        
        Stats.currentSpeed = moveDirection.magnitude;
        Stats.maxSpeedReached = Mathf.Max(Stats.maxSpeedReached, Stats.currentSpeed);
    }
    
    void DisplayDebugInfo()
    {
        string info = $"Speed: {moveDirection.magnitude:F2}\n";
        info += $"Grounded: {isGrounded}\n";
        info += $"Crouching: {isCrouching}\n";
        info += $"Running: {isRunning}\n";
        info += $"Vertical Velocity: {verticalVelocity:F2}\n";
        info += $"Air Jumps: {currentAirJumps}/{config.maxAirJumps}";
        
        // 可以在这里添加UI显示或使用Debug.Log
    }
    
    // 公共接口
    public void SetConfig(PlayerConfig newConfig)
    {
        config = newConfig;
        InitializeSettings();
    }
    
    public void SetMouseSensitivity(float sensitivity)
    {
        if (config) config.mouseSensitivity = sensitivity;
    }
    
    public void ResetPosition(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        moveDirection = Vector3.zero;
        verticalVelocity = 0f;
    }
    
    // 事件
    public System.Action OnJump;
    public System.Action OnLand;
    public System.Action OnStartCrouch;
    public System.Action OnStopCrouch;
}

[System.Serializable]
public class PlayerStats
{
    public float playTime;
    public float distanceTraveled;
    public float runTime;
    public float crouchTime;
    public float airTime;
    public int jumpCount;
    public int airJumpCount;
    public int bunnyHopCount;
    public int landingCount;
    public float currentSpeed;
    public float maxSpeedReached;
    
    public void Reset()
    {
        playTime = 0;
        distanceTraveled = 0;
        runTime = 0;
        crouchTime = 0;
        airTime = 0;
        jumpCount = 0;
        airJumpCount = 0;
        bunnyHopCount = 0;
        landingCount = 0;
        currentSpeed = 0;
        maxSpeedReached = 0;
    }
}