using UnityEngine;

public class ConfigManager : Singleton<ConfigManager>
{
    [Header("配置文件")]
    public GameConfig gameConfig;
    
    // 便捷访问属性
    public GameConfig Config => gameConfig;
    public DifficultyConfig Difficulty => gameConfig?.GetCurrentDifficulty();
    public FamilyConfig Family => gameConfig?.familyConfig;
    public WeaponSystemConfig Weapon => gameConfig?.weaponConfig;
    public ItemSystemConfig Item => gameConfig?.itemConfig;
    public MapSystemConfig Map => gameConfig?.mapConfig;
    public EventSystemConfig Event => gameConfig?.eventConfig;
    public AudioSystemConfig Audio => gameConfig?.audioConfig;
    public UISystemConfig UI => gameConfig?.uiConfig;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (gameConfig == null)
        {
            Debug.LogError("[ConfigManager] GameConfig not assigned!");
            enabled = false;
        }
    }
    
    // 动态配置修改方法
    public void SetDifficulty(DifficultyLevel difficulty)
    {
        if (gameConfig != null)
        {
            gameConfig.currentDifficulty = difficulty;
            ApplyDifficultySettings();
        }
    }
    
    void ApplyDifficultySettings()
    {
        var difficulty = Difficulty;
        if (difficulty == null) return;
        
        // 应用难度设置到各个系统
        Debug.Log($"[ConfigManager] Applied difficulty: {difficulty.difficultyName}");
    }
    
    // 配置验证
    public bool ValidateConfig()
    {
        if (gameConfig == null) return false;
        
        bool isValid = true;
        
        // 验证基础设置
        if (gameConfig.maxDays <= 0)
        {
            Debug.LogError("[ConfigManager] maxDays must be greater than 0");
            isValid = false;
        }
        
        if (gameConfig.explorationTimeLimit <= 0)
        {
            Debug.LogError("[ConfigManager] explorationTimeLimit must be greater than 0");
            isValid = false;
        }
        
        // 验证配置完整性
        if (gameConfig.enemyConfigs == null || gameConfig.enemyConfigs.Length == 0)
        {
            Debug.LogWarning("[ConfigManager] No enemy configs found");
        }
        
        if (gameConfig.weaponConfig?.pistol == null)
        {
            Debug.LogError("[ConfigManager] Pistol weapon data missing");
            isValid = false;
        }
        
        return isValid;
    }
    
    // 配置重载
    public void ReloadConfig()
    {
        if (gameConfig != null)
        {
            // 重新应用配置
            ApplyDifficultySettings();
            Debug.Log("[ConfigManager] Config reloaded");
        }
    }
}
