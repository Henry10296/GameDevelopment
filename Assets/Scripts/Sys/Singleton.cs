using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;
    
    // 添加初始化状态
    public static bool IsInitialized { get; private set; }
    
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[SafeSingleton] Instance '{typeof(T)}' already destroyed.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = $"(singleton) {typeof(T)}";
                        DontDestroyOnLoad(singleton);
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
            Destroy(gameObject);
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
            _applicationIsQuitting = true;
        }
    }
}