using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WorldItemDisplay : MonoBehaviour
{
    [Header("显示设置")]
    public bool billboardToCamera = true;  // 始终面向相机
    public bool animateFloat = true;       // 上下浮动动画
    public bool animateRotate = false;     // 旋转动画
    
    [Header("动画参数")]
    public float floatAmplitude = 0.1f;    // 浮动幅度
    public float floatSpeed = 2f;          // 浮动速度
    public float rotateSpeed = 45f;        // 旋转速度（度/秒）
    
    [Header("Pickup效果")]
    public float pickupScaleMultiplier = 1.2f;
    public float pickupAnimDuration = 0.3f;
    
    private SpriteRenderer spriteRenderer;
    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private float animTimer = 0f;
    private bool isPickedUp = false;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cameraTransform = Camera.main?.transform;
        originalPosition = transform.position;
        originalScale = transform.localScale;
        
        // 随机开始时间，避免所有物品同步动画
        animTimer = Random.Range(0f, Mathf.PI * 2f);
        
        SetupSpriteRenderer();
    }
    
    void SetupSpriteRenderer()
    {
        if (spriteRenderer != null)
        {
            // 确保正确的渲染设置
            spriteRenderer.sortingOrder = 10; // 确保在场景前方
            
            // 如果没有设置材质，使用默认的Sprite材质
            if (spriteRenderer.material.name.Contains("Default"))
            {
                spriteRenderer.material = Resources.Load<Material>("Sprites-Default") ?? spriteRenderer.material;
            }
        }
    }
    
    void Update()
    {
        if (isPickedUp) return;
        
        animTimer += Time.deltaTime;
        
        // Billboard效果 - 始终面向相机
        if (billboardToCamera && cameraTransform != null)
        {
            Vector3 directionToCamera = cameraTransform.position - transform.position;
            directionToCamera.y = 0; // 只在水平面旋转
            
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
        
        // 浮动动画
        if (animateFloat)
        {
            Vector3 floatOffset = Vector3.up * Mathf.Sin(animTimer * floatSpeed) * floatAmplitude;
            transform.position = originalPosition + floatOffset;
        }
        
        // 旋转动画（绕Y轴）
        if (animateRotate)
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }
    }
    
    public void SetItemSprite(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
    
    public void SetItemData(ItemData itemData)
    {
        if (itemData != null && itemData.icon != null)
        {
            SetItemSprite(itemData.icon);
            gameObject.name = $"WorldItem_{itemData.itemName}";
        }
    }
    
    public void SetWeaponData(WeaponData weaponData)
    {
        if (weaponData != null && weaponData.weaponIcon != null)
        {
            SetItemSprite(weaponData.weaponIcon);
            gameObject.name = $"WorldWeapon_{weaponData.weaponName}";
        }
    }
    
    public void OnPickedUp()
    {
        if (isPickedUp) return;
        
        isPickedUp = true;
        StartCoroutine(PickupAnimation());
    }
    
    System.Collections.IEnumerator PickupAnimation()
    {
        Vector3 targetScale = originalScale * pickupScaleMultiplier;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * 0.5f;
        
        float elapsed = 0f;
        
        // 放大并上升
        while (elapsed < pickupAnimDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (pickupAnimDuration * 0.5f);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            // 淡出
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = color;
            }
            
            yield return null;
        }
        
        // 销毁物体
        Destroy(gameObject);
    }
    
    // 设置颜色（用于不同稀有度等）
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
    
    // 设置排序层
    public void SetSortingOrder(int order)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 显示拾取范围（如果父对象有BaseInteractable组件）
        BaseInteractable interactable = GetComponentInParent<BaseInteractable>();
        if (interactable != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactable.interactionRange);
        }
    }
}