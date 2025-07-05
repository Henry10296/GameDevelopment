using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
public class WeaponManager : MonoBehaviour
{
    [Header("武器配置")]
    [SerializeField] private List<WeaponController> weapons = new List<WeaponController>();
    [SerializeField] private int currentWeaponIndex = 0;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform weaponCamera; // 用于武器渲染的独立相机
    
    [Header("空手状态")]
    public bool allowEmptyHands = true;
    private bool isEmptyHands = false;
    [Header("切换设置")]
    [SerializeField] private float switchSpeed = 5f;
    [SerializeField] private float dropDistance = 0.5f;
    [SerializeField] private AnimationCurve switchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("手部设置")]
    [SerializeField] private GameObject handModel;
    [SerializeField] private Sprite handSprite; // 2D手部贴图选项
    
    [Header("UI引用")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image crosshair;
    
    private WeaponController currentWeapon;
    private bool isSwitching = false;
    private bool isAiming = false;
    private Coroutine switchCoroutine;
    
    void Start()
    {
        InitializeWeapons();
        if (weapons.Count > 0)
        {
            EquipWeapon(currentWeaponIndex);
        }
    }
    
    void InitializeWeapons()
    {
        // 查找所有子武器
        if (weaponHolder && weapons.Count == 0)
        {
            weapons.AddRange(weaponHolder.GetComponentsInChildren<WeaponController>(true));
        }
        
        // 初始化所有武器
        foreach (var weapon in weapons)
        {
            weapon.Initialize(this);
            weapon.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (!isSwitching && currentWeapon != null)
        {
            HandleShooting();
            UpdateUI();
        }
    }
    
    void HandleShooting()
    {
        if (Input.GetMouseButton(0))
        {
            currentWeapon.TryShoot();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            currentWeapon.StopShooting();
        }
    }
    
    
    public void CycleWeapon(int direction)
    {
        int newIndex = currentWeaponIndex + direction;
        
        if (newIndex >= weapons.Count)
            newIndex = 0;
        else if (newIndex < 0)
            newIndex = weapons.Count - 1;
        
        SwitchWeapon(newIndex);
    }
    
    IEnumerator SwitchWeaponCoroutine(int newIndex)
    {
        isSwitching = true;
        
        // 降下当前武器
        if (currentWeapon != null)
        {
            yield return StartCoroutine(LowerWeapon());
            currentWeapon.gameObject.SetActive(false);
        }
        
        // 切换到新武器
        currentWeaponIndex = newIndex;
        currentWeapon = weapons[currentWeaponIndex];
        currentWeapon.gameObject.SetActive(true);
        
        // 抬起新武器
        yield return StartCoroutine(RaiseWeapon());
        
        isSwitching = false;
    }
    public void SetEmptyHands()
    {
        if (!allowEmptyHands) return;
        
        if (currentWeapon)
        {
            currentWeapon.gameObject.SetActive(false);
        }
        
        currentWeapon = null;
        currentWeaponIndex = -1;
        isEmptyHands = true;
        
        Debug.Log("切换到空手状态");
    }
    
    // 修改现有的SwitchWeapon方法
    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count || isSwitching)
            return;
            
        isEmptyHands = false; // 退出空手状态
        
        // 你现有的武器切换逻辑...
        StartCoroutine(SwitchWeaponCoroutine(index));
    }
    IEnumerator LowerWeapon()
    {
        if (currentWeapon == null) yield break;
        
        Vector3 startPos = currentWeapon.transform.localPosition;
        Vector3 endPos = startPos - Vector3.up * dropDistance;
        
        float timer = 0f;
        float duration = 0.3f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            currentWeapon.transform.localPosition = Vector3.Lerp(startPos, endPos, switchCurve.Evaluate(t));
            yield return null;
        }
    }
    
    IEnumerator RaiseWeapon()
    {
        if (currentWeapon == null) yield break;
        
        Vector3 startPos = currentWeapon.transform.localPosition - Vector3.up * dropDistance;
        Vector3 endPos = currentWeapon.originalPosition;
        
        float timer = 0f;
        float duration = 0.3f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            currentWeapon.transform.localPosition = Vector3.Lerp(startPos, endPos, switchCurve.Evaluate(t));
            yield return null;
        }
        
        currentWeapon.transform.localPosition = endPos;
    }
    
    void EquipWeapon(int index)
    {
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
        }
        
        currentWeaponIndex = index;
        currentWeapon = weapons[index];
        currentWeapon.gameObject.SetActive(true);
        currentWeapon.transform.localPosition = currentWeapon.originalPosition;
    }
    
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        if (currentWeapon != null)
        {
            currentWeapon.SetAiming(aiming);
        }
    }
    
    public void Reload()
    {
        if (currentWeapon != null && !isSwitching)
        {
            currentWeapon.Reload();
        }
    }
    
    void UpdateUI()
    {
        if (currentWeapon == null) return;
        
        // 更新弹药显示
        if (ammoText)
        {
            ammoText.text = $"{currentWeapon.CurrentAmmo}/{currentWeapon.MaxAmmo}";
        }
        
        // 更新武器名称
        if (weaponNameText)
        {
            weaponNameText.text = currentWeapon.weaponName;
        }
        
        // 更新准星
        UpdateCrosshair();
    }
    
    void UpdateCrosshair()
    {
        if (crosshair == null) return;
        
        // 根据状态调整准星
        if (isAiming)
        {
            crosshair.transform.localScale = Vector3.one * 0.5f;
            crosshair.color = Color.red;
        }
        else
        {
            float spread = currentWeapon.GetCurrentSpread();
            crosshair.transform.localScale = Vector3.one * (1f + spread);
            crosshair.color = Color.white;
        }
    }
    
    // 公共方法
    public WeaponController GetCurrentWeapon()
    {
        if (isEmptyHands) return null;
        return currentWeapon;
    }
    
    public bool IsEmptyHands() => isEmptyHands;

    public bool IsSwitching() => isSwitching;
    public bool HasAmmo() => currentWeapon != null && currentWeapon.HasAmmo();
}
