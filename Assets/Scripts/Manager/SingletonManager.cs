// 2. 改进的SingletonManager
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ImprovedSingletonManager : MonoBehaviour
{
    [Header("初始化配置")] public bool autoInitializeOnAwake = true;
    public float initializationDelay = 0.1f;

    [Header("调试选项")] public bool enableDebugLogs = true;
    public bool validateDependencies = true;

    // 管理器初始化信息
    private readonly Dictionary<string, ManagerInitInfo> managerInfos = new()
    {
        {
            "ConfigManager",
            new ManagerInitInfo { Type = typeof(ConfigManager), Order = 0, Dependencies = new string[0] }
        },
        {
            "GameData",
            new ManagerInitInfo { Type = typeof(GameData), Order = 1, Dependencies = new[] { "ConfigManager" } }
        },
        {
            "AudioManager",
            new ManagerInitInfo { Type = typeof(AudioManager), Order = 2, Dependencies = new[] { "ConfigManager" } }
        },
        {
            "SaveManager",
            new ManagerInitInfo { Type = typeof(SaveManager), Order = 3, Dependencies = new[] { "ConfigManager" } }
        },
        {
            "FamilyManager",
            new ManagerInitInfo
                { Type = typeof(FamilyManager), Order = 4, Dependencies = new[] { "ConfigManager", "GameData" } }
        },
        {
            "InventoryManager",
            new ManagerInitInfo { Type = typeof(InventoryManager), Order = 5, Dependencies = new[] { "GameData" } }
        },
        {
            "JournalManager",
            new ManagerInitInfo { Type = typeof(JournalManager), Order = 6, Dependencies = new[] { "GameData" } }
        },
        {
            "MapManager",
            new ManagerInitInfo { Type = typeof(MapManager), Order = 7, Dependencies = new[] { "ConfigManager" } }
        },
        {
            "HomeEventManager",
            new ManagerInitInfo
            {
                Type = typeof(HomeEventManager), Order = 8, Dependencies = new[] { "ConfigManager", "FamilyManager" }
            }
        },
        {
            "GameEventManager",
            new ManagerInitInfo
            {
                Type = typeof(GameEventManager), Order = 9, Dependencies = new[] { "ConfigManager", "FamilyManager" }
            }
        },
        {
            "QuestExtensionManager",
            new ManagerInitInfo
                { Type = typeof(QuestExtensionManager), Order = 10, Dependencies = new[] { "GameEventManager" } }
        },
        {
            "ExplorationManager",
            new ManagerInitInfo
            {
                Type = typeof(ExplorationManager), Order = 11, Dependencies = new[] { "MapManager", "InventoryManager" }
            }
        },
        {
            "EndGameManager",
            new ManagerInitInfo
                { Type = typeof(EndGameManager), Order = 12, Dependencies = new[] { "FamilyManager", "GameData" } }
        },
        {
            "UIManager",
            new ManagerInitInfo
            {
                Type = typeof(UIManager), Order = 13,
                Dependencies = new[] { "ConfigManager", "FamilyManager", "InventoryManager" }
            }
        },
        {
            "GameManager",
            new ManagerInitInfo
            {
                Type = typeof(GameManager), Order = 14,
                Dependencies = new[] { "ConfigManager", "UIManager", "FamilyManager", "AudioManager" }
            }
        }
    };

    private readonly Dictionary<string, bool> initializationStatus = new();
    private bool initializationCompleted = false;

    void Awake()
    {
        if (autoInitializeOnAwake)
        {
            StartCoroutine(InitializeAllManagers());
        }
    }

    public void ManualInitialize()
    {
        if (!initializationCompleted)
        {
            StartCoroutine(InitializeAllManagers());
        }
    }

    IEnumerator InitializeAllManagers()
    {
        LogDebug("[SingletonManager] Starting manager initialization...");

        // 按依赖顺序排序
        var sortedManagers = SortManagersByDependencies();

        foreach (var managerName in sortedManagers)
        {
            if (managerInfos.TryGetValue(managerName, out var info))
            {
                yield return StartCoroutine(InitializeManager(managerName, info));
                yield return new WaitForSeconds(initializationDelay);
            }
        }

        // 验证所有管理器都已正确初始化
        if (validateDependencies)
        {
            ValidateAllManagers();
        }

        initializationCompleted = true;
        LogDebug("[SingletonManager] All managers initialized successfully!");

        // 通知游戏可以开始
        OnAllManagersInitialized();
    }

    IEnumerator InitializeManager(string managerName, ManagerInitInfo info)
    {
        LogDebug($"[SingletonManager] Initializing {managerName}...");

        // 检查依赖是否已初始化
        if (!CheckDependencies(managerName, info.Dependencies))
        {
            Debug.LogError($"[SingletonManager] Dependencies not met for {managerName}");
            initializationStatus[managerName] = false;
            yield break;
        }

        // 将可能抛出异常的操作封装到单独的方法中
        var initResult = TryInitializeManagerInstance(managerName, info);
        if (!initResult.success)
        {
            initializationStatus[managerName] = false;
            yield break;
        }

        // 等待一帧确保实例创建完成
        yield return null;

        // 验证初始化结果
        var verifyResult = VerifyManagerInitialization(managerName, info);
        if (verifyResult.success)
        {
            initializationStatus[managerName] = true;
            LogDebug($"[SingletonManager] {managerName} initialized successfully");

            // 如果管理器有特殊的初始化后处理，等待它完成
            if (verifyResult.instance is MonoBehaviour mb && mb.gameObject.activeInHierarchy)
            {
                // 等待管理器的Start方法执行
                yield return new WaitForEndOfFrame();
            }
        }
        else
        {
            initializationStatus[managerName] = false;
        }
    }

    // 尝试初始化管理器实例（无协程，可以使用try/catch）
    private InitializationResult TryInitializeManagerInstance(string managerName, ManagerInitInfo info)
    {
        try
        {
            // 获取或创建管理器实例
            var instanceProperty = info.Type.GetProperty("Instance");
            var instance = instanceProperty?.GetValue(null);

            if (instance == null)
            {
                // 尝试在场景中查找
                var existingManager = FindObjectOfType(info.Type);
                if (existingManager == null)
                {
                    // 创建新的管理器GameObject
                    GameObject managerObj = new GameObject(managerName);
                    managerObj.AddComponent(info.Type);
                    DontDestroyOnLoad(managerObj);
                }
            }

            return new InitializationResult { success = true, instance = instance };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SingletonManager] Exception while initializing {managerName}: {e.Message}");
            return new InitializationResult { success = false, exception = e };
        }
    }

    // 验证管理器初始化（无协程，可以使用try/catch）
    private InitializationResult VerifyManagerInitialization(string managerName, ManagerInitInfo info)
    {
        try
        {
            var instanceProperty = info.Type.GetProperty("Instance");
            var instance = instanceProperty?.GetValue(null);

            if (instance != null)
            {
                return new InitializationResult { success = true, instance = instance };
            }
            else
            {
                Debug.LogError($"[SingletonManager] Failed to initialize {managerName}");
                return new InitializationResult { success = false };
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SingletonManager] Exception while verifying {managerName}: {e.Message}");
            return new InitializationResult { success = false, exception = e };
        }
    }

    List<string> SortManagersByDependencies()
    {
        var sorted = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var managerName in managerInfos.Keys)
        {
            if (!visited.Contains(managerName))
            {
                VisitManager(managerName, sorted, visited, visiting);
            }
        }

        return sorted;
    }

    void VisitManager(string managerName, List<string> sorted, HashSet<string> visited, HashSet<string> visiting)
    {
        if (visiting.Contains(managerName))
        {
            Debug.LogError($"[SingletonManager] Circular dependency detected involving {managerName}");
            return;
        }

        if (visited.Contains(managerName))
            return;

        visiting.Add(managerName);

        if (managerInfos.TryGetValue(managerName, out var info))
        {
            foreach (var dependency in info.Dependencies)
            {
                if (managerInfos.ContainsKey(dependency))
                {
                    VisitManager(dependency, sorted, visited, visiting);
                }
            }
        }

        visiting.Remove(managerName);
        visited.Add(managerName);
        sorted.Add(managerName);
    }

    bool CheckDependencies(string managerName, string[] dependencies)
    {
        foreach (var dependency in dependencies)
        {
            if (!initializationStatus.GetValueOrDefault(dependency, false))
            {
                LogDebug($"[SingletonManager] {managerName} waiting for dependency: {dependency}");
                return false;
            }
        }

        return true;
    }

    void ValidateAllManagers()
    {
        LogDebug("[SingletonManager] Validating all managers...");

        foreach (var kvp in managerInfos)
        {
            var managerName = kvp.Key;
            var info = kvp.Value;

            var instanceProperty = info.Type.GetProperty("Instance");
            var instance = instanceProperty?.GetValue(null);

            if (instance == null)
            {
                Debug.LogError($"[SingletonManager] Manager {managerName} failed validation - instance is null");
            }
            else
            {
                LogDebug($"[SingletonManager] Manager {managerName} validated successfully");
            }
        }
    }

    void OnAllManagersInitialized()
    {
        // 发送初始化完成事件
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            // 可以在这里通知GameManager初始化完成
            LogDebug("[SingletonManager] Notifying GameManager that initialization is complete");
        }
    }

    void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }

    // 公共方法检查特定管理器是否已初始化
    public bool IsManagerInitialized(string managerName)
    {
        return initializationStatus.GetValueOrDefault(managerName, false);
    }

    // 检查所有管理器是否已初始化
    public bool AreAllManagersInitialized()
    {
        return initializationCompleted;
    }

    // 获取初始化进度
    public float GetInitializationProgress()
    {
        if (managerInfos.Count == 0) return 1f;

        int initializedCount = initializationStatus.Values.Count(status => status);
        return (float)initializedCount / managerInfos.Count;
    }

   
}
// 3. 管理器初始化信息类
[System.Serializable]
public class ManagerInitInfo
{
    public System.Type Type;
    public int Order;
    public string[] Dependencies;
}

// 初始化结果结构
public struct InitializationResult
{
    public bool success;
    public object instance;
    public System.Exception exception;
}