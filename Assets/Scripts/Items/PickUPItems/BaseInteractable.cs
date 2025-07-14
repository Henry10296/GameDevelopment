using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour
{
    [Header("交互设置")]
    public float interactionRange = 2f;
    public KeyCode interactionKey = KeyCode.F;
    public GameObject interactionPrompt;
    
    protected Transform player;
    protected bool playerInRange = false;
    
    // 添加这个标志来控制是否使用内置输入检测
    [Header("输入设置")]
    public bool useBuiltInInput = false; // 默认关闭，让PlayerController处理
    
    protected virtual void Start()
    {
        FindPlayer();
        
        if (interactionPrompt) 
            interactionPrompt.SetActive(false);
    }
    
    void FindPlayer()
    {
        // 方法1：通过标签查找
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"[BaseInteractable] Found player by tag: {playerObj.name}");
            return;
        }
        
        // 方法2：通过PlayerController查找
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
            Debug.Log($"[BaseInteractable] Found player by PlayerController: {player.name}");
            return;
        }
        
        // 方法3：通过Player类查找
        if (Player.Instance != null)
        {
            player = Player.Instance.transform;
            Debug.Log($"[BaseInteractable] Found player by Player.Instance: {player.name}");
            return;
        }
        
        Debug.LogWarning($"[BaseInteractable] {gameObject.name} - Player not found!");
    }
    
    protected virtual void Update()
    {
        // 修复：如果没找到玩家，重新查找
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= interactionRange;
        
        // 修复：添加视线检测，确保能看到物品
        bool hasLineOfSight = true;
        if (inRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 startPos = transform.position + Vector3.up * 0.5f;
            
            // 检查是否有障碍物遮挡
            if (Physics.Raycast(startPos, directionToPlayer, distance - 0.1f))
            {
                hasLineOfSight = false;
            }
        }
        
        bool canInteract = inRange && hasLineOfSight;
        
        if (canInteract != playerInRange)
        {
            playerInRange = canInteract;
            OnPlayerRangeChanged(canInteract);
        }
        
        // 只有在启用内置输入时才检测按键
        if (useBuiltInInput && playerInRange && Input.GetKeyDown(interactionKey))
        {
            Debug.Log($"[BaseInteractable] {gameObject.name} - Interaction triggered!");
            OnInteract();
        }
    }
    
    // 新增：供外部调用的交互方法
    public virtual bool CanInteract()
    {
        return playerInRange;
    }
    
    protected virtual void OnPlayerRangeChanged(bool inRange)
    {
        if (interactionPrompt) 
            interactionPrompt.SetActive(inRange);
            
        // 通知UI系统
        if (inRange)
        {
            ShowInteractionUI();
        }
        else
        {
            HideInteractionUI();
        }
    }
    
    protected virtual void ShowInteractionUI()
    {
        if (UIManager.Instance)
        {
            string itemName = GetInteractionText();
            UIManager.Instance.ShowInteractionPrompt($"按 E {itemName}");
        }
    }
    
    protected virtual void HideInteractionUI()
    {
        if (UIManager.Instance)
        {
            UIManager.Instance.HideInteractionPrompt();
        }
    }
    
    public abstract void OnInteract();
    
    public virtual string GetInteractionText()
    {
        return "交互";
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 显示到玩家的连线
        if (player != null)
        {
            Gizmos.color = playerInRange ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}