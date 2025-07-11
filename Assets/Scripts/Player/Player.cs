using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class Player : Singleton<Player>, IDamageable
{
    [Header("生命值系统")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 0f;
    public float healthRegenDelay = 5f;
    
    [Header("护甲系统")]
    public float maxArmor = 100f;
    public float currentArmor = 0f;
    public float armorAbsorption = 0.5f;
    
    [Header("移动设置")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 7f;
    public float crouchSpeed = 1.5f;
    public float jumpHeight = 1.5f;
    public float gravity = 20f;
    
    [Header("鼠标设置")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    public bool invertMouseY = false;
    
    [Header("武器系统")]
    public Transform weaponHolder;
    public WeaponManager weaponManager;
    
    [Header("音效")]
    public AudioClip[] hurtSounds;
    public AudioClip[] footstepSounds;
    public AudioClip deathSound;
    public AudioClip healSound;
    
    [Header("UI组件")]
    public PlayerUI playerUI;
    
    // 组件引用
    private CharacterController controller;
    private Camera playerCamera;
    private AudioSource audioSource;
    
    // 状态变量
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;
    private bool isCrouching;
    private bool isAiming;
    private bool isDead = false;
    private float cameraRotationX;
    private float lastDamageTime;
    
    // 输入状态
    private Vector2 moveInput;
    private Vector2 mouseInput;
    
    // 事件
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDeath;
    public System.Action<float> OnDamaged;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // 查找或创建相机
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            GameObject cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.7f, 0);
            playerCamera = cameraObj.AddComponent<Camera>();
            playerCamera.fieldOfView = 60f;
        }
        
        // 查找武器管理器
        weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null)
        {
            weaponManager = gameObject.AddComponent<WeaponManager>();
        }
        
        // 查找UI
        if (playerUI == null)
        {
            playerUI = FindObjectOfType<PlayerUI>();
        }
        
        // 初始化设置
        currentHealth = maxHealth;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        if (isDead) return;
        
        HandleInput();
        HandleMovement();
        HandleRotation();
        HandleWeapons();
        UpdateUI();
        HandleHealthRegen();
    }
    
    void HandleInput()
    {
        // 移动输入
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (moveInput.magnitude > 1f) moveInput.Normalize();
        
        // 鼠标输入
        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        
        // 状态输入
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        
        if (Input.GetKeyDown(KeyCode.C))
            isCrouching = !isCrouching;
            
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            Jump();
            
        isAiming = Input.GetMouseButton(1);
        
        // 交互输入
        if (Input.GetKeyDown(KeyCode.F))
            TryInteract();
    }
    
    void HandleMovement()
    {
        // 地面检测
        isGrounded = controller.isGrounded;
        
        // 计算移动速度
        float targetSpeed = isAiming ? walkSpeed * 0.5f :
                           isCrouching ? crouchSpeed :
                           isRunning ? runSpeed : walkSpeed;
        
        // 移动计算
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        
        if (isGrounded)
        {
            velocity.x = moveDirection.x * targetSpeed;
            velocity.z = moveDirection.z * targetSpeed;
            
            if (velocity.y < 0)
                velocity.y = -2f;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        
        // 应用移动
        controller.Move(velocity * Time.deltaTime);
        
        // 脚步声
        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            PlayFootstep();
        }
    }
    
    void HandleRotation()
    {
        // 水平旋转
        transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);
        
        // 垂直旋转
        cameraRotationX -= mouseInput.y * mouseSensitivity;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
    }
    
    void HandleWeapons()
    {
        // 武器切换

        
        // 滚轮切换
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            weaponManager?.CycleWeapon(scroll > 0 ? 1 : -1);
        }
        
        // 换弹
        if (Input.GetKeyDown(KeyCode.R))
            weaponManager?.Reload();
        
        // 瞄准
        weaponManager?.SetAiming(isAiming);
    }
    
    void UpdateUI()
    {
        if (playerUI != null)
        {
            // UI会自动从Player读取数据
        }
    }
    
    void HandleHealthRegen()
    {
        if (healthRegenRate > 0 && currentHealth < maxHealth)
        {
            if (Time.time - lastDamageTime > healthRegenDelay)
            {
                Heal(healthRegenRate * Time.deltaTime, false);
            }
        }
    }
    
    void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
    }
    
    void TryInteract()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 3f))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }
    
    void PlayFootstep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    // IDamageable 实现
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        lastDamageTime = Time.time;
        
        // 护甲吸收
        float actualDamage = damage;
        if (currentArmor > 0)
        {
            float armorDamage = damage * armorAbsorption;
            float healthDamage = damage * (1f - armorAbsorption);
            
            float armorLost = Mathf.Min(currentArmor, armorDamage);
            currentArmor -= armorLost;
            
            if (armorLost < armorDamage)
            {
                healthDamage += (armorDamage - armorLost);
            }
            
            actualDamage = healthDamage;
        }
        
        currentHealth -= actualDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        // 播放受伤音效
        if (hurtSounds != null && hurtSounds.Length > 0)
        {
            AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
        
        // 触发事件
        OnHealthChanged?.Invoke(currentHealth);
        OnDamaged?.Invoke(damage);
        
        // 通知UI
        if (playerUI != null)
        {
            playerUI.OnPlayerDamaged(damage);
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
        
        Debug.Log($"[Player] Took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }
    
    public void Heal(float amount, bool playSound = true)
    {
        if (isDead || currentHealth >= maxHealth) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        if (playSound && healSound)
        {
            audioSource.PlayOneShot(healSound);
        }
        
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void AddArmor(float amount)
    {
        if (isDead) return;
        
        currentArmor += amount;
        currentArmor = Mathf.Clamp(currentArmor, 0f, maxArmor);
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        
        if (deathSound)
            audioSource.PlayOneShot(deathSound);
        
        OnPlayerDeath?.Invoke();
        
        // 禁用控制
        enabled = false;
        
        // 通知游戏管理器
        if (GameManager.Instance)
        {
            GameManager.Instance.ChangePhase(GamePhase.GameEnd);
        }
    }
    
    // IDamageable 接口
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => !isDead;
    
    // 公共访问方法
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetArmorPercentage() => currentArmor / maxArmor;
    public bool IsRunning() => isRunning;
    public bool IsCrouching() => isCrouching;
    public bool IsAiming() => isAiming;
    public bool IsGrounded() => isGrounded;
    public Vector3 GetVelocity() => velocity;
    public Camera GetCamera() => playerCamera;
    public WeaponManager GetWeaponManager() => weaponManager;
    
    // 调试方法
    [ContextMenu("Take Damage")]
    void DebugTakeDamage() => TakeDamage(25f);
    
    [ContextMenu("Full Heal")]
    void DebugFullHeal() => Heal(maxHealth);
}