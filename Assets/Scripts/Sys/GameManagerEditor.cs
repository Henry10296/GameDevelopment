#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
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