using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [Header("武器配置")]
    public List<WeaponController> availableWeapons = new List<WeaponController>();
    public Transform weaponHolder;
    
    [Header("武器切换设置")]
    public float switchSpeed = 5f;
    public float dropDistance = 0.5f;
    public AnimationCurve switchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("空手设置")]
    public bool allowEmptyHands = true;
    public GameObject handModel;
    
    private int currentWeaponIndex = -1;
    private WeaponController currentWeapon;
    private bool isSwitching = false;
    private bool isEmptyHands = true;
    private bool isAiming = false;
    private Coroutine switchCoroutine;
    
    // 属性访问器
    public WeaponController GetCurrentWeapon() => isEmptyHands ? null : currentWeapon;
    public bool IsEmptyHands() => isEmptyHands;
    public bool IsSwitching() => isSwitching;
    public bool IsAiming() => isAiming;
    public bool HasAmmo() => currentWeapon != null && currentWeapon.HasAmmo();
    
    void Start()
    {
        InitializeWeapons();
        
        // 默认空手状态
        SetEmptyHands();
    }
    
    void InitializeWeapons()
    {
        // 查找所有子武器
        if (weaponHolder != null && availableWeapons.Count == 0)
        {
            availableWeapons.AddRange(weaponHolder.GetComponentsInChildren<WeaponController>(true));
        }
        
        // 初始化所有武器
        foreach (var weapon in availableWeapons)
        {
            weapon.Initialize(this);
            weapon.gameObject.SetActive(false);
        }
        
        Debug.Log($"[WeaponManager] Initialized {availableWeapons.Count} weapons");
    }
    
    void Update()
    {
        HandleInput();
        HandleShooting();
    }
    
    void HandleInput()
    {
        // 武器切换
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetEmptyHands();
        
        // 滚轮切换
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            CycleWeapon(scroll > 0 ? 1 : -1);
        }
        
        // 换弹
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
        
        // 瞄准
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
        if (index < 0 || index >= availableWeapons.Count || isSwitching) return;
        
        if (switchCoroutine != null)
            StopCoroutine(switchCoroutine);
        
        switchCoroutine = StartCoroutine(SwitchToWeaponCoroutine(index));
    }
    
    public void SetEmptyHands()
    {
        if (!allowEmptyHands || isSwitching) return;
        
        if (switchCoroutine != null)
            StopCoroutine(switchCoroutine);
        
        switchCoroutine = StartCoroutine(SwitchToEmptyHandsCoroutine());
    }
    
    public void CycleWeapon(int direction)
    {
        if (isSwitching) return;
        
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
    
    IEnumerator SwitchToWeaponCoroutine(int newIndex)
    {
        isSwitching = true;
        
        // 降下当前武器或手
        if (!isEmptyHands && currentWeapon != null)
        {
            yield return StartCoroutine(LowerWeapon(currentWeapon.transform));
            currentWeapon.gameObject.SetActive(false);
        }
        else if (isEmptyHands && handModel != null)
        {
            yield return StartCoroutine(LowerWeapon(handModel.transform));
            handModel.SetActive(false);
        }
        
        // 切换到新武器
        currentWeaponIndex = newIndex;
        currentWeapon = availableWeapons[currentWeaponIndex];
        currentWeapon.gameObject.SetActive(true);
        isEmptyHands = false;
        
        // 抬起新武器
        yield return StartCoroutine(RaiseWeapon(currentWeapon.transform));
        
        isSwitching = false;
        
        // 通知UI和其他系统
        NotifyWeaponSwitch();
        
        Debug.Log($"[WeaponManager] Switched to {currentWeapon.weaponName}");
    }
    
    IEnumerator SwitchToEmptyHandsCoroutine()
    {
        isSwitching = true;
        
        // 降下当前武器
        if (!isEmptyHands && currentWeapon != null)
        {
            yield return StartCoroutine(LowerWeapon(currentWeapon.transform));
            currentWeapon.gameObject.SetActive(false);
        }
        
        // 切换到空手
        currentWeapon = null;
        currentWeaponIndex = -1;
        isEmptyHands = true;
        
        // 抬起手
        if (handModel != null)
        {
            handModel.SetActive(true);
            yield return StartCoroutine(RaiseWeapon(handModel.transform));
        }
        
        isSwitching = false;
        
        // 通知UI和其他系统
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
        
        // 设置起始位置
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
        isAiming = aiming;
        
        if (currentWeapon != null)
        {
            currentWeapon.SetAiming(aiming);
        }
        
        // 通知2D武器显示
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
    
    void NotifyWeaponSwitch()
    {
        // 通知UI管理器更新UI
        UIManager.Instance?.UpdateAmmoDisplay(
            currentWeapon?.CurrentAmmo ?? 0, 
            currentWeapon?.MaxAmmo ?? 0
        );
        
        // 通知2D武器显示系统
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
    
    // 添加武器到管理器
    public void AddWeapon(WeaponController weapon)
    {
        if (weapon != null && !availableWeapons.Contains(weapon))
        {
            availableWeapons.Add(weapon);
            weapon.Initialize(this);
            weapon.gameObject.SetActive(false);
            
            Debug.Log($"[WeaponManager] Added weapon: {weapon.weaponName}");
        }
    }
    
    // 移除武器
    public void RemoveWeapon(WeaponController weapon)
    {
        if (weapon != null && availableWeapons.Contains(weapon))
        {
            if (currentWeapon == weapon)
            {
                SetEmptyHands();
            }
            
            availableWeapons.Remove(weapon);
            Debug.Log($"[WeaponManager] Removed weapon: {weapon.weaponName}");
        }
    }
    
    // 根据类型获取武器
    public WeaponController GetWeaponByType(WeaponType weaponType)
    {
        foreach (var weapon in availableWeapons)
        {
            if (weapon.weaponType == weaponType)
            {
                return weapon;
            }
        }
        return null;
    }
    
    // 检查是否拥有某种武器
    public bool HasWeaponType(WeaponType weaponType)
    {
        return GetWeaponByType(weaponType) != null;
    }
    
    // 获取武器状态信息
    public WeaponStatus GetWeaponStatus()
    {
        return new WeaponStatus
        {
            isEmptyHands = this.isEmptyHands,
            currentWeaponName = currentWeapon?.weaponName ?? "Empty Hands",
            currentAmmo = currentWeapon?.CurrentAmmo ?? 0,
            maxAmmo = currentWeapon?.MaxAmmo ?? 0,
            isReloading = currentWeapon?.IsReloading ?? false,
            isAiming = this.isAiming,
            isSwitching = this.isSwitching
        };
    }
}

[System.Serializable]
public class WeaponStatus
{
    public bool isEmptyHands;
    public string currentWeaponName;
    public int currentAmmo;
    public int maxAmmo;
    public bool isReloading;
    public bool isAiming;
    public bool isSwitching;
}