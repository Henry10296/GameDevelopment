using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [Header("生成预制体")]
    public GameObject itemPickupPrefab; // EnhancedPickupItem预制体
    
    [Header("可生成的道具")]
    public ItemData[] availableItems;
    public WeaponData[] availableWeapons;
    
    [Header("生成设置")]
    public bool spawnOnStart = false;
    public Transform[] spawnPoints;
    public int itemsToSpawn = 5;
    
    [Header("随机生成")]
    public bool useRandomSpawn = true;
    public float spawnRadius = 10f;
    public LayerMask groundLayerMask = 1; // 地面图层
    
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnItems();
        }
    }
    
    [ContextMenu("生成物品")]
    public void SpawnItems()
    {
        if (useRandomSpawn)
        {
            SpawnItemsRandomly();
        }
        else
        {
            SpawnItemsAtPoints();
        }
    }
    
    void SpawnItemsRandomly()
    {
        for (int i = 0; i < itemsToSpawn; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            if (spawnPos != Vector3.zero)
            {
                SpawnRandomItem(spawnPos);
            }
        }
    }
    
    void SpawnItemsAtPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[ItemSpawner] No spawn points assigned!");
            return;
        }
        
        foreach (var point in spawnPoints)
        {
            if (point != null)
            {
                SpawnRandomItem(point.position);
            }
        }
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 testPos = transform.position + new Vector3(randomCircle.x, 20f, randomCircle.y);
            
            // 从上方射线检测地面
            if (Physics.Raycast(testPos, Vector3.down, out RaycastHit hit, 40f, groundLayerMask))
            {
                return hit.point + Vector3.up * 0.5f; // 稍微抬高避免嵌入地面
            }
        }
        
        Debug.LogWarning("[ItemSpawner] Could not find valid spawn position");
        return Vector3.zero;
    }
    
    void SpawnRandomItem(Vector3 position)
    {
        if (itemPickupPrefab == null)
        {
            Debug.LogError("[ItemSpawner] Item pickup prefab not assigned!");
            return;
        }
        
        GameObject itemObj = Instantiate(itemPickupPrefab, position, Quaternion.identity);
        PickupItem pickup = itemObj.GetComponent<PickupItem>();
        
        if (pickup != null)
        {
            // 随机选择道具或武器
            bool spawnWeapon = Random.value < 0.3f && availableWeapons.Length > 0; // 30%概率生成武器
            
            if (spawnWeapon)
            {
                WeaponData weaponData = availableWeapons[Random.Range(0, availableWeapons.Length)];
                pickup.SetWeaponData(weaponData);
            }
            else if (availableItems.Length > 0)
            {
                ItemData itemData = availableItems[Random.Range(0, availableItems.Length)];
                int quantity = GetRandomQuantity(itemData);
                pickup.SetItemData(itemData, quantity);
            }
        }
        
        Debug.Log($"[ItemSpawner] Spawned item at {position}");
    }
    
    int GetRandomQuantity(ItemData itemData)
    {
        return itemData.itemType switch
        {
            ItemType.Food => Random.Range(1, 4),
            ItemType.Water => Random.Range(1, 3),
            ItemType.Medicine => Random.Range(1, 2),
            ItemType.Ammo => Random.Range(10, 31),
            _ => 1
        };
    }
    
    // 手动生成指定物品
    public void SpawnSpecificItem(ItemData itemData, Vector3 position, int quantity = 1)
    {
        if (itemPickupPrefab == null || itemData == null) return;
        
        GameObject itemObj = Instantiate(itemPickupPrefab, position, Quaternion.identity);
        PickupItem pickup = itemObj.GetComponent<PickupItem>();
        
        if (pickup != null)
        {
            pickup.SetItemData(itemData, quantity);
        }
    }
    
    public void SpawnSpecificWeapon(WeaponData weaponData, Vector3 position)
    {
        if (itemPickupPrefab == null || weaponData == null) return;
        
        GameObject itemObj = Instantiate(itemPickupPrefab, position, Quaternion.identity);
        PickupItem pickup = itemObj.GetComponent<PickupItem>();
        
        if (pickup != null)
        {
            pickup.SetWeaponData(weaponData);
        }
    }
    
    // 清理已生成的物品
    [ContextMenu("清理物品")]
    public void ClearSpawnedItems()
    {
        PickupItem[] items = FindObjectsOfType<PickupItem>();
        for (int i = items.Length - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(items[i].gameObject);
            }
            else
            {
                DestroyImmediate(items[i].gameObject);
            }
        }
        
        Debug.Log($"[ItemSpawner] Cleared {items.Length} items");
    }
    
    void OnDrawGizmos()
    {
        // 显示生成半径
        if (useRandomSpawn)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
        
        // 显示生成点
        if (spawnPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                }
            }
        }
    }
}