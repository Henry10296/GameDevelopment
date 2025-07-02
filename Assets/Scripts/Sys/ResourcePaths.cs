using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourcePaths", menuName = "Game/Resource Paths")]
public class ResourcePaths : ScriptableObject
{
    [Header("音频资源路径")]
    public string audioBasePath = "Audio/";
    public string musicPath = "Audio/Music/";
    public string sfxPath = "Audio/SFX/";
    public string voicePath = "Audio/Voice/";
    
    [Header("预制体路径")]
    public string uiPrefabsPath = "Prefabs/UI/";
    public string enemyPrefabsPath = "Prefabs/Enemies/";
    public string itemPrefabsPath = "Prefabs/Items/";
    public string effectsPath = "Prefabs/Effects/";
    
    [Header("材质资源路径")]
    public string materialsPath = "Materials/";
    public string texturesPath = "Textures/";
    public string spritesPath = "Sprites/";
    
    [Header("特殊资源名称")]
    public string bulletTrailMaterial = "BulletTrailMaterial";
    public string muzzleFlashPrefab = "MuzzleFlashPrefab";
    public string hitEffectPrefab = "HitEffectPrefab";
    
    public string GetFullPath(string category, string fileName)
    {
        string basePath = category.ToLower() switch
        {
            "audio" => audioBasePath,
            "music" => musicPath,
            "sfx" => sfxPath,
            "voice" => voicePath,
            "ui" => uiPrefabsPath,
            "enemy" => enemyPrefabsPath,
            "item" => itemPrefabsPath,
            "effects" => effectsPath,
            "materials" => materialsPath,
            "textures" => texturesPath,
            "sprites" => spritesPath,
            _ => ""
        };
        
        return basePath + fileName;
    }
}
