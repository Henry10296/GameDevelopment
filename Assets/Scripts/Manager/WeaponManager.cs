using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("武器配置")]
    [SerializeField] private List<WeaponController> availableWeapons = new List<WeaponController>();
    public Transform weaponHolder;
    
    [Header("武器切换设置")]
    public float switchSpeed = 5f;
    public float dropDistance = 0.5f;
    public AnimationCurve switchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("空手设置")]
    public bool allowEmptyHands = true;
    public GameObject handModel;
    
    [Header("武器拾取丢弃")]
    public GameObject weaponPickupPrefab;
    public KeyCode dropWeaponKey = KeyCode.G;
    
    private int currentWeaponIndex = -1;
    private WeaponController currentWeapon;
    private bool isSwitching = false;
    private bool isEmptyHands = true;
    private bool isAiming = false;
    private Coroutine switchCoroutine;
    
    // 修复：使用属性而不是直接访问字段
    public List<WeaponController> AvailableWeapons => availableWeapons;
    public WeaponController GetCurrentWeapon() => isEmptyHands ? null : currentWeapon;
    public bool IsEmptyHands() => isEmptyHands;
    public bool IsSwitching() => isSwitching;
    public bool IsAiming() => isAiming;
    public bool HasAmmo() => currentWeapon != null && currentWeapon.HasAmmo();
    
    void Start()
    {
        InitializeWeapons();
        SetEmptyHands();
    }
    
    void InitializeWeapons()
    {
        // 修复：安全初始化武器列表
        if (availableWeapons == null)
        {
            availableWeapons = new List<WeaponController>();
        }
        
        // 清理空引用
        for (int i = availableWeapons.Count - 1; i >= 0; i--)
        {
            if (availableWeapons[i] == null)
            {
                availableWeapons.RemoveAt(i);
                Debug.LogWarning($"[WeaponManager] Removed null weapon at index {i}");
            }
        }
        
        // 查找所有子武器
        if (weaponHolder != null && availableWeapons.Count == 0)
        {
            var childWeapons = weaponHolder.GetComponentsInChildren<WeaponController>(true);
            availableWeapons.AddRange(childWeapons);
        }
        
        // 初始化所有武器
        foreach (var weapon in availableWeapons)
        {
            if (weapon != null)
            {
                weapon.Initialize(this);
                weapon.gameObject.SetActive(false);
            }
        }
        
        Debug.Log($"[WeaponManager] Initialized {availableWeapons.Count} weapons");
    }
    
    // 修复：安全添加武器方法
    public void AddWeapon(WeaponController weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("[WeaponManager] Cannot add null weapon!");
            return;
        }
        
        if (availableWeapons == null)
        {
            availableWeapons = new List<WeaponController>();
        }
        
        if (!availableWeapons.Contains(weapon))
        {
            availableWeapons.Add(weapon);
            weapon.Initialize(this);
            weapon.gameObject.SetActive(false);
            
            Debug.Log($"[WeaponManager] Added weapon: {weapon.weaponName} (Total: {availableWeapons.Count})");
            
            // 如果是第一把武器，自动切换到它
            if (availableWeapons.Count == 1 && isEmptyHands)
            {
                SwitchToWeapon(0);
            }
        }
        else
        {
            Debug.LogWarning($"[WeaponManager] Weapon {weapon.weaponName} already exists!");
        }
    }
    
    // 修复：检查武器类型方法
    public bool HasWeaponType(WeaponType weaponType)
    {
        if (availableWeapons == null) return false;
        
        foreach (var weapon in availableWeapons)
        {
            if (weapon != null && weapon.weaponType == weaponType)
            {
                return true;
            }
        }
        return false;
    }
    
    public WeaponController GetWeaponByType(WeaponType weaponType)
    {
        if (availableWeapons == null) return null;
        
        foreach (var weapon in availableWeapons)
        {
            if (weapon != null && weapon.weaponType == weaponType)
            {
                return weapon;
            }
        }
        return null;
    }
    
    void Update()
    {
        HandleInput();
        HandleShooting();
        
        if (Input.GetKeyDown(dropWeaponKey))
        {
            DropCurrentWeapon();
        }
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && availableWeapons.Count > 0) SwitchToWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && availableWeapons.Count > 1) SwitchToWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && availableWeapons.Count > 2) SwitchToWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetEmptyHands();
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            CycleWeapon(scroll > 0 ? 1 : -1);
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
        
        SetAiming(Input.GetMouseButton(1));
    }
    
    void HandleShooting()
    {
        if (isSwitching || isEmptyHands || currentWeapon == null) return;
        
        if (Input.GetMouseButton(0))
        {
            currentWeapon.TryShoot();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            currentWeapon.StopShooting();
        }
    }
    
    public void SwitchToWeapon(int index)
    {
        if (availableWeapons == null || index < 0 || index >= availableWeapons.Count || isSwitching) 
        {
            Debug.LogWarning($"[WeaponManager] Cannot switch to weapon index {index}");
            return;
        }
        
        if (switchCoroutine != null)
            StopCoroutine(switchCoroutine);
        
        switchCoroutine = StartCoroutine(SwitchToWeaponCoroutine(index));
    }
    
    IEnumerator SwitchToWeaponCoroutine(int newIndex)
    {
        isSwitching = true;

        // 降下当前武器
        if (!isEmptyHands && currentWeapon != null)
        {
            yield return StartCoroutine(LowerWeapon(currentWeapon.transform));
            currentWeapon.gameObject.SetActive(false);
        }

        // 切换武器和处理逻辑放到 try-catch 中（不包含 yield）
        try
        {
            currentWeaponIndex = newIndex;
            currentWeapon = availableWeapons[currentWeaponIndex];
            currentWeapon.gameObject.SetActive(true);
            isEmptyHands = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WeaponManager] Error preparing new weapon: {e.Message}");
            isSwitching = false;
            yield break; // 提前退出协程
        }

        // 抬起新武器
        yield return StartCoroutine(RaiseWeapon(currentWeapon.transform));

        // 剩余逻辑
        NotifyWeaponSwitch();
        Debug.Log($"[WeaponManager] Switched to {currentWeapon.weaponName}");

        isSwitching = false;
    }
    
    public void SetEmptyHands()
    {
        if (!allowEmptyHands || isSwitching) return;
        
        if (switchCoroutine != null)
            StopCoroutine(switchCoroutine);
        
        switchCoroutine = StartCoroutine(SwitchToEmptyHandsCoroutine());
    }
    
    IEnumerator SwitchToEmptyHandsCoroutine()
    {
        isSwitching = true;
        
        if (!isEmptyHands && currentWeapon != null)
        {
            yield return StartCoroutine(LowerWeapon(currentWeapon.transform));
            currentWeapon.gameObject.SetActive(false);
        }
        
        currentWeapon = null;
        currentWeaponIndex = -1;
        isEmptyHands = true;
        
        if (handModel != null)
        {
            handModel.SetActive(true);
            yield return StartCoroutine(RaiseWeapon(handModel.transform));
        }
        
        isSwitching = false;
        NotifyWeaponSwitch();
        
        Debug.Log("[WeaponManager] Switched to empty hands");
    }
    
    IEnumerator LowerWeapon(Transform weaponTransform)
    {
        if (weaponTransform == null) yield break;
        
        Vector3 startPos = weaponTransform.localPosition;
        Vector3 endPos = startPos - Vector3.up * dropDistance;
        
        float timer = 0f;
        float duration = 0.3f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            weaponTransform.localPosition = Vector3.Lerp(startPos, endPos, switchCurve.Evaluate(t));
            yield return null;
        }
    }
    
    IEnumerator RaiseWeapon(Transform weaponTransform)
    {
        if (weaponTransform == null) yield break;
        
        Vector3 startPos = weaponTransform.localPosition - Vector3.up * dropDistance;
        Vector3 endPos = weaponTransform.localPosition + Vector3.up * dropDistance;
        
        weaponTransform.localPosition = startPos;
        
        float timer = 0f;
        float duration = 0.3f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            weaponTransform.localPosition = Vector3.Lerp(startPos, endPos, switchCurve.Evaluate(t));
            yield return null;
        }
        
        weaponTransform.localPosition = endPos;
    }
    
    public void SetAiming(bool aiming)
    {
        // 修复：避免重复设置相同状态
        if (isAiming == aiming) return;
        
        isAiming = aiming;
        
        if (currentWeapon != null)
        {
            currentWeapon.SetAiming(aiming);
        }
        
        WeaponDisplay weaponDisplay = FindObjectOfType<WeaponDisplay>();
        if (weaponDisplay != null)
        {
            weaponDisplay.SetAiming(aiming);
        }
    }
    
    public void Reload()
    {
        if (!isEmptyHands && currentWeapon != null && !isSwitching)
        {
            currentWeapon.Reload();
        }
    }
    
    public void CycleWeapon(int direction)
    {
        if (isSwitching || availableWeapons == null) return;
        
        int newIndex;
        
        if (isEmptyHands)
        {
            newIndex = direction > 0 ? 0 : availableWeapons.Count - 1;
        }
        else
        {
            newIndex = currentWeaponIndex + direction;
            
            if (newIndex >= availableWeapons.Count)
            {
                if (allowEmptyHands)
                {
                    SetEmptyHands();
                    return;
                }
                else
                {
                    newIndex = 0;
                }
            }
            else if (newIndex < 0)
            {
                if (allowEmptyHands)
                {
                    SetEmptyHands();
                    return;
                }
                else
                {
                    newIndex = availableWeapons.Count - 1;
                }
            }
        }
        
        SwitchToWeapon(newIndex);
    }
    
    public void DropCurrentWeapon()
    {
        if (isEmptyHands || currentWeapon == null) 
        {
            Debug.Log("没有武器可以丢弃");
            return;
        }

        Transform playerTransform = transform.root;
        Vector3 dropPosition = playerTransform.position + playerTransform.forward * 2f + Vector3.up * 0.5f;

        CreateDroppedWeapon(currentWeapon, dropPosition);

        if (UIManager.Instance)
        {
            UIManager.Instance.ShowMessage($"丢弃了 {currentWeapon.weaponName}", 2f);
        }

        RemoveWeapon(currentWeapon);
        SetEmptyHands();

        Debug.Log($"丢弃了武器: {currentWeapon.weaponName}");
    }
    
    void CreateDroppedWeapon(WeaponController weapon, Vector3 position)
    {
        GameObject droppedWeapon = new GameObject($"DroppedWeapon_{weapon.weaponName}");
        droppedWeapon.transform.position = position;

        WeaponPickup pickup = droppedWeapon.AddComponent<WeaponPickup>();
        pickup.SetupWeapon(weapon.weaponData, weapon.CurrentAmmo);

        SphereCollider col = droppedWeapon.AddComponent<SphereCollider>();
        col.radius = 1.5f;
        col.isTrigger = true;

        WorldItemDisplay display = droppedWeapon.AddComponent<WorldItemDisplay>();
        display.SetWeaponData(weapon.weaponData);

        Rigidbody rb = droppedWeapon.AddComponent<Rigidbody>();
        rb.AddForce(Random.insideUnitSphere * 3f + Vector3.up * 2f, ForceMode.Impulse);
        
        StartCoroutine(StopPhysicsAfterDelay(rb, 2f));
    }
    
    IEnumerator StopPhysicsAfterDelay(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    public void RemoveWeapon(WeaponController weapon)
    {
        if (weapon != null && availableWeapons != null && availableWeapons.Contains(weapon))
        {
            if (currentWeapon == weapon)
            {
                SetEmptyHands();
            }
            
            availableWeapons.Remove(weapon);
            Debug.Log($"[WeaponManager] Removed weapon: {weapon.weaponName}");
        }
    }
    
    void NotifyWeaponSwitch()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateAmmoDisplay(
                currentWeapon?.CurrentAmmo ?? 0, 
                currentWeapon?.MaxAmmo ?? 0
            );
        }
        
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
}