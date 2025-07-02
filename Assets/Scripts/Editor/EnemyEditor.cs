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
    
        // 现代UI样式
        private GUIStyle modernCardStyle;
        private GUIStyle modernHeaderStyle;
        private GUIStyle modernButtonStyle;
        private GUIStyle modernToolbarStyle;
        private GUIStyle modernSelectedStyle;
        private GUIStyle modernSectionStyle;
        private GUIStyle modernIconButtonStyle;
    
        // 颜色主题
        private static readonly Color PrimaryColor = new Color(0.2f, 0.6f, 1f);
        private static readonly Color SecondaryColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color AccentColor = new Color(0.3f, 0.8f, 0.4f);
        private static readonly Color BackgroundColor = new Color(0.12f, 0.12f, 0.12f);
        private static readonly Color CardColor = new Color(0.18f, 0.18f, 0.18f);
        private static readonly Color TextColor = new Color(0.9f, 0.9f, 0.9f);
        private static readonly Color DangerColor = new Color(0.9f, 0.3f, 0.3f);
    
        // 图标Unicode字符
        private const string IconAdd = "＋";
        private const string IconDelete = "🗑";
        private const string IconCopy = "📋";
        private const string IconSettings = "⚙";
        private const string IconSearch = "🔍";
        private const string IconFilter = "🔽";
        private const string IconPreview = "👁";
        private const string IconSave = "💾";
        private const string IconRefresh = "🔄";
    
        [MenuItem("Game Tools/Modern Enemy Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<EnemyEditor>("敌人编辑器");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }
    
        void OnEnable()
        {
            LoadAllEnemyAssets();
            InitializeModernStyles();
            InitializeFoldouts();
        }
    
        void OnDisable()
        {
            CleanupPreview();
        }
    
        void InitializeModernStyles()
        {
            // 现代卡片样式
            modernCardStyle = new GUIStyle();
            modernCardStyle.normal.background = CreateRoundedTexture(CardColor, 8);
            modernCardStyle.border = new RectOffset(8, 8, 8, 8);
            modernCardStyle.padding = new RectOffset(12, 12, 12, 12);
            modernCardStyle.margin = new RectOffset(4, 4, 4, 4);
        
            // 现代标题样式
            modernHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            modernHeaderStyle.fontSize = 16;
            modernHeaderStyle.normal.textColor = TextColor;
            modernHeaderStyle.padding = new RectOffset(0, 0, 8, 8);
        
            // 现代按钮样式
            modernButtonStyle = new GUIStyle();
            modernButtonStyle.normal.background = CreateRoundedTexture(PrimaryColor, 6);
            modernButtonStyle.hover.background = CreateRoundedTexture(new Color(PrimaryColor.r * 1.2f, PrimaryColor.g * 1.2f, PrimaryColor.b * 1.2f), 6);
            modernButtonStyle.active.background = CreateRoundedTexture(new Color(PrimaryColor.r * 0.8f, PrimaryColor.g * 0.8f, PrimaryColor.b * 0.8f), 6);
            modernButtonStyle.normal.textColor = Color.white;
            modernButtonStyle.hover.textColor = Color.white;
            modernButtonStyle.active.textColor = Color.white;
            modernButtonStyle.border = new RectOffset(6, 6, 6, 6);
            modernButtonStyle.padding = new RectOffset(12, 12, 8, 8);
            modernButtonStyle.margin = new RectOffset(2, 2, 2, 2);
            modernButtonStyle.alignment = TextAnchor.MiddleCenter;
            modernButtonStyle.fontStyle = FontStyle.Bold;
        
            // 图标按钮样式
            modernIconButtonStyle = new GUIStyle(modernButtonStyle);
            modernIconButtonStyle.padding = new RectOffset(8, 8, 6, 6);
            modernIconButtonStyle.fontSize = 16;
        
            // 工具栏样式
            modernToolbarStyle = new GUIStyle();
            modernToolbarStyle.normal.background = CreateColorTexture(SecondaryColor);
            modernToolbarStyle.padding = new RectOffset(8, 8, 8, 8);
        
            // 选中样式
            modernSelectedStyle = new GUIStyle();
            modernSelectedStyle.normal.background = CreateRoundedTexture(new Color(PrimaryColor.r, PrimaryColor.g, PrimaryColor.b, 0.3f), 6);
            modernSelectedStyle.border = new RectOffset(6, 6, 6, 6);
            modernSelectedStyle.padding = new RectOffset(8, 8, 6, 6);
            modernSelectedStyle.margin = new RectOffset(2, 2, 1, 1);
        
            // 区域样式
            modernSectionStyle = new GUIStyle();
            modernSectionStyle.normal.background = CreateRoundedTexture(new Color(0.1f, 0.1f, 0.1f), 6);
            modernSectionStyle.border = new RectOffset(6, 6, 6, 6);
            modernSectionStyle.padding = new RectOffset(12, 12, 12, 12);
            modernSectionStyle.margin = new RectOffset(2, 2, 2, 2);
        }
    
        Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    
        Texture2D CreateRoundedTexture(Color color, int radius)
        {
            int size = radius * 4;
            Texture2D texture = new Texture2D(size, size);
        
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size/2f, size/2f));
                    float alpha = distance < radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha * color.a));
                }
            }
            texture.Apply();
            return texture;
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
        
            Debug.Log($"[ModernEnemyEditor] 加载了 {allEnemyConfigs.Count} 个配置, {allEnemyData.Count} 个数据");
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
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BackgroundColor);
        
            DrawModernToolbar();
        
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
        
            // 左侧面板 - 使用固定宽度和现代样式
            EditorGUILayout.BeginVertical(modernSectionStyle, GUILayout.Width(350), GUILayout.ExpandHeight(true));
            DrawModernLeftPanel();
            EditorGUILayout.EndVertical();
        
            GUILayout.Space(8);
        
            // 右侧面板
            EditorGUILayout.BeginVertical(modernSectionStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawModernRightPanel();
            EditorGUILayout.EndVertical();
        
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawModernToolbar()
        {
            EditorGUILayout.BeginHorizontal(modernToolbarStyle, GUILayout.Height(50));
        
            GUILayout.Space(8);
        
            // 主要操作按钮
            if (GUILayout.Button(IconAdd + " 新建配置", modernButtonStyle, GUILayout.Height(32)))
            {
                CreateNewEnemyConfig();
            }
        
            if (GUILayout.Button(IconAdd + " 新建数据", modernButtonStyle, GUILayout.Height(32)))
            {
                CreateNewEnemyData();
            }
        
            GUILayout.Space(12);
        
            // 工具按钮
            if (GUILayout.Button(IconRefresh, modernIconButtonStyle, GUILayout.Width(40), GUILayout.Height(32)))
            {
                LoadAllEnemyAssets();
            }
        
            if (GUILayout.Button(IconSave, modernIconButtonStyle, GUILayout.Width(40), GUILayout.Height(32)))
            {
                SaveAllAssets();
            }
        
            GUILayout.FlexibleSpace();
        
            // 搜索区域
            DrawModernSearchBar();
        
            GUILayout.Space(8);
        
            // 预览切换
            var prevPreview = showPreview;
            showPreview = GUILayout.Toggle(showPreview, IconPreview, modernIconButtonStyle, GUILayout.Width(40), GUILayout.Height(32));
            if (prevPreview != showPreview)
            {
                UpdatePreview();
            }
        
            GUILayout.Space(8);
        
            EditorGUILayout.EndHorizontal();
        
            // 绘制分隔线
            EditorGUI.DrawRect(new Rect(0, 50, position.width, 1), new Color(0.3f, 0.3f, 0.3f));
        }
    
        void DrawModernSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
        
            // 搜索图标
            GUILayout.Label(IconSearch, GUILayout.Width(20));
        
            // 搜索框
            var newSearchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarTextField, GUILayout.Width(200));
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
            }
        
            // 过滤器
            var prevUseFilter = useTypeFilter;
            useTypeFilter = GUILayout.Toggle(useTypeFilter, IconFilter, EditorStyles.toolbarButton, GUILayout.Width(30));
        
            if (useTypeFilter)
            {
                filterType = (EnemyType)EditorGUILayout.EnumPopup(filterType, EditorStyles.toolbarPopup, GUILayout.Width(100));
            }
        
            // 清除按钮
            if (!string.IsNullOrEmpty(searchText) || useTypeFilter)
            {
                if (GUILayout.Button("✗", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    searchText = "";
                    useTypeFilter = false;
                }
            }
        
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawModernLeftPanel()
        {
            // 标签页
            EditorGUILayout.BeginHorizontal();
            string[] tabs = { "⚙ 配置", "📊 数据" };
        
            for (int i = 0; i < tabs.Length; i++)
            {
                var style = (selectedTabIndex == i) ? modernSelectedStyle : EditorStyles.miniButton;
                if (GUILayout.Button(tabs[i], style, GUILayout.Height(30)))
                {
                    selectedTabIndex = i;
                }
            }
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(8);
        
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
        
            switch (selectedTabIndex)
            {
                case 0: DrawModernConfigList(); break;
                case 1: DrawModernDataList(); break;
            }
        
            EditorGUILayout.EndScrollView();
        
            GUILayout.Space(8);
        
            // 快速创建区域
            DrawModernQuickCreate();
        }
    
        void DrawModernConfigList()
        {
            EditorGUILayout.LabelField("敌人配置", modernHeaderStyle);
        
            var filteredTypes = GetFilteredEnemyTypes();
        
            foreach (EnemyType type in filteredTypes)
            {
                if (!configsByType.ContainsKey(type) || configsByType[type].Count == 0)
                    continue;
            
                var configs = GetFilteredConfigs(configsByType[type]);
                if (configs.Count == 0) continue;
            
                EditorGUILayout.BeginVertical(modernCardStyle);
            
                // 类型标题行
                EditorGUILayout.BeginHorizontal();
            
                string typeIcon = GetTypeIcon(type);
                var foldoutRect = GUILayoutUtility.GetRect(20, 20);
                typeFoldouts[type] = EditorGUI.Foldout(foldoutRect, typeFoldouts[type], "", true);
            
                GUILayout.Label($"{typeIcon} {GetTypeDisplayName(type)}", EditorStyles.boldLabel);
            
                GUILayout.FlexibleSpace();
            
                // 数量标签
                var countStyle = new GUIStyle(EditorStyles.miniLabel);
                countStyle.normal.textColor = PrimaryColor;
                GUILayout.Label($"{configs.Count}", countStyle);
            
                if (GUILayout.Button(IconAdd, modernIconButtonStyle, GUILayout.Width(25), GUILayout.Height(20)))
                {
                    CreateEnemyConfigOfType(type);
                }
            
                EditorGUILayout.EndHorizontal();
            
                if (typeFoldouts[type])
                {
                    GUILayout.Space(4);
                    foreach (var config in configs)
                    {
                        DrawModernConfigItem(config);
                    }
                }
            
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }
        }
    
        void DrawModernDataList()
        {
            EditorGUILayout.LabelField("敌人数据", modernHeaderStyle);
        
            var filteredTypes = GetFilteredEnemyTypes();
        
            foreach (EnemyType type in filteredTypes)
            {
                if (!dataByType.ContainsKey(type) || dataByType[type].Count == 0)
                    continue;
            
                var data = GetFilteredData(dataByType[type]);
                if (data.Count == 0) continue;
            
                EditorGUILayout.BeginVertical(modernCardStyle);
            
                EditorGUILayout.BeginHorizontal();
            
                string typeIcon = GetTypeIcon(type);
                var foldoutRect = GUILayoutUtility.GetRect(20, 20);
                typeFoldouts[type] = EditorGUI.Foldout(foldoutRect, typeFoldouts[type], "", true);
            
                GUILayout.Label($"{typeIcon} {GetTypeDisplayName(type)}", EditorStyles.boldLabel);
            
                GUILayout.FlexibleSpace();
            
                var countStyle = new GUIStyle(EditorStyles.miniLabel);
                countStyle.normal.textColor = PrimaryColor;
                GUILayout.Label($"{data.Count}", countStyle);
            
                if (GUILayout.Button(IconAdd, modernIconButtonStyle, GUILayout.Width(25), GUILayout.Height(20)))
                {
                    CreateEnemyDataOfType(type);
                }
            
                EditorGUILayout.EndHorizontal();
            
                if (typeFoldouts[type])
                {
                    GUILayout.Space(4);
                    foreach (var dataItem in data)
                    {
                        DrawModernDataItem(dataItem);
                    }
                }
            
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }
        }
    
        void DrawModernConfigItem(EnemyConfig config)
        {
            bool isSelected = selectedConfig == config;
        
            var itemStyle = isSelected ? modernSelectedStyle : new GUIStyle();
            itemStyle.padding = new RectOffset(8, 8, 6, 6);
            itemStyle.margin = new RectOffset(0, 0, 1, 1);
        
            EditorGUILayout.BeginVertical(itemStyle);
        
            EditorGUILayout.BeginHorizontal();
        
            // 主按钮
            if (GUILayout.Button(config.enemyName, EditorStyles.label))
            {
                selectedConfig = config;
                selectedData = null;
                UpdatePreview();
            }
        
            GUILayout.FlexibleSpace();
        
            // 操作按钮
            if (GUILayout.Button(IconSettings, EditorStyles.miniButton, GUILayout.Width(20)))
            {
                ShowConfigContextMenu(config);
            }
        
            EditorGUILayout.EndHorizontal();
        
            if (isSelected)
            {
                GUILayout.Space(2);
                DrawConfigPreviewStats(config);
            }
        
            EditorGUILayout.EndVertical();
        }
    
        void DrawModernDataItem(EnemyData data)
        {
            bool isSelected = selectedData == data;
        
            var itemStyle = isSelected ? modernSelectedStyle : new GUIStyle();
            itemStyle.padding = new RectOffset(8, 8, 6, 6);
            itemStyle.margin = new RectOffset(0, 0, 1, 1);
        
            EditorGUILayout.BeginVertical(itemStyle);
        
            EditorGUILayout.BeginHorizontal();
        
            if (GUILayout.Button(data.enemyName, EditorStyles.label))
            {
                selectedData = data;
                selectedConfig = null;
                UpdatePreview();
            }
        
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button(IconSettings, EditorStyles.miniButton, GUILayout.Width(20)))
            {
                ShowDataContextMenu(data);
            }
        
            EditorGUILayout.EndHorizontal();
        
            if (isSelected)
            {
                GUILayout.Space(2);
                DrawDataPreviewStats(data);
            }
        
            EditorGUILayout.EndVertical();
        }
    
        void DrawConfigPreviewStats(EnemyConfig config)
        {
            var miniStyle = new GUIStyle(EditorStyles.miniLabel);
            miniStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"♥ {config.health}", miniStyle, GUILayout.Width(50));
            GUILayout.Label($"⚔ {config.attackDamage}", miniStyle, GUILayout.Width(50));
            GUILayout.Label($"🏃 {config.chaseSpeed}", miniStyle);
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawDataPreviewStats(EnemyData data)
        {
            var miniStyle = new GUIStyle(EditorStyles.miniLabel);
            miniStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"♥ {data.health}", miniStyle, GUILayout.Width(50));
            GUILayout.Label($"🛡 {data.armor}", miniStyle, GUILayout.Width(50));
            GUILayout.Label($"🏃 {data.moveSpeed}", miniStyle);
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawModernQuickCreate()
        {
            EditorGUILayout.BeginVertical(modernCardStyle);
            EditorGUILayout.LabelField("快速创建", modernHeaderStyle);
        
            GUILayout.Label("配置模板:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🧟 僵尸", modernButtonStyle, GUILayout.Height(28))) CreateEnemyConfigOfType(EnemyType.Zombie);
            if (GUILayout.Button("🏹 射手", modernButtonStyle, GUILayout.Height(28))) CreateEnemyConfigOfType(EnemyType.Shooter);
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(4);
        
            GUILayout.Label("数据模板:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🧟 僵尸数据", modernButtonStyle, GUILayout.Height(28))) CreateEnemyDataOfType(EnemyType.Zombie);
            if (GUILayout.Button("🏹 射手数据", modernButtonStyle, GUILayout.Height(28))) CreateEnemyDataOfType(EnemyType.Shooter);
            EditorGUILayout.EndHorizontal();
        
            EditorGUILayout.EndVertical();
        }
    
        void DrawModernRightPanel()
        {
            if (selectedConfig != null)
            {
                DrawModernConfigDetails();
            }
            else if (selectedData != null)
            {
                DrawModernDataDetails();
            }
            else
            {
                DrawModernWelcomePanel();
            }
        }
    
        void DrawModernWelcomePanel()
        {
            GUILayout.FlexibleSpace();
        
            EditorGUILayout.BeginVertical(modernCardStyle);
        
            // 标题
            var titleStyle = new GUIStyle(EditorStyles.largeLabel);
            titleStyle.fontSize = 24;
            titleStyle.normal.textColor = PrimaryColor;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("🎮 怪物编辑器", titleStyle);
        
            GUILayout.Space(20);
        
            // 功能介绍
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionStyle.fontSize = 14;
            sectionStyle.normal.textColor = AccentColor;
        
            GUILayout.Label("✨ 功能说明", sectionStyle);
            GUILayout.Space(5);
        
            var descStyle = new GUIStyle(EditorStyles.label);
            descStyle.normal.textColor = TextColor;
            descStyle.wordWrap = true;
        
            GUILayout.Label("• EnemyConfig: 游戏逻辑配置 (AI行为、数值等)", descStyle);
            GUILayout.Label("• EnemyData: Doom风格数据 (精灵、音效等)", descStyle);
            GUILayout.Label("• 左侧选择要编辑的敌人", descStyle);
            GUILayout.Label("• 使用快速创建制作新敌人", descStyle);
        
            GUILayout.Space(15);
        
            GUILayout.Label("🎯 敌人类型", sectionStyle);
            GUILayout.Space(5);
        
            GUILayout.Label("• 🧟 僵尸 (Zombie): 近战敌人", descStyle);
            GUILayout.Label("• 🏹 射手 (Shooter): 远程敌人", descStyle);
            GUILayout.Label("• 🎯 狙击手 (Snipers): 精确射击", descStyle);
        
            GUILayout.Space(20);
        
            // 快速开始按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button("🚀 创建第一个敌人", modernButtonStyle, GUILayout.Width(200), GUILayout.Height(40)))
            {
                selectedTabIndex = 0;
                CreateNewEnemyConfig();
            }
        
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        
            EditorGUILayout.EndVertical();
        
            GUILayout.FlexibleSpace();
        }
    
        void DrawModernConfigDetails()
        {
            // 标题栏
            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.largeLabel);
            titleStyle.normal.textColor = PrimaryColor;
            EditorGUILayout.LabelField($"⚙ {selectedConfig.enemyName}", titleStyle);
        
            GUILayout.FlexibleSpace();
        
            // 操作按钮
            if (GUILayout.Button(IconSave, modernIconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(selectedConfig);
                AssetDatabase.SaveAssets();
            }
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(8);
        
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        
            EditorGUI.BeginChangeCheck();
        
            // 基础属性卡片
            DrawModernSection("🎯 基础属性", () => {
                selectedConfig.enemyName = EditorGUILayout.TextField("敌人名称", selectedConfig.enemyName);
                selectedConfig.enemyType = (EnemyType)EditorGUILayout.EnumPopup("敌人类型", selectedConfig.enemyType);
                selectedConfig.health = EditorGUILayout.FloatField("血量", selectedConfig.health);
            });
        
            // 移动设置卡片
            DrawModernSection("🏃 移动设置", () => {
                selectedConfig.patrolSpeed = EditorGUILayout.FloatField("巡逻速度", selectedConfig.patrolSpeed);
                selectedConfig.chaseSpeed = EditorGUILayout.FloatField("追击速度", selectedConfig.chaseSpeed);
                selectedConfig.rotationSpeed = EditorGUILayout.FloatField("旋转速度", selectedConfig.rotationSpeed);
            });
        
            // 感知系统卡片
            DrawModernSection("👁 感知系统", () => {
                selectedConfig.visionRange = EditorGUILayout.FloatField("视野范围", selectedConfig.visionRange);
                selectedConfig.visionAngle = EditorGUILayout.FloatField("视野角度", selectedConfig.visionAngle);
                selectedConfig.hearingRange = EditorGUILayout.FloatField("听觉范围", selectedConfig.hearingRange);
            });
        
            // 攻击设置卡片
            DrawModernSection("⚔ 攻击设置", () => {
                selectedConfig.attackDamage = EditorGUILayout.FloatField("攻击伤害", selectedConfig.attackDamage);
                selectedConfig.attackRange = EditorGUILayout.FloatField("攻击范围", selectedConfig.attackRange);
                selectedConfig.attackCooldown = EditorGUILayout.FloatField("攻击冷却", selectedConfig.attackCooldown);
            });
        
            // 射击设置（条件显示）
            if (selectedConfig.enemyType == EnemyType.Shooter || selectedConfig.enemyType == EnemyType.Snipers)
            {
                DrawModernSection("🏹 射击设置", () => {
                    selectedConfig.shootRange = EditorGUILayout.FloatField("射击范围", selectedConfig.shootRange);
                    selectedConfig.shootInterval = EditorGUILayout.FloatField("射击间隔", selectedConfig.shootInterval);
                    selectedConfig.shootAccuracy = EditorGUILayout.Slider("射击精度", selectedConfig.shootAccuracy, 0f, 1f);
                });
            }
        
            // AI行为卡片
            DrawModernSection("🤖 AI行为", () => {
                selectedConfig.alertDuration = EditorGUILayout.FloatField("警戒持续时间", selectedConfig.alertDuration);
                selectedConfig.investigationTime = EditorGUILayout.FloatField("调查时间", selectedConfig.investigationTime);
                selectedConfig.canOpenDoors = EditorGUILayout.Toggle("可以开门", selectedConfig.canOpenDoors);
                selectedConfig.canClimbStairs = EditorGUILayout.Toggle("可以爬楼梯", selectedConfig.canClimbStairs);
            });
        
            // 掉落设置卡片
            DrawModernSection("💎 掉落设置", () => {
                SerializedObject serializedConfig = new SerializedObject(selectedConfig);
                SerializedProperty dropItemsProperty = serializedConfig.FindProperty("dropItems");
                EditorGUILayout.PropertyField(dropItemsProperty, new GUIContent("掉落物品"), true);
            
                selectedConfig.dropChance = EditorGUILayout.Slider("掉落概率", selectedConfig.dropChance, 0f, 1f);
            
                serializedConfig.ApplyModifiedProperties();
            });
        
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedConfig);
            }
        
            EditorGUILayout.EndScrollView();
        
            GUILayout.Space(8);
            DrawModernConfigBottomButtons();
        }
    
        void DrawModernDataDetails()
        {
            // 标题栏
            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.largeLabel);
            titleStyle.normal.textColor = PrimaryColor;
            EditorGUILayout.LabelField($"📊 {selectedData.enemyName}", titleStyle);
        
            GUILayout.FlexibleSpace();
        
            if (GUILayout.Button(IconSave, modernIconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(selectedData);
                AssetDatabase.SaveAssets();
            }
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(8);
        
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        
            EditorGUI.BeginChangeCheck();
        
            // 各个设置区域...
            DrawModernSection("🎯 基础信息", () => {
                selectedData.enemyName = EditorGUILayout.TextField("敌人名称", selectedData.enemyName);
                selectedData.enemyType = (EnemyType)EditorGUILayout.EnumPopup("敌人类型", selectedData.enemyType);
                selectedData.health = EditorGUILayout.FloatField("血量", selectedData.health);
                selectedData.armor = EditorGUILayout.FloatField("装甲", selectedData.armor);
            });
        
            DrawModernSection("🏃 移动设置", () => {
                selectedData.moveSpeed = EditorGUILayout.FloatField("移动速度", selectedData.moveSpeed);
                selectedData.chaseSpeed = EditorGUILayout.FloatField("追击速度", selectedData.chaseSpeed);
                selectedData.rotationSpeed = EditorGUILayout.FloatField("旋转速度", selectedData.rotationSpeed);
                selectedData.canFly = EditorGUILayout.Toggle("可以飞行", selectedData.canFly);
            });
        
            DrawModernSection("🤖 AI行为", () => {
                selectedData.detectionRange = EditorGUILayout.FloatField("检测范围", selectedData.detectionRange);
                selectedData.attackRange = EditorGUILayout.FloatField("攻击范围", selectedData.attackRange);
                selectedData.loseTargetTime = EditorGUILayout.FloatField("失去目标时间", selectedData.loseTargetTime);
                selectedData.alwaysHostile = EditorGUILayout.Toggle("始终敌对", selectedData.alwaysHostile);
                selectedData.canOpenDoors = EditorGUILayout.Toggle("可以开门", selectedData.canOpenDoors);
                selectedData.immuneToInfighting = EditorGUILayout.Toggle("免疫内斗", selectedData.immuneToInfighting);
                selectedData.painChance = EditorGUILayout.Slider("疼痛概率", selectedData.painChance, 0f, 1f);
            });
        
            DrawModernSection("⚔ 攻击设置", () => {
                selectedData.attackDamage = EditorGUILayout.FloatField("攻击伤害", selectedData.attackDamage);
                selectedData.attackCooldown = EditorGUILayout.FloatField("攻击冷却", selectedData.attackCooldown);
            
                SerializedObject serializedData = new SerializedObject(selectedData);
                SerializedProperty attackTypesProperty = serializedData.FindProperty("attackTypes");
                EditorGUILayout.PropertyField(attackTypesProperty, new GUIContent("攻击类型"), true);
                serializedData.ApplyModifiedProperties();
            });
        
            DrawModernSection("🎨 Sprite动画", () => {
                SerializedObject serializedData = new SerializedObject(selectedData);
                SerializedProperty spriteSetProperty = serializedData.FindProperty("spriteSet");
                EditorGUILayout.PropertyField(spriteSetProperty, new GUIContent("精灵集合"), true);
                serializedData.ApplyModifiedProperties();
            });
        
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedData);
            }
        
            EditorGUILayout.EndScrollView();
        
            GUILayout.Space(8);
            DrawModernDataBottomButtons();
        }
    
        void DrawModernSection(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(modernCardStyle);
        
            EditorGUILayout.LabelField(title, modernHeaderStyle);
            GUILayout.Space(4);
        
            content?.Invoke();
        
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);
        }
    
        void DrawModernConfigBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();
        
            if (GUILayout.Button("💾 保存", modernButtonStyle, GUILayout.Height(32)))
            {
                EditorUtility.SetDirty(selectedConfig);
                AssetDatabase.SaveAssets();
            }
        
            if (GUILayout.Button("📋 复制", modernButtonStyle, GUILayout.Height(32)))
            {
                DuplicateConfig(selectedConfig);
            }
        
            if (GUILayout.Button("🎮 创建预制体", modernButtonStyle, GUILayout.Height(32)))
            {
                CreateEnemyPrefab(selectedConfig);
            }
        
            GUILayout.FlexibleSpace();
        
            // 危险操作按钮
            var deleteStyle = new GUIStyle(modernButtonStyle);
            deleteStyle.normal.background = CreateRoundedTexture(DangerColor, 6);
            deleteStyle.hover.background = CreateRoundedTexture(new Color(DangerColor.r * 1.2f, DangerColor.g * 1.2f, DangerColor.b * 1.2f), 6);
        
            if (GUILayout.Button("🗑 删除", deleteStyle, GUILayout.Height(32)))
            {
                DeleteConfig(selectedConfig);
            }
        
            EditorGUILayout.EndHorizontal();
        }
    
        void DrawModernDataBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();
        
            if (GUILayout.Button("💾 保存", modernButtonStyle, GUILayout.Height(32)))
            {
                EditorUtility.SetDirty(selectedData);
                AssetDatabase.SaveAssets();
            }
        
            if (GUILayout.Button("📋 复制", modernButtonStyle, GUILayout.Height(32)))
            {
                DuplicateData(selectedData);
            }
        
            GUILayout.FlexibleSpace();
        
            var deleteStyle = new GUIStyle(modernButtonStyle);
            deleteStyle.normal.background = CreateRoundedTexture(DangerColor, 6);
            deleteStyle.hover.background = CreateRoundedTexture(new Color(DangerColor.r * 1.2f, DangerColor.g * 1.2f, DangerColor.b * 1.2f), 6);
        
            if (GUILayout.Button("🗑 删除", deleteStyle, GUILayout.Height(32)))
            {
                DeleteData(selectedData);
            }
        
            EditorGUILayout.EndHorizontal();
        }
    
        // 辅助方法
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
    
        string GetTypeIcon(EnemyType type)
        {
            return type switch
            {
                EnemyType.Zombie => "🧟",
                EnemyType.Shooter => "🏹",
                EnemyType.Snipers => "🎯",
                _ => "👾"
            };
        }
    
        string GetTypeDisplayName(EnemyType type)
        {
            return type switch
            {
                EnemyType.Zombie => "僵尸",
                EnemyType.Shooter => "射手",
                EnemyType.Snipers => "狙击手",
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
            string path = EditorUtility.SaveFilePanel("创建敌人配置", "Assets/Data/Enemies", $"New{type}Config", "asset");
            if (string.IsNullOrEmpty(path)) return;
        
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);
        
            EnemyConfig newConfig = CreateInstance<EnemyConfig>();
            newConfig.enemyName = $"新{GetTypeDisplayName(type)}";
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
            string path = EditorUtility.SaveFilePanel("创建敌人数据", "Assets/Data/Enemies", $"New{type}Data", "asset");
            if (string.IsNullOrEmpty(path)) return;
        
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);
        
            EnemyData newData = CreateInstance<EnemyData>();
            newData.enemyName = $"新{GetTypeDisplayName(type)}";
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
                    duplicate.enemyName += " (副本)";
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
                    duplicate.enemyName += " (副本)";
                    EditorUtility.SetDirty(duplicate);
                    selectedData = duplicate;
                    UpdatePreview();
                }
            }
        }
    
        void CreateEnemyPrefab(EnemyConfig config)
        {
            string path = EditorUtility.SaveFilePanel("创建敌人预制体", "Assets/Prefabs/Enemies", config.enemyName, "prefab");
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
        
            EditorUtility.DisplayDialog("创建成功", $"敌人预制体已创建: {path}", "确定");
        }
    
        void DeleteConfig(EnemyConfig config)
        {
            if (EditorUtility.DisplayDialog("删除确认", $"确定要删除配置 '{config.enemyName}' 吗？", "删除", "取消"))
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
            if (EditorUtility.DisplayDialog("删除确认", $"确定要删除数据 '{data.enemyName}' 吗？", "删除", "取消"))
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
            EditorUtility.DisplayDialog("保存完成", "所有敌人资源已保存", "确定");
        }
    
        void ShowConfigContextMenu(EnemyConfig config)
        {
            GenericMenu menu = new GenericMenu();
        
            menu.AddItem(new GUIContent("编辑"), false, () => {
                selectedConfig = config; selectedData = null; UpdatePreview();
            });
            menu.AddItem(new GUIContent("复制"), false, () => DuplicateConfig(config));
            menu.AddItem(new GUIContent("创建预制体"), false, () => CreateEnemyPrefab(config));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除"), false, () => DeleteConfig(config));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("在项目中显示"), false, () => EditorGUIUtility.PingObject(config));
        
            menu.ShowAsContext();
        }
    
        void ShowDataContextMenu(EnemyData data)
        {
            GenericMenu menu = new GenericMenu();
        
            menu.AddItem(new GUIContent("编辑"), false, () => {
                selectedData = data; selectedConfig = null; UpdatePreview();
            });
            menu.AddItem(new GUIContent("复制"), false, () => DuplicateData(data));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除"), false, () => DeleteData(data));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("在项目中显示"), false, () => EditorGUIUtility.PingObject(data));
        
            menu.ShowAsContext();
        }
    }
}
#endif