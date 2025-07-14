using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;
using UnityEngine;
using System.Collections;

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
    
    [Header("下蹲设置")]
    [SerializeField] private float crouchCameraOffset = -0.8f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    
    [Header("倾斜设置")]
    [SerializeField] private float leanAngle = 15f;
    [SerializeField] private float leanSpeed = 5f;
    [SerializeField] private float leanOffset = 0.3f;
    
    [Header("瞄准设置")]
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float aimTransitionSpeed = 10f;
    
    [Header("相机摇晃设置")]
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobSpeed = 12f;
    [SerializeField] private float runBobAmount = 0.1f;
    [SerializeField] private float damageShakeIntensity = 0.5f;
    
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
    
    // 状态变量
    private Vector3 velocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isRunning;
    private bool isCrouching;
    private bool isAiming;
    private float currentLeanAngle;
    private float currentCrouchOffset;
    private float bobTimer;
    private float footstepTimer;
    
    // 输入缓存
    private Vector2 moveInput;
    private Vector2 mouseInput;
    
    // 相机相关
    private float cameraRotationX;
    private Vector3 originalCameraPosition;
    private Coroutine damageShakeCoroutine;
    
    // 组件初始化状态
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeComponents();
        InitializeSettings();
    }

    void InitializeComponents()
    {
        try
        {
            controller = GetComponent<CharacterController>();
            weaponManager = GetComponent<WeaponManager>();
            
            // 查找或创建相机
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                CreatePlayerCamera();
            }
            
            // 获取或添加音频源
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
            }
            
            // 设置初始位置
            originalCameraPosition = playerCamera.transform.localPosition;
            currentCrouchOffset = 0f;
            
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
    
    void CreatePlayerCamera()
    {
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(transform);
        cameraObj.transform.localPosition = new Vector3(0, 1.7f, 0);
        cameraObj.transform.localRotation = Quaternion.identity;
    
        playerCamera = cameraObj.AddComponent<Camera>();
        playerCamera.fieldOfView = normalFOV;
    }
    
    void EnsureAudioListener()
    {
        AudioListener existingListener = FindObjectOfType<AudioListener>();
        if (existingListener == null)
        {
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
            
            controller.height = 2f;
            controller.center = new Vector3(0, 1f, 0);
            
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
            HandleWeaponControls();
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
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !isAiming;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching)
        {
            Jump();
        }
    }

    void HandleMovement()
    {
        wasGrounded = isGrounded;
        CheckGrounded();
        
        if (!wasGrounded && isGrounded && velocity.y < 0)
        {
            PlaySound(landSound);
        }

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        float targetSpeed = GetTargetSpeed();

        if (isGrounded)
        {
            HandleGroundMovement(moveDirection, targetSpeed);
        }
        else
        {
            HandleAirMovement(moveDirection, targetSpeed);
        }

        controller.Move(velocity * Time.deltaTime);

        HandleFootsteps();
    }
    
    float GetTargetSpeed()
    {
        if (isAiming) return aimSpeed;
        if (isCrouching) return crouchSpeed;
        if (isRunning && moveInput.y > 0) return runSpeed;
        return walkSpeed;
    }
    
    void HandleGroundMovement(Vector3 moveDirection, float targetSpeed)
    {
        if (moveDirection.magnitude > 0.1f)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            Vector3 targetVelocity = moveDirection * currentSpeed;
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, deceleration * Time.deltaTime);
            velocity.x = Mathf.Lerp(velocity.x, 0f, deceleration * Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, 0f, deceleration * Time.deltaTime);
        }
        
        if (velocity.y <= 0f)
        {
            velocity.y = -0.5f;
        }
    }
    
    void HandleAirMovement(Vector3 moveDirection, float targetSpeed)
    {
        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 targetVelocity = moveDirection * (targetSpeed * airControl);
            velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, airControl * Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, airControl * Time.deltaTime);
        }
        
        velocity.y -= gravity * Time.deltaTime;
    }
    
    void CheckGrounded()
    {
        isGrounded = controller.isGrounded;
        
        if (!isGrounded)
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            float rayDistance = 0.2f;
            
            isGrounded = Physics.Raycast(rayStart, Vector3.down, rayDistance);
            Debug.DrawRay(rayStart, Vector3.down * rayDistance, isGrounded ? Color.green : Color.red);
        }
    }
    
    void HandleFootsteps()
    {
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
            transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);
            
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
        
        float targetCrouchOffset = isCrouching ? crouchCameraOffset : 0f;
        currentCrouchOffset = Mathf.Lerp(currentCrouchOffset, targetCrouchOffset, crouchTransitionSpeed * Time.deltaTime);
        
        if (playerCamera != null)
        {
            Vector3 cameraPos = originalCameraPosition;
            cameraPos.y += currentCrouchOffset;
            playerCamera.transform.localPosition = cameraPos;
        }
    }
    
    void HandleLean()
    {
        if (playerCamera == null) return;
        
        float targetLean = 0f;
        Vector3 targetOffset = Vector3.zero;
        
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
        
        Vector3 currentCameraPos = playerCamera.transform.localPosition;
        Vector3 desiredPos = originalCameraPosition + 
                           new Vector3(0, currentCrouchOffset, 0) + 
                           targetOffset;
        playerCamera.transform.localPosition = Vector3.Lerp(currentCameraPos, desiredPos, leanSpeed * Time.deltaTime);
    }
    
    void HandleAiming()
    {
        if (playerCamera == null) return;
        
        isAiming = Input.GetMouseButton(1);
        
        float targetFOV = isAiming ? aimFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, aimTransitionSpeed * Time.deltaTime);
        
        if (weaponManager != null)
        {
            weaponManager.SetAiming(isAiming);
        }
    }
    
    void HandleWeaponControls()
    {
        if (weaponManager == null) return;
        
        // 武器切换
        if (Input.GetKeyDown(KeyCode.Alpha1)) weaponManager.SwitchToWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) weaponManager.SwitchToWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) weaponManager.SwitchToWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha0)) weaponManager.SetEmptyHands();
        
        // 滚轮切换
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            weaponManager.CycleWeapon(scroll > 0 ? 1 : -1);
        }
        
        // 换弹
        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponManager.Reload();
        }
    }
    
    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            PerformInteraction();
        }
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            PerformPickup();
        }
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }
    
    void UpdateCameraEffects()
    {
        if (playerCamera == null) return;
        
        if (isGrounded && currentSpeed > 0.1f)
        {
            UpdateHeadBob();
        }
    }
    
    void UpdateHeadBob()
    {
        float bobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
        float bobAmount = isRunning ? runBobAmount : walkBobAmount;
        
        if (isCrouching)
        {
            bobAmount *= 0.5f;
        }
        
        bobTimer += Time.deltaTime * bobSpeed;
        
        float bobX = Mathf.Sin(bobTimer) * bobAmount;
        float bobY = Mathf.Abs(Mathf.Cos(bobTimer)) * bobAmount;
        
        Vector3 bobOffset = new Vector3(bobX * 0.5f, bobY, 0);
        Vector3 baseCameraPos = originalCameraPosition;
        baseCameraPos.y += currentCrouchOffset;
        
        playerCamera.transform.localPosition = baseCameraPos + bobOffset;
    }
    
    void Jump()
    {
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
    
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
    
        RaycastHit[] hits = Physics.RaycastAll(ray, 3f);
    
        // 按距离排序
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
    
        foreach (var hit in hits)
        {
            // 优先检查武器拾取
            WeaponPickup weaponPickup = hit.collider.GetComponent<WeaponPickup>();
            if (weaponPickup != null)
            {
                Debug.Log($"[PlayerController] Trying to pickup weapon: {weaponPickup.gameObject.name}");
                weaponPickup.OnInteract();
                return;
            }
        
            // 然后检查普通物品拾取
            PickupItem pickup = hit.collider.GetComponent<PickupItem>();
            if (pickup != null)
            {
                Debug.Log($"[PlayerController] Trying to pickup item: {pickup.gameObject.name}");
                pickup.OnInteract();
                return;
            }
        
            // 最后检查其他交互物品
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                Debug.Log($"[PlayerController] Trying to interact with: {hit.collider.name}");
                interactable.Interact();
                return;
            }
        }
    
        Debug.Log("[PlayerController] No interactable objects found");
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
                Debug.Log($"Trying to pickup: {pickup.itemData.itemName}");
                // 关键修复：实际调用拾取逻辑
                pickup.OnInteract(); // 这行原来缺失了！
            }
        }
    }
    
    void ToggleInventory()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleInventory();
        }
    }
    
    // 伤害震动效果
    public void TriggerDamageShake(float intensity)
    {
        if (damageShakeCoroutine != null)
            StopCoroutine(damageShakeCoroutine);
        damageShakeCoroutine = StartCoroutine(DamageShakeCoroutine(intensity));
    }
    
    IEnumerator DamageShakeCoroutine(float intensity)
    {
        Vector3 originalPos = playerCamera.transform.localPosition;
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            float shakeAmount = Mathf.Lerp(intensity * damageShakeIntensity, 0f, elapsed / duration);
            Vector3 shakeOffset = Random.insideUnitSphere * shakeAmount * 0.1f;
            shakeOffset.z = 0f; // 避免前后震动
            
            playerCamera.transform.localPosition = originalPos + shakeOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerCamera.transform.localPosition = originalPos;
    }
    
    // 公共方法
    public float GetCurrentSpeed() => currentSpeed;
    public bool IsGrounded() => isGrounded;
    public bool IsRunning() => isRunning;
    public bool IsCrouching() => isCrouching;
    public bool IsAiming() => isAiming;
    public Vector3 GetVelocity() => velocity;
    public Camera GetPlayerCamera() => playerCamera;
    public bool IsInitialized() => isInitialized;
}
// 交互接口
public interface IInteractable
{
    void Interact();
    string GetInteractionText();
}