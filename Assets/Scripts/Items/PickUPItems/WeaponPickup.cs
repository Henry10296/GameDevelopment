using UnityEngine;

public class WeaponPickup : BaseInteractable
{
    public WeaponData weaponData;
    public int currentAmmo;
    
    [Header("显示")]
    public SpriteRenderer weaponSprite;
    
    public void SetupWeapon(WeaponData data, int ammo)
    {
        weaponData = data;
        currentAmmo = ammo;
        
        // 设置碰撞器
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.radius = 1.5f;
        col.isTrigger = true;
        
        // 创建武器显示
        CreateWeaponDisplay();
        
        gameObject.name = $"Pickup_{data.weaponName}";
    }
    
    void CreateWeaponDisplay()
    {
        // 创建简单的2D显示
        GameObject spriteObj = new GameObject("WeaponSprite");
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = Vector3.zero;
        
        weaponSprite = spriteObj.AddComponent<SpriteRenderer>();
        weaponSprite.sprite = weaponData.weaponIcon;
        weaponSprite.color = Color.cyan; // 武器用青色标识
        weaponSprite.transform.localScale = Vector3.one * 1.5f;
        
        // 面向相机
        StartCoroutine(FaceCamera());
        
        // 添加浮动效果
        StartCoroutine(FloatAnimation());
    }
    
    System.Collections.IEnumerator FaceCamera()
    {
        while (this != null)
        {
            if (Camera.main != null && weaponSprite != null)
            {
                weaponSprite.transform.LookAt(Camera.main.transform);
                weaponSprite.transform.Rotate(0, 180, 0);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    System.Collections.IEnumerator FloatAnimation()
    {
        Vector3 startPos = transform.position;
        float time = 0f;
        
        while (this != null)
        {
            time += Time.deltaTime;
            transform.position = startPos + Vector3.up * Mathf.Sin(time * 2f) * 0.1f;
            transform.Rotate(0, 45f * Time.deltaTime, 0);
            yield return null;
        }
    }
    
    public override void OnInteract()
    {
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null) return;
        
        // 检查是否已有此武器
        if (weaponManager.HasWeaponType(GetWeaponType()))
        {
            // 已有武器，补充弹药
            if (InventoryManager.Instance && currentAmmo > 0)
            {
                // 查找弹药ItemData
                ItemData ammoItem = FindAmmoItem(weaponData.ammoType);
                if (ammoItem != null)
                {
                    InventoryManager.Instance.AddItem(ammoItem, currentAmmo);
                    ShowMessage($"获得了 {currentAmmo} 发弹药");
                    Destroy(gameObject);
                    return;
                }
            }
            ShowMessage("已拥有该武器");
            return;
        }
        
        // 添加新武器
        if (AddWeaponToManager(weaponManager))
        {
            ShowMessage($"获得了 {weaponData.weaponName}!");
            Destroy(gameObject);
        }
    }
    
    bool AddWeaponToManager(WeaponManager weaponManager)
    {
        // 在weaponHolder下创建武器GameObject
        GameObject weaponObj = new GameObject(weaponData.weaponName);
        weaponObj.transform.SetParent(weaponManager.weaponHolder);
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;
        
        // 根据武器类型添加对应的Controller
        WeaponController controller = null;
        WeaponType weaponType = GetWeaponType();
        
        switch (weaponType)
        {
            case WeaponType.Pistol:
                controller = weaponObj.AddComponent<PistolController>();
                break;
            case WeaponType.Rifle:
                controller = weaponObj.AddComponent<AutoRifleController>();
                break;
            default:
                controller = weaponObj.AddComponent<WeaponController>();
                break;
        }
        
        if (controller != null)
        {
            controller.weaponData = weaponData;
            controller.currentAmmo = currentAmmo;
            weaponManager.AddWeapon(controller);
            return true;
        }
        
        Destroy(weaponObj);
        return false;
    }
    
    WeaponType GetWeaponType()
    {
        string name = weaponData.weaponName.ToLower();
        if (name.Contains("pistol")) return WeaponType.Pistol;
        if (name.Contains("rifle")) return WeaponType.Rifle;
        return WeaponType.Pistol;
    }
    
    ItemData FindAmmoItem(string ammoType)
    {
        // 从ItemDatabase查找弹药
        ItemDatabase itemDB = FindObjectOfType<ItemDatabase>();
        if (itemDB != null)
        {
            foreach (var item in itemDB.ammoItems)
            {
                if (item.ammoType == ammoType)
                    return item;
            }
        }
        return null;
    }
    
    void ShowMessage(string message)
    {
        if (UIManager.Instance)
            UIManager.Instance.ShowMessage(message, 2f);
    }
}