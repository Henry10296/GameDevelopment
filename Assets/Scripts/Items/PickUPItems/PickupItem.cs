using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SphereCollider))]
public class PickupItem : BaseInteractable
{
    [Header("物品数据")]
    public ItemData itemData;
    public int quantity = 1;
    
    [Header("显示组件")]
    public SpriteRenderer spriteRenderer;
    public MeshRenderer meshRenderer;
    public Transform visualContainer;
    
    [Header("Doom风格动画")]
    public bool enableDoomAnimation = true;
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;
    public float rotationSpeed = 90f;
    
    [Header("发光效果")]
    public Light glowLight;
    public bool enableGlow = true;
    
    [Header("UI提示")]
    public Canvas uiCanvas;
    public TMPro.TextMeshPro itemNameText;
    public TMPro.TextMeshPro quantityText;
    
    private Vector3 originalPosition;
    private AudioSource audioSource;
    
    protected override void Start()
    {
        base.Start();
        
        // 设置碰撞器
        var collider = GetComponent<SphereCollider>();
        collider.radius = 1.5f;
        collider.isTrigger = true;
        
        // 设置音频
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }
        
        originalPosition = transform.position;
        
        // 根据ItemData设置显示
        if (itemData != null)
        {
            SetupDisplay();
        }
        
        // 开始动画
        if (enableDoomAnimation)
        {
            StartCoroutine(DoomBobAnimation());
        }
    }
    
    public void SetItemData(ItemData data, int qty = 1)
    {
        itemData = data;
        quantity = qty;
        
        if (data != null)
        {
            gameObject.name = $"Pickup_{data.itemName}";
            SetupDisplay();
        }
    }
    
    void SetupDisplay()
    {
        if (itemData == null) return;
        
        // 设置显示模式
        if (itemData.useSprite && itemData.worldSprite != null)
        {
            SetupSpriteDisplay();
        }
        else if (itemData.worldPrefab != null)
        {
            SetupMeshDisplay();
        }
        else
        {
            SetupSpriteDisplay(); // 默认使用icon
        }
        
        // 设置颜色
        SetItemColor();
        
        // 设置发光
        SetupGlow();
        
        // 设置UI文本
        SetupUIText();
    }
    protected override void Update()
    {
        if (player == null) return;
    
        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= interactionRange;
    
        // 修复：添加视线检测
        bool hasLineOfSight = false;
        if (inRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            if (!Physics.Raycast(transform.position, directionToPlayer, distance, LayerMask.GetMask("Default")))
            {
                hasLineOfSight = true;
            }
        }
    
        bool canInteract = inRange && hasLineOfSight;
    
        if (canInteract != playerInRange)
        {
            playerInRange = canInteract;
            if (interactionPrompt) 
                interactionPrompt.SetActive(canInteract);
            
            // 通知UI系统
            if (canInteract && UIManager.Instance)
            {
                UIManager.Instance.ShowInteractionPrompt($"按 F 拾取 {itemData.itemName}");
            }
            else if (!canInteract && UIManager.Instance)
            {
                UIManager.Instance.HideInteractionPrompt();
            }
        }
    
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            OnInteract();
        }
    }
    void SetupSpriteDisplay()
    {
        if (spriteRenderer == null)
        {
            var spriteObj = new GameObject("SpriteDisplay");
            spriteObj.transform.SetParent(visualContainer ? visualContainer : transform);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        }
    
        // 设置精灵
        if (itemData.worldSprite != null)
        {
            spriteRenderer.sprite = itemData.worldSprite;
        }
        else if (itemData.icon != null)
        {
            spriteRenderer.sprite = itemData.icon;
        }
    
        // 设置大小 - 使用改进的缩放系统
        float scale = GetItemScale();
        spriteRenderer.transform.localScale = Vector3.one * scale;
    
        // 让精灵面向相机
        spriteRenderer.transform.LookAt(Camera.main.transform);
        spriteRenderer.transform.Rotate(0, 180, 0);
    
        // 隐藏mesh
        if (meshRenderer) meshRenderer.enabled = false;
    
        Debug.Log($"[PickupItem] Set scale for {itemData.itemName}: {scale}");
    }

    
    void SetupMeshDisplay()
    {
        if (itemData.worldPrefab != null && visualContainer != null)
        {
            GameObject meshObj = Instantiate(itemData.worldPrefab, visualContainer);
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.transform.localScale = Vector3.one * GetItemScale();
        }
        
        // 隐藏sprite
        if (spriteRenderer) spriteRenderer.enabled = false;
    }
    

    
    void SetItemColor()
    {
        Color targetColor = itemData.itemColor;
        
        // 根据类型设置颜色
        if (targetColor == Color.white)
        {
            targetColor = itemData.itemType switch
            {
                ItemType.Weapon => Color.cyan,
                ItemType.Ammo => Color.yellow,
                ItemType.Food => Color.green,
                ItemType.Water => Color.blue,
                ItemType.Medicine => Color.red,
                _ => Color.white
            };
        }
        
        if (spriteRenderer)
        {
            spriteRenderer.color = targetColor;
        }
    }
    float GetItemScale()
    {
        // 根据物品类型调整大小 - 减小整体尺寸
        return itemData.itemType switch
        {
            ItemType.Weapon => 1.0f,    // 武器正常大小
            ItemType.Ammo => 0.5f,      // 弹药稍小
            ItemType.Food => 0.5f,      // 食物小一些
            ItemType.Water => 0.5f,     // 水小一些
            ItemType.Medicine => 0.5f,  // 药品稍小
            _ => 0.8f                   // 默认稍小
        };
    }
    void SetupGlow()
    {
        if (!enableGlow || !itemData.hasGlow) return;
        
        if (glowLight == null)
        {
            var lightObj = new GameObject("GlowLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            glowLight = lightObj.AddComponent<Light>();
        }
        
        glowLight.type = LightType.Point;
        glowLight.color = itemData.glowColor;
        glowLight.intensity = 1.5f;
        glowLight.range = 3f;
        
        // 闪烁效果
        StartCoroutine(GlowPulse());
    }
    
    void SetupUIText()
    {
        if (itemNameText)
        {
            itemNameText.text = itemData.itemName;
        }
        
        if (quantityText)
        {
            if (itemData.IsAmmo)
            {
                quantityText.text = $"+{itemData.GetPickupAmount()}";
            }
            else if (quantity > 1)
            {
                quantityText.text = $"x{quantity}";
            }
            else
            {
                quantityText.text = "";
            }
        }
        
        // UI面向相机
        if (uiCanvas)
        {
            uiCanvas.worldCamera = Camera.main;
        }
    }
    
    IEnumerator DoomBobAnimation()
    {
        while (this != null)
        {
            float time = Time.time * bobSpeed;
            
            // 上下浮动
            Vector3 newPos = originalPosition + Vector3.up * Mathf.Sin(time) * bobHeight;
            transform.position = newPos;
            
            // 旋转
            if (visualContainer)
            {
                visualContainer.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
            else
            {
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
            
            yield return null;
        }
    }
    
    IEnumerator GlowPulse()
    {
        float baseIntensity = glowLight.intensity;
        
        while (this != null && glowLight != null)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
            glowLight.intensity = baseIntensity * (0.5f + pulse * 0.5f);
            yield return null;
        }
    }
    
    public override void OnInteract()
    {
        if (TryPickup())
        {
            PlayPickupAnimation();
        }
    }
    
    bool TryPickup()
    {
        if (!InventoryManager.Instance)
        {
            Debug.LogWarning("InventoryManager not found!");
            return false;
        }
    
        int pickupAmount = itemData.IsAmmo ? itemData.GetPickupAmount() : quantity;
    
        if (InventoryManager.Instance.AddItem(itemData, pickupAmount))
        {
            string message = $"拾取了 {pickupAmount}x {itemData.itemName}";
            if (UIManager.Instance)
            {
                UIManager.Instance.ShowMessage(message, 2f);
            }
        
            Debug.Log(message);
            return true;
        }
        else
        {
            if (UIManager.Instance)
            {
                UIManager.Instance.ShowMessage("背包已满!", 2f);
            }
            return false;
        }
    }

    
    void PlayPickupAnimation()
    {
        // 播放拾取音效
        if (audioSource && itemData.itemType == ItemType.Ammo)
        {
            // 可以播放弹药拾取音效
        }
    
        StartCoroutine(PickupEffect());
    }
    
    IEnumerator PickupEffect()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * 1f;
        Vector3 startScale = transform.localScale;
    
        float duration = 0.3f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
        
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
        
            // 淡出
            if (spriteRenderer)
            {
                Color color = spriteRenderer.color;
                color.a = 1f - progress;
                spriteRenderer.color = color;
            }
        
            yield return null;
        }
    
        // 关键修复：确保销毁物体
        Destroy(gameObject);
    }
}