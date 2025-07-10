// 修改后的ConfigManager.cs
using UnityEngine;
using System.Collections.Generic;

public class ConfigManager : Singleton<ConfigManager>
{
    [Header("主配置文件")]
    public GameConfig gameConfig;
    
    [Header("配置缓存设置")]
    public bool enableConfigCaching = true;
    public bool autoValidateOnLoad = true;
    
    private Dictionary<System.Type, BaseGameConfig> configCache;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeConfigSystem();
    }
    
    void InitializeConfigSystem()
    {
        if (gameConfig == null)
        {
            Debug.LogError("[ConfigManager] GameConfig not assigned!");
            enabled = false;
            return;
        }
        
        if (enableConfigCaching)
        {
            configCache = new Dictionary<System.Type, BaseGameConfig>();
        }
        
        if (autoValidateOnLoad)
        {
            ValidateAllConfigs();
        }
    }
    
    // 泛型配置获取 - 带缓存
    public T GetConfig<T>() where T : BaseGameConfig
    {
        if (enableConfigCaching && configCache.TryGetValue(typeof(T), out BaseGameConfig cached))
        {
            return cached as T;
        }
        
        var config = gameConfig.GetConfig<T>();
        
        if (enableConfigCaching && config != null)
        {
            configCache[typeof(T)] = config;
        }
        
        return config;
    }
    
    // 兼容性属性 - 保持现有代码工作
    public GameConfig Config => gameConfig;
    public DifficultyConfig Difficulty => GetConfig<DifficultyConfig>();
    public FamilyConfig Family => GetConfig<FamilyConfig>();
    public WeaponConfig Weapon => GetConfig<WeaponConfig>();
    public ItemSystemConfig Item => GetConfig<ItemSystemConfig>();
    public AudioSystemConfig Audio => GetConfig<AudioSystemConfig>();
    public UISystemConfig UI => GetConfig<UISystemConfig>();
    public InputSettings Input => GetConfig<InputSettings>();
    
    public void ValidateAllConfigs()
    {
        bool allValid = true;
        
        // 验证所有已注册的配置
        var configTypes = new System.Type[]
        {
            typeof(DifficultyConfig),
            typeof(FamilyConfig), 
            typeof(WeaponConfig),
            typeof(ItemSystemConfig),
            typeof(AudioSystemConfig),
            typeof(UISystemConfig),
            typeof(InputSettings)
        };
        
        foreach (var configType in configTypes)
        {
            var config = gameConfig.GetConfig(configType) as BaseGameConfig;
            if (config != null)
            {
                if (!config.ValidateConfig())
                {
                    allValid = false;
                    Debug.LogError($"[ConfigManager] Validation failed for {configType.Name}");
                }
            }
            else
            {
                Debug.LogWarning($"[ConfigManager] Missing config: {configType.Name}");
            }
        }
        
        if (allValid)
        {
            Debug.Log("[ConfigManager] All configs validated successfully");
        }
    }
    
    public void ClearConfigCache()
    {
        configCache?.Clear();
    }
    
    public void ReloadConfigs()
    {
        ClearConfigCache();
        gameConfig.OnEnable(); // 重新构建配置注册表
        
        if (autoValidateOnLoad)
        {
            ValidateAllConfigs();
        }
    }
}