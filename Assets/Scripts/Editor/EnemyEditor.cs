#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class EnemyEditor : EditorWindow
    {
        // UI状态
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private int selectedTabIndex = 0;
    
        // 数据
        private List<EnemyConfig> allEnemyConfigs = new();
        private List<EnemyData> allEnemyData = new();
        private EnemyConfig selectedConfig;
        private EnemyData selectedData;
    
        // 分类
        private Dictionary<EnemyType, List<EnemyConfig>> configsByType = new();
        private Dictionary<EnemyType, List<EnemyData>> dataByType = new();
        private Dictionary<EnemyType, bool> typeFoldouts = new();
    
        // 预览
        private GameObject previewObject;
        private bool showPreview = true;
    
        // 搜索和过滤
        private string searchText = "";
        private EnemyType filterType = EnemyType.Zombie;
        private bool useTypeFilter = false;
    
        // 专业UI样式系统
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle cardStyle;
        private GUIStyle selectedCardStyle;
        private GUIStyle toolbarStyle;
        private GUIStyle buttonPrimaryStyle;
        private GUIStyle buttonSecondaryStyle;
        private GUIStyle buttonDangerStyle;
        private GUIStyle tabActiveStyle;
        private GUIStyle tabInactiveStyle;
        private GUIStyle sectionStyle;
        private GUIStyle labelStyle;
        private GUIStyle searchFieldStyle;
    
        // 专业配色方案
        private static readonly Color Primary = new Color(0.26f, 0.54f, 0.96f);      // 主色调 - 蓝色
        private static readonly Color PrimaryDark = new Color(0.21f, 0.43f, 0.77f);  // 主色调深色
        private static readonly Color Secondary = new Color(0.45f, 0.55f, 0.60f);    // 次要色
        private static readonly Color Success = new Color(0.30f, 0.69f, 0.31f);      // 成功色
        private static readonly Color Warning = new Color(0.96f, 0.61f, 0.07f);      // 警告色
        private static readonly Color Danger = new Color(0.86f, 0.21f, 0.27f);       // 危险色
        private static readonly Color Background = new Color(0.94f, 0.94f, 0.96f);   // 背景色
        private static readonly Color Surface = new Color(1f, 1f, 1f);               // 表面色
        private static readonly Color Border = new Color(0.86f, 0.86f, 0.88f);       // 边框色
        private static readonly Color TextPrimary = new Color(0.13f, 0.13f, 0.13f);  // 主文本
        private static readonly Color TextSecondary = new Color(0.46f, 0.46f, 0.46f); // 次要文本
    
        [MenuItem("Game Tools/Enemy Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<EnemyEditor>("Enemy Editor");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }
    
        void OnEnable()
        {
            LoadAllEnemyAssets();
            InitializeProfessionalStyles();
            InitializeFoldouts();
        }
    
        void OnDisable()
        {
            CleanupPreview();
        }
    
        void InitializeProfessionalStyles()
        {
            // 标题样式
            headerStyle = new GUIStyle(EditorStyles.largeLabel);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = TextPrimary;
            headerStyle.margin = new RectOffset(0, 0, 8, 12);
        
            // 子标题样式
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            subHeaderStyle.fontSize = 14;
            subHeaderStyle.normal.textColor = TextPrimary;
            subHeaderStyle.margin = new RectOffset(0, 0, 8, 8);
        
            // 卡片样式
            cardStyle = new GUIStyle();
            cardStyle.normal.background = CreateSolidTexture(Surface);
            cardStyle.border = new RectOffset(1, 1, 1, 1);
            cardStyle.padding = new RectOffset(16, 16, 12, 12);
            cardStyle.margin = new RectOffset(0, 0, 0, 8);
        
            // 选中卡片样式
            selectedCardStyle = new GUIStyle(cardStyle);
            selectedCardStyle.normal.background = CreateBorderedTexture(Surface, Primary, 2);
        
            // 工具栏样式
            toolbarStyle = new GUIStyle();
            toolbarStyle.normal.background = CreateSolidTexture(Surface);
            toolbarStyle.border = new RectOffset(0, 0, 0, 1);
            toolbarStyle.padding = new RectOffset(16, 16, 12, 12);
        
            // 主按钮样式
            buttonPrimaryStyle = CreateButtonStyle(Primary, Color.white);
        
            // 次要按钮样式  
            buttonSecondaryStyle = CreateButtonStyle(Secondary, Color.white);
        
            // 危险按钮样式
            buttonDangerStyle = CreateButtonStyle(Danger, Color.white);
        
            // 活动标签样式
            tabActiveStyle = new GUIStyle();
            tabActiveStyle.normal.background = CreateSolidTexture(Primary);
            tabActiveStyle.normal.textColor = Color.white;
            tabActiveStyle.padding = new RectOffset(16, 16, 8, 8);
            tabActiveStyle.margin = new RectOffset(0, 1, 0, 0);
            tabActiveStyle.alignment = TextAnchor.MiddleCenter;
            tabActiveStyle.fontStyle = FontStyle.Bold;
        
            // 非活动标签样式
            tabInactiveStyle = new GUIStyle();
            tabInactiveStyle.normal.background = CreateSolidTexture(Background);
            tabInactiveStyle.normal.textColor = TextSecondary;
            tabInactiveStyle.padding = new RectOffset(16, 16, 8, 8);
            tabInactiveStyle.margin = new RectOffset(0, 1, 0, 0);
            tabInactiveStyle.alignment = TextAnchor.MiddleCenter;
        
            // 区域样式
            sectionStyle = new GUIStyle();
            sectionStyle.normal.background = CreateSolidTexture(Background);
            sectionStyle.padding = new RectOffset(16, 16, 16, 16);
        
            // 标签样式
            labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = TextPrimary;
        
            // 搜索框样式
            searchFieldStyle = new GUIStyle(EditorStyles.textField);
            searchFieldStyle.padding = new RectOffset(8, 8, 6, 6);
        }
    
        GUIStyle CreateButtonStyle(Color bgColor, Color textColor)
        {
            var style = new GUIStyle();
            style.normal.background = CreateSolidTexture(bgColor);
            style.hover.background = CreateSolidTexture(AdjustBrightness(bgColor, 1.1f));
            style.active.background = CreateSolidTexture(AdjustBrightness(bgColor, 0.9f));
            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            style.padding = new RectOffset(16, 16, 8, 8);
            style.margin = new RectOffset(0, 4, 0, 0);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            return style;
        }
    
        Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    
        Texture2D CreateBorderedTexture(Color fillColor, Color borderColor, int borderWidth)
        {
            int size = 20;
            Texture2D texture = new Texture2D(size, size);
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    bool isBorder = x < borderWidth || x >= size - borderWidth || 
                                   y < borderWidth || y >= size - borderWidth;
                    texture.SetPixel(x, y, isBorder ? borderColor : fillColor);
                }
            }
            texture.Apply();
            return texture;
        }
    
        Color AdjustBrightness(Color color, float factor)
        {
            return new Color(
                Mathf.Clamp01(color.r * factor),
                Mathf.Clamp01(color.g * factor),
                Mathf.Clamp01(color.b * factor),
                color.a
            );
        }
    
        void LoadAllEnemyAssets()
        {
            // 加载EnemyConfig
            allEnemyConfigs.Clear();
            configsByType.Clear();
        
            string[] configGuids = AssetDatabase.FindAssets("t:EnemyConfig");
            foreach (string guid in configGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
                if (config != null)
                {
                    allEnemyConfigs.Add(config);
                
                    if (!configsByType.ContainsKey(config.enemyType))
                        configsByType[config.enemyType] = new List<EnemyConfig>();
                    configsByType[config.enemyType].Add(config);
                }
            }
        
            // 加载EnemyData
            allEnemyData.Clear();
            dataByType.Clear();
        
            string[] dataGuids = AssetDatabase.FindAssets("t:EnemyData");
            foreach (string guid in dataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (data != null)
                {
                    allEnemyData.Add(data);
                
                    if (!dataByType.ContainsKey(data.enemyType))
                        dataByType[data.enemyType] = new List<EnemyData>();
                    dataByType[data.enemyType].Add(data);
                }
            }
        
            Debug.Log($"[EnemyEditor] Loaded {allEnemyConfigs.Count} configs, {allEnemyData.Count} data assets");
        }
    
        void InitializeFoldouts()
        {
            foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
            {
                typeFoldouts[type] = true;
            }
        }
    
        void OnGUI()
        {
            // 设置背景色
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), Background);
        
            DrawToolbar();
        
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
        
            // 左侧面板
            EditorGUILayout.BeginVertical(sectionStyle, GUILayout.Width(350), GUILayout.ExpandHeight(true));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();
        
            // 分割线
            EditorGUI.DrawRect(new Rect(366, 50, 1, position.height - 50), Border);
        
            // 右侧面板
            EditorGUILayout.BeginVertical(sectionStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightPanel();
            EditorGUILayout.EndVertical();
        
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(toolbarStyle, GUILayout.Height(50));
        
            // 主要操作
            if (GUILayout.Button("New Config", buttonPrimaryStyle, GUILayout.Height(32)))
            {
                CreateNewEnemyConfig();
            }
        
            if (GUILayout.Button("New Data", buttonSecondaryStyle, GUILayout.Height(32)))
            {
                CreateNewEnemyData();
            }
        
            GUILayout.Space(16);
        
            // 工具操作
            if (GUILayout.Button("Refresh", buttonSecondaryStyle, GUILayout.Width(80), GUILayout.Height(32)))
            {
                LoadAllEnemyAssets();
            }
        
            if (GUILayout.Button("Save All", buttonSecondaryStyle, GUILayout.Width(80), GUILayout.Height(32)))
            {
                SaveAllAssets();
            }
        
            GUILayout.FlexibleSpace();
        
            // 搜索区域
            DrawSearchBar();
        
            GUILayout.Space(16);
        
            // 预览切换
            var prevPreview = showPreview;
            showPreview = GUILayout.Toggle(showPreview, "Preview", "Button", GUILayout.Width(80), GUILayout.Height(32));
            if (prevPreview != showPreview)
            {
                UpdatePreview();
            }
        
            EditorGUILayout.EndHorizontal();
        
            // 工具栏底部边框
            EditorGUI.DrawRect(new Rect(0, 50, position.width, 1), Border);
        }
    
        void DrawSearchBar()
        {
            EditorGUILayout.BeginVertical();
        
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            
            var newSearchText = EditorGUILayout.TextField(searchText, searchFieldStyle, GUILayout.Width(150));
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
            }
        
            if (!string.IsNullOrEmpty(searchText))
            {
                if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    searchText = "";
                }
            }
            EditorGUILayout.EndHorizontal();
        
            // 过滤器
            EditorGUILayout.BeginHorizontal();
            useTypeFilter = GUILayout.Toggle(useTypeFilter, "Filter:", GUILayout.Width(50));
            
            GUI.enabled = useTypeFilter;
            filterType = (EnemyType)EditorGUILayout.EnumPopup(filterType, GUILayout.Width(100));
            GUI.enabled = true;
            
            if (useTypeFilter)
            {
                if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    useTypeFilter = false;
                }
            }
            EditorGUILayout.EndHorizontal();
        
            EditorGUILayout.EndVertical();
        }
    
        void DrawLeftPanel()
        {
            GUILayout.Label("Asset Management", headerStyle);
        
            // 标签页
            EditorGUILayout.BeginHorizontal();
            string[] tabs = { "Configs", "Data" };
        
            for (int i = 0; i < tabs.Length; i++)
            {
                var style = (selectedTabIndex == i) ? tabActiveStyle : tabInactiveStyle;
                if (GUILayout.Button(tabs[i], style, GUILayout.Height(32)))
                {
                    selectedTabIndex = i;
                }
            }
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(16);
        
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
        
            switch (selectedTabIndex)
            {
                case 0: DrawConfigList(); break;
                case 1: DrawDataList(); break;
            }
        
            EditorGUILayout.EndScrollView();
        
            GUILayout.Space(16);
            DrawQuickCreate();
        }
    
        void DrawConfigList()
        {
            var filteredTypes = GetFilteredEnemyTypes();
        
            foreach (EnemyType type in filteredTypes)
            {
                if (!configsByType.ContainsKey(type) || configsByType[type].Count == 0)
                    continue;
            
                var configs = GetFilteredConfigs(configsByType[type]);
                if (configs.Count == 0) continue;
            
                EditorGUILayout.BeginVertical(cardStyle);
            
                // 类型标题行
                EditorGUILayout.BeginHorizontal();
            
                var foldoutRect = GUILayoutUtility.GetRect(20, 20);
                typeFoldouts[type] = EditorGUI.Foldout(foldoutRect, typeFoldouts[type], "", true);
            
                GUILayout.Label($"{GetTypeDisplayName(type)}", subHeaderStyle);
            
                GUILayout.FlexibleSpace();
            
                // 数量标签
                var countStyle = new GUIStyle(EditorStyles.miniLabel);
                countStyle.normal.textColor = TextSecondary;
                GUILayout.Label($"({configs.Count})", countStyle);
            
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    CreateEnemyConfigOfType(type);
                }
            
                EditorGUILayout.EndHorizontal();
            
                if (typeFoldouts[type])
                {
                    GUILayout.Space(4);
                    foreach (var config in configs)
                    {
                        DrawConfigItem(config);
                    }
                }
            
                EditorGUILayout.EndVertical();
            }
        }
    
        void DrawDataList()
        {
            var filteredTypes = GetFilteredEnemyTypes();
        
            foreach (EnemyType type in filteredTypes)
            {
                if (!dataByType.ContainsKey(type) || dataByType[type].Count == 0)
                    continue;
            
                var data = GetFilteredData(dataByType[type]);
                if (data.Count == 0) continue;
            
                EditorGUILayout.BeginVertical(cardStyle);
            
                EditorGUILayout.BeginHorizontal();
            
                var foldoutRect = GUILayoutUtility.GetRect(20, 20);
                typeFoldouts[type] = EditorGUI.Foldout(foldoutRect, typeFoldouts[type], "", true);
            
                GUILayout.Label($"{GetTypeDisplayName(type)}", subHeaderStyle);
            
                GUILayout.FlexibleSpace();
            
                var countStyle = new GUIStyle(EditorStyles.miniLabel);
                countStyle.normal.textColor = TextSecondary;
                GUILayout.Label($"({data.Count})", countStyle);
            
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    CreateEnemyDataOfType(type);
                }
            
                EditorGUILayout.EndHorizontal();
            
                if (typeFoldouts[type])
                {
                    GUILayout.Space(4);
                    foreach (var dataItem in data)
                    {
                        DrawDataItem(dataItem);
                    }
                }
            
                EditorGUILayout.EndVertical();
            }
        }
    
        void DrawConfigItem(EnemyConfig config)
        {
            bool isSelected = selectedConfig == config;
        
            var itemStyle = isSelected ? selectedCardStyle : new GUIStyle();
            itemStyle.padding = new RectOffset(12, 12, 8, 8);
            itemStyle.margin = new RectOffset(0, 0, 1, 1);
        
            EditorGUILayout.BeginVertical(itemStyle);
        
            EditorGUILayout.BeginHorizontal();
        
            // 主按钮
            if (GUILayout.Button(config.enemyName, labelStyle))
            {
                selectedConfig = config;
                selectedData = null;
                UpdatePreview();
            }
        
            GUILayout.FlexibleSpace();
        
            // 操作按钮
            if (GUILayout.Button("⋯", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                ShowConfigContextMenu(config);
            }
        
            EditorGUILayout.EndHorizontal();
        
            if (isSelected)
            {
                GUILayout.Space(4);
                DrawConfigPreviewStats(config);
            }
        
            EditorGUILayout.EndVertical();
        }
    
        void DrawDataItem(EnemyData data)
        {
            bool isSelected = selectedData == data;
        
            var itemStyle = isSelected ? selectedCardStyle : new GUIStyle();
            itemStyle.padding = new RectOffset(12, 12, 8, 8);
            itemStyle.margin = new RectOffset(0, 0, 1, 1);
        
            EditorGUILayout.BeginVertical(itemStyle);
        
            EditorGUILayout.BeginHorizontal();
        
            if (GUILayout.Button(data.enemyName, labelStyle))
            {
                selectedData = data;
                selectedConfig = null;
                UpdatePreview();
            }
        
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button("⋯", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                ShowDataContextMenu(data);
            }
        
            EditorGUILayout.EndHorizontal();
        
            if (isSelected)
            {
                GUILayout.Space(4);
                DrawDataPreviewStats(data);
            }
        
            EditorGUILayout.EndVertical();
        }
    
        void DrawConfigPreviewStats(EnemyConfig config)
        {
            var miniStyle = new GUIStyle(EditorStyles.miniLabel);
            miniStyle.normal.textColor = TextSecondary;
        
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Health: {config.health}", miniStyle, GUILayout.Width(80));
            GUILayout.Label($"Damage: {config.attackDamage}", miniStyle, GUILayout.Width(80));
            GUILayout.Label($"Speed: {config.chaseSpeed}", miniStyle);
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawDataPreviewStats(EnemyData data)
        {
            var miniStyle = new GUIStyle(EditorStyles.miniLabel);
            miniStyle.normal.textColor = TextSecondary;
        
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Health: {data.health}", miniStyle, GUILayout.Width(80));
            GUILayout.Label($"Armor: {data.armor}", miniStyle, GUILayout.Width(80));
            GUILayout.Label($"Speed: {data.moveSpeed}", miniStyle);
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawQuickCreate()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Quick Create", subHeaderStyle);
        
            GUILayout.Label("Config Templates:", labelStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Zombie", buttonSecondaryStyle, GUILayout.Height(28))) CreateEnemyConfigOfType(EnemyType.Zombie);
            if (GUILayout.Button("Shooter", buttonSecondaryStyle, GUILayout.Height(28))) CreateEnemyConfigOfType(EnemyType.Shooter);
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(8);
        
            GUILayout.Label("Data Templates:", labelStyle);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Zombie Data", buttonSecondaryStyle, GUILayout.Height(28))) CreateEnemyDataOfType(EnemyType.Zombie);
            if (GUILayout.Button("Shooter Data", buttonSecondaryStyle, GUILayout.Height(28))) CreateEnemyDataOfType(EnemyType.Shooter);
            EditorGUILayout.EndHorizontal();
        
            EditorGUILayout.EndVertical();
        }
    
        void DrawRightPanel()
        {
            if (selectedConfig != null)
            {
                DrawConfigDetails();
            }
            else if (selectedData != null)
            {
                DrawDataDetails();
            }
            else
            {
                DrawWelcomePanel();
            }
        }
    
        void DrawWelcomePanel()
        {
            GUILayout.FlexibleSpace();
        
            EditorGUILayout.BeginVertical(cardStyle);
        
            // 标题
            var titleStyle = new GUIStyle(headerStyle);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 24;
            GUILayout.Label("Enemy Editor", titleStyle);
        
            GUILayout.Space(20);
        
            // 功能介绍
            GUILayout.Label("Features", subHeaderStyle);
            GUILayout.Space(8);
        
            GUILayout.Label("• EnemyConfig: Game logic configuration (AI behavior, stats)", labelStyle);
            GUILayout.Label("• EnemyData: Doom-style data (sprites, audio)", labelStyle);
            GUILayout.Label("• Select enemies from the left panel to edit", labelStyle);
            GUILayout.Label("• Use quick create for new enemy templates", labelStyle);
        
            GUILayout.Space(20);
        
            GUILayout.Label("Enemy Types", subHeaderStyle);
            GUILayout.Space(8);
        
            GUILayout.Label("• Zombie: Melee enemies", labelStyle);
            GUILayout.Label("• Shooter: Ranged enemies", labelStyle);
            GUILayout.Label("• Snipers: Precision shooting", labelStyle);
        
            GUILayout.Space(30);
        
            // 快速开始按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button("Create First Enemy", buttonPrimaryStyle, GUILayout.Width(180), GUILayout.Height(40)))
            {
                selectedTabIndex = 0;
                CreateNewEnemyConfig();
            }
        
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        
            EditorGUILayout.EndVertical();
        
            GUILayout.FlexibleSpace();
        }
    
        void DrawConfigDetails()
        {
            // 标题栏
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Configuration: {selectedConfig.enemyName}", headerStyle);
        
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button("Save", buttonPrimaryStyle, GUILayout.Width(60), GUILayout.Height(32)))
            {
                EditorUtility.SetDirty(selectedConfig);
                AssetDatabase.SaveAssets();
            }
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(16);
        
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        
            EditorGUI.BeginChangeCheck();
        
            // 基础属性卡片
            DrawSection("Basic Properties", () => {
                selectedConfig.enemyName = EditorGUILayout.TextField("Enemy Name", selectedConfig.enemyName);
                selectedConfig.enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", selectedConfig.enemyType);
                selectedConfig.health = EditorGUILayout.FloatField("Health", selectedConfig.health);
            });
        
            // 移动设置卡片
            DrawSection("Movement Settings", () => {
                selectedConfig.patrolSpeed = EditorGUILayout.FloatField("Patrol Speed", selectedConfig.patrolSpeed);
                selectedConfig.chaseSpeed = EditorGUILayout.FloatField("Chase Speed", selectedConfig.chaseSpeed);
                selectedConfig.rotationSpeed = EditorGUILayout.FloatField("Rotation Speed", selectedConfig.rotationSpeed);
            });
        
            // 感知系统卡片
            DrawSection("Detection System", () => {
                selectedConfig.visionRange = EditorGUILayout.FloatField("Vision Range", selectedConfig.visionRange);
                selectedConfig.visionAngle = EditorGUILayout.FloatField("Vision Angle", selectedConfig.visionAngle);
                selectedConfig.hearingRange = EditorGUILayout.FloatField("Hearing Range", selectedConfig.hearingRange);
            });
        
            // 攻击设置卡片
            DrawSection("Combat Settings", () => {
                selectedConfig.attackDamage = EditorGUILayout.FloatField("Attack Damage", selectedConfig.attackDamage);
                selectedConfig.attackRange = EditorGUILayout.FloatField("Attack Range", selectedConfig.attackRange);
                selectedConfig.attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", selectedConfig.attackCooldown);
            });
        
            // 射击设置（条件显示）
            if (selectedConfig.enemyType == EnemyType.Shooter || selectedConfig.enemyType == EnemyType.Snipers)
            {
                DrawSection("Shooting Settings", () => {
                    selectedConfig.shootRange = EditorGUILayout.FloatField("Shoot Range", selectedConfig.shootRange);
                    selectedConfig.shootInterval = EditorGUILayout.FloatField("Shoot Interval", selectedConfig.shootInterval);
                    selectedConfig.shootAccuracy = EditorGUILayout.Slider("Shoot Accuracy", selectedConfig.shootAccuracy, 0f, 1f);
                });
            }
        
            // AI行为卡片
            DrawSection("AI Behavior", () => {
                selectedConfig.alertDuration = EditorGUILayout.FloatField("Alert Duration", selectedConfig.alertDuration);
                selectedConfig.investigationTime = EditorGUILayout.FloatField("Investigation Time", selectedConfig.investigationTime);
                selectedConfig.canOpenDoors = EditorGUILayout.Toggle("Can Open Doors", selectedConfig.canOpenDoors);
                selectedConfig.canClimbStairs = EditorGUILayout.Toggle("Can Climb Stairs", selectedConfig.canClimbStairs);
            });
        
            // 掉落设置卡片
            DrawSection("Drop Settings", () => {
                SerializedObject serializedConfig = new SerializedObject(selectedConfig);
                SerializedProperty dropItemsProperty = serializedConfig.FindProperty("dropItems");
                EditorGUILayout.PropertyField(dropItemsProperty, new GUIContent("Drop Items"), true);
            
                selectedConfig.dropChance = EditorGUILayout.Slider("Drop Chance", selectedConfig.dropChance, 0f, 1f);
            
                serializedConfig.ApplyModifiedProperties();
            });
        
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedConfig);
            }
        
            EditorGUILayout.EndScrollView();
        
            GUILayout.Space(16);
            DrawConfigBottomButtons();
        }
    
        void DrawDataDetails()
        {
            // 标题栏
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Data: {selectedData.enemyName}", headerStyle);
        
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button("Save", buttonPrimaryStyle, GUILayout.Width(60), GUILayout.Height(32)))
            {
                EditorUtility.SetDirty(selectedData);
                AssetDatabase.SaveAssets();
            }
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(16);
        
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        
            EditorGUI.BeginChangeCheck();
        
            // 各个设置区域...
            DrawSection("Basic Information", () => {
                selectedData.enemyName = EditorGUILayout.TextField("Enemy Name", selectedData.enemyName);
                selectedData.enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", selectedData.enemyType);
                selectedData.health = EditorGUILayout.FloatField("Health", selectedData.health);
                selectedData.armor = EditorGUILayout.FloatField("Armor", selectedData.armor);
            });
        
            DrawSection("Movement Settings", () => {
                selectedData.moveSpeed = EditorGUILayout.FloatField("Move Speed", selectedData.moveSpeed);
                selectedData.chaseSpeed = EditorGUILayout.FloatField("Chase Speed", selectedData.chaseSpeed);
                selectedData.rotationSpeed = EditorGUILayout.FloatField("Rotation Speed", selectedData.rotationSpeed);
                selectedData.canFly = EditorGUILayout.Toggle("Can Fly", selectedData.canFly);
            });
        
            DrawSection("AI Behavior", () => {
                selectedData.detectionRange = EditorGUILayout.FloatField("Detection Range", selectedData.detectionRange);
                selectedData.attackRange = EditorGUILayout.FloatField("Attack Range", selectedData.attackRange);
                selectedData.loseTargetTime = EditorGUILayout.FloatField("Lose Target Time", selectedData.loseTargetTime);
                selectedData.alwaysHostile = EditorGUILayout.Toggle("Always Hostile", selectedData.alwaysHostile);
                selectedData.canOpenDoors = EditorGUILayout.Toggle("Can Open Doors", selectedData.canOpenDoors);
                selectedData.immuneToInfighting = EditorGUILayout.Toggle("Immune To Infighting", selectedData.immuneToInfighting);
                selectedData.painChance = EditorGUILayout.Slider("Pain Chance", selectedData.painChance, 0f, 1f);
            });
        
            DrawSection("Combat Settings", () => {
                selectedData.attackDamage = EditorGUILayout.FloatField("Attack Damage", selectedData.attackDamage);
                selectedData.attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", selectedData.attackCooldown);
            
                SerializedObject serializedData = new SerializedObject(selectedData);
                SerializedProperty attackTypesProperty = serializedData.FindProperty("attackTypes");
                EditorGUILayout.PropertyField(attackTypesProperty, new GUIContent("Attack Types"), true);
                serializedData.ApplyModifiedProperties();
            });
        
            DrawSection("Sprite Animation", () => {
                SerializedObject serializedData = new SerializedObject(selectedData);
                SerializedProperty spriteSetProperty = serializedData.FindProperty("spriteSet");
                EditorGUILayout.PropertyField(spriteSetProperty, new GUIContent("Sprite Set"), true);
                serializedData.ApplyModifiedProperties();
            });
        
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedData);
            }
        
            EditorGUILayout.EndScrollView();
        
            GUILayout.Space(16);
            DrawDataBottomButtons();
        }
    
        void DrawSection(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(cardStyle);
        
            GUILayout.Label(title, subHeaderStyle);
            GUILayout.Space(8);
        
            content?.Invoke();
        
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }
    
        void DrawConfigBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();
        
            if (GUILayout.Button("Save", buttonPrimaryStyle, GUILayout.Height(32)))
            {
                EditorUtility.SetDirty(selectedConfig);
                AssetDatabase.SaveAssets();
            }
        
            if (GUILayout.Button("Duplicate", buttonSecondaryStyle, GUILayout.Height(32)))
            {
                DuplicateConfig(selectedConfig);
            }
        
            if (GUILayout.Button("Create Prefab", buttonSecondaryStyle, GUILayout.Height(32)))
            {
                CreateEnemyPrefab(selectedConfig);
            }
        
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button("Delete", buttonDangerStyle, GUILayout.Height(32)))
            {
                DeleteConfig(selectedConfig);
            }
        
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawDataBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();
        
            if (GUILayout.Button("Save", buttonPrimaryStyle, GUILayout.Height(32)))
            {
                EditorUtility.SetDirty(selectedData);
                AssetDatabase.SaveAssets();
            }
        
            if (GUILayout.Button("Duplicate", buttonSecondaryStyle, GUILayout.Height(32)))
            {
                DuplicateData(selectedData);
            }
        
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button("Delete", buttonDangerStyle, GUILayout.Height(32)))
            {
                DeleteData(selectedData);
            }
        
            EditorGUILayout.EndHorizontal();
        }
    
        // 辅助方法保持不变...
        List<EnemyType> GetFilteredEnemyTypes()
        {
            var types = System.Enum.GetValues(typeof(EnemyType)).Cast<EnemyType>().ToList();
            if (useTypeFilter)
            {
                types = types.Where(t => t == filterType).ToList();
            }
            return types;
        }
    
        List<EnemyConfig> GetFilteredConfigs(List<EnemyConfig> configs)
        {
            if (string.IsNullOrEmpty(searchText)) return configs;
        
            return configs.Where(c => c.enemyName.ToLower().Contains(searchText.ToLower())).ToList();
        }
    
        List<EnemyData> GetFilteredData(List<EnemyData> data)
        {
            if (string.IsNullOrEmpty(searchText)) return data;
        
            return data.Where(d => d.enemyName.ToLower().Contains(searchText.ToLower())).ToList();
        }
    
        string GetTypeDisplayName(EnemyType type)
        {
            return type switch
            {
                EnemyType.Zombie => "Zombie",
                EnemyType.Shooter => "Shooter",
                EnemyType.Snipers => "Sniper",
                _ => type.ToString()
            };
        }
    
        // 保持所有原有功能方法...
        void UpdatePreview()
        {
            if (!showPreview) return;
        
            CleanupPreview();
        
            if (selectedConfig != null)
            {
                CreateConfigPreview();
            }
            else if (selectedData != null)
            {
                CreateDataPreview();
            }
        }
    
        void CreateConfigPreview()
        {
            previewObject = new GameObject($"Preview_{selectedConfig.enemyName}");
            previewObject.AddComponent<EnemyAI>().enemyConfig = selectedConfig;
            previewObject.AddComponent<EnemyHealth>();
        
            var renderer = previewObject.AddComponent<MeshRenderer>();
            var filter = previewObject.AddComponent<MeshFilter>();
            filter.mesh = CreateSimpleMesh();
        
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = selectedConfig.enemyType switch
            {
                EnemyType.Zombie => Color.green,
                EnemyType.Shooter => Color.red,
                EnemyType.Snipers => Color.blue,
                _ => Color.gray
            };
            renderer.material = mat;
        
            previewObject.transform.position = Vector3.zero;
        }
    
        void CreateDataPreview()
        {
            previewObject = new GameObject($"Preview_{selectedData.enemyName}");
            previewObject.AddComponent<Enemy>().enemyData = selectedData;
        
            if (selectedData.spriteSet?.idleSprites != null && selectedData.spriteSet.idleSprites.Length > 0)
            {
                var spriteRenderer = previewObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = selectedData.spriteSet.idleSprites[0];
            }
        
            previewObject.transform.position = Vector3.zero;
        }
    
        Mesh CreateSimpleMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 1, -0.5f), new Vector3(-0.5f, 1, -0.5f),
                new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 1, 0.5f), new Vector3(-0.5f, 1, 0.5f)
            };
        
            mesh.triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 5, 2, 6, 0, 7, 4, 0, 3, 7, 3, 6, 7, 3, 2, 6, 0, 4, 5, 0, 5, 1
            };
        
            mesh.RecalculateNormals();
            return mesh;
        }
    
        void CleanupPreview()
        {
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
                previewObject = null;
            }
        }
    
        // 保持所有原有的创建、删除、复制等方法...
        void CreateNewEnemyConfig() => CreateEnemyConfigOfType(EnemyType.Zombie);
        void CreateNewEnemyData() => CreateEnemyDataOfType(EnemyType.Zombie);
    
        void CreateEnemyConfigOfType(EnemyType type)
        {
            string path = EditorUtility.SaveFilePanel("Create Enemy Config", "Assets/Data/Enemies", $"New{type}Config", "asset");
            if (string.IsNullOrEmpty(path)) return;
        
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);
        
            EnemyConfig newConfig = CreateInstance<EnemyConfig>();
            newConfig.enemyName = $"New {GetTypeDisplayName(type)}";
            newConfig.enemyType = type;
        
            SetConfigDefaults(newConfig, type);
        
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();
        
            LoadAllEnemyAssets();
            selectedConfig = newConfig;
            selectedData = null;
            UpdatePreview();
        }
    
        void CreateEnemyDataOfType(EnemyType type)
        {
            string path = EditorUtility.SaveFilePanel("Create Enemy Data", "Assets/Data/Enemies", $"New{type}Data", "asset");
            if (string.IsNullOrEmpty(path)) return;
        
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);
        
            EnemyData newData = CreateInstance<EnemyData>();
            newData.enemyName = $"New {GetTypeDisplayName(type)}";
            newData.enemyType = type;
        
            SetDataDefaults(newData, type);
        
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();
        
            LoadAllEnemyAssets();
            selectedData = newData;
            selectedConfig = null;
            UpdatePreview();
        }
    
        void SetConfigDefaults(EnemyConfig config, EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Zombie:
                    config.health = 50f; config.patrolSpeed = 2f; config.chaseSpeed = 4f;
                    config.attackDamage = 15f; config.attackRange = 1.5f;
                    config.visionRange = 10f; config.visionAngle = 60f;
                    break;
                case EnemyType.Shooter:
                    config.health = 75f; config.patrolSpeed = 3f; config.chaseSpeed = 5f;
                    config.attackDamage = 20f; config.attackRange = 15f;
                    config.shootRange = 20f; config.shootAccuracy = 0.7f;
                    config.visionRange = 20f; config.visionAngle = 90f;
                    break;
                case EnemyType.Snipers:
                    config.health = 60f; config.patrolSpeed = 2f; config.chaseSpeed = 3f;
                    config.attackDamage = 40f; config.attackRange = 30f;
                    config.shootRange = 35f; config.shootAccuracy = 0.9f;
                    config.visionRange = 30f; config.visionAngle = 45f;
                    break;
            }
        }
    
        void SetDataDefaults(EnemyData data, EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Zombie:
                    data.health = 50f; data.moveSpeed = 2f; data.chaseSpeed = 4f;
                    data.attackDamage = 15f; data.detectionRange = 10f; data.attackRange = 1.5f;
                    break;
                case EnemyType.Shooter:
                    data.health = 75f; data.moveSpeed = 3f; data.chaseSpeed = 5f;
                    data.attackDamage = 20f; data.detectionRange = 20f; data.attackRange = 15f;
                    break;
                case EnemyType.Snipers:
                    data.health = 60f; data.moveSpeed = 2f; data.chaseSpeed = 3f;
                    data.attackDamage = 40f; data.detectionRange = 30f; data.attackRange = 25f;
                    break;
            }
        }
    
        void DuplicateConfig(EnemyConfig original)
        {
            string originalPath = AssetDatabase.GetAssetPath(original);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
        
            if (AssetDatabase.CopyAsset(originalPath, newPath))
            {
                AssetDatabase.SaveAssets();
                LoadAllEnemyAssets();
            
                EnemyConfig duplicate = AssetDatabase.LoadAssetAtPath<EnemyConfig>(newPath);
                if (duplicate != null)
                {
                    duplicate.enemyName += " (Copy)";
                    EditorUtility.SetDirty(duplicate);
                    selectedConfig = duplicate;
                    UpdatePreview();
                }
            }
        }
    
        void DuplicateData(EnemyData original)
        {
            string originalPath = AssetDatabase.GetAssetPath(original);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
        
            if (AssetDatabase.CopyAsset(originalPath, newPath))
            {
                AssetDatabase.SaveAssets();
                LoadAllEnemyAssets();
            
                EnemyData duplicate = AssetDatabase.LoadAssetAtPath<EnemyData>(newPath);
                if (duplicate != null)
                {
                    duplicate.enemyName += " (Copy)";
                    EditorUtility.SetDirty(duplicate);
                    selectedData = duplicate;
                    UpdatePreview();
                }
            }
        }
    
        void CreateEnemyPrefab(EnemyConfig config)
        {
            string path = EditorUtility.SaveFilePanel("Create Enemy Prefab", "Assets/Prefabs/Enemies", config.enemyName, "prefab");
            if (string.IsNullOrEmpty(path)) return;
        
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);
        
            GameObject prefab = new GameObject(config.enemyName);
        
            prefab.AddComponent<EnemyAI>().enemyConfig = config;
            prefab.AddComponent<EnemyHealth>();
            prefab.AddComponent<UnityEngine.AI.NavMeshAgent>();
        
            var capsule = prefab.AddComponent<CapsuleCollider>();
            capsule.height = 2f; capsule.radius = 0.5f; capsule.center = new Vector3(0, 1, 0);
        
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            DestroyImmediate(prefab);
        
            EditorUtility.DisplayDialog("Success", $"Enemy prefab created: {path}", "OK");
        }
    
        void DeleteConfig(EnemyConfig config)
        {
            if (EditorUtility.DisplayDialog("Delete Confirmation", $"Are you sure you want to delete '{config.enemyName}'?", "Delete", "Cancel"))
            {
                string path = AssetDatabase.GetAssetPath(config);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
            
                selectedConfig = null;
                LoadAllEnemyAssets();
                CleanupPreview();
            }
        }
    
        void DeleteData(EnemyData data)
        {
            if (EditorUtility.DisplayDialog("Delete Confirmation", $"Are you sure you want to delete '{data.enemyName}'?", "Delete", "Cancel"))
            {
                string path = AssetDatabase.GetAssetPath(data);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
            
                selectedData = null;
                LoadAllEnemyAssets();
                CleanupPreview();
            }
        }
    
        void SaveAllAssets()
        {
            foreach (var config in allEnemyConfigs) EditorUtility.SetDirty(config);
            foreach (var data in allEnemyData) EditorUtility.SetDirty(data);
        
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Save Complete", "All enemy assets have been saved", "OK");
        }
    
        void ShowConfigContextMenu(EnemyConfig config)
        {
            GenericMenu menu = new GenericMenu();
        
            menu.AddItem(new GUIContent("Edit"), false, () => {
                selectedConfig = config; selectedData = null; UpdatePreview();
            });
            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateConfig(config));
            menu.AddItem(new GUIContent("Create Prefab"), false, () => CreateEnemyPrefab(config));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteConfig(config));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show in Project"), false, () => EditorGUIUtility.PingObject(config));
        
            menu.ShowAsContext();
        }
    
        void ShowDataContextMenu(EnemyData data)
        {
            GenericMenu menu = new GenericMenu();
        
            menu.AddItem(new GUIContent("Edit"), false, () => {
                selectedData = data; selectedConfig = null; UpdatePreview();
            });
            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateData(data));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteData(data));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show in Project"), false, () => EditorGUIUtility.PingObject(data));
        
            menu.ShowAsContext();
        }
    }
}
#endif