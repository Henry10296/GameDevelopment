using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerConfigManager : Singleton<PlayerConfigManager>
{
    [Header("配置文件")]
    public PlayerConfig currentConfig;
    public PlayerConfig[] presetConfigs;
    
    [Header("运行时设置")]
    public bool allowConfigSwitching = true;
    public bool savePlayerPreferences = true;
    
    private PlayerController playerController;
    
    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        LoadPlayerPreferences();
        ApplyCurrentConfig();
    }
    
    public void ApplyCurrentConfig()
    {
        if (!currentConfig || !playerController) return;
        
        // 应用配置到玩家控制器
        // 这里需要在DoomLikePlayerController中添加相应的setter方法
        
        Debug.Log($"[PlayerConfigManager] Applied config: {currentConfig.name}");
    }
    
    public void SwitchConfig(int index)
    {
        if (!allowConfigSwitching) return;
        
        if (index >= 0 && index < presetConfigs.Length)
        {
            currentConfig = presetConfigs[index];
            ApplyCurrentConfig();
        }
    }
    
    public void SavePlayerPreferences()
    {
        if (!savePlayerPreferences || !currentConfig) return;
        
        // 保存玩家偏好设置
        PlayerPrefs.SetFloat("MouseSensitivity", currentConfig.mouseSensitivity);
        PlayerPrefs.SetFloat("AimSensitivity", currentConfig.aimSensitivity);
        PlayerPrefs.SetInt("InvertMouseY", currentConfig.invertMouseY ? 1 : 0);
        PlayerPrefs.SetFloat("FOV", currentConfig.normalFOV);
        PlayerPrefs.SetFloat("FootstepVolume", currentConfig.footstepVolume);
        
        PlayerPrefs.Save();
    }
    
    public void LoadPlayerPreferences()
    {
        if (!savePlayerPreferences || !currentConfig) return;
        
        // 加载玩家偏好设置
        currentConfig.mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", currentConfig.mouseSensitivity);
        currentConfig.aimSensitivity = PlayerPrefs.GetFloat("AimSensitivity", currentConfig.aimSensitivity);
        currentConfig.invertMouseY = PlayerPrefs.GetInt("InvertMouseY", 0) == 1;
        currentConfig.normalFOV = PlayerPrefs.GetFloat("FOV", currentConfig.normalFOV);
        currentConfig.footstepVolume = PlayerPrefs.GetFloat("FootstepVolume", currentConfig.footstepVolume);
    }
}
