#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
        private bool showConfigSection = true;
    
    /*public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GameManager gameManager = (GameManager)target;
        
        EditorGUILayout.Space();
        
        // 配置验证区域
        showConfigSection = EditorGUILayout.Foldout(showConfigSection, "Configuration Status");
        if (showConfigSection)
        {
            DrawConfigurationStatus(gameManager);
        }
        
        // 现有的调试控制保持不变...
    }*/
    
    void DrawConfigurationStatus(GameManager gameManager)
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("SO Configuration Status:", EditorStyles.boldLabel);
        
        // 检查各种配置
        DrawConfigStatusLine("Scene Settings", gameManager.sceneSettings);
        DrawConfigStatusLine("Input Settings", gameManager.inputSettings);
        DrawConfigStatusLine("UI Text Settings", gameManager.uiTextSettings);
        DrawConfigStatusLine("Game Values", gameManager.gameValues);
        DrawConfigStatusLine("Resource Paths", gameManager.resourcePaths);
        
        EditorGUILayout.Space();
        
        // 快速修复按钮
        if (GUILayout.Button("Auto-Assign Missing Configs"))
        {
            AutoAssignConfigs(gameManager);
        }
        
        if (GUILayout.Button("Validate All Configs"))
        {
            ValidateAllConfigs(gameManager);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawConfigStatusLine(string configName, ScriptableObject config)
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.color = config != null ? Color.green : Color.red;
        EditorGUILayout.LabelField("●", GUILayout.Width(15));
        GUI.color = Color.white;
        
        EditorGUILayout.LabelField(configName, GUILayout.Width(120));
        EditorGUILayout.LabelField(config != null ? config.name : "Missing");
        
        EditorGUILayout.EndHorizontal();
    }
    
    void AutoAssignConfigs(GameManager gameManager)
    {
        bool changed = false;
        
        if (gameManager.sceneSettings == null)
        {
            var sceneSettings = FindAssetByType<SceneSettings>();
            if (sceneSettings != null)
            {
                gameManager.sceneSettings = sceneSettings;
                changed = true;
            }
        }
        
        // 对其他配置进行类似处理...
        
        if (changed)
        {
            EditorUtility.SetDirty(gameManager);
            EditorUtility.DisplayDialog("Auto-Assign Complete", "Missing configurations have been assigned", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Auto-Assign", "No missing configurations found", "OK");
        }
    }
    
    T FindAssetByType<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return null;
    }
    
    void ValidateAllConfigs(GameManager gameManager)
    {
        var issues = new List<string>();
        
        // 验证各种配置的完整性
        // 实现具体的验证逻辑...
        
        string result = issues.Count > 0 ? 
            "Configuration Issues:\n" + string.Join("\n", issues) : 
            "All configurations are valid!";
        
        EditorUtility.DisplayDialog("Configuration Validation", result, "OK");
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GameManager gameManager = (GameManager)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Current State: {gameManager.CurrentPhase}");
            EditorGUILayout.LabelField($"Current Day: {gameManager.CurrentDay}");
            //EditorGUILayout.LabelField($"Time of Day: {gameManager.TimeOfDay:0.00}");
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Trigger Next Day"))
            {
                
            }
            
            if (GUILayout.Button("End Game"))
            {
                
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Debug controls available in Play Mode", MessageType.Info);
        }
    }
}

[CustomEditor(typeof(FamilyManager))]
public class FamilyManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        FamilyManager familyManager = (FamilyManager)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Family Status:", EditorStyles.boldLabel);
            
            foreach (var member in familyManager.FamilyMembers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(member.name, GUILayout.Width(60));
                EditorGUILayout.LabelField($"H:{member.health:0}", GUILayout.Width(40));
                EditorGUILayout.LabelField($"F:{member.hunger:0}", GUILayout.Width(40));
                EditorGUILayout.LabelField($"T:{member.thirst:0}", GUILayout.Width(40));
                if (member.isSick)
                {
                    EditorGUILayout.LabelField("[SICK]", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Make All Sick"))
            {
                familyManager.DebugMakeAllSick();
            }
            
            if (GUILayout.Button("Add Resources"))
            {
                familyManager.DebugAddResources();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Debug controls available in Play Mode", MessageType.Info);
        }
    }
}
#endif