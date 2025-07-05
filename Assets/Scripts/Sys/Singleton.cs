using UnityEngine;
using System.Collections.Generic;

// 改进的单例系统，解决销毁顺序问题
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;
    
    // 单例管理器，确保正确的销毁顺序
    private static SingletonManager _singletonManager;
    
    protected virtual int InitializationOrder => 0;
    protected virtual int DestroyOrder => 0; // 添加销毁顺序
    
    public static bool IsInitialized { get; private set; }

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                return null; // 不输出警告，静默返回
            }

            lock (_lock)
            {
                if (_instance == null && !_applicationIsQuitting)
                {
                    _instance = FindObjectOfType<T>();
                    
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject($"[Singleton] {typeof(T).Name}");
                        _instance = singleton.AddComponent<T>();
                        
                        // 确保单例管理器存在
                        EnsureSingletonManager();
                        
                        DontDestroyOnLoad(singleton);
                        Debug.Log($"[Singleton] Created new instance of {typeof(T).Name}");
                    }
                    else
                    {
                        Debug.Log($"[Singleton] Found existing instance of {typeof(T).Name}");
                    }
                    
                    // 注册到管理器
                    if (_singletonManager != null)
                    {
                        _singletonManager.RegisterSingleton(_instance as Singleton<T>);
                    }
                }

                return _instance;
            }
        }
    }

    private static void EnsureSingletonManager()
    {
        if (_singletonManager == null)
        {
            GameObject managerObj = new GameObject("[SingletonManager]");
            _singletonManager = managerObj.AddComponent<SingletonManager>();
            DontDestroyOnLoad(managerObj);
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnSingletonAwake();
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Destroying duplicate instance of {typeof(T).Name}");
            Destroy(gameObject);
            return;
        }
    }
    
    protected virtual void Start()
    {
        if (_instance == this)
        {
            OnSingletonStart();
            IsInitialized = true;
        }
    }
    
    protected virtual void OnSingletonAwake() { }
    protected virtual void OnSingletonStart() { }
    
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            OnSingletonDestroy();
            IsInitialized = false;
            _instance = null;
        }
    }
    
    protected virtual void OnSingletonDestroy() { }
    
    // 应用退出时的清理
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
        OnSingletonApplicationQuit();
    }
    
    protected virtual void OnSingletonApplicationQuit() { }
    
    public static bool HasInstance => _instance != null && !_applicationIsQuitting;
    
    public static void DestroyInstance()
    {
        if (_instance != null)
        {
            if (Application.isPlaying)
                Destroy(_instance.gameObject);
            else
                DestroyImmediate(_instance.gameObject);
            
            _instance = null;
            IsInitialized = false;
        }
    }
}

// 单例管理器，处理销毁顺序
public class SingletonManager : MonoBehaviour
{
    private List<object> registeredSingletons = new List<object>();
    
    public void RegisterSingleton(object singleton)
    {
        if (!registeredSingletons.Contains(singleton))
        {
            registeredSingletons.Add(singleton);
        }
    }
    
    void OnApplicationQuit()
    {
        // 按销毁顺序倒序销毁单例
        registeredSingletons.Sort((a, b) => {
            int orderA = GetDestroyOrder(a);
            int orderB = GetDestroyOrder(b);
            return orderB.CompareTo(orderA); // 倒序
        });
        
        foreach (var singleton in registeredSingletons)
        {
            if (singleton is MonoBehaviour mb && mb != null)
            {
                try
                {
                    // 调用清理方法
                    var method = singleton.GetType().GetMethod("OnSingletonApplicationQuit", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(singleton, null);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error cleaning up singleton {singleton.GetType().Name}: {e.Message}");
                }
            }
        }
        
        registeredSingletons.Clear();
    }
    
    private int GetDestroyOrder(object singleton)
    {
        var property = singleton.GetType().GetProperty("DestroyOrder", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return property != null ? (int)property.GetValue(singleton) : 0;
    }
}