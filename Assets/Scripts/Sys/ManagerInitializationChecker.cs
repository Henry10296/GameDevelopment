using UnityEngine;

[System.Serializable]
public class ManagerInitializationChecker : MonoBehaviour
{
    [Header("检查设置")]
    public bool checkOnStart = true;
    public float checkInterval = 1f;
    
    void Start()
    {
        if (checkOnStart)
        {
            InvokeRepeating(nameof(CheckManagers), 0f, checkInterval);
        }
    }

    void CheckManagers()
    {
        // 检查关键管理器是否正常工作
        CheckManager("GameManager", GameManager.Instance);
        CheckManager("UIManager", UIManager.Instance);
        CheckManager("FamilyManager", FamilyManager.Instance);
        CheckManager("InventoryManager", InventoryManager.Instance);
        CheckManager("AudioManager", AudioManager.Instance);
    }

    void CheckManager(string name, object instance)
    {
        if (instance == null)
        {
            Debug.LogWarning($"[ManagerChecker] {name} is not initialized!");
        }
    }
}