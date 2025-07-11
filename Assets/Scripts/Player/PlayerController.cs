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
    [SerializeField] private float groundCheckDistance = 0.2f;
    
    [Header("鼠标设置")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private bool invertMouseY = false;
    
    [Header("下蹲设置 (Doom风格)")]
    [SerializeField] private float crouchCameraOffset = -0.8f; // 只降低相机，不改变碰撞体
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
    private bool wasGrounded; // 用于落地检测
    private bool isRunning;
    private bool isCrouching;
    private bool isAiming;
    private bool isLeaning;
    private float leanDirection;
    private float currentLeanAngle;
    private float currentCrouchOffset; // 改为相机偏移而不是高度
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
            currentCrouchOffset = 0f; // 初始化下蹲偏移
            
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
void HandleWeaponSwitching()//TODO:武器切换
{
    if (weaponManager == null) return;
    
    // 保持你现有的武器切换逻辑
    /*if (Input.GetKeyDown(KeyCode.Alpha1))
    {
        weaponManager.SwitchWeapon(0);
        NotifyWeaponDisplay(0); // 修改：传入武器索引
    }
    else if (Input.GetKeyDown(KeyCode.Alpha2))
    {
        weaponManager.SwitchWeapon(1);
        NotifyWeaponDisplay(1);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha3)) // 新增：近战武器
    {
        weaponManager.SwitchWeapon(2);
        NotifyWeaponDisplay(2);
    }*/
    
    // 滚轮切换
    float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
    if (scrollDelta != 0)
    {
        int direction = scrollDelta > 0 ? 1 : -1;
        weaponManager.CycleWeapon(direction);
        NotifyWeaponDisplayCycle(); // 新增：循环切换通知
    }
    
    // 空手切换（新增）
    if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.H))
    {
        weaponManager.SetEmptyHands(); // 需要在WeaponManager中实现
        NotifyWeaponDisplayEmptyHands();
    }
    
    // 换弹（保持原有逻辑，但排除近战武器）
    if (Input.GetKeyDown(KeyCode.R))
    {
        var currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon != null && currentWeapon.weaponType != WeaponType.Knife)
        {
            weaponManager.Reload();
        }
    }
}

// 新增：通知武器显示系统
void NotifyWeaponDisplay(int weaponIndex)
{
    WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
    if (weaponDisplay != null && weaponManager != null)
    {
        var currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            weaponDisplay.OnWeaponSwitch(currentWeapon.weaponType);
        }
    }
}

void NotifyWeaponDisplayCycle()
{
    WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
    if (weaponDisplay != null && weaponManager != null)
    {
        var currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            weaponDisplay.OnWeaponSwitch(currentWeapon.weaponType);
        }
        else
        {
            weaponDisplay.OnGoEmptyHands();
        }
    }
}

void NotifyWeaponDisplayEmptyHands()
{
    WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
    if (weaponDisplay != null)
    {
        weaponDisplay.OnGoEmptyHands();
    }
}
// 新增：通知2D武器显示系统
    void NotifyWeaponDisplay()
    {
        WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
        if (weaponDisplay != null && weaponManager != null)
        {
            var currentWeapon = weaponManager.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                weaponDisplay.OnWeaponSwitch(currentWeapon.weaponType);
            }
            else
            {
                // 空手状态
                weaponDisplay.OnGoEmptyHands(); // ✅ 正确的方法名
            }
        }
    }
    void CreatePlayerCamera()
    {
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(transform);
        // Doom风格：相机位置应该在角色眼睛高度
        cameraObj.transform.localPosition = new Vector3(0, 1.7f, 0); // 不要太往前
        cameraObj.transform.localRotation = Quaternion.identity;
    
        playerCamera = cameraObj.AddComponent<Camera>();
        playerCamera.fieldOfView = normalFOV;
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
            
            // 修复：不要在运行时改变CharacterController的高度，保持固定
            controller.height = 2f; // 固定高度
            controller.center = new Vector3(0, 1f, 0); // 固定中心点
            
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
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        // 读取鼠标输入
        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // 状态判断
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isAiming;

        // 跳跃判断
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching)
        {
            Jump();
        }
    }

    void HandleMovement()
    {
        // 记录上一帧是否在地面
        wasGrounded = isGrounded;
        
        // 修复：改进地面检测
        CheckGrounded();
        
        // 检测落地
        if (!wasGrounded && isGrounded && velocity.y < 0)
        {
            // 刚落地
            PlaySound(landSound);
            Debug.Log("Player landed!");
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

        // 修复：简化移动逻辑
        if (isGrounded)
        {
            // 地面移动
            if (moveDirection.magnitude > 0.1f)
            {
                // 有输入时加速
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
                Vector3 targetVelocity = moveDirection * currentSpeed;
                velocity.x = targetVelocity.x;
                velocity.z = targetVelocity.z;
            }
            else
            {
                // 无输入时减速
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
                velocity.x = Mathf.Lerp(velocity.x, 0f, deceleration * Time.deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, 0f, deceleration * Time.deltaTime);
            }
            
            // 修复：在地面时重置垂直速度
            if (velocity.y <= 0f)
            {
                velocity.y = -0.5f; // 轻微的负值保持贴地
            }
        }
        else
        {
            // 空中移动 - 限制控制力
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 targetVelocity = moveDirection * (targetSpeed * airControl);
                velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, airControl * Time.deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, airControl * Time.deltaTime);
            }
            
            // 修复：确保重力持续作用
            velocity.y -= gravity * Time.deltaTime;
        }

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

    // 修复：更简单有效的地面检测
    void CheckGrounded()
    {
        // 使用CharacterController自带的isGrounded作为主要判断
        isGrounded = controller.isGrounded;
        
        // 如果CharacterController说不在地面，再用射线检测确认
        if (!isGrounded)
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            float rayDistance = 0.2f;
            
            isGrounded = Physics.Raycast(rayStart, Vector3.down, rayDistance);
            
            // 调试可视化
            Debug.DrawRay(rayStart, Vector3.down * rayDistance, isGrounded ? Color.green : Color.red);
        }
    }
    
    void HandleRotation()
    {
        if (playerCamera == null) return;
        
        try
        {
            // 水平旋转（Y轴）- 旋转整个玩家
            transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);
            
            // 垂直旋转（X轴）- 只旋转相机
            float mouseY = invertMouseY ? mouseInput.y : -mouseInput.y;
            cameraRotationX += mouseY * mouseSensitivity;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -maxLookAngle, maxLookAngle);
            
            // 应用相机旋转（包括倾斜）
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
        
        // 修复：Doom风格下蹲 - 只改变相机位置，不改变碰撞体
        float targetCrouchOffset = isCrouching ? crouchCameraOffset : 0f;
        currentCrouchOffset = Mathf.Lerp(currentCrouchOffset, targetCrouchOffset, crouchTransitionSpeed * Time.deltaTime);
        
        // 不再修改CharacterController的高度和中心点，保持固定
        // controller.height = currentCrouchHeight;
        // controller.center = new Vector3(0, currentCrouchHeight / 2, 0);
        
        // 只更新相机位置
        if (playerCamera != null)
        {
            Vector3 cameraPos = originalCameraPosition;
            cameraPos.y += currentCrouchOffset; // 只是上下移动相机
            playerCamera.transform.localPosition = cameraPos;
        }
    }
    
    void HandleLean()
    {
        if (playerCamera == null) return;
        
        float targetLean = 0f;
        Vector3 targetOffset = Vector3.zero;
        
        // 修复：交换Q和E的倾斜方向
        if (Input.GetKey(KeyCode.Q))
        {
            targetLean = leanAngle;
            targetOffset = transform.right * leanOffset;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            targetLean = -leanAngle;
            targetOffset = -transform.right * leanOffset;
        }
        
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLean, leanSpeed * Time.deltaTime);
        
        // 应用倾斜偏移到相机位置（结合下蹲偏移）
        Vector3 currentCameraPos = playerCamera.transform.localPosition;
        Vector3 desiredPos = originalCameraPosition + 
                           new Vector3(0, currentCrouchOffset, 0) + // 下蹲偏移
                           targetOffset; // 倾斜偏移
        playerCamera.transform.localPosition = Vector3.Lerp(currentCameraPos, desiredPos, leanSpeed * Time.deltaTime);
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
        
        // 相机摇晃（行走时）
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
            
            // 计算基础相机位置（包括下蹲和倾斜调整）
            Vector3 baseCameraPos = originalCameraPosition;
            baseCameraPos.y += currentCrouchOffset; // 添加下蹲偏移
            
            playerCamera.transform.localPosition = baseCameraPos + bobOffset;
        }
        
        // 瞄准摇晃
        if (isAiming)
        {
            swayTimer += Time.deltaTime * aimSwaySpeed;
            float swayX = Mathf.Sin(swayTimer) * aimSwayAmount;
            float swayY = Mathf.Sin(swayTimer * 0.7f) * aimSwayAmount * 0.7f;
            
            playerCamera.transform.localRotation = originalCameraRotation * Quaternion.Euler(cameraRotationX + swayY, swayX, currentLeanAngle);
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
        // 修复：使用正确的跳跃计算
        velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
        PlaySound(jumpSound);
        Debug.Log($"Jump executed! velocity.y = {velocity.y}");
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