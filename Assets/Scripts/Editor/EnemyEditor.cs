#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    // 数据模型定义
    [System.Serializable]
    public class EnemyEditorData : ScriptableObject
    {
        [SerializeField] public List<EnemyConfig> enemyConfigs = new();
        [SerializeField] public List<EnemyData> enemyData = new();
        [SerializeField] public string currentFilter = "";
        [SerializeField] public EnemyType filterType = EnemyType.Zombie;
        [SerializeField] public bool useTypeFilter = false;
        
        private static EnemyEditorData instance;
        public static EnemyEditorData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<EnemyEditorData>("EnemyEditorData");
                    if (instance == null)
                    {
                        instance = CreateInstance<EnemyEditorData>();
                        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                            AssetDatabase.CreateFolder("Assets", "Resources");
                        AssetDatabase.CreateAsset(instance, "Assets/Resources/EnemyEditorData.asset");
                        AssetDatabase.SaveAssets();
                    }
                }
                return instance;
            }
        }
    }

    public class ModernEnemyEditor : EditorWindow
    {
        // UI Elements
        private VisualElement root;
        private TwoPaneSplitView mainSplitView;
        private TwoPaneSplitView rightSplitView;
        
        // 左侧面板
        private VisualElement leftPanel;
        private TextField searchField;
        private ListView enemyListView;
        
        // 中央预览区
        private VisualElement previewContainer;
        private IMGUIContainer previewIMGUI;
        
        // 右侧检视器
        private VisualElement inspectorContainer;
        private ScrollView inspectorScrollView;
        
        // 数据
        private EnemyEditorData editorData;
        private EnemyConfig selectedConfig;
        private EnemyData selectedData;
        private List<Object> filteredItems = new();
        
        // 预览系统
        private PreviewRenderUtility previewUtility;
        private GameObject previewObject;
        private Vector2 previewRotation = new Vector2(15, 0);
        
        [MenuItem("Tools/Modern Enemy Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ModernEnemyEditor>("Modern Enemy Editor");
            window.minSize = new Vector2(1200, 800);
        }
        
        private void CreateGUI()
        {
            root = rootVisualElement;
            editorData = EnemyEditorData.Instance;
            
            CreateLayout();
            SetupEventHandlers();
            RefreshData();
        }
        
        private void OnDestroy()
        {
            CleanupPreview();
        }
        
        private void CreateLayout()
        {
            // 主要布局容器
            var mainContainer = new VisualElement();
            mainContainer.name = "main-container";
            mainContainer.style.flexGrow = 1;
            mainContainer.style.flexDirection = FlexDirection.Column;
            
            // 工具栏
            CreateToolbar(mainContainer);
            
            // 主分割视图
            mainSplitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            
            // 创建三个主要区域
            CreateLeftPanel();
            CreateCenterPreviewArea();
            CreateRightInspector();
            
            // 右侧分割视图（预览 + 检视器）
            rightSplitView = new TwoPaneSplitView(1, 350, TwoPaneSplitViewOrientation.Vertical);
            rightSplitView.Add(previewContainer);
            rightSplitView.Add(inspectorContainer);
            
            // 组装布局
            mainSplitView.Add(leftPanel);
            mainSplitView.Add(rightSplitView);
            
            mainContainer.Add(mainSplitView);
            root.Add(mainContainer);
        }
        
        private void CreateToolbar(VisualElement parent)
        {
            var toolbar = new Toolbar();
            
            // 主要操作按钮
            var createButton = new ToolbarButton(CreateNewEnemy) { text = "➕ Create" };
            var duplicateButton = new ToolbarButton(DuplicateSelected) { text = "📋 Duplicate" };
            var deleteButton = new ToolbarButton(DeleteSelected) { text = "🗑 Delete" };
            var refreshButton = new ToolbarButton(RefreshData) { text = "🔄 Refresh" };
            
            toolbar.Add(createButton);
            toolbar.Add(duplicateButton);
            toolbar.Add(deleteButton);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(refreshButton);
            
            // 视图选项
            toolbar.Add(new ToolbarSpacer { style = { flexGrow = 1 } });
            
            var viewToggle = new ToolbarToggle { text = "3D Preview" };
            viewToggle.value = true;
            viewToggle.RegisterValueChangedCallback(evt => 
            {
                previewContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            
            toolbar.Add(viewToggle);
            
            parent.Add(toolbar);
        }
        
        private void CreateLeftPanel()
        {
            leftPanel = new VisualElement();
            leftPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            leftPanel.style.paddingTop = 10;
            leftPanel.style.paddingBottom = 10;
            leftPanel.style.paddingLeft = 10;
            leftPanel.style.paddingRight = 10;
            
            // 搜索区域
            var searchContainer = new VisualElement();
            
            var searchLabel = new Label("🔍 Search & Filter");
            searchLabel.style.fontSize = 14;
            searchLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            searchLabel.style.marginBottom = 5;
            
            searchField = new TextField();
            searchField.value = "";
            searchField.RegisterValueChangedCallback(OnSearchChanged);
            
            // 过滤器
            var filterContainer = new VisualElement();
            filterContainer.style.marginTop = 10;
            
            var typeFilterToggle = new Toggle("Filter by Type");
            var typeFilterEnum = new EnumField("Type", EnemyType.Zombie);
            
            typeFilterToggle.RegisterValueChangedCallback(evt =>
            {
                editorData.useTypeFilter = evt.newValue;
                typeFilterEnum.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                RefreshEnemyList();
            });
            
            typeFilterEnum.RegisterValueChangedCallback(evt =>
            {
                editorData.filterType = (EnemyType)evt.newValue;
                RefreshEnemyList();
            });
            
            searchContainer.Add(searchLabel);
            searchContainer.Add(searchField);
            filterContainer.Add(typeFilterToggle);
            filterContainer.Add(typeFilterEnum);
            
            // 敌人列表
            var listLabel = new Label("🧟 Enemy List");
            listLabel.style.fontSize = 14;
            listLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            listLabel.style.marginTop = 15;
            listLabel.style.marginBottom = 5;
            
            enemyListView = new ListView();
            enemyListView.itemsSource = filteredItems;
            enemyListView.makeItem = MakeEnemyListItem;
            enemyListView.bindItem = BindEnemyListItem;
            enemyListView.selectionChanged += OnEnemySelectionChanged;
            enemyListView.style.flexGrow = 1;
            
            leftPanel.Add(searchContainer);
            leftPanel.Add(filterContainer);
            leftPanel.Add(listLabel);
            leftPanel.Add(enemyListView);
        }
        
        private void CreateCenterPreviewArea()
        {
            previewContainer = new VisualElement();
            previewContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            previewContainer.style.paddingTop = 10;
            previewContainer.style.paddingBottom = 10;
            previewContainer.style.paddingLeft = 10;
            previewContainer.style.paddingRight = 10;
            
            var previewHeader = new Label("👁 3D Preview");
            previewHeader.style.fontSize = 14;
            previewHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewHeader.style.marginBottom = 5;
            
            // IMGUI预览区域
            previewIMGUI = new IMGUIContainer(DrawPreviewGUI);
            previewIMGUI.style.flexGrow = 1;
            previewIMGUI.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            
            previewContainer.Add(previewHeader);
            previewContainer.Add(previewIMGUI);
        }
        
        private void CreateRightInspector()
        {
            inspectorContainer = new VisualElement();
            inspectorContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            inspectorContainer.style.paddingTop = 10;
            inspectorContainer.style.paddingBottom = 10;
            inspectorContainer.style.paddingLeft = 10;
            inspectorContainer.style.paddingRight = 10;
            
            var inspectorHeader = new Label("⚙ Inspector");
            inspectorHeader.style.fontSize = 14;
            inspectorHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            inspectorHeader.style.marginBottom = 5;
            
            inspectorScrollView = new ScrollView();
            inspectorScrollView.style.flexGrow = 1;
            
            inspectorContainer.Add(inspectorHeader);
            inspectorContainer.Add(inspectorScrollView);
        }
        
        private VisualElement MakeEnemyListItem()
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.paddingTop = 5;
            item.style.paddingBottom = 5;
            item.style.paddingLeft = 5;
            item.style.paddingRight = 5;
            item.style.borderBottomWidth = 1;
            item.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            
            var iconLabel = new Label();
            iconLabel.style.fontSize = 16;
            iconLabel.style.marginRight = 8;
            iconLabel.style.minWidth = 20;
            
            var infoContainer = new VisualElement();
            infoContainer.style.flexGrow = 1;
            
            var nameLabel = new Label();
            nameLabel.style.fontSize = 12;
            nameLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
            
            var typeLabel = new Label();
            typeLabel.style.fontSize = 10;
            typeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            
            infoContainer.Add(nameLabel);
            infoContainer.Add(typeLabel);
            
            item.Add(iconLabel);
            item.Add(infoContainer);
            
            return item;
        }
        
        private void BindEnemyListItem(VisualElement element, int index)
        {
            if (index >= filteredItems.Count) return;
            
            var item = filteredItems[index];
            var iconLabel = element.Children().ElementAt(0) as Label;
            var infoContainer = element.Children().ElementAt(1);
            var nameLabel = infoContainer.Children().ElementAt(0) as Label;
            var typeLabel = infoContainer.Children().ElementAt(1) as Label;
            
            if (item is EnemyConfig config)
            {
                iconLabel.text = GetEnemyTypeIcon(config.enemyType);
                nameLabel.text = config.enemyName;
                typeLabel.text = $"Config • {config.enemyType}";
                
                // 健康状态指示器
                var healthRatio = config.health / 100f;
                var healthColor = Color.Lerp(Color.red, Color.green, healthRatio);
                element.style.borderLeftColor = healthColor;
                element.style.borderLeftWidth = 3;
            }
            else if (item is EnemyData data)
            {
                iconLabel.text = GetEnemyTypeIcon(data.enemyType);
                nameLabel.text = data.enemyName;
                typeLabel.text = $"Data • {data.enemyType}";
                
                element.style.borderLeftColor = Color.blue;
                element.style.borderLeftWidth = 3;
            }
        }
        
        private void SetupEventHandlers()
        {
            // 键盘快捷键
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                    if (selectedConfig != null || selectedData != null)
                    {
                        DeleteSelected();
                        evt.StopPropagation();
                    }
                    break;
                    
                case KeyCode.D:
                    if (evt.ctrlKey && (selectedConfig != null || selectedData != null))
                    {
                        DuplicateSelected();
                        evt.StopPropagation();
                    }
                    break;
            }
        }
        
        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            editorData.currentFilter = evt.newValue;
            RefreshEnemyList();
        }
        
        private void OnEnemySelectionChanged(IEnumerable<object> selection)
        {
            var selected = selection.FirstOrDefault();
            
            selectedConfig = selected as EnemyConfig;
            selectedData = selected as EnemyData;
            
            UpdateInspector();
            UpdatePreview();
        }
        
        private void UpdateInspector()
        {
            inspectorScrollView.Clear();
            
            if (selectedConfig != null)
            {
                CreateConfigInspector(selectedConfig);
            }
            else if (selectedData != null)
            {
                CreateDataInspector(selectedData);
            }
            else
            {
                var welcomeLabel = new Label("Select an enemy to edit its properties");
                welcomeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                welcomeLabel.style.marginTop = 50;
                welcomeLabel.style.alignSelf = Align.Center;
                inspectorScrollView.Add(welcomeLabel);
            }
        }
        
        private void CreateConfigInspector(EnemyConfig config)
        {
            var serializedObject = new SerializedObject(config);
            
            // 基础属性组
            var basicGroup = CreateInspectorGroup("🎯 Basic Properties");
            
            var nameField = new PropertyField(serializedObject.FindProperty("enemyName"));
            var typeField = new PropertyField(serializedObject.FindProperty("enemyType"));
            var healthField = new PropertyField(serializedObject.FindProperty("health"));
            
            basicGroup.Add(nameField);
            basicGroup.Add(typeField);
            basicGroup.Add(healthField);
            
            // 移动设置组
            var movementGroup = CreateInspectorGroup("🏃 Movement Settings");
            
            var patrolSpeedField = new PropertyField(serializedObject.FindProperty("patrolSpeed"));
            var chaseSpeedField = new PropertyField(serializedObject.FindProperty("chaseSpeed"));
            var rotationSpeedField = new PropertyField(serializedObject.FindProperty("rotationSpeed"));
            
            movementGroup.Add(patrolSpeedField);
            movementGroup.Add(chaseSpeedField);
            movementGroup.Add(rotationSpeedField);
            
            // 感知系统组
            var perceptionGroup = CreateInspectorGroup("👁 Perception System");
            
            var visionRangeField = new PropertyField(serializedObject.FindProperty("visionRange"));
            var visionAngleField = new PropertyField(serializedObject.FindProperty("visionAngle"));
            var hearingRangeField = new PropertyField(serializedObject.FindProperty("hearingRange"));
            
            perceptionGroup.Add(visionRangeField);
            perceptionGroup.Add(visionAngleField);
            perceptionGroup.Add(hearingRangeField);
            
            // 攻击设置组
            var combatGroup = CreateInspectorGroup("⚔ Combat Settings");
            
            var attackDamageField = new PropertyField(serializedObject.FindProperty("attackDamage"));
            var attackRangeField = new PropertyField(serializedObject.FindProperty("attackRange"));
            var attackCooldownField = new PropertyField(serializedObject.FindProperty("attackCooldown"));
            
            combatGroup.Add(attackDamageField);
            combatGroup.Add(attackRangeField);
            combatGroup.Add(attackCooldownField);
            
            // 操作按钮
            var actionsGroup = CreateInspectorGroup("⚡ Actions");
            
            var saveButton = new Button(() => 
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }) { text = "💾 Save" };
            
            var createPrefabButton = new Button(() => CreatePrefabFromConfig(config)) 
            { 
                text = "🎮 Create Prefab" 
            };
            
            var validateButton = new Button(() => ValidateConfig(config)) 
            { 
                text = "✓ Validate" 
            };
            
            actionsGroup.Add(saveButton);
            actionsGroup.Add(createPrefabButton);
            actionsGroup.Add(validateButton);
            
            // 添加所有组到检视器
            inspectorScrollView.Add(basicGroup);
            inspectorScrollView.Add(movementGroup);
            inspectorScrollView.Add(perceptionGroup);
            inspectorScrollView.Add(combatGroup);
            inspectorScrollView.Add(actionsGroup);
            
            // 绑定序列化对象变化
            inspectorScrollView.Bind(serializedObject);
        }
        
        private void CreateDataInspector(EnemyData data)
        {
            var serializedObject = new SerializedObject(data);
            
            // 基础数据组
            var basicGroup = CreateInspectorGroup("📊 Basic Data");
            
            var nameField = new PropertyField(serializedObject.FindProperty("enemyName"));
            var typeField = new PropertyField(serializedObject.FindProperty("enemyType"));
            var healthField = new PropertyField(serializedObject.FindProperty("health"));
            var armorField = new PropertyField(serializedObject.FindProperty("armor"));
            
            basicGroup.Add(nameField);
            basicGroup.Add(typeField);
            basicGroup.Add(healthField);
            basicGroup.Add(armorField);
            
            // 战斗数据组
            var combatGroup = CreateInspectorGroup("⚔ Combat Data");
            
            var moveSpeedField = new PropertyField(serializedObject.FindProperty("moveSpeed"));
            var chaseSpeedField = new PropertyField(serializedObject.FindProperty("chaseSpeed"));
            var attackDamageField = new PropertyField(serializedObject.FindProperty("attackDamage"));
            var detectionRangeField = new PropertyField(serializedObject.FindProperty("detectionRange"));
            var attackRangeField = new PropertyField(serializedObject.FindProperty("attackRange"));
            
            combatGroup.Add(moveSpeedField);
            combatGroup.Add(chaseSpeedField);
            combatGroup.Add(attackDamageField);
            combatGroup.Add(detectionRangeField);
            combatGroup.Add(attackRangeField);
            
            inspectorScrollView.Add(basicGroup);
            inspectorScrollView.Add(combatGroup);
            
            inspectorScrollView.Bind(serializedObject);
        }
        
        private VisualElement CreateInspectorGroup(string title)
        {
            var group = new VisualElement();
            group.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            group.style.borderTopLeftRadius = 5;
            group.style.borderTopRightRadius = 5;
            group.style.borderBottomLeftRadius = 5;
            group.style.borderBottomRightRadius = 5;
            group.style.marginBottom = 10;
            group.style.paddingTop = 8;
            group.style.paddingBottom = 8;
            group.style.paddingLeft = 8;
            group.style.paddingRight = 8;
            
            var header = new Label(title);
            header.style.fontSize = 12;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.9f, 0.9f, 0.9f);
            header.style.marginBottom = 5;
            
            group.Add(header);
            
            return group;
        }
        
        private void DrawPreviewGUI()
        {
            if (selectedConfig == null && selectedData == null)
            {
                GUILayout.Label("Select an enemy to see preview", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            
            if (previewUtility == null)
            {
                InitializePreview();
            }
            
            var rect = GUILayoutUtility.GetRect(200, 300, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            if (Event.current.type == EventType.Repaint)
            {
                RenderPreview(rect);
            }
            
            HandlePreviewInput(rect);
        }
        
        private void InitializePreview()
        {
            previewUtility = new PreviewRenderUtility();
            previewUtility.camera.transform.position = new Vector3(0, 1, -3);
            previewUtility.camera.transform.LookAt(Vector3.up);
            
            // 设置光照
            previewUtility.lights[0].intensity = 1.4f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
            previewUtility.lights[1].intensity = 1.4f;
        }
        
        private void RenderPreview(Rect rect)
        {
            if (previewObject == null)
            {
                CreatePreviewObject();
            }
            
            if (previewObject == null) return;
            
            previewUtility.BeginPreview(rect, GUIStyle.none);
            
            // 设置相机
            var distance = 3f;
            var rotation = Quaternion.Euler(previewRotation.y, previewRotation.x, 0);
            previewUtility.camera.transform.position = rotation * Vector3.back * distance;
            previewUtility.camera.transform.LookAt(Vector3.zero);
            
            // 渲染预览对象
            var meshFilter = previewObject.GetComponent<MeshFilter>();
            var meshRenderer = previewObject.GetComponent<MeshRenderer>();
            
            if (meshFilter != null && meshRenderer != null && meshFilter.sharedMesh != null)
            {
                previewUtility.DrawMesh(
                    meshFilter.sharedMesh,
                    Matrix4x4.identity,
                    meshRenderer.sharedMaterial,
                    0
                );
            }
            
            previewUtility.camera.Render();
            
            var texture = previewUtility.EndPreview();
            GUI.DrawTexture(rect, texture);
        }
        
        private void CreatePreviewObject()
        {
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
            }
            
            if (selectedConfig != null)
            {
                previewObject = CreateEnemyPreviewObject(selectedConfig);
            }
            else if (selectedData != null)
            {
                previewObject = CreateEnemyPreviewObject(selectedData);
            }
        }
        
        private GameObject CreateEnemyPreviewObject(object enemyObject)
        {
            var obj = new GameObject("PreviewEnemy");
            obj.hideFlags = HideFlags.HideAndDontSave;
            
            // 添加基础几何体
            var meshFilter = obj.AddComponent<MeshFilter>();
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            
            EnemyType type = EnemyType.Zombie;
            if (enemyObject is EnemyConfig config) type = config.enemyType;
            else if (enemyObject is EnemyData data) type = data.enemyType;
            
            // 根据类型创建不同的预览模型
            switch (type)
            {
                case EnemyType.Zombie:
                    meshFilter.mesh = CreateCapsuleMesh();
                    meshRenderer.material = CreateMaterial(new Color(0.6f, 0.8f, 0.4f));
                    break;
                case EnemyType.Shooter:
                    meshFilter.mesh = CreateCubeMesh();
                    meshRenderer.material = CreateMaterial(new Color(0.8f, 0.4f, 0.4f));
                    break;
                case EnemyType.Snipers:
                    meshFilter.mesh = CreateCubeMesh();
                    meshRenderer.material = CreateMaterial(new Color(0.4f, 0.4f, 0.8f));
                    break;
            }
            
            return obj;
        }
        
        private void HandlePreviewInput(Rect rect)
        {
            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.GetTypeForControl(controlID);
            
            switch (eventType)
            {
                case EventType.MouseDown:
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        previewRotation.x += Event.current.delta.x;
                        previewRotation.y += Event.current.delta.y;
                        Event.current.Use();
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                    break;
            }
        }
        
        private void RefreshData()
        {
            editorData.enemyConfigs.Clear();
            editorData.enemyData.Clear();
            
            // 加载EnemyConfig资产
            var configGuids = AssetDatabase.FindAssets("t:EnemyConfig");
            foreach (var guid in configGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
                if (config != null) editorData.enemyConfigs.Add(config);
            }
            
            // 加载EnemyData资产
            var dataGuids = AssetDatabase.FindAssets("t:EnemyData");
            foreach (var guid in dataGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (data != null) editorData.enemyData.Add(data);
            }
            
            RefreshEnemyList();
        }
        
        private void RefreshEnemyList()
        {
            filteredItems.Clear();
            
            // 过滤配置
            var configs = editorData.enemyConfigs.AsEnumerable();
            if (editorData.useTypeFilter)
            {
                configs = configs.Where(c => c.enemyType == editorData.filterType);
            }
            if (!string.IsNullOrEmpty(editorData.currentFilter))
            {
                configs = configs.Where(c => c.enemyName.IndexOf(editorData.currentFilter, 
                    System.StringComparison.OrdinalIgnoreCase) >= 0);
            }
            
            // 过滤数据
            var data = editorData.enemyData.AsEnumerable();
            if (editorData.useTypeFilter)
            {
                data = data.Where(d => d.enemyType == editorData.filterType);
            }
            if (!string.IsNullOrEmpty(editorData.currentFilter))
            {
                data = data.Where(d => d.enemyName.IndexOf(editorData.currentFilter, 
                    System.StringComparison.OrdinalIgnoreCase) >= 0);
            }
            
            filteredItems.AddRange(configs);
            filteredItems.AddRange(data);
            
            if (enemyListView != null)
            {
                enemyListView.RefreshItems();
            }
        }
        
        // 操作方法
        private void CreateNewEnemy()
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Enemy Config/🧟 Zombie"), false, () => 
                CreateNewEnemyConfig(EnemyType.Zombie));
            menu.AddItem(new GUIContent("Enemy Config/🏹 Shooter"), false, () => 
                CreateNewEnemyConfig(EnemyType.Shooter));
            menu.AddItem(new GUIContent("Enemy Config/🎯 Sniper"), false, () => 
                CreateNewEnemyConfig(EnemyType.Snipers));
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Enemy Data/🧟 Zombie Data"), false, () => 
                CreateNewEnemyData(EnemyType.Zombie));
            menu.AddItem(new GUIContent("Enemy Data/🏹 Shooter Data"), false, () => 
                CreateNewEnemyData(EnemyType.Shooter));
            menu.AddItem(new GUIContent("Enemy Data/🎯 Sniper Data"), false, () => 
                CreateNewEnemyData(EnemyType.Snipers));
            
            menu.ShowAsContext();
        }
        
        private void CreateNewEnemyConfig(EnemyType type)
        {
            var path = EditorUtility.SaveFilePanel(
                "Create Enemy Config", 
                "Assets/Data/Enemies", 
                $"New{type}Config", 
                "asset");
                
            if (string.IsNullOrEmpty(path)) return;
            
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }
            
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.enemyName = $"New {type}";
            config.enemyType = type;
            SetConfigDefaults(config, type);
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            RefreshData();
            
            // 选择新创建的配置
            var index = filteredItems.IndexOf(config);
            if (index >= 0 && enemyListView != null)
            {
                enemyListView.selectedIndex = index;
            }
        }
        
        private void CreateNewEnemyData(EnemyType type)
        {
            var path = EditorUtility.SaveFilePanel(
                "Create Enemy Data", 
                "Assets/Data/Enemies", 
                $"New{type}Data", 
                "asset");
                
            if (string.IsNullOrEmpty(path)) return;
            
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }
            
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = $"New {type} Data";
            data.enemyType = type;
            SetDataDefaults(data, type);
            
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            
            RefreshData();
            
            // 选择新创建的数据
            var index = filteredItems.IndexOf(data);
            if (index >= 0 && enemyListView != null)
            {
                enemyListView.selectedIndex = index;
            }
        }
        
        private void DuplicateSelected()
        {
            if (selectedConfig != null)
            {
                DuplicateAsset(selectedConfig);
            }
            else if (selectedData != null)
            {
                DuplicateAsset(selectedData);
            }
        }
        
        private void DuplicateAsset(Object asset)
        {
            var originalPath = AssetDatabase.GetAssetPath(asset);
            var newPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
            
            if (AssetDatabase.CopyAsset(originalPath, newPath))
            {
                AssetDatabase.SaveAssets();
                RefreshData();
                
                var duplicate = AssetDatabase.LoadAssetAtPath<Object>(newPath);
                var index = filteredItems.IndexOf(duplicate);
                if (index >= 0 && enemyListView != null)
                {
                    enemyListView.selectedIndex = index;
                }
            }
        }
        
        private void DeleteSelected()
        {
            Object toDelete = selectedConfig ?? (Object)selectedData;
            if (toDelete == null) return;
            
            var name = toDelete.name;
            if (EditorUtility.DisplayDialog("Delete Confirmation", 
                    $"Are you sure you want to delete '{name}'?", "Delete", "Cancel"))
            {
                var path = AssetDatabase.GetAssetPath(toDelete);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                
                selectedConfig = null;
                selectedData = null;
                RefreshData();
                UpdateInspector();
                UpdatePreview();
            }
        }
        
        private void CreatePrefabFromConfig(EnemyConfig config)
        {
            var path = EditorUtility.SaveFilePanel(
                "Create Enemy Prefab", 
                "Assets/Prefabs/Enemies", 
                config.enemyName, 
                "prefab");
                
            if (string.IsNullOrEmpty(path)) return;
            
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }
            
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
            
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            DestroyImmediate(prefab);
            
            EditorUtility.DisplayDialog("Success", $"Enemy prefab created: {path}", "OK");
        }
        
        private void ValidateConfig(EnemyConfig config)
        {
            var issues = new List<string>();
            
            if (string.IsNullOrEmpty(config.enemyName))
                issues.Add("Enemy name is empty");
            
            if (config.health <= 0)
                issues.Add("Health must be greater than 0");
            
            if (config.patrolSpeed < 0)
                issues.Add("Patrol speed cannot be negative");
            
            if (config.chaseSpeed < config.patrolSpeed)
                issues.Add("Chase speed should be greater than patrol speed");
            
            if (config.visionRange <= 0)
                issues.Add("Vision range must be greater than 0");
            
            if (config.visionAngle <= 0 || config.visionAngle > 360)
                issues.Add("Vision angle must be between 0 and 360 degrees");
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation", "Enemy configuration is valid!", "OK");
            }
            else
            {
                var message = "Validation issues found:\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            }
        }
        
        private void UpdatePreview()
        {
            CreatePreviewObject();
        }
        
        private void CleanupPreview()
        {
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
            }
            
            previewUtility?.Cleanup();
        }
        
        // 工具方法
        private void SetConfigDefaults(EnemyConfig config, EnemyType type)
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
        
        private void SetDataDefaults(EnemyData data, EnemyType type)
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
        
        private string GetEnemyTypeIcon(EnemyType type)
        {
            return type switch
            {
                EnemyType.Zombie => "Zb",
                EnemyType.Shooter => "St",
                EnemyType.Snipers => "Sn",
                _ => "👾"
            };
        }
        
        private Mesh CreateCapsuleMesh()
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var mesh = capsule.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(capsule);
            return mesh;
        }
        
        private Mesh CreateCubeMesh()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = cube.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(cube);
            return mesh;
        }
        
        private Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }
    }


}
#endif