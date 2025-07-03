using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    
    void Start()
    {
        InitializeComponents();
        InitializeSettings();
    }
    
    void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        audioSource = GetComponent<AudioSource>();
        weaponManager = GetComponent<WeaponManager>();
        inventoryManager = InventoryManager.Instance;
        
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        originalCameraPosition = playerCamera.transform.localPosition;
        originalCameraRotation = playerCamera.transform.localRotation;
        
        currentCrouchHeight = standingHeight;
    }
    
    void InitializeSettings()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        controller.height = standingHeight;
        controller.center = new Vector3(0, standingHeight / 2, 0);
        
        playerCamera.fieldOfView = normalFOV;
    }
    
    void Update()
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
    
    void HandleInput()
    {
        // 移动输入
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (moveInput.magnitude > 1f) moveInput.Normalize();
        
        // 鼠标输入
        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        
        // 状态输入
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isAiming;
        
        // 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            Jump();
        }
    }
    
    void HandleMovement()
    {
        // 地面检测
        isGrounded = Physics.Raycast(transform.position, Vector3.down, controller.height / 2 + groundCheckDistance);
        
        // 计算移动方向
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        
        // 计算目标速度
        if (isAiming)
            targetSpeed = aimSpeed;
        else if (isCrouching)
            targetSpeed = crouchSpeed;
        else if (isRunning && moveInput.y > 0)
            targetSpeed = runSpeed;
        else
            targetSpeed = walkSpeed;
        
        // 平滑速度过渡
        if (moveDirection.magnitude > 0.1f)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
        }
        
        // 应用移动
        Vector3 horizontalVelocity = moveDirection * currentSpeed;
        
        // 重力
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        
        // 空中控制
        if (!isGrounded)
        {
            horizontalVelocity = Vector3.Lerp(velocity, horizontalVelocity, airControl);
        }
        
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
        
        controller.Move(velocity * Time.deltaTime);
        
        // 脚步声
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
        // 水平旋转（Y轴）
        transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);
        
        // 垂直旋转（X轴）
        float mouseY = invertMouseY ? mouseInput.y : -mouseInput.y;
        cameraRotationX += mouseY * mouseSensitivity;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -maxLookAngle, maxLookAngle);
        
        playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0f, currentLeanAngle);
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
        Vector3 cameraPos = originalCameraPosition;
        cameraPos.y = currentCrouchHeight - 0.2f;
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraPos, crouchTransitionSpeed * Time.deltaTime);
    }
    
    void HandleLean()
    {
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
        Vector3 currentOffset = playerCamera.transform.parent.localPosition;
        playerCamera.transform.parent.localPosition = Vector3.Lerp(currentOffset, targetOffset, leanSpeed * Time.deltaTime);
    }
    
    void HandleAiming()
    {
        isAiming = Input.GetMouseButton(1);
        
        float targetFOV = isAiming ? aimFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, aimTransitionSpeed * Time.deltaTime);
        
        // 通知武器管理器
        if (weaponManager)
        {
            weaponManager.SetAiming(isAiming);
        }
    }
    
    void HandleWeaponSwitching()
    {
        // 数字键切换
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            weaponManager?.SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            weaponManager?.SwitchWeapon(1);
        }
        
        // 滚轮切换
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            int direction = scrollDelta > 0 ? 1 : -1;
            weaponManager?.CycleWeapon(direction);
        }
        
        // 换弹
        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponManager?.Reload();
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
        if (weaponHolder)
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
        if (footstepSounds.Length > 0)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
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
    
    void PerformInteraction()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 3f))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            interactable?.Interact();
        }
    }
    
    void PerformPickup()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 3f))
        {
            PickupItem pickup = hit.collider.GetComponent<PickupItem>();
            if (pickup)
            {
                // 尝试拾取
                Debug.Log($"Picked up: {pickup.itemData.itemName}");
            }
        }
    }
    
    void ToggleInventory()
    {
        if (UIManager.Instance)
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
}

// 交互接口
public interface IInteractable
{
    void Interact();
    string GetInteractionText();
}