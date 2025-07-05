using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float aimSpeed = 2f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.3f;
    
    [Header("跳跃设置")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [Header("鼠标设置")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private bool invertMouseY = false;
    
    [Header("下蹲设置")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    
    [Header("倾斜设置")]
    [SerializeField] private float leanAngle = 15f;
    [SerializeField] private float leanSpeed = 5f;
    [SerializeField] private float leanOffset = 0.3f;
    
    [Header("瞄准设置")]
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float aimTransitionSpeed = 10f;
    [SerializeField] private float aimSwayAmount = 0.1f;
    [SerializeField] private float aimSwaySpeed = 2f;
    
    [Header("相机摇晃设置")]
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobSpeed = 12f;
    [SerializeField] private float runBobAmount = 0.1f;
    [SerializeField] private float crouchBobMultiplier = 0.5f;
    
    [Header("手部摇晃设置")]
    [SerializeField] private float idleSwayAmount = 0.02f;
    [SerializeField] private float idleSwaySpeed = 1f;
    [SerializeField] private float moveSwayAmount = 0.05f;
    [SerializeField] private float moveSwaySpeed = 2f;
    
    [Header("武器设置")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private float weaponSwitchSpeed = 5f;
    [SerializeField] private float weaponDropHeight = 0.5f;
    
    [Header("音效")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float footstepInterval = 0.5f;
    
    // 组件引用
    private CharacterController controller;
    private Camera playerCamera;
    private AudioSource audioSource;
    private WeaponManager weaponManager;
    private InventoryManager inventoryManager;
    
    // 状态变量
    private Vector3 velocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool isRunning;
    private bool isCrouching;
    private bool isAiming;
    private bool isLeaning;
    private float leanDirection;
    private float currentLeanAngle;
    private float currentCrouchHeight;
    private float bobTimer;
    private float footstepTimer;
    private float swayTimer;
    
    // 输入缓存
    private Vector2 moveInput;
    private Vector2 mouseInput;
    private float targetSpeed;
    
    // 相机相关
    private float cameraRotationX;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    
    // 组件初始化状态
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeComponents();
        if (isInitialized)
        {
            InitializeSettings();
        }
    }

  
    void InitializeComponents()
    {
        try
        {
            // 获取必需的组件
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogError("[PlayerController] CharacterController component not found!");
                return;
            }
            
            // 查找或创建相机
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogWarning("[PlayerController] No camera found in children, creating one...");
                CreatePlayerCamera();
            }
            
            // 确保相机有效
            if (playerCamera == null)
            {
                Debug.LogError("[PlayerController] Failed to create or find player camera!");
                return;
            }
            
            // 获取或添加音频源
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D音频
            }
            
            // 获取其他组件（可以为null）
            weaponManager = GetComponent<WeaponManager>();
            
            // 安全获取管理器实例
            StartCoroutine(WaitForManagersAndInitialize());
            
            // 设置初始位置和旋转
            originalCameraPosition = playerCamera.transform.localPosition;
            originalCameraRotation = playerCamera.transform.localRotation;
            currentCrouchHeight = standingHeight;
            
            // 确保有AudioListener
            EnsureAudioListener();
            
            isInitialized = true;
            Debug.Log("[PlayerController] Components initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerController] Error during component initialization: {e.Message}");
            isInitialized = false;
        }
    }
    
    System.Collections.IEnumerator WaitForManagersAndInitialize()
    {
        // 等待InventoryManager初始化
        float timeout = 5f;
        float elapsed = 0f;
        
        while (InventoryManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (InventoryManager.Instance != null)
        {
            inventoryManager = InventoryManager.Instance;
            Debug.Log("[PlayerController] InventoryManager connected successfully");
        }
        else
        {
            Debug.LogWarning("[PlayerController] InventoryManager not found after timeout");
        }
    }
    
    void CreatePlayerCamera()
    {
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(transform);
        cameraObj.transform.localPosition = new Vector3(0, standingHeight * 0.9f, 0);
        cameraObj.transform.localRotation = Quaternion.identity;
        
        playerCamera = cameraObj.AddComponent<Camera>();
        playerCamera.fieldOfView = normalFOV;
        
        Debug.Log("[PlayerController] Created new player camera");
    }
    
    void EnsureAudioListener()
    {
        // 检查是否已有AudioListener
        AudioListener existingListener = FindObjectOfType<AudioListener>();
        if (existingListener == null)
        {
            // 在玩家相机上添加AudioListener
            if (playerCamera != null)
            {
                playerCamera.gameObject.AddComponent<AudioListener>();
                Debug.Log("[PlayerController] Added AudioListener to player camera");
            }
        }
    }
    
    void InitializeSettings()
    {
        if (!isInitialized) return;
        
        try
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            controller.height = standingHeight;
            controller.center = new Vector3(0, standingHeight / 2, 0);
            
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = normalFOV;
            }
            
            Debug.Log("[PlayerController] Settings initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerController] Error during settings initialization: {e.Message}");
        }
    }
    
    void Update()
    {
        if (!isInitialized || playerCamera == null) return;
        transform.position = new Vector3(transform.position.x, 1.1f, transform.position.z);
        Debug.DrawRay(transform.position, Vector3.down * (controller.height / 2 + groundCheckDistance), Color.red);
        Debug.Log($"[状态] isGrounded={isGrounded}, Velocity={velocity}, Controller.isGrounded={controller.isGrounded}");

        try
        {
            HandleInput();
            HandleMovement();
            HandleRotation();
            HandleCrouch();
            HandleLean();
            HandleAiming();
            HandleWeaponSwitching();
            HandleInteraction();
            UpdateCameraEffects();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerController] Error in Update: {e.Message}");
        }
    }
    
    void HandleInput()
    {
        // 读取移动输入
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        // 读取鼠标输入
        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // 状态判断
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isAiming;

        // 缓冲跳跃判断
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            Jump();
        }
    }

    
    // 跳跃容错时间（Coyote Time）
    private float groundedTimer = 0f;
    private float coyoteTime = 0.2f;

    void HandleMovement()
    {
        // 更稳定的地面检测
        if (controller.isGrounded)
        {
            isGrounded = true;
            groundedTimer = coyoteTime;
        }
        else
        {
            groundedTimer -= Time.deltaTime;
            isGrounded = groundedTimer > 0f;
        }

        // 计算移动方向（本地空间 → 世界空间）
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // 选择目标速度
        if (isAiming)
            targetSpeed = aimSpeed;
        else if (isCrouching)
            targetSpeed = crouchSpeed;
        else if (isRunning && moveInput.y > 0)
            targetSpeed = runSpeed;
        else
            targetSpeed = walkSpeed;

        // 平滑过渡速度
        if (moveDirection.magnitude > 0.1f)
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);

        // 最终水平速度
        Vector3 targetVelocity = moveDirection * currentSpeed;

        // 空中控制处理（只对 XZ）
        if (!isGrounded)
        {
            Vector3 prevHorizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 blended = Vector3.Lerp(prevHorizontal, targetVelocity, airControl);
            velocity.x = blended.x;
            velocity.z = blended.z;
        }
        else
        {
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
        }

        // 重力处理
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; // 稳定贴地
        else
            velocity.y -= gravity * Time.deltaTime;

        // 最终移动应用
        controller.Move(velocity * Time.deltaTime);

        // 脚步音效播放
        if (isGrounded && currentSpeed > 0.1f)
        {
            footstepTimer += Time.deltaTime;
            float interval = isRunning ? footstepInterval * 0.7f : footstepInterval;

            if (footstepTimer >= interval)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
    }

    
    void HandleRotation()
    {
        if (playerCamera == null) return;
        
        try
        {
            // 水平旋转（Y轴）
            transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);
            
            // 垂直旋转（X轴）
            float mouseY = invertMouseY ? mouseInput.y : -mouseInput.y;
            cameraRotationX += mouseY * mouseSensitivity;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -maxLookAngle, maxLookAngle);
            
            playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0f, currentLeanAngle);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerController] Error in HandleRotation: {e.Message}");
        }
    }
    
    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
        }
        
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        currentCrouchHeight = Mathf.Lerp(currentCrouchHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        
        controller.height = currentCrouchHeight;
        controller.center = new Vector3(0, currentCrouchHeight / 2, 0);
        
        // 更新相机高度
        if (playerCamera != null)
        {
            Vector3 cameraPos = originalCameraPosition;
            cameraPos.y = currentCrouchHeight - 0.2f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraPos, crouchTransitionSpeed * Time.deltaTime);
        }
    }
    
    void HandleLean()
    {
        if (playerCamera == null) return;
        
        float targetLean = 0f;
        Vector3 targetOffset = Vector3.zero;
        
        if (Input.GetKey(KeyCode.Q))
        {
            targetLean = -leanAngle;
            targetOffset = -transform.right * leanOffset;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            targetLean = leanAngle;
            targetOffset = transform.right * leanOffset;
        }
        
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLean, leanSpeed * Time.deltaTime);
        
        // 应用倾斜偏移
        if (playerCamera.transform.parent != null)
        {
            Vector3 currentOffset = playerCamera.transform.parent.localPosition;
            playerCamera.transform.parent.localPosition = Vector3.Lerp(currentOffset, targetOffset, leanSpeed * Time.deltaTime);
        }
    }
    
    void HandleAiming()
    {
        if (playerCamera == null) return;
        
        isAiming = Input.GetMouseButton(1);
        
        float targetFOV = isAiming ? aimFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, aimTransitionSpeed * Time.deltaTime);
        
        // 通知武器管理器
        if (weaponManager != null)
        {
            weaponManager.SetAiming(isAiming);
        }
    }
    
    void HandleWeaponSwitching()
    {
        if (weaponManager == null) return;
        
        // 数字键切换
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            weaponManager.SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            weaponManager.SwitchWeapon(1);
        }
        
        // 滚轮切换
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            int direction = scrollDelta > 0 ? 1 : -1;
            weaponManager.CycleWeapon(direction);
        }
        
        // 换弹
        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponManager.Reload();
        }
    }
    
    void HandleInteraction()
    {
        // 交互
        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformInteraction();
        }
        
        // 拾取
        if (Input.GetKeyDown(KeyCode.F))
        {
            PerformPickup();
        }
        
        // 背包
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }
    
    void UpdateCameraEffects()
    {
        if (playerCamera == null) return;
        
        // 相机摇晃
        if (isGrounded && currentSpeed > 0.1f)
        {
            float bobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float bobAmount = isRunning ? runBobAmount : walkBobAmount;
            
            if (isCrouching)
            {
                bobAmount *= crouchBobMultiplier;
            }
            
            bobTimer += Time.deltaTime * bobSpeed;
            
            float bobX = Mathf.Sin(bobTimer) * bobAmount;
            float bobY = Mathf.Abs(Mathf.Cos(bobTimer)) * bobAmount;
            
            Vector3 bobOffset = new Vector3(bobX * 0.5f, bobY, 0);
            playerCamera.transform.localPosition = originalCameraPosition + bobOffset;
        }
        
        // 瞄准摇晃
        if (isAiming)
        {
            swayTimer += Time.deltaTime * aimSwaySpeed;
            float swayX = Mathf.Sin(swayTimer) * aimSwayAmount;
            float swayY = Mathf.Sin(swayTimer * 0.7f) * aimSwayAmount * 0.7f;
            
            playerCamera.transform.localRotation = originalCameraRotation * Quaternion.Euler(swayY, swayX, 0);
        }
        
        // 手部/武器摇晃
        if (weaponHolder != null)
        {
            UpdateWeaponSway();
        }
    }
    
    void UpdateWeaponSway()
    {
        float swayAmount = currentSpeed > 0.1f ? moveSwayAmount : idleSwayAmount;
        float swaySpeed = currentSpeed > 0.1f ? moveSwaySpeed : idleSwaySpeed;
        
        swayTimer += Time.deltaTime * swaySpeed;
        
        float swayX = Mathf.Sin(swayTimer) * swayAmount;
        float swayY = Mathf.Sin(swayTimer * 0.6f) * swayAmount * 0.6f;
        
        weaponHolder.localPosition = Vector3.Lerp(
            weaponHolder.localPosition,
            new Vector3(swayX, swayY, 0),
            Time.deltaTime * 5f
        );
    }
    
    void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
        PlaySound(jumpSound);
    }
    
    void PlayFootstep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            PlaySound(clip);
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void PerformInteraction()
    {
        if (playerCamera == null) return;
        
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
    
    void PerformPickup()
    {
        if (playerCamera == null) return;
        
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 3f))
        {
            PickupItem pickup = hit.collider.GetComponent<PickupItem>();
            if (pickup != null)
            {
                Debug.Log($"Picked up: {pickup.itemData.itemName}");
            }
        }
    }
    
    void ToggleInventory()
    {
        if (UIManager.Instance != null && UIManager.Instance.inventoryUI != null)
        {
            UIManager.Instance.ToggleInventory();
            
            // 切换鼠标状态
            bool inventoryOpen = UIManager.Instance.inventoryUI.IsVisible();
            Cursor.lockState = inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = inventoryOpen;
        }
    }
    
    // 公共方法
    public float GetCurrentSpeed() => currentSpeed;
    public bool IsGrounded() => isGrounded;
    public bool IsRunning() => isRunning;
    public bool IsCrouching() => isCrouching;
    public bool IsAiming() => isAiming;
    public Vector3 GetVelocity() => velocity;
    public bool IsInitialized() => isInitialized;
}

// 交互接口
public interface IInteractable
{
    void Interact();
    string GetInteractionText();
}