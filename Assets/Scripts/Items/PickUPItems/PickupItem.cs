using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("物品设置")]
    public ItemData itemData;
    public int quantity = 1;

    [Header("拾取设置")]
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;

    [Header("UI提示")]
    public GameObject pickupPrompt;

    private Transform player;
    private bool playerInRange = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (pickupPrompt)
            pickupPrompt.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= pickupRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                if (pickupPrompt) pickupPrompt.SetActive(true);
            }

            if (Input.GetKeyDown(pickupKey))
            {
                TryPickup();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (pickupPrompt) pickupPrompt.SetActive(false);
            }
        }
    }

    void TryPickup()
    {
        if (InventoryManager.Instance && itemData)
        {
            if (InventoryManager.Instance.AddItem(itemData, quantity))
            {
                Debug.Log($"拾取了 {quantity} 个 {itemData.itemName}");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("背包已满!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}