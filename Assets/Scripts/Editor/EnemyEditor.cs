#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
    
    // 样式
    private GUIStyle headerStyle;
    private GUIStyle selectedStyle;
    
    [MenuItem("Game Tools/Enemy Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<EnemyEditor>("怪物编辑器");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }
    
    void OnEnable()
    {
        LoadAllEnemyAssets();
        InitializeStyles();
        InitializeFoldouts();
    }
    
    void OnDisable()
    {
        CleanupPreview();
    }
    
    void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        
        selectedStyle = new GUIStyle(EditorStyles.label);
        selectedStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.6f, 1f, 0.3f));
        selectedStyle.padding = new RectOffset(5, 5, 3, 3);
    }
    
    Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
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
        
        Debug.Log($"[EnemyEditor] 加载了 {allEnemyConfigs.Count} 个配置, {allEnemyData.Count} 个数据");
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
        DrawToolbar();
        
        EditorGUILayout.BeginHorizontal();
        
        // 左侧面板
        DrawLeftPanel();
        
        // 右侧面板
        DrawRightPanel();
        
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("新建配置", EditorStyles.toolbarButton))
        {
            CreateNewEnemyConfig();
        }
        
        if (GUILayout.Button("新建数据", EditorStyles.toolbarButton))
        {
            CreateNewEnemyData();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
        {
            LoadAllEnemyAssets();
        }
        
        if (GUILayout.Button("保存所有", EditorStyles.toolbarButton))
        {
            SaveAllAssets();
        }
        
        GUILayout.FlexibleSpace();
        
        showPreview = GUILayout.Toggle(showPreview, "显示预览", EditorStyles.toolbarButton);
        
        GUILayout.Label($"配置: {allEnemyConfigs.Count} | 数据: {allEnemyData.Count}", EditorStyles.miniLabel);
        
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        
        // 标签页
        string[] tabs = { "配置 (Config)", "数据 (Data)" };
        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabs);
        
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
        
        switch (selectedTabIndex)
        {
            case 0: DrawConfigList(); break;
            case 1: DrawDataList(); break;
        }
        
        EditorGUILayout.EndScrollView();
        
        // 快速创建区域
        DrawQuickCreate();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawConfigList()
    {
        EditorGUILayout.LabelField("敌人配置 (EnemyConfig)", EditorStyles.boldLabel);
        
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            if (!configsByType.ContainsKey(type) || configsByType[type].Count == 0)
                continue;
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            typeFoldouts[type] = EditorGUILayout.Foldout(typeFoldouts[type], $"{GetTypeDisplayName(type)} ({configsByType[type].Count})", true);
            
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                CreateEnemyConfigOfType(type);
            }
            EditorGUILayout.EndHorizontal();
            
            if (typeFoldouts[type])
            {
                foreach (var config in configsByType[type])
                {
                    DrawConfigItem(config);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    void DrawDataList()
    {
        EditorGUILayout.LabelField("敌人数据 (EnemyData)", EditorStyles.boldLabel);
        
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            if (!dataByType.ContainsKey(type) || dataByType[type].Count == 0)
                continue;
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            typeFoldouts[type] = EditorGUILayout.Foldout(typeFoldouts[type], $"{GetTypeDisplayName(type)} ({dataByType[type].Count})", true);
            
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                CreateEnemyDataOfType(type);
            }
            EditorGUILayout.EndHorizontal();
            
            if (typeFoldouts[type])
            {
                foreach (var data in dataByType[type])
                {
                    DrawDataItem(data);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    void DrawConfigItem(EnemyConfig config)
    {
        bool isSelected = selectedConfig == config;
        GUIStyle style = isSelected ? selectedStyle : EditorStyles.label;
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(config.enemyName, style))
        {
            selectedConfig = config;
            selectedData = null;
            UpdatePreview();
        }
        
        // 快速操作
        if (GUILayout.Button("…", EditorStyles.miniButton, GUILayout.Width(20)))
        {
            ShowConfigContextMenu(config);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (isSelected)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"血量: {config.health}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"伤害: {config.attackDamage}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"速度: {config.patrolSpeed}-{config.chaseSpeed}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }
    
    void DrawDataItem(EnemyData data)
    {
        bool isSelected = selectedData == data;
        GUIStyle style = isSelected ? selectedStyle : EditorStyles.label;
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(data.enemyName, style))
        {
            selectedData = data;
            selectedConfig = null;
            UpdatePreview();
        }
        
        if (GUILayout.Button("…", EditorStyles.miniButton, GUILayout.Width(20)))
        {
            ShowDataContextMenu(data);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (isSelected)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"血量: {data.health}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"装甲: {data.armor}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"移动速度: {data.moveSpeed}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }
    
    void DrawQuickCreate()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("快速创建", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("配置模板:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("僵尸")) CreateEnemyConfigOfType(EnemyType.Zombie);
        if (GUILayout.Button("射手")) CreateEnemyConfigOfType(EnemyType.Shooter);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("数据模板:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("僵尸数据")) CreateEnemyDataOfType(EnemyType.Zombie);
        if (GUILayout.Button("射手数据")) CreateEnemyDataOfType(EnemyType.Shooter);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();
        
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
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawWelcomePanel()
    {
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("怪物编辑器", EditorStyles.largeLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("功能说明:", EditorStyles.boldLabel);
        GUILayout.Label("• EnemyConfig: 游戏逻辑配置 (AI行为、数值等)");
        GUILayout.Label("• EnemyData: Doom风格数据 (精灵、音效等)");
        GUILayout.Label("• 左侧选择要编辑的敌人");
        GUILayout.Label("• 使用快速创建制作新敌人");
        
        GUILayout.Space(10);
        
        GUILayout.Label("敌人类型:", EditorStyles.boldLabel);
        GUILayout.Label("• 僵尸 (Zombie): 近战敌人");
        GUILayout.Label("• 射手 (Shooter): 远程敌人");
        GUILayout.Label("• 狙击手 (Snipers): 精确射击");
        
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
    }
    
    void DrawConfigDetails()
    {
        EditorGUILayout.LabelField($"配置: {selectedConfig.enemyName}", EditorStyles.largeLabel);
        
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        
        EditorGUI.BeginChangeCheck();
        
        // 基础属性
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("基础属性", EditorStyles.boldLabel);
        
        selectedConfig.enemyName = EditorGUILayout.TextField("敌人名称", selectedConfig.enemyName);
        selectedConfig.enemyType = (EnemyType)EditorGUILayout.EnumPopup("敌人类型", selectedConfig.enemyType);
        selectedConfig.health = EditorGUILayout.FloatField("血量", selectedConfig.health);
        
        EditorGUILayout.EndVertical();
        
        // 移动设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("移动设置", EditorStyles.boldLabel);
        
        selectedConfig.patrolSpeed = EditorGUILayout.FloatField("巡逻速度", selectedConfig.patrolSpeed);
        selectedConfig.chaseSpeed = EditorGUILayout.FloatField("追击速度", selectedConfig.chaseSpeed);
        selectedConfig.rotationSpeed = EditorGUILayout.FloatField("旋转速度", selectedConfig.rotationSpeed);
        
        EditorGUILayout.EndVertical();
        
        // 感知系统
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("感知系统", EditorStyles.boldLabel);
        
        selectedConfig.visionRange = EditorGUILayout.FloatField("视野范围", selectedConfig.visionRange);
        selectedConfig.visionAngle = EditorGUILayout.FloatField("视野角度", selectedConfig.visionAngle);
        selectedConfig.hearingRange = EditorGUILayout.FloatField("听觉范围", selectedConfig.hearingRange);
        
        EditorGUILayout.EndVertical();
        
        // 攻击设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("攻击设置", EditorStyles.boldLabel);
        
        selectedConfig.attackDamage = EditorGUILayout.FloatField("攻击伤害", selectedConfig.attackDamage);
        selectedConfig.attackRange = EditorGUILayout.FloatField("攻击范围", selectedConfig.attackRange);
        selectedConfig.attackCooldown = EditorGUILayout.FloatField("攻击冷却", selectedConfig.attackCooldown);
        
        EditorGUILayout.EndVertical();
        
        // 射击敌人专用
        if (selectedConfig.enemyType == EnemyType.Shooter || selectedConfig.enemyType == EnemyType.Snipers)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("射击设置", EditorStyles.boldLabel);
            
            selectedConfig.shootRange = EditorGUILayout.FloatField("射击范围", selectedConfig.shootRange);
            selectedConfig.shootInterval = EditorGUILayout.FloatField("射击间隔", selectedConfig.shootInterval);
            selectedConfig.shootAccuracy = EditorGUILayout.Slider("射击精度", selectedConfig.shootAccuracy, 0f, 1f);
            
            EditorGUILayout.EndVertical();
        }
        
        // AI行为
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("AI行为", EditorStyles.boldLabel);
        
        selectedConfig.alertDuration = EditorGUILayout.FloatField("警戒持续时间", selectedConfig.alertDuration);
        selectedConfig.investigationTime = EditorGUILayout.FloatField("调查时间", selectedConfig.investigationTime);
        selectedConfig.canOpenDoors = EditorGUILayout.Toggle("可以开门", selectedConfig.canOpenDoors);
        selectedConfig.canClimbStairs = EditorGUILayout.Toggle("可以爬楼梯", selectedConfig.canClimbStairs);
        
        EditorGUILayout.EndVertical();
        
        // 掉落物品
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("掉落设置", EditorStyles.boldLabel);
        
        SerializedObject serializedConfig = new SerializedObject(selectedConfig);
        SerializedProperty dropItemsProperty = serializedConfig.FindProperty("dropItems");
        EditorGUILayout.PropertyField(dropItemsProperty, new GUIContent("掉落物品"), true);
        
        selectedConfig.dropChance = EditorGUILayout.Slider("掉落概率", selectedConfig.dropChance, 0f, 1f);
        
        serializedConfig.ApplyModifiedProperties();
        
        EditorGUILayout.EndVertical();
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedConfig);
        }
        
        EditorGUILayout.EndScrollView();
        
        DrawConfigBottomButtons();
    }
    
    void DrawDataDetails()
    {
        EditorGUILayout.LabelField($"数据: {selectedData.enemyName}", EditorStyles.largeLabel);
        
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        
        EditorGUI.BeginChangeCheck();
        
        // 基础信息
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        
        selectedData.enemyName = EditorGUILayout.TextField("敌人名称", selectedData.enemyName);
        selectedData.enemyType = (EnemyType)EditorGUILayout.EnumPopup("敌人类型", selectedData.enemyType);
        selectedData.health = EditorGUILayout.FloatField("血量", selectedData.health);
        selectedData.armor = EditorGUILayout.FloatField("装甲", selectedData.armor);
        
        EditorGUILayout.EndVertical();
        
        // 移动设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("移动设置", EditorStyles.boldLabel);
        
        selectedData.moveSpeed = EditorGUILayout.FloatField("移动速度", selectedData.moveSpeed);
        selectedData.chaseSpeed = EditorGUILayout.FloatField("追击速度", selectedData.chaseSpeed);
        selectedData.rotationSpeed = EditorGUILayout.FloatField("旋转速度", selectedData.rotationSpeed);
        selectedData.canFly = EditorGUILayout.Toggle("可以飞行", selectedData.canFly);
        
        EditorGUILayout.EndVertical();
        
        // AI行为
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("AI行为", EditorStyles.boldLabel);
        
        selectedData.detectionRange = EditorGUILayout.FloatField("检测范围", selectedData.detectionRange);
        selectedData.attackRange = EditorGUILayout.FloatField("攻击范围", selectedData.attackRange);
        selectedData.loseTargetTime = EditorGUILayout.FloatField("失去目标时间", selectedData.loseTargetTime);
        selectedData.alwaysHostile = EditorGUILayout.Toggle("始终敌对", selectedData.alwaysHostile);
        selectedData.canOpenDoors = EditorGUILayout.Toggle("可以开门", selectedData.canOpenDoors);
        selectedData.immuneToInfighting = EditorGUILayout.Toggle("免疫内斗", selectedData.immuneToInfighting);
        selectedData.painChance = EditorGUILayout.Slider("疼痛概率", selectedData.painChance, 0f, 1f);
        
        EditorGUILayout.EndVertical();
        
        // 攻击设置
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("攻击设置", EditorStyles.boldLabel);
        
        selectedData.attackDamage = EditorGUILayout.FloatField("攻击伤害", selectedData.attackDamage);
        selectedData.attackCooldown = EditorGUILayout.FloatField("攻击冷却", selectedData.attackCooldown);
        
        // 攻击类型数组
        SerializedObject serializedData = new SerializedObject(selectedData);
        SerializedProperty attackTypesProperty = serializedData.FindProperty("attackTypes");
        EditorGUILayout.PropertyField(attackTypesProperty, new GUIContent("攻击类型"), true);
        
        serializedData.ApplyModifiedProperties();
        
        EditorGUILayout.EndVertical();
        
        // Sprite动画
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Sprite动画", EditorStyles.boldLabel);
        
        SerializedProperty spriteSetProperty = serializedData.FindProperty("spriteSet");
        EditorGUILayout.PropertyField(spriteSetProperty, new GUIContent("精灵集合"), true);
        
        EditorGUILayout.EndVertical();
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedData);
        }
        
        EditorGUILayout.EndScrollView();
        
        DrawDataBottomButtons();
    }
    
    void DrawConfigBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("保存"))
        {
            EditorUtility.SetDirty(selectedConfig);
            AssetDatabase.SaveAssets();
        }
        
        if (GUILayout.Button("复制"))
        {
            DuplicateConfig(selectedConfig);
        }
        
        if (GUILayout.Button("创建预制体"))
        {
            CreateEnemyPrefab(selectedConfig);
        }
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("删除"))
        {
            DeleteConfig(selectedConfig);
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawDataBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("保存"))
        {
            EditorUtility.SetDirty(selectedData);
            AssetDatabase.SaveAssets();
        }
        
        if (GUILayout.Button("复制"))
        {
            DuplicateData(selectedData);
        }
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("删除"))
        {
            DeleteData(selectedData);
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
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
        // 创建简单的预览对象
        previewObject = new GameObject($"Preview_{selectedConfig.enemyName}");
        previewObject.AddComponent<EnemyAI>().enemyConfig = selectedConfig;
        previewObject.AddComponent<EnemyHealth>();
        
        // 添加可视化组件
        var renderer = previewObject.AddComponent<MeshRenderer>();
        var filter = previewObject.AddComponent<MeshFilter>();
        filter.mesh = CreateSimpleMesh();
        
        // 根据敌人类型设置颜色
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
        
        // 添加SpriteRenderer如果有精灵
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
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(0.5f, 1, -0.5f),
            new Vector3(-0.5f, 1, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(0.5f, 1, 0.5f),
            new Vector3(-0.5f, 1, 0.5f)
        };
        
        mesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2, // Front
            2, 3, 4, 2, 4, 5, // Right
            1, 2, 5, 5, 2, 6, // Back
            0, 7, 4, 0, 3, 7, // Left
            3, 6, 7, 3, 2, 6, // Top
            0, 4, 5, 0, 5, 1  // Bottom
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
    
    // 工具方法
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
    
    void CreateNewEnemyConfig()
    {
        CreateEnemyConfigOfType(EnemyType.Zombie);
    }
    
    void CreateNewEnemyData()
    {
        CreateEnemyDataOfType(EnemyType.Zombie);
    }
    
    void CreateEnemyConfigOfType(EnemyType type)
    {
        string path = EditorUtility.SaveFilePanel("创建敌人配置", "Assets/Data/Enemies", $"New{type}Config", "asset");
        if (string.IsNullOrEmpty(path)) return;
        
        if (path.StartsWith(Application.dataPath))
            path = "Assets" + path.Substring(Application.dataPath.Length);
        
        EnemyConfig newConfig = CreateInstance<EnemyConfig>();
        newConfig.enemyName = $"新{GetTypeDisplayName(type)}";
        newConfig.enemyType = type;
        
        // 根据类型设置默认值
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
        
        // 根据类型设置默认值
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
    }
    
    void SetDataDefaults(EnemyData data, EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Zombie:
                data.health = 50f;
                data.moveSpeed = 2f;
                data.chaseSpeed = 4f;
                data.attackDamage = 15f;
                data.detectionRange = 10f;
                data.attackRange = 1.5f;
                break;
                
            case EnemyType.Shooter:
                data.health = 75f;
                data.moveSpeed = 3f;
                data.chaseSpeed = 5f;
                data.attackDamage = 20f;
                data.detectionRange = 20f;
                data.attackRange = 15f;
                break;
                
            case EnemyType.Snipers:
                data.health = 60f;
                data.moveSpeed = 2f;
                data.chaseSpeed = 3f;
                data.attackDamage = 40f;
                data.detectionRange = 30f;
                data.attackRange = 25f;
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
        
        // 添加必要组件
        prefab.AddComponent<EnemyAI>().enemyConfig = config;
        prefab.AddComponent<EnemyHealth>();
        prefab.AddComponent<UnityEngine.AI.NavMeshAgent>();
        
        // 添加碰撞器
        var capsule = prefab.AddComponent<CapsuleCollider>();
        capsule.height = 2f;
        capsule.radius = 0.5f;
        capsule.center = new Vector3(0, 1, 0);
        
        // 保存为预制体
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
        foreach (var config in allEnemyConfigs)
        {
            EditorUtility.SetDirty(config);
        }
        
        foreach (var data in allEnemyData)
        {
            EditorUtility.SetDirty(data);
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("保存完成", "所有敌人资源已保存", "确定");
    }
    
    void ShowConfigContextMenu(EnemyConfig config)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("编辑"), false, () => {
            selectedConfig = config;
            selectedData = null;
            UpdatePreview();
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
            selectedData = data;
            selectedConfig = null;
            UpdatePreview();
        });
        menu.AddItem(new GUIContent("复制"), false, () => DuplicateData(data));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("删除"), false, () => DeleteData(data));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("在项目中显示"), false, () => EditorGUIUtility.PingObject(data));
        
        menu.ShowAsContext();
    }
}
#endif