using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;
    
    // 初始化顺序控制
    protected virtual int InitializationOrder => 0;
    
    // 添加初始化状态
    public static bool IsInitialized { get; private set; }
    
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject($"[Singleton] {typeof(T).Name}");
                        _instance = singleton.AddComponent<T>();
                        DontDestroyOnLoad(singleton);
                        
                        Debug.Log($"[Singleton] Created new instance of {typeof(T).Name}");
                    }
                    else
                    {
                        Debug.Log($"[Singleton] Found existing instance of {typeof(T).Name}");
                    }
                }

                return _instance;
            }
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
    
    // 子类重写这些方法而不是Awake/Start
    protected virtual void OnSingletonAwake() { }
    protected virtual void OnSingletonStart() { }
    
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            IsInitialized = false;
        }
    }
    
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
    
    // 安全的实例访问方法
    public static bool HasInstance => _instance != null && !_applicationIsQuitting;
    
    // 强制销毁单例（测试用）
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