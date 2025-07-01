using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonManager : MonoBehaviour
{
    [Header("初始化顺序")]
    public int[] initializationOrder = { 
        0,  // ConfigManager
        1,  // GameData
        2,  // AudioManager
        3,  // SaveManager
        4,  // FamilyManager
        5,  // InventoryManager
        6,  // UIManager
        7   // GameManager
    };
    
    private readonly Dictionary<int, System.Type> singletonTypes = new()
    {
        { 0, typeof(ConfigManager) },
        { 1, typeof(GameData) },
        { 2, typeof(AudioManager) },
        { 3, typeof(SaveManager) },
        { 4, typeof(FamilyManager) },
        { 5, typeof(InventoryManager) },
        { 6, typeof(UIManager) },
        { 7, typeof(GameManager) }
    };
    
    void Awake()
    {
        StartCoroutine(InitializeSingletons());
    }
    
    IEnumerator InitializeSingletons()
    {
        foreach (int order in initializationOrder)
        {
            if (singletonTypes.TryGetValue(order, out System.Type type))
            {
                // 确保单例已创建
                var instanceProperty = type.GetProperty("Instance");
                var instance = instanceProperty?.GetValue(null);
                
                if (instance == null)
                {
                    Debug.LogError($"Failed to initialize singleton: {type.Name}");
                }
                else
                {
                    Debug.Log($"Initialized singleton: {type.Name}");
                }
                
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
