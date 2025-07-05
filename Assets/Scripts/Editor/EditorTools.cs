
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Text;
using UnityEventType = UnityEngine.EventType;


    // =========================
    // 编辑器工具和实用功能
    // =========================

    /// <summary>
    /// 编辑器实用工具类，提供各种辅助功能
    /// </summary>
    public static class EditorToolsUtilities
    {
        // 常用路径
        public const string ENEMIES_PATH = "Assets/Data/Enemies";
        public const string EVENTS_PATH = "Assets/Data/Events";
        public const string TEMPLATES_PATH = "Assets/Templates";
        public const string EXPORTS_PATH = "Assets/Exports";
        
        /// <summary>
        /// 确保目录存在，不存在则创建
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parentPath = Path.GetDirectoryName(path);
                var folderName = Path.GetFileName(path);
                
                if (!string.IsNullOrEmpty(parentPath))
                {
                    EnsureDirectoryExists(parentPath);
                }
                
                if (!string.IsNullOrEmpty(folderName) && !string.IsNullOrEmpty(parentPath))
                {
                    AssetDatabase.CreateFolder(parentPath, folderName);
                }
                else if (!string.IsNullOrEmpty(folderName))
                {
                    AssetDatabase.CreateFolder("Assets", folderName);
                }
            }
        }
        
        /// <summary>
        /// 获取项目中所有指定类型的资产
        /// </summary>
        public static List<T> FindAllAssetsOfType<T>() where T : UnityEngine.Object
        {
            var results = new List<T>();
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 安全地创建ScriptableObject资产
        /// </summary>
        public static T CreateAssetSafely<T>(string path, string defaultName = null) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel(
                    $"Create {typeof(T).Name}",
                    "Assets/Data",
                    defaultName ?? $"New{typeof(T).Name}",
                    "asset"
                );
                
                if (string.IsNullOrEmpty(path)) return null;
                
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            
            EnsureDirectoryExists(Path.GetDirectoryName(path));
            
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            return asset;
        }
        
        /// <summary>
        /// 复制ScriptableObject
        /// </summary>
        public static T DuplicateAsset<T>(T original, string newName = null) where T : ScriptableObject
        {
            if (original == null) return null;
            
            var originalPath = AssetDatabase.GetAssetPath(original);
            var newPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
            
            if (AssetDatabase.CopyAsset(originalPath, newPath))
            {
                AssetDatabase.SaveAssets();
                
                var duplicate = AssetDatabase.LoadAssetAtPath<T>(newPath);
                if (duplicate != null && !string.IsNullOrEmpty(newName))
                {
                    duplicate.name = newName;
                    EditorUtility.SetDirty(duplicate);
                }
                
                return duplicate;
            }
            
            return null;
        }
        
        /// <summary>
        /// 批量设置资产的脏标记并保存
        /// </summary>
        public static void MarkDirtyAndSave(params UnityEngine.Object[] assets)
        {
            if (assets == null) return;
            
            foreach (var asset in assets)
            {
                if (asset != null)
                {
                    EditorUtility.SetDirty(asset);
                }
            }
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// 显示进度条
        /// </summary>
        public static void ShowProgressBar(string title, string info, float progress)
        {
            EditorUtility.DisplayProgressBar(title, info, progress);
        }
        
        /// <summary>
        /// 隐藏进度条
        /// </summary>
        public static void HideProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // =========================
    // 敌人编辑器专用工具
    // =========================

    /// <summary>
    /// 敌人编辑器的导入导出工具
    /// </summary>
    public static class EnemyEditorImportExport
    {
        [System.Serializable]
        public class EnemyConfigExportData
        {
            public List<EnemyConfigData> configs = new();
            public string exportVersion = "1.0";
            public System.DateTime exportDate = System.DateTime.Now;
        }
        
        [System.Serializable]
        public class EnemyConfigData
        {
            public string enemyName;
            public EnemyType enemyType;
            public float health;
            public float patrolSpeed;
            public float chaseSpeed;
            public float rotationSpeed;
            public float visionRange;
            public float visionAngle;
            public float hearingRange;
            public float attackDamage;
            public float attackRange;
            public float attackCooldown;
            public float shootRange;
            public float shootInterval;
            public float shootAccuracy;
            public float alertDuration;
            public float investigationTime;
            public bool canOpenDoors;
            public bool canClimbStairs;
            public float dropChance;
        }
        
        /// <summary>
        /// 导出敌人配置到JSON文件
        /// </summary>
        public static void ExportConfigs(List<EnemyConfig> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Error", "No configs to export", "OK");
                return;
            }
            
            var exportPath = EditorUtility.SaveFilePanel(
                "Export Enemy Configs",
                Application.dataPath,
                "EnemyConfigs",
                "json"
            );
            
            if (string.IsNullOrEmpty(exportPath)) return;
            
            var exportData = new EnemyConfigExportData();
            
            foreach (var config in configs)
            {
                if (config == null) continue;
                
                var configData = new EnemyConfigData
                {
                    enemyName = config.enemyName,
                    enemyType = config.enemyType,
                    health = config.health,
                    patrolSpeed = config.patrolSpeed,
                    chaseSpeed = config.chaseSpeed,
                    rotationSpeed = config.rotationSpeed,
                    visionRange = config.visionRange,
                    visionAngle = config.visionAngle,
                    hearingRange = config.hearingRange,
                    attackDamage = config.attackDamage,
                    attackRange = config.attackRange,
                    attackCooldown = config.attackCooldown,
                    shootRange = config.shootRange,
                    shootInterval = config.shootInterval,
                    shootAccuracy = config.shootAccuracy,
                    alertDuration = config.alertDuration,
                    investigationTime = config.investigationTime,
                    canOpenDoors = config.canOpenDoors,
                    canClimbStairs = config.canClimbStairs,
                    dropChance = config.dropChance
                };
                
                exportData.configs.Add(configData);
            }
            
            try
            {
                var json = JsonUtility.ToJson(exportData, true);
                File.WriteAllText(exportPath, json);
                
                EditorUtility.DisplayDialog("Export Complete", 
                    $"Exported {configs.Count} enemy configs to {exportPath}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Error", 
                    $"Failed to export configs: {ex.Message}", "OK");
            }
        }
        
        /// <summary>
        /// 从JSON文件导入敌人配置
        /// </summary>
        public static List<EnemyConfig> ImportConfigs()
        {
            var importPath = EditorUtility.OpenFilePanel(
                "Import Enemy Configs",
                Application.dataPath,
                "json"
            );
            
            if (string.IsNullOrEmpty(importPath)) return new List<EnemyConfig>();
            
            try
            {
                var json = File.ReadAllText(importPath);
                var importData = JsonUtility.FromJson<EnemyConfigExportData>(json);
                var importedConfigs = new List<EnemyConfig>();
                
                if (importData?.configs == null)
                {
                    EditorUtility.DisplayDialog("Import Error", "Invalid file format", "OK");
                    return new List<EnemyConfig>();
                }
                
                EditorToolsUtilities.EnsureDirectoryExists(EditorToolsUtilities.ENEMIES_PATH);
                
                for (int i = 0; i < importData.configs.Count; i++)
                {
                    var configData = importData.configs[i];
                    
                    EditorToolsUtilities.ShowProgressBar("Importing Configs", 
                        $"Importing {configData.enemyName}...", 
                        (float)i / importData.configs.Count);
                    
                    var config = ScriptableObject.CreateInstance<EnemyConfig>();
                    
                    // 应用数据
                    config.enemyName = configData.enemyName;
                    config.enemyType = configData.enemyType;
                    config.health = configData.health;
                    config.patrolSpeed = configData.patrolSpeed;
                    config.chaseSpeed = configData.chaseSpeed;
                    config.rotationSpeed = configData.rotationSpeed;
                    config.visionRange = configData.visionRange;
                    config.visionAngle = configData.visionAngle;
                    config.hearingRange = configData.hearingRange;
                    config.attackDamage = configData.attackDamage;
                    config.attackRange = configData.attackRange;
                    config.attackCooldown = configData.attackCooldown;
                    config.shootRange = configData.shootRange;
                    config.shootInterval = configData.shootInterval;
                    config.shootAccuracy = configData.shootAccuracy;
                    config.alertDuration = configData.alertDuration;
                    config.investigationTime = configData.investigationTime;
                    config.canOpenDoors = configData.canOpenDoors;
                    config.canClimbStairs = configData.canClimbStairs;
                    config.dropChance = configData.dropChance;
                    
                    // 创建资产
                    var assetPath = $"{EditorToolsUtilities.ENEMIES_PATH}/Imported_{configData.enemyName}.asset";
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    
                    AssetDatabase.CreateAsset(config, assetPath);
                    importedConfigs.Add(config);
                }
                
                AssetDatabase.SaveAssets();
                EditorToolsUtilities.HideProgressBar();
                
                EditorUtility.DisplayDialog("Import Complete", 
                    $"Imported {importedConfigs.Count} enemy configs", "OK");
                
                return importedConfigs;
            }
            catch (System.Exception ex)
            {
                EditorToolsUtilities.HideProgressBar();
                EditorUtility.DisplayDialog("Import Error", 
                    $"Failed to import configs: {ex.Message}", "OK");
                return new List<EnemyConfig>();
            }
        }
    }

    /// <summary>
    /// 敌人编辑器的批量操作工具
    /// </summary>
    public static class EnemyBatchOperations
    {
        /// <summary>
        /// 批量调整敌人属性
        /// </summary>
        public static void BatchAdjustStats(List<EnemyConfig> configs, 
            float healthMultiplier = 1f, 
            float damageMultiplier = 1f, 
            float speedMultiplier = 1f)
        {
            if (configs == null || configs.Count == 0)
            {
                EditorUtility.DisplayDialog("Batch Operation", "No configs selected", "OK");
                return;
            }
            
            var proceed = EditorUtility.DisplayDialog("Batch Adjust Stats",
                $"This will modify {configs.Count} enemy configs.\n" +
                $"Health: ×{healthMultiplier:F2}\n" +
                $"Damage: ×{damageMultiplier:F2}\n" +
                $"Speed: ×{speedMultiplier:F2}\n\n" +
                "Continue?", "Yes", "Cancel");
            
            if (!proceed) return;
            
            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                if (config == null) continue;
                
                EditorToolsUtilities.ShowProgressBar("Adjusting Stats", 
                    $"Processing {config.enemyName}...", 
                    (float)i / configs.Count);
                
                config.health *= healthMultiplier;
                config.attackDamage *= damageMultiplier;
                config.patrolSpeed *= speedMultiplier;
                config.chaseSpeed *= speedMultiplier;
                
                EditorUtility.SetDirty(config);
            }
            
            AssetDatabase.SaveAssets();
            EditorToolsUtilities.HideProgressBar();
            
            EditorUtility.DisplayDialog("Batch Operation Complete", 
                $"Modified {configs.Count} enemy configs", "OK");
        }
        
        /// <summary>
        /// 批量设置敌人类型
        /// </summary>
        public static void BatchSetEnemyType(List<EnemyConfig> configs, EnemyType newType)
        {
            if (configs == null || configs.Count == 0)
            {
                EditorUtility.DisplayDialog("Batch Operation", "No configs selected", "OK");
                return;
            }
            
            var proceed = EditorUtility.DisplayDialog("Batch Set Type",
                $"This will change {configs.Count} enemy configs to type '{newType}'.\n\n" +
                "Continue?", "Yes", "Cancel");
            
            if (!proceed) return;
            
            foreach (var config in configs)
            {
                if (config == null) continue;
                
                config.enemyType = newType;
                EditorUtility.SetDirty(config);
            }
            
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Batch Operation Complete", 
                $"Changed {configs.Count} enemy configs to type '{newType}'", "OK");
        }
        
        /// <summary>
        /// 批量生成预制体
        /// </summary>
        public static void BatchCreatePrefabs(List<EnemyConfig> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                EditorUtility.DisplayDialog("Batch Operation", "No configs selected", "OK");
                return;
            }
            
            var prefabFolder = EditorUtility.SaveFolderPanel(
                "Select Prefab Folder", 
                "Assets/Prefabs", 
                "");
            
            if (string.IsNullOrEmpty(prefabFolder)) return;
            
            if (prefabFolder.StartsWith(Application.dataPath))
            {
                prefabFolder = "Assets" + prefabFolder.Substring(Application.dataPath.Length);
            }
            
            EditorToolsUtilities.EnsureDirectoryExists(prefabFolder);
            
            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                if (config == null) continue;
                
                EditorToolsUtilities.ShowProgressBar("Creating Prefabs", 
                    $"Creating prefab for {config.enemyName}...", 
                    (float)i / configs.Count);
                
                var prefab = new GameObject(config.enemyName);
                
                // 添加组件
                var enemyAI = prefab.AddComponent<EnemyAI>();
                enemyAI.enemyConfig = config;
                prefab.AddComponent<EnemyHealth>();
                
                // 添加NavMeshAgent（如果可用）
                var navMeshAgentType = System.Type.GetType("UnityEngine.AI.NavMeshAgent, UnityEngine.AIModule");
                if (navMeshAgentType != null)
                {
                    prefab.AddComponent(navMeshAgentType);
                }
                
                // 添加碰撞器
                var capsule = prefab.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                capsule.center = new Vector3(0, 1, 0);
                
                // 保存预制体
                var prefabPath = $"{prefabFolder}/{config.enemyName}.prefab";
                prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
                
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
               // DestroyImmediate(prefab);

            
            }
            
            AssetDatabase.SaveAssets();
            EditorToolsUtilities.HideProgressBar();
            
            EditorUtility.DisplayDialog("Batch Operation Complete", 
                $"Created {configs.Count} enemy prefabs in {prefabFolder}", "OK");
        }
    }

    // =========================
    // 事件编辑器专用工具
    // =========================

    /// <summary>
    /// 事件编辑器的导入导出工具
    /// </summary>
    public static class EventEditorImportExport
    {
        [System.Serializable]
        public class EventExportData
        {
            public List<EventData> events = new();
            public string exportVersion = "1.0";
            public System.DateTime exportDate = System.DateTime.Now;
        }
        
        [System.Serializable]
        public class EventData
        {
            public string eventName;
            public string eventDescription;
            public GameEventType eventType;
            public GameEventPriority priority;
            public int minDay;
            public int maxDay;
            public float baseTriggerChance;
            public bool canRepeat;
            public bool requiresChoice;
            public bool isQuest;
            public bool isMainQuest;
            public bool isSideQuest;
            public string questChain;
            public int questOrder;
            public string[] tags;
            public string authorNotes;
        }
        
        /// <summary>
        /// 导出事件到JSON文件
        /// </summary>
        public static void ExportEvents(List<RandomEvent> events)
        {
            if (events == null || events.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Error", "No events to export", "OK");
                return;
            }
            
            var exportPath = EditorUtility.SaveFilePanel(
                "Export Events",
                Application.dataPath,
                "GameEvents",
                "json"
            );
            
            if (string.IsNullOrEmpty(exportPath)) return;
            
            var exportData = new EventExportData();
            
            foreach (var evt in events)
            {
                if (evt == null) continue;
                
                var eventData = new EventData
                {
                    eventName = evt.eventName,
                    eventDescription = evt.eventDescription,
                    eventType = evt.eventType,
                    priority = evt.priority,
                    minDay = evt.minDay,
                    maxDay = evt.maxDay,
                    baseTriggerChance = evt.baseTriggerChance,
                    canRepeat = evt.canRepeat,
                    requiresChoice = evt.requiresChoice,
                    isQuest = evt.isQuest,
                    isMainQuest = evt.isMainQuest,
                    isSideQuest = evt.isSideQuest,
                    questChain = evt.questChain,
                    questOrder = evt.questOrder,
                    tags = evt.tags ?? new string[0],
                    //authorNotes = evt.authorNotes
                };
                
                exportData.events.Add(eventData);
            }
            
            try
            {
                var json = JsonUtility.ToJson(exportData, true);
                File.WriteAllText(exportPath, json);
                
                EditorUtility.DisplayDialog("Export Complete", 
                    $"Exported {events.Count} events to {exportPath}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Error", 
                    $"Failed to export events: {ex.Message}", "OK");
            }
        }
        
        /// <summary>
        /// 生成事件统计报告
        /// </summary>
        public static void GenerateStatisticsReport(List<RandomEvent> events)
        {
            if (events == null || events.Count == 0)
            {
                EditorUtility.DisplayDialog("Report Error", "No events to analyze", "OK");
                return;
            }
            
            var reportPath = EditorUtility.SaveFilePanel(
                "Save Statistics Report",
                Application.dataPath,
                "EventStatistics",
                "txt"
            );
            
            if (string.IsNullOrEmpty(reportPath)) return;
            
            var report = new StringBuilder();
            report.AppendLine("=== Game Events Statistics Report ===");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine($"Total Events: {events.Count}");
            report.AppendLine();
            
            // 按类型统计
            report.AppendLine("=== Events by Type ===");
            var typeGroups = events.Where(e => e != null).GroupBy(e => e.eventType);
            foreach (var group in typeGroups.OrderBy(g => g.Key))
            {
                report.AppendLine($"{group.Key}: {group.Count()} events");
            }
            report.AppendLine();
            
            // 按优先级统计
            report.AppendLine("=== Events by Priority ===");
            var priorityGroups = events.Where(e => e != null).GroupBy(e => e.priority);
            foreach (var group in priorityGroups.OrderBy(g => g.Key))
            {
                report.AppendLine($"{group.Key}: {group.Count()} events");
            }
            report.AppendLine();
            
            // 任务统计
            var questEvents = events.Where(e => e != null && e.isQuest).ToList();
            report.AppendLine("=== Quest Events ===");
            report.AppendLine($"Total Quest Events: {questEvents.Count}");
            report.AppendLine($"Main Quests: {questEvents.Count(e => e.isMainQuest)}");
            report.AppendLine($"Side Quests: {questEvents.Count(e => e.isSideQuest)}");
            report.AppendLine();
            
            // 任务链统计
            var questChains = questEvents.Where(e => !string.IsNullOrEmpty(e.questChain))
                .GroupBy(e => e.questChain);
            report.AppendLine("=== Quest Chains ===");
            foreach (var chain in questChains.OrderBy(c => c.Key))
            {
                report.AppendLine($"{chain.Key}: {chain.Count()} events");
            }
            report.AppendLine();
            
            // 触发概率统计
            report.AppendLine("=== Trigger Probability Distribution ===");
            var validEvents = events.Where(e => e != null).ToList();
            var lowProb = validEvents.Count(e => e.baseTriggerChance < 0.2f);
            var medProb = validEvents.Count(e => e.baseTriggerChance >= 0.2f && e.baseTriggerChance < 0.5f);
            var highProb = validEvents.Count(e => e.baseTriggerChance >= 0.5f);
            report.AppendLine($"Low (< 20%): {lowProb} events");
            report.AppendLine($"Medium (20-50%): {medProb} events");
            report.AppendLine($"High (> 50%): {highProb} events");
            report.AppendLine();
            
            // 选择统计
            var eventsWithChoices = validEvents.Where(e => e.choices != null && e.choices.Length > 0).ToList();
            report.AppendLine("=== Choice Statistics ===");
            report.AppendLine($"Events with Choices: {eventsWithChoices.Count}");
            if (eventsWithChoices.Count > 0)
            {
                var avgChoices = eventsWithChoices.Average(e => e.choices.Length);
                var maxChoices = eventsWithChoices.Max(e => e.choices.Length);
                report.AppendLine($"Average Choices per Event: {avgChoices:F1}");
                report.AppendLine($"Maximum Choices in Single Event: {maxChoices}");
            }
            report.AppendLine();
            
            // 日期范围统计
            report.AppendLine("=== Day Range Statistics ===");
            var minDayOverall = validEvents.Min(e => e.minDay);
            var maxDayOverall = validEvents.Max(e => e.maxDay);
            report.AppendLine($"Overall Day Range: {minDayOverall} - {maxDayOverall}");
            
            var earlyEvents = validEvents.Count(e => e.maxDay <= 10);
            var midEvents = validEvents.Count(e => e.minDay > 10 && e.maxDay <= 50);
            var lateEvents = validEvents.Count(e => e.minDay > 50);
            report.AppendLine($"Early Game (≤ day 10): {earlyEvents} events");
            report.AppendLine($"Mid Game (day 11-50): {midEvents} events");
            report.AppendLine($"Late Game (> day 50): {lateEvents} events");
            
            try
            {
                File.WriteAllText(reportPath, report.ToString());
                
                EditorUtility.DisplayDialog("Report Generated", 
                    $"Statistics report saved to: {reportPath}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Report Error", 
                    $"Failed to generate report: {ex.Message}", "OK");
            }
        }
    }

    // =========================
    // 模板系统
    // =========================

    /// <summary>
    /// 编辑器模板管理器
    /// </summary>
    public static class TemplateManager
    {
        /// <summary>
        /// 创建敌人配置模板
        /// </summary>
        public static void CreateEnemyConfigTemplate(EnemyType type, string templateName = null)
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            
            templateName = templateName ?? $"{type}Template";
            config.enemyName = templateName;
            config.enemyType = type;
            
            // 根据类型设置模板值
            switch (type)
            {
                case EnemyType.Zombie:
                    config.health = 50f;
                    config.patrolSpeed = 2f;
                    config.chaseSpeed = 4f;
                    config.attackDamage = 15f;
                    config.attackRange = 1.5f;
                    config.visionRange = 10f;
                    config.visionAngle = 60f;
                    break;
                    
                case EnemyType.Shooter:
                    config.health = 75f;
                    config.patrolSpeed = 3f;
                    config.chaseSpeed = 5f;
                    config.attackDamage = 20f;
                    config.attackRange = 15f;
                    config.shootRange = 20f;
                    config.shootAccuracy = 0.7f;
                    config.visionRange = 20f;
                    config.visionAngle = 90f;
                    break;
                    
                case EnemyType.Snipers:
                    config.health = 60f;
                    config.patrolSpeed = 2f;
                    config.chaseSpeed = 3f;
                    config.attackDamage = 40f;
                    config.attackRange = 30f;
                    config.shootRange = 35f;
                    config.shootAccuracy = 0.9f;
                    config.visionRange = 30f;
                    config.visionAngle = 45f;
                    break;
            }
            
            EditorToolsUtilities.EnsureDirectoryExists(EditorToolsUtilities.TEMPLATES_PATH);
            
            var path = $"{EditorToolsUtilities.TEMPLATES_PATH}/{templateName}.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Template Created", 
                $"Enemy config template '{templateName}' created at {path}", "OK");
        }
        
        /// <summary>
        /// 从模板创建敌人配置
        /// </summary>
        public static EnemyConfig CreateFromTemplate(EnemyConfig template, string newName)
        {
            if (template == null) return null;
            
            var newConfig = EditorToolsUtilities.DuplicateAsset(template, newName);
            if (newConfig != null)
            {
                newConfig.enemyName = newName;
                EditorUtility.SetDirty(newConfig);
                AssetDatabase.SaveAssets();
            }
            
            return newConfig;
        }
        
        /// <summary>
        /// 获取所有可用模板
        /// </summary>
        public static List<T> GetAllTemplates<T>() where T : ScriptableObject
        {
            var templates = new List<T>();
            
            if (AssetDatabase.IsValidFolder(EditorToolsUtilities.TEMPLATES_PATH))
            {
                var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", 
                    new[] { EditorToolsUtilities.TEMPLATES_PATH });
                
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var template = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (template != null)
                    {
                        templates.Add(template);
                    }
                }
            }
            
            return templates;
        }
    }

    // =========================
    // 编辑器设置管理
    // =========================

    /// <summary>
    /// 编辑器设置管理器
    /// </summary>
    [System.Serializable]
    public class EditorSettings
    {
        public bool autoSave = true;
        public float autoSaveInterval = 300f; // 5 minutes
        public bool showTooltips = true;
        public bool validateOnSave = true;
        public bool backupOnSave = false;
        public string defaultEnemyPath = "Assets/Data/Enemies";
        public string defaultEventPath = "Assets/Data/Events";
        public bool enableDebugLogging = false;
        
        private const string SETTINGS_KEY = "ModernEditorTools.Settings";
        
        public static EditorSettings Load()
        {
            var json = EditorPrefs.GetString(SETTINGS_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                return new EditorSettings();
            }
            
            try
            {
                return JsonUtility.FromJson<EditorSettings>(json);
            }
            catch
            {
                return new EditorSettings();
            }
        }
        
        public void Save()
        {
            var json = JsonUtility.ToJson(this, true);
            EditorPrefs.SetString(SETTINGS_KEY, json);
        }
    }

    /// <summary>
    /// 编辑器设置窗口
    /// </summary>
    public class EditorSettingsWindow : EditorWindow
    {
        private EditorSettings settings;
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/Modern Editor Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorSettingsWindow>("Editor Settings");
            window.minSize = new Vector2(400, 500);
        }
        
        private void OnEnable()
        {
            settings = EditorSettings.Load();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Modern Editor Tools Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 自动保存设置
            GUILayout.Label("Auto Save", EditorStyles.boldLabel);
            settings.autoSave = EditorGUILayout.Toggle("Enable Auto Save", settings.autoSave);
            GUI.enabled = settings.autoSave;
            settings.autoSaveInterval = EditorGUILayout.FloatField("Auto Save Interval (seconds)", settings.autoSaveInterval);
            GUI.enabled = true;
            
            GUILayout.Space(10);
            
            // 验证设置
            GUILayout.Label("Validation", EditorStyles.boldLabel);
            settings.validateOnSave = EditorGUILayout.Toggle("Validate on Save", settings.validateOnSave);
            settings.backupOnSave = EditorGUILayout.Toggle("Backup on Save", settings.backupOnSave);
            
            GUILayout.Space(10);
            
            // 界面设置
            GUILayout.Label("Interface", EditorStyles.boldLabel);
            settings.showTooltips = EditorGUILayout.Toggle("Show Tooltips", settings.showTooltips);
            
            GUILayout.Space(10);
            
            // 路径设置
            GUILayout.Label("Default Paths", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            settings.defaultEnemyPath = EditorGUILayout.TextField("Enemy Path", settings.defaultEnemyPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Enemy Folder", settings.defaultEnemyPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    settings.defaultEnemyPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            settings.defaultEventPath = EditorGUILayout.TextField("Event Path", settings.defaultEventPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Event Folder", settings.defaultEventPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    settings.defaultEventPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // 调试设置
            GUILayout.Label("Debug", EditorStyles.boldLabel);
            settings.enableDebugLogging = EditorGUILayout.Toggle("Enable Debug Logging", settings.enableDebugLogging);
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(20);
            
            // 底部按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Settings"))
            {
                settings.Save();
                EditorUtility.DisplayDialog("Settings", "Settings saved successfully!", "OK");
            }
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", 
                    "Are you sure you want to reset all settings to defaults?", "Yes", "No"))
                {
                    settings = new EditorSettings();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void OnDisable()
        {
            settings?.Save();
        }
    }

#endif