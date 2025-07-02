using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneSettings", menuName = "Game/Scene Settings")]
public class SceneSettings : ScriptableObject
{
    [System.Serializable]
    public class SceneData
    {
        public GamePhase gamePhase;
        public string sceneName;
        public string displayName;
        public Sprite scenePreview;
    }
    
    [Header("游戏场景配置")]
    public SceneData[] gameScenes = new SceneData[]
    {
        new() { gamePhase = GamePhase.MainMenu, sceneName = "0_MainMenu", displayName = "主菜单" },
        new() { gamePhase = GamePhase.Home, sceneName = "1_Home", displayName = "家庭" },
        new() { gamePhase = GamePhase.MapSelection, sceneName = "1_Home", displayName = "地图选择" }
    };
    
    [Header("探索场景配置")]
    public string[] explorationScenes = new string[]
    {
        "2_Hospital",
        "3_School", 
        "4_Supermarket",
        "5_Park"
    };
    
    public string GetSceneName(GamePhase phase)
    {
        var scene = System.Array.Find(gameScenes, s => s.gamePhase == phase);
        return scene?.sceneName ?? "";
    }
    
    public string GetExplorationScene(int index)
    {
        return index >= 0 && index < explorationScenes.Length ? explorationScenes[index] : "";
    }
}
