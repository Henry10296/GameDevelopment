using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour
{
    [Header("交互设置")]
    public float interactionRange = 2f;
    public KeyCode interactionKey = KeyCode.E;
    public GameObject interactionPrompt;
    
    protected Transform player;
    protected bool playerInRange = false;
    
    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogWarning($"[BaseInteractable] {gameObject.name} - Player not found!");
        }
        
        if (interactionPrompt) 
            interactionPrompt.SetActive(false);
    }
    
    protected virtual void Update()
    {
        if (player == null) return; // 修复：添加空检查
        
        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= interactionRange;
        
        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (interactionPrompt) 
                interactionPrompt.SetActive(inRange);
        }
        
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            OnInteract();
        }
    }
    
    protected abstract void OnInteract();
    
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}