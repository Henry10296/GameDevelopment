// 新增：Assets/Scripts/Config/BaseGameConfig.cs
using UnityEngine;

public abstract class BaseGameConfig : ScriptableObject
{
    [Header("配置信息")]
    public string configName;
    public string configVersion = "1.0";
    [TextArea(2, 4)]
    public string description;
    
    protected virtual void OnEnable()
    {
        if (string.IsNullOrEmpty(configName))
        {
            configName = GetType().Name;
        }
    }
    
    // 配置验证方法
    public virtual bool ValidateConfig()
    {
        return true;
    }
    
    // 获取配置摘要信息
    public virtual string GetConfigSummary()
    {
        return $"{configName} v{configVersion}";
    }
}