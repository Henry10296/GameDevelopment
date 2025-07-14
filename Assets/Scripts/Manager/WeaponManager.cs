using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("武器系统")]
    public Transform weaponHolder;
    public List<WeaponController> weapons = new List<WeaponController>();
    public int maxWeapons = 3;
    
    [Header("武器丢弃设置")]
    public GameObject weaponPickupPrefab;
    public float dropDistance = 2f;
    
    [Header("切换设置")]
    public float switchTime = 0.5f;
    public AudioClip switchSound;
    
    // 私有变量
    private int currentWeaponIndex = -1;
    private WeaponController currentWeapon;
    private bool isSwitching = false;
    private bool isEmptyHands = true;
    private AudioSource audioSource;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // 确保有武器容器
        if (weaponHolder == null)
        {
            GameObject holder = new GameObject("WeaponHolder");
            holder.transform.SetParent(transform);
            holder.transform.localPosition = Vector3.zero;
            weaponHolder = holder.transform;
        }
        
        // 获取音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 初始化为空手状态
        SetEmptyHands();
        
        Debug.Log("[WeaponManager] Initialized successfully");
    }
    
    void Update()
    {
        if (!isSwitching)
        {
            HandleShootingInput();
            CheckDropWeaponInput();
        }
    }
    
    void HandleShootingInput()
    {
        if (currentWeapon != null && !isEmptyHands)
        {
            currentWeapon.TryShoot();
            
            if (Input.GetMouseButtonUp(0))
            {
                currentWeapon.StopShooting();
            }
        }
    }
    
    void CheckDropWeaponInput()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropCurrentWeapon();
        }
    }
    
    // ==== 武器管理 ====
    public bool AddWeapon(WeaponController weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("[WeaponManager] Cannot add null weapon");
            return false;
        }
        
        // 检查是否已达到最大武器数量
        if (weapons.Count >= maxWeapons)
        {
            Debug.LogWarning("[WeaponManager] Maximum weapons reached. Cannot add more weapons.");
            return false;
        }
        
        // 检查是否已有相同类型的武器
        foreach (var existingWeapon in weapons)
        {
            if (existingWeapon.weaponType == weapon.weaponType)
            {
                Debug.LogWarning($"[WeaponManager] Already have weapon of type: {weapon.weaponType}");
                return false;
            }
        }
        
        // 添加武器
        weapons.Add(weapon);
        weapon.Initialize(this);
        weapon.gameObject.SetActive(false); // 初始时隐藏
        
        // 如果当前是空手，切换到新武器
        if (isEmptyHands)
        {
            SwitchToWeapon(weapons.Count - 1);
        }
        
        Debug.Log($"[WeaponManager] Added weapon: {weapon.weaponName} (Total: {weapons.Count})");
        return true;
    }
    
    public void RemoveWeapon(WeaponController weapon)
    {
        if (weapon == null) return;

        Debug.Log($"[WeaponManager] Removing weapon: {weapon.weaponName}");

        // 从武器列表中移除
        int weaponIndex = weapons.IndexOf(weapon);
        if (weaponIndex >= 0)
        {
            weapons.RemoveAt(weaponIndex);
        }

        // 如果是当前武器，切换到其他武器
        if (currentWeapon == weapon)
        {
            currentWeapon = null;
            currentWeaponIndex = -1;

            // 尝试切换到下一个武器
            if (weapons.Count > 0)
            {
                int newIndex = Mathf.Min(weaponIndex, weapons.Count - 1);
                SwitchToWeapon(newIndex);
            }
            else
            {
                SetEmptyHands();
            }
        }
        else
        {
            // 重新计算当前武器索引
            if (currentWeapon != null)
            {
                currentWeaponIndex = weapons.IndexOf(currentWeapon);
            }
        }

        // 销毁武器对象
        if (weapon.gameObject != null)
        {
            Destroy(weapon.gameObject);
        }

        Debug.Log($"[WeaponManager] Weapon removed. Remaining: {weapons.Count}");
    }
    
    // ==== 武器切换 ====
    public void SwitchToWeapon(int index)
    {
        if (isSwitching || index < 0 || index >= weapons.Count)
        {
            Debug.LogWarning($"[WeaponManager] Cannot switch to weapon index: {index}");
            return;
        }
        
        if (index == currentWeaponIndex)
        {
            Debug.Log("[WeaponManager] Already using this weapon");
            return;
        }
        
        StartCoroutine(SwitchWeaponCoroutine(index));
    }
    
    IEnumerator SwitchWeaponCoroutine(int newIndex)
    {
        isSwitching = true;
        
        // 隐藏当前武器
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
            Debug.Log($"[WeaponManager] Hiding current weapon: {currentWeapon.weaponName}");
        }
        
        // 播放切换音效
        if (switchSound && audioSource)
        {
            audioSource.PlayOneShot(switchSound);
        }
        
        // 等待切换时间
        yield return new WaitForSeconds(switchTime * 0.5f);
        
        // 切换到新武器
        currentWeaponIndex = newIndex;
        currentWeapon = weapons[newIndex];
        isEmptyHands = false;
        
        // 显示新武器
        currentWeapon.gameObject.SetActive(true);
        
        // 通知武器显示系统
        NotifyWeaponDisplay();
        
        yield return new WaitForSeconds(switchTime * 0.5f);
        
        isSwitching = false;
        
        Debug.Log($"[WeaponManager] Switched to weapon: {currentWeapon.weaponName}");
    }
    
    public void SetEmptyHands()
    {
        if (isSwitching) return;
        
        StartCoroutine(SetEmptyHandsCoroutine());
    }
    
    IEnumerator SetEmptyHandsCoroutine()
    {
        isSwitching = true;
        
        // 隐藏当前武器
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
        }
        
        yield return new WaitForSeconds(switchTime * 0.5f);
        
        // 设置空手状态
        currentWeapon = null;
        currentWeaponIndex = -1;
        isEmptyHands = true;
        
        // 通知武器显示系统
        NotifyWeaponDisplay();
        
        yield return new WaitForSeconds(switchTime * 0.5f);
        
        isSwitching = false;
        
        Debug.Log("[WeaponManager] Set to empty hands");
    }
    
    public void CycleWeapon(int direction)
    {
        if (weapons.Count == 0)
        {
            SetEmptyHands();
            return;
        }
        
        int newIndex;
        
        if (isEmptyHands)
        {
            newIndex = direction > 0 ? 0 : weapons.Count - 1;
        }
        else
        {
            newIndex = currentWeaponIndex + direction;
            
            if (newIndex >= weapons.Count)
            {
                newIndex = 0;
            }
            else if (newIndex < 0)
            {
                newIndex = weapons.Count - 1;
            }
        }
        
        SwitchToWeapon(newIndex);
    }
    
    // ==== 武器丢弃 ====
    public void DropCurrentWeapon()
    {
        if (currentWeapon == null || isEmptyHands)
        {
            Debug.Log("[WeaponManager] No weapon to drop");
            return;
        }

        WeaponController weaponToDrop = currentWeapon;
        
        Debug.Log($"[WeaponManager] Dropping weapon: {weaponToDrop.weaponName} with {weaponToDrop.CurrentAmmo} ammo");

        // 创建拾取物品
        CreateDroppedWeapon(weaponToDrop);
        
        // 移除武器
        RemoveWeapon(weaponToDrop);
    }
    
    void CreateDroppedWeapon(WeaponController droppedWeapon)
    {
        if (droppedWeapon == null || droppedWeapon.weaponData == null)
        {
            Debug.LogError("[WeaponManager] Cannot create dropped weapon - invalid data");
            return;
        }

        Vector3 dropPosition = CalculateDropPosition();

        GameObject droppedObj;
        
        if (weaponPickupPrefab != null)
        {
            droppedObj = Instantiate(weaponPickupPrefab, dropPosition, Quaternion.identity);
        }
        else
        {
            droppedObj = new GameObject($"Dropped_{droppedWeapon.weaponName}");
            droppedObj.transform.position = dropPosition;
            droppedObj.AddComponent<WeaponPickup>();
            droppedObj.AddComponent<SphereCollider>().isTrigger = true;
        }

        WeaponPickup pickup = droppedObj.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.SetupFromDroppedWeapon(droppedWeapon);
            Debug.Log($"[WeaponManager] Created dropped weapon pickup with {droppedWeapon.CurrentAmmo} ammo");
        }
        else
        {
            Debug.LogError("[WeaponManager] Failed to get WeaponPickup component from dropped weapon");
        }
    }
    
    Vector3 CalculateDropPosition()
    {
        Vector3 playerPos = transform.position;
        Vector3 forward = transform.forward;
        
        RaycastHit hit;
        if (Physics.Raycast(playerPos, forward, out hit, dropDistance))
        {
            return hit.point + hit.normal * 0.5f;
        }
        
        return playerPos + forward * dropDistance + Vector3.up * 0.5f;
    }
    
    // ==== 武器操作 ====
    public void Reload()
    {
        if (currentWeapon != null && !isEmptyHands)
        {
            currentWeapon.Reload();
        }
    }
    
    public void SetAiming(bool aiming)
    {
        if (currentWeapon != null && !isEmptyHands)
        {
            currentWeapon.SetAiming(aiming);
        }
        
        // 通知武器显示系统
        WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
        if (weaponDisplay != null)
        {
            weaponDisplay.SetAiming(aiming);
        }
    }
    
    // ==== 查询方法 ====
    public WeaponController GetCurrentWeapon()
    {
        return currentWeapon;
    }
    
    public bool IsEmptyHands()
    {
        return isEmptyHands;
    }
    
    public bool HasWeaponType(WeaponType weaponType)
    {
        foreach (var weapon in weapons)
        {
            if (weapon.weaponType == weaponType)
                return true;
        }
        return false;
    }
    
    public WeaponController GetWeaponByType(WeaponType weaponType)
    {
        foreach (var weapon in weapons)
        {
            if (weapon.weaponType == weaponType)
                return weapon;
        }
        return null;
    }
    
    public int GetWeaponCount()
    {
        return weapons.Count;
    }
    
    public bool IsSwitching()
    {
        return isSwitching;
    }
    
    // ==== 通知系统 ====
    void NotifyWeaponDisplay()
    {
        WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
        if (weaponDisplay != null)
        {
            if (isEmptyHands)
            {
                weaponDisplay.OnGoEmptyHands();
            }
            else if (currentWeapon != null)
            {
                weaponDisplay.OnWeaponSwitch(currentWeapon.weaponType);
            }
        }
    }
    
    // ==== 调试信息 ====
    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
        GUILayout.Label("=== WeaponManager Debug ===");
        GUILayout.Label($"Weapons: {weapons.Count}/{maxWeapons}");
        GUILayout.Label($"Current: {(currentWeapon ? currentWeapon.weaponName : "Empty Hands")}");
        GUILayout.Label($"Index: {currentWeaponIndex}");
        GUILayout.Label($"Switching: {isSwitching}");
        
        if (currentWeapon != null)
        {
            GUILayout.Label($"Ammo: {currentWeapon.CurrentAmmo}/{currentWeapon.MaxAmmo}");
        }
        
        GUILayout.Label("Controls:");
        GUILayout.Label("1-3: Switch weapon");
        GUILayout.Label("0: Empty hands");
        GUILayout.Label("G: Drop weapon");
        GUILayout.Label("R: Reload");
        
        GUILayout.EndArea();
    }
}