using System.Collections.Generic;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ModernEventEditorWindow : EditorWindow
{
    [System.Serializable]
    public class EditorEventNode
    {
        public RandomEvent eventData;
        public Rect rect;
        public bool isSelected;
    }

    // 核心字段
    private Vector2 scrollPos;
    private Vector2 nodeScrollPos;
    private Vector2 statisticsScrollPos;
    private Rect nodeArea = new Rect(0, 0, 2000, 2000);
    
    // 事件数据
    private RandomEvent selectedEvent;
    private List<RandomEvent> allEvents = new List<RandomEvent>();
    private List<EditorEventNode> eventNodes = new List<EditorEventNode>();
    private HashSet<RandomEvent> selectedEvents = new HashSet<RandomEvent>();
    
    // 节点编辑状态
    private bool isDragging = false;
    private Vector2 dragStartPos;
    private EditorEventNode selectedNode;
    private EditorEventNode draggingNode;
    
    // 过滤和搜索
    private string eventSearchFilter = "";
    private EventType eventTypeFilter = EventType.ResourceGain;
    private bool useEventTypeFilter = false;
    private EventPriority eventPriorityFilter = EventPriority.Normal;
    private bool useEventPriorityFilter = false;
    
    // UI状态
    private bool showEventTesting = false;
    private bool showEventStatistics = false;
    private bool showQuestFields = false;
    private bool isRenamingEvent = false;
    private string renamingText = "";
    private RandomEvent renamingEvent = null;
    
    // 现代UI样式
    private GUIStyle modernCardStyle;
    private GUIStyle modernHeaderStyle;
    private GUIStyle modernButtonStyle;
    private GUIStyle modernToolbarStyle;
    private GUIStyle modernSelectedStyle;
    private GUIStyle modernSectionStyle;
    private GUIStyle modernIconButtonStyle;
    private GUIStyle modernNodeStyle;
    private GUIStyle modernSelectedNodeStyle;
    private GUIStyle modernSearchStyle;
    private GUIStyle modernTabStyle;
    private GUIStyle modernActiveTabStyle;
    
    // 颜色主题
    private static readonly Color PrimaryColor = new Color(0.3f, 0.7f, 1f);
    private static readonly Color SecondaryColor = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color AccentColor = new Color(0.4f, 0.9f, 0.5f);
    private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.08f);
    private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.12f);
    private static readonly Color CardColor = new Color(0.18f, 0.18f, 0.18f);
    private static readonly Color TextColor = new Color(0.9f, 0.9f, 0.9f);
    private static readonly Color DangerColor = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color WarningColor = new Color(1f, 0.7f, 0.2f);
    private static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.4f);
    
    // 图标和符号
    private const string IconEvent = "📅";
    private const string IconAdd = "＋";
    private const string IconDelete = "🗑";
    private const string IconCopy = "📋";
    private const string IconSettings = "⚙";
    private const string IconSearch = "🔍";
    private const string IconFilter = "🔽";
    private const string IconSave = "💾";
    private const string IconRefresh = "🔄";
    private const string IconPlay = "▶";
    private const string IconStats = "📊";
    private const string IconHelp = "❓";
    private const string IconRename = "✏";
    private const string IconLink = "🔗";
    private const string IconNode = "◯";
    private const string IconQuest = "🎯";
    private const string IconChoice = "🔀";

    [MenuItem("Game Tools/Modern Event Editor")]
    public static void OpenWindow()
    {
        ModernEventEditorWindow window = GetWindow<ModernEventEditorWindow>("事件编辑器");
        window.minSize = new Vector2(1200, 800);
        window.Show();
    }
    
    void OnEnable()
    {
        LoadAllEvents();
        InitializeModernStyles();
    }
    
    void InitializeModernStyles()
    {
        // 现代卡片样式
        modernCardStyle = new GUIStyle();
        modernCardStyle.normal.background = CreateRoundedTexture(CardColor, 8);
        modernCardStyle.border = new RectOffset(8, 8, 8, 8);
        modernCardStyle.padding = new RectOffset(16, 16, 12, 12);
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
        modernButtonStyle.padding = new RectOffset(16, 16, 8, 8);
        modernButtonStyle.margin = new RectOffset(2, 2, 2, 2);
        modernButtonStyle.alignment = TextAnchor.MiddleCenter;
        modernButtonStyle.fontStyle = FontStyle.Bold;
        
        // 图标按钮样式
        modernIconButtonStyle = new GUIStyle(modernButtonStyle);
        modernIconButtonStyle.padding = new RectOffset(8, 8, 6, 6);
        modernIconButtonStyle.fontSize = 14;
        
        // 工具栏样式
        modernToolbarStyle = new GUIStyle();
        modernToolbarStyle.normal.background = CreateGradientTexture(SecondaryColor, new Color(0.2f, 0.2f, 0.2f));
        modernToolbarStyle.padding = new RectOffset(12, 12, 10, 10);
        
        // 选中样式
        modernSelectedStyle = new GUIStyle();
        modernSelectedStyle.normal.background = CreateRoundedTexture(new Color(PrimaryColor.r, PrimaryColor.g, PrimaryColor.b, 0.3f), 6);
        modernSelectedStyle.border = new RectOffset(6, 6, 6, 6);
        modernSelectedStyle.padding = new RectOffset(12, 12, 8, 8);
        modernSelectedStyle.margin = new RectOffset(2, 2, 2, 2);
        
        // 区域样式
        modernSectionStyle = new GUIStyle();
        modernSectionStyle.normal.background = CreateRoundedTexture(PanelColor, 8);
        modernSectionStyle.border = new RectOffset(8, 8, 8, 8);
        modernSectionStyle.padding = new RectOffset(16, 16, 16, 16);
        modernSectionStyle.margin = new RectOffset(4, 4, 4, 4);
        
        // 节点样式
        modernNodeStyle = new GUIStyle();
        modernNodeStyle.normal.background = CreateRoundedTexture(CardColor, 8);
        modernNodeStyle.border = new RectOffset(8, 8, 8, 8);
        modernNodeStyle.padding = new RectOffset(12, 12, 12, 12);
        modernNodeStyle.normal.textColor = TextColor;
        modernNodeStyle.alignment = TextAnchor.UpperLeft;
        modernNodeStyle.fontSize = 11;
        
        modernSelectedNodeStyle = new GUIStyle(modernNodeStyle);
        modernSelectedNodeStyle.normal.background = CreateRoundedTexture(new Color(PrimaryColor.r, PrimaryColor.g, PrimaryColor.b, 0.8f), 8);
        
        // 搜索样式
        modernSearchStyle = new GUIStyle(EditorStyles.textField);
        modernSearchStyle.normal.background = CreateRoundedTexture(new Color(0.2f, 0.2f, 0.2f), 4);
        modernSearchStyle.border = new RectOffset(4, 4, 4, 4);
        modernSearchStyle.padding = new RectOffset(8, 8, 6, 6);
        
        // 标签页样式
        modernTabStyle = new GUIStyle();
        modernTabStyle.normal.background = CreateRoundedTexture(new Color(0.15f, 0.15f, 0.15f), 4);
        modernTabStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        modernTabStyle.padding = new RectOffset(12, 12, 8, 8);
        modernTabStyle.margin = new RectOffset(2, 2, 2, 2);
        modernTabStyle.alignment = TextAnchor.MiddleCenter;
        
        modernActiveTabStyle = new GUIStyle(modernTabStyle);
        modernActiveTabStyle.normal.background = CreateRoundedTexture(PrimaryColor, 4);
        modernActiveTabStyle.normal.textColor = Color.white;
        modernActiveTabStyle.fontStyle = FontStyle.Bold;
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
    
    Texture2D CreateGradientTexture(Color startColor, Color endColor)
    {
        Texture2D texture = new Texture2D(1, 32);
        for (int y = 0; y < 32; y++)
        {
            float t = y / 31f;
            Color color = Color.Lerp(startColor, endColor, t);
            texture.SetPixel(0, y, color);
        }
        texture.Apply();
        return texture;
    }
    
    void LoadAllEvents()
    {
        allEvents.Clear();
        string[] guids = AssetDatabase.FindAssets("t:RandomEvent");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RandomEvent eventAsset = AssetDatabase.LoadAssetAtPath<RandomEvent>(path);
            if (eventAsset != null)
            {
                allEvents.Add(eventAsset);
            }
        }
        
        LoadEventNodes();
    }
    
    void LoadEventNodes()
    {
        eventNodes.Clear();
        for (int i = 0; i < allEvents.Count; i++)
        {
            var eventData = allEvents[i];
            
            Vector2 nodePos = Vector2.zero;
            if (eventData.nodePosition != null && eventData.nodePosition.position != Vector2.zero)
            {
                nodePos = eventData.nodePosition.position;
            }
            else
            {
                nodePos = new Vector2(100 + (i % 5) * 220, 100 + (i / 5) * 160);
            }
            
            var node = new EditorEventNode
            {
                eventData = eventData,
                rect = new Rect(nodePos.x, nodePos.y, 200, 140),
                isSelected = false
            };
            eventNodes.Add(node);
        }
    }
    
    void OnGUI()
    {
        if (modernCardStyle == null) InitializeModernStyles();
        
        // 设置背景色
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BackgroundColor);
        
        DrawModernToolbar();
        
        // 主要分割布局
        Rect leftPanel = new Rect(8, 60, 400, position.height - 68);
        Rect rightPanel = new Rect(416, 60, position.width - 424, position.height - 68);
        
        DrawModernLeftPanel(leftPanel);
        DrawModernRightPanel(rightPanel);
        
        HandleKeyboardInput();
        ProcessNodeEvents();
    }
    
    void DrawModernToolbar()
    {
        EditorGUILayout.BeginHorizontal(modernToolbarStyle, GUILayout.Height(55));
        
        // 主要操作按钮组
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button($"{IconAdd} 新建事件", modernButtonStyle, GUILayout.Height(35)))
        {
            CreateNewEvent();
        }
        
        GUILayout.Space(8);
        
        if (GUILayout.Button(IconRefresh, modernIconButtonStyle, GUILayout.Width(40), GUILayout.Height(35)))
        {
            LoadAllEvents();
        }
        
        if (GUILayout.Button(IconSave, modernIconButtonStyle, GUILayout.Width(40), GUILayout.Height(35)))
        {
            SaveAllEvents();
        }
        
        if (GUILayout.Button("✓", modernIconButtonStyle, GUILayout.Width(40), GUILayout.Height(35)))
        {
            ValidateAllEvents();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.FlexibleSpace();
        
        // 搜索和过滤区域
        DrawModernSearchAndFilter();
        
        GUILayout.FlexibleSpace();
        
        // 工具切换区域
        EditorGUILayout.BeginHorizontal();
        
        var testingStyle = showEventTesting ? modernActiveTabStyle : modernTabStyle;
        if (GUILayout.Button($"{IconPlay} 测试", testingStyle, GUILayout.Height(35)))
        {
            showEventTesting = !showEventTesting;
        }
        
        var statsStyle = showEventStatistics ? modernActiveTabStyle : modernTabStyle;
        if (GUILayout.Button($"{IconStats} 统计", statsStyle, GUILayout.Height(35)))
        {
            showEventStatistics = !showEventStatistics;
        }
        
        if (GUILayout.Button(IconHelp, modernIconButtonStyle, GUILayout.Width(40), GUILayout.Height(35)))
        {
            ShowHelp();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndHorizontal();
        
        // 绘制工具栏分隔线
        EditorGUI.DrawRect(new Rect(0, 55, position.width, 2), new Color(0.3f, 0.3f, 0.3f));
    }
    
    void DrawModernSearchAndFilter()
    {
        EditorGUILayout.BeginVertical();
        
        // 搜索栏
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(IconSearch, GUILayout.Width(20));
        
        var newSearchText = EditorGUILayout.TextField(eventSearchFilter, modernSearchStyle, GUILayout.Width(200));
        if (newSearchText != eventSearchFilter)
        {
            eventSearchFilter = newSearchText;
        }
        
        if (!string.IsNullOrEmpty(eventSearchFilter))
        {
            if (GUILayout.Button("✗", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                eventSearchFilter = "";
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 过滤器
        if (useEventTypeFilter || useEventPriorityFilter)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (useEventTypeFilter)
            {
                var typeColor = GetEventTypeColor(eventTypeFilter);
                GUI.color = typeColor;
                GUILayout.Label($"类型: {eventTypeFilter}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            
            if (useEventPriorityFilter)
            {
                var priorityColor = GetPriorityColor(eventPriorityFilter);
                GUI.color = priorityColor;
                GUILayout.Label($"优先级: {eventPriorityFilter}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            
            if (GUILayout.Button("清除", EditorStyles.miniButton))
            {
                useEventTypeFilter = false;
                useEventPriorityFilter = false;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawModernLeftPanel(Rect panel)
    {
        GUILayout.BeginArea(panel);
        
        EditorGUILayout.BeginVertical(modernSectionStyle, GUILayout.ExpandHeight(true));
        
        // 标题栏
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{IconEvent} 事件列表", modernHeaderStyle);
        
        GUILayout.FlexibleSpace();
        
        var eventCount = GetFilteredEvents().Count();
        var countStyle = new GUIStyle(EditorStyles.miniLabel);
        countStyle.normal.textColor = PrimaryColor;
        GUILayout.Label($"{eventCount}/{allEvents.Count}", countStyle);
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(8);
        
        DrawModernEventFilters();
        
        GUILayout.Space(8);
        
        // 事件列表
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        var filteredEvents = GetFilteredEvents();
        foreach (var eventData in filteredEvents)
        {
            DrawModernEventListItem(eventData);
        }
        
        if (filteredEvents.Count() == 0)
        {
            EditorGUILayout.BeginVertical(modernCardStyle);
            var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            emptyStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("没有找到匹配的事件", emptyStyle);
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
        
        // 详细信息面板
        if (selectedEvent != null)
        {
            GUILayout.Space(8);
            DrawModernEventDetails();
        }
        
        // 测试和统计面板
        if (showEventTesting)
        {
            GUILayout.Space(8);
            DrawModernEventTestingPanel();
        }
        
        if (showEventStatistics)
        {
            GUILayout.Space(8);
            DrawModernEventStatisticsPanel();
        }
        
        EditorGUILayout.EndVertical();
        
        GUILayout.EndArea();
    }
    
    void DrawModernEventFilters()
    {
        EditorGUILayout.BeginVertical(modernCardStyle);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"{IconFilter} 过滤器", EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        if (useEventTypeFilter || useEventPriorityFilter)
        {
            if (GUILayout.Button("重置", EditorStyles.miniButton))
            {
                useEventTypeFilter = false;
                useEventPriorityFilter = false;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(4);
        
        // 类型过滤器
        EditorGUILayout.BeginHorizontal();
        useEventTypeFilter = EditorGUILayout.Toggle(useEventTypeFilter, GUILayout.Width(20));
        GUI.enabled = useEventTypeFilter;
        GUILayout.Label("类型:", GUILayout.Width(40));
        eventTypeFilter = (EventType)EditorGUILayout.EnumPopup(eventTypeFilter);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        // 优先级过滤器
        EditorGUILayout.BeginHorizontal();
        useEventPriorityFilter = EditorGUILayout.Toggle(useEventPriorityFilter, GUILayout.Width(20));
        GUI.enabled = useEventPriorityFilter;
        GUILayout.Label("优先级:", GUILayout.Width(40));
        eventPriorityFilter = (EventPriority)EditorGUILayout.EnumPopup(eventPriorityFilter);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    List<RandomEvent> GetFilteredEvents()
    {
        var filtered = allEvents.AsEnumerable();
        
        if (!string.IsNullOrEmpty(eventSearchFilter))
        {
            string lowerFilter = eventSearchFilter.ToLower();
            filtered = filtered.Where(e => 
                e.eventName.ToLower().Contains(lowerFilter) ||
                e.eventDescription.ToLower().Contains(lowerFilter) ||
                e.eventType.ToString().ToLower().Contains(lowerFilter));
        }
        
        if (useEventTypeFilter)
        {
            filtered = filtered.Where(e => e.eventType == eventTypeFilter);
        }
        
        if (useEventPriorityFilter)
        {
            filtered = filtered.Where(e => e.priority == eventPriorityFilter);
        }
        
        return filtered.ToList();
    }
    
    void DrawModernEventListItem(RandomEvent eventData)
    {
        bool isSelected = selectedEvent == eventData;
        
        var itemStyle = isSelected ? modernSelectedStyle : modernCardStyle;
        var itemStyleCopy = new GUIStyle(itemStyle);
        itemStyleCopy.margin = new RectOffset(0, 0, 2, 2);
        
        EditorGUILayout.BeginVertical(itemStyleCopy);
        
        EditorGUILayout.BeginHorizontal();
        
        // 事件图标和优先级指示器
        var priorityColor = GetPriorityColor(eventData.priority);
        GUI.color = priorityColor;
        GUILayout.Label("●", GUILayout.Width(15));
        GUI.color = Color.white;
        
        // 重命名处理
        if (isRenamingEvent && renamingEvent == eventData)
        {
            GUI.SetNextControlName("RenameField");
            renamingText = EditorGUILayout.TextField(renamingText);
            
            if (GUILayout.Button("✓", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                if (!string.IsNullOrEmpty(renamingText))
                {
                    RenameEvent(eventData, renamingText);
                }
                ExitRenameMode();
            }
            
            if (GUILayout.Button("✗", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                ExitRenameMode();
            }
        }
        else
        {
            // 主按钮
            if (GUILayout.Button(eventData.eventName, EditorStyles.label))
            {
                selectedEvent = eventData;
                selectedNode = eventNodes.FirstOrDefault(n => n.eventData == eventData);
                UpdateNodeSelection();
            }
            
            GUILayout.FlexibleSpace();
            
            // 快速信息指示器
            DrawEventIndicators(eventData);
            
            // 操作按钮
            if (GUILayout.Button("⋯", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                ShowEventContextMenu(eventData);
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 选中时显示详细信息
        if (isSelected)
        {
            GUILayout.Space(4);
            DrawEventQuickInfo(eventData);
        }
        
        EditorGUILayout.EndVertical();
        
        // 处理右键菜单
        Event currentEvent = Event.current;
        if (currentEvent.type == UnityEngine.EventType.ContextClick && 
            GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
        {
            ShowEventContextMenu(eventData);
            currentEvent.Use();
        }
    }
    
    void DrawEventIndicators(RandomEvent eventData)
    {
        // 选择数量指示器
        if (eventData.choices != null && eventData.choices.Length > 0)
        {
            GUI.color = AccentColor;
            GUILayout.Label($"{IconChoice}{eventData.choices.Length}", EditorStyles.miniLabel, GUILayout.Width(30));
            GUI.color = Color.white;
        }
        
        // 任务指示器
        if (eventData.isQuest)
        {
            GUI.color = WarningColor;
            GUILayout.Label(IconQuest, EditorStyles.miniLabel, GUILayout.Width(15));
            GUI.color = Color.white;
        }
        
        // 类型指示器
        var typeIcon = GetEventTypeIcon(eventData.eventType);
        var typeColor = GetEventTypeColor(eventData.eventType);
        GUI.color = typeColor;
        GUILayout.Label(typeIcon, EditorStyles.miniLabel, GUILayout.Width(15));
        GUI.color = Color.white;
    }
    
    void DrawEventQuickInfo(RandomEvent eventData)
    {
        var miniStyle = new GUIStyle(EditorStyles.miniLabel);
        miniStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"类型: {eventData.eventType}", miniStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"天数: {eventData.minDay}-{eventData.maxDay}", miniStyle);
        EditorGUILayout.EndHorizontal();
        
        if (!string.IsNullOrEmpty(eventData.eventDescription))
        {
            var descStyle = new GUIStyle(EditorStyles.miniLabel);
            descStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            descStyle.wordWrap = true;
            
            string shortDesc = eventData.eventDescription.Length > 60 ? 
                eventData.eventDescription.Substring(0, 60) + "..." : 
                eventData.eventDescription;
            GUILayout.Label(shortDesc, descStyle);
        }
    }
    
    void DrawModernEventDetails()
    {
        EditorGUILayout.BeginVertical(modernCardStyle);
        
        EditorGUILayout.LabelField("📝 事件详情", modernHeaderStyle);
        
        EditorGUI.BeginChangeCheck();
        
        selectedEvent.eventName = EditorGUILayout.TextField("事件名称", selectedEvent.eventName);
        selectedEvent.eventType = (EventType)EditorGUILayout.EnumPopup("事件类型", selectedEvent.eventType);
        selectedEvent.priority = (EventPriority)EditorGUILayout.EnumPopup("优先级", selectedEvent.priority);
        
        GUILayout.Space(4);
        
        GUILayout.Label("描述:");
        selectedEvent.eventDescription = EditorGUILayout.TextArea(selectedEvent.eventDescription, GUILayout.Height(50));
        
        GUILayout.Space(4);
        
        EditorGUILayout.BeginHorizontal();
        selectedEvent.minDay = EditorGUILayout.IntField("最小天数", selectedEvent.minDay);
        selectedEvent.maxDay = EditorGUILayout.IntField("最大天数", selectedEvent.maxDay);
        EditorGUILayout.EndHorizontal();
        
        selectedEvent.baseTriggerChance = EditorGUILayout.Slider("触发概率", selectedEvent.baseTriggerChance, 0f, 1f);
        
        EditorGUILayout.BeginHorizontal();
        selectedEvent.requiresChoice = EditorGUILayout.Toggle("需要选择", selectedEvent.requiresChoice);
        selectedEvent.canRepeat = EditorGUILayout.Toggle("可重复", selectedEvent.canRepeat);
        EditorGUILayout.EndHorizontal();
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedEvent);
        }
        
        GUILayout.Space(8);
        
        if (GUILayout.Button($"{IconChoice} 编辑选择", modernButtonStyle, GUILayout.Height(30)))
        {
            ModernEventChoiceEditor.OpenWindow(selectedEvent);
        }
        
        // 任务设置
        selectedEvent.isQuest = EditorGUILayout.Toggle("是任务", selectedEvent.isQuest);
        
        if (selectedEvent.isQuest)
        {
            showQuestFields = EditorGUILayout.Foldout(showQuestFields, "任务设置");
            if (showQuestFields)
            {
                DrawQuestFields();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawQuestFields()
    {
        EditorGUILayout.BeginVertical("box");
        
        selectedEvent.isMainQuest = EditorGUILayout.Toggle("主线任务", selectedEvent.isMainQuest);
        selectedEvent.isSideQuest = EditorGUILayout.Toggle("支线任务", selectedEvent.isSideQuest);
        selectedEvent.questChain = EditorGUILayout.TextField("任务链", selectedEvent.questChain ?? "");
        selectedEvent.questOrder = EditorGUILayout.IntField("任务顺序", selectedEvent.questOrder);
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawModernRightPanel(Rect panel)
    {
        GUILayout.BeginArea(panel);
        
        EditorGUILayout.BeginVertical(modernSectionStyle, GUILayout.ExpandHeight(true));
        
        EditorGUILayout.LabelField($"{IconNode} 事件流程图", modernHeaderStyle);
        
        GUILayout.Space(8);
        
        Rect graphArea = new Rect(0, 40, panel.width - 32, panel.height - 72);
        DrawModernEventGraph(graphArea);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.EndArea();
    }
    
    void DrawModernEventGraph(Rect area)
    {
        DrawModernGrid(area);
        
        nodeScrollPos = GUI.BeginScrollView(area, nodeScrollPos, nodeArea);
        
        DrawModernConnections();
        DrawModernNodes();
        
        GUI.EndScrollView();
    }
    
    void DrawModernGrid(Rect area)
    {
        int gridSpacing = 25;
        Color gridColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
        Color majorGridColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        
        Handles.BeginGUI();
        
        // 小网格
        Handles.color = gridColor;
        for (int x = 0; x < area.width; x += gridSpacing)
        {
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, area.height));
        }
        
        for (int y = 0; y < area.height; y += gridSpacing)
        {
            Handles.DrawLine(new Vector3(0, y), new Vector3(area.width, y));
        }
        
        // 主网格
        Handles.color = majorGridColor;
        for (int x = 0; x < area.width; x += gridSpacing * 4)
        {
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, area.height));
        }
        
        for (int y = 0; y < area.height; y += gridSpacing * 4)
        {
            Handles.DrawLine(new Vector3(0, y), new Vector3(area.width, y));
        }
        
        Handles.EndGUI();
    }
    
    void DrawModernNodes()
    {
        BeginWindows();
        
        for (int i = 0; i < eventNodes.Count; i++)
        {
            var node = eventNodes[i];
            if (node.eventData != null)
            {
                GUIStyle style = node.isSelected ? modernSelectedNodeStyle : modernNodeStyle;
                node.rect = GUILayout.Window(i, node.rect, DrawModernNodeWindow, "", style);
            }
        }
        
        EndWindows();
    }
    
    void DrawModernNodeWindow(int id)
    {
        if (id < 0 || id >= eventNodes.Count) return;
        
        var node = eventNodes[id];
        var eventData = node.eventData;
        
        // 标题
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.normal.textColor = Color.white;
        titleStyle.fontSize = 12;
        
        // 事件名称（限制长度）
        string displayName = eventData.eventName.Length > 18 ? 
            eventData.eventName.Substring(0, 15) + "..." : 
            eventData.eventName;
        
        GUILayout.Label(displayName, titleStyle);
        
        GUILayout.Space(2);
        
        // 事件类型和优先级
        EditorGUILayout.BeginHorizontal();
        
        var typeColor = GetEventTypeColor(eventData.eventType);
        GUI.color = typeColor;
        GUILayout.Label(GetEventTypeIcon(eventData.eventType), GUILayout.Width(15));
        GUI.color = Color.white;
        
        var typeStyle = new GUIStyle(EditorStyles.miniLabel);
        typeStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        GUILayout.Label(eventData.eventType.ToString(), typeStyle);
        
        EditorGUILayout.EndHorizontal();
        
        // 优先级指示器
        EditorGUILayout.BeginHorizontal();
        var priorityColor = GetPriorityColor(eventData.priority);
        GUI.color = priorityColor;
        GUILayout.Label("●", GUILayout.Width(15));
        GUI.color = Color.white;
        
        GUILayout.Label($"{eventData.priority}", typeStyle);
        EditorGUILayout.EndHorizontal();
        
        // 统计信息
        GUILayout.Space(2);
        
        var statsStyle = new GUIStyle(EditorStyles.miniLabel);
        statsStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        
        GUILayout.Label($"天数: {eventData.minDay}-{eventData.maxDay}", statsStyle);
        GUILayout.Label($"概率: {eventData.baseTriggerChance:P0}", statsStyle);
        
        if (eventData.choices != null && eventData.choices.Length > 0)
        {
            GUILayout.Label($"选择: {eventData.choices.Length}", statsStyle);
        }
        
        // 处理事件
        Event currentEvent = Event.current;
        if (currentEvent.type == UnityEngine.EventType.MouseDown)
        {
            selectedEvent = node.eventData;
            selectedNode = node;
            UpdateNodeSelection();
            Repaint();
        }
        
        // 保存节点位置
        if (currentEvent.type == UnityEngine.EventType.MouseDrag || 
            currentEvent.type == UnityEngine.EventType.MouseUp)
        {
            SaveNodePosition(node);
        }
        
        GUI.DragWindow();
    }
    
    void DrawModernConnections()
    {
        foreach (var node in eventNodes)
        {
            if (node.eventData.followupEvent != null)
            {
                EditorEventNode targetNode = eventNodes.FirstOrDefault(n => n.eventData == node.eventData.followupEvent);
                if (targetNode != null)
                {
                    DrawModernConnection(node, targetNode);
                }
            }
        }
    }
    
    void DrawModernConnection(EditorEventNode from, EditorEventNode to)
    {
        Vector3 startPos = new Vector3(from.rect.x + from.rect.width, from.rect.y + from.rect.height / 2);
        Vector3 endPos = new Vector3(to.rect.x, to.rect.y + to.rect.height / 2);
        
        Handles.BeginGUI();
        
        // 绘制阴影
        Handles.color = new Color(0, 0, 0, 0.3f);
        Handles.DrawBezier(
            startPos + Vector3.one * 2, 
            endPos + Vector3.one * 2, 
            startPos + Vector3.right * 60 + Vector3.one * 2, 
            endPos + Vector3.left * 60 + Vector3.one * 2, 
            Color.black, 
            null, 
            4f
        );
        
        // 绘制连接线
        Handles.color = PrimaryColor;
        Handles.DrawBezier(
            startPos, 
            endPos, 
            startPos + Vector3.right * 60, 
            endPos + Vector3.left * 60, 
            PrimaryColor, 
            null, 
            3f
        );
        
        // 绘制箭头
        Vector3 direction = (endPos - startPos).normalized;
        Vector3 arrowHead = endPos - direction * 15;
        Vector3 arrowSide1 = arrowHead + new Vector3(-direction.y, direction.x) * 8;
        Vector3 arrowSide2 = arrowHead + new Vector3(direction.y, -direction.x) * 8;
        
        Handles.DrawLine(endPos, arrowSide1);
        Handles.DrawLine(endPos, arrowSide2);
        
        Handles.EndGUI();
    }
    
    void DrawModernEventTestingPanel()
    {
        EditorGUILayout.BeginVertical(modernCardStyle);
        
        EditorGUILayout.LabelField($"{IconPlay} 事件测试", modernHeaderStyle);
        
        if (selectedEvent == null)
        {
            var helpStyle = new GUIStyle(EditorStyles.helpBox);
            helpStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.HelpBox("选择一个事件进行测试", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"测试事件: {selectedEvent.eventName}");
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入播放模式以测试事件", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button($"{IconPlay} 触发事件", modernButtonStyle, GUILayout.Height(28)))
                {
                    TriggerEventInGame(selectedEvent);
                }
                
                if (GUILayout.Button("✓ 验证", modernButtonStyle, GUILayout.Height(28)))
                {
                    var issues = ValidateEvent(selectedEvent);
                    string message = issues.Count > 0 ? 
                        "发现问题:\n" + string.Join("\n", issues) : 
                        "事件验证通过!";
                    EditorUtility.DisplayDialog("验证结果", message, "确定");
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawModernEventStatisticsPanel()
    {
        EditorGUILayout.BeginVertical(modernCardStyle);
        
        EditorGUILayout.LabelField($"{IconStats} 事件统计", modernHeaderStyle);
        
        statisticsScrollPos = EditorGUILayout.BeginScrollView(statisticsScrollPos, GUILayout.Height(120));
        
        var typeGroups = allEvents.GroupBy(e => e.eventType);
        EditorGUILayout.LabelField("按类型分组:", EditorStyles.boldLabel);
        foreach (var group in typeGroups)
        {
            var typeColor = GetEventTypeColor(group.Key);
            var typeIcon = GetEventTypeIcon(group.Key);
            
            EditorGUILayout.BeginHorizontal();
            GUI.color = typeColor;
            GUILayout.Label(typeIcon, GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label($"{group.Key}: {group.Count()}");
            EditorGUILayout.EndHorizontal();
        }
        
        GUILayout.Space(8);
        
        var priorityGroups = allEvents.GroupBy(e => e.priority);
        EditorGUILayout.LabelField("按优先级分组:", EditorStyles.boldLabel);
        foreach (var group in priorityGroups)
        {
            var priorityColor = GetPriorityColor(group.Key);
            
            EditorGUILayout.BeginHorizontal();
            GUI.color = priorityColor;
            GUILayout.Label("●", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label($"{group.Key}: {group.Count()}");
            EditorGUILayout.EndHorizontal();
        }
        
        GUILayout.Space(8);
        
        var withChoices = allEvents.Count(e => e.choices != null && e.choices.Length > 0);
        var questEvents = allEvents.Count(e => e.isQuest);
        
        EditorGUILayout.LabelField("其他统计:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  总事件数: {allEvents.Count}");
        EditorGUILayout.LabelField($"  有选择的: {withChoices}");
        EditorGUILayout.LabelField($"  任务事件: {questEvents}");
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    // 辅助方法
    Color GetPriorityColor(EventPriority priority)
    {
        return priority switch
        {
            EventPriority.Critical => DangerColor,
            EventPriority.High => WarningColor,
            EventPriority.Normal => SuccessColor,
            EventPriority.Low => new Color(0.6f, 0.6f, 0.6f),
            _ => Color.white
        };
    }
    
    Color GetEventTypeColor(EventType type)
    {
        return type switch
        {
            EventType.ResourceGain => SuccessColor,
            EventType.ResourceLoss => DangerColor,
            EventType.HealthEvent => WarningColor,
            EventType.Discovery => PrimaryColor,
            EventType.Encounter => new Color(0.8f, 0.4f, 0.8f),
            _ => Color.white
        };
    }
    
    string GetEventTypeIcon(EventType type)
    {
        return type switch
        {
            EventType.ResourceGain => "📈",
            EventType.ResourceLoss => "📉",
            EventType.HealthEvent => "❤",
            EventType.Discovery => "🔍",
            EventType.Encounter => "👥",
            _ => "📅"
        };
    }
    
    void SaveNodePosition(EditorEventNode node)
    {
        if (node.eventData.nodePosition == null)
            node.eventData.nodePosition = new EventNode();
            
        node.eventData.nodePosition.position = new Vector2(node.rect.x, node.rect.y);
        EditorUtility.SetDirty(node.eventData);
    }
    
    void UpdateNodeSelection()
    {
        foreach (var node in eventNodes)
        {
            node.isSelected = (node == selectedNode);
        }
    }
    
    void ProcessNodeEvents()
    {
        Event currentEvent = Event.current;
        
        if (currentEvent.type == UnityEngine.EventType.MouseDown && currentEvent.button == 1)
        {
            Vector2 mousePos = currentEvent.mousePosition;
            mousePos.y -= 60;
            
            if (mousePos.x > 416)
            {
                Vector2 graphMousePos = mousePos - new Vector2(416, 40) + nodeScrollPos;
                ShowGraphContextMenu(graphMousePos);
                currentEvent.Use();
            }
        }
    }
    
    void ShowGraphContextMenu(Vector2 position)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent($"{IconAdd} 创建新事件"), false, () => {
            CreateNewEventAt(position);
        });
        
        menu.ShowAsContext();
    }
    
    void CreateNewEvent()
    {
        CreateNewEventAt(new Vector2(100, 100));
    }
    
    void CreateNewEventAt(Vector2 position)
    {
        string fileName = EditorUtility.SaveFilePanel(
            "创建新事件", 
            "Assets/GameData/Events", 
            "NewEvent", 
            "asset");
            
        if (string.IsNullOrEmpty(fileName)) return;
        
        if (fileName.StartsWith(Application.dataPath))
        {
            fileName = "Assets" + fileName.Substring(Application.dataPath.Length);
        }
        
        if (System.IO.File.Exists(fileName))
        {
            if (!EditorUtility.DisplayDialog("文件已存在", 
                $"文件 {System.IO.Path.GetFileName(fileName)} 已存在，是否覆盖？", 
                "覆盖", "取消"))
            {
                return;
            }
        }
        
        string directory = System.IO.Path.GetDirectoryName(fileName);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        RandomEvent newEvent = CreateInstance<RandomEvent>();
        
        string eventName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        newEvent.eventName = eventName;
        
        newEvent.eventDescription = "请输入事件描述";
        newEvent.eventType = EventType.ResourceGain;
        newEvent.priority = EventPriority.Normal;
        newEvent.minDay = 1;
        newEvent.maxDay = 5;
        newEvent.baseTriggerChance = 0.3f;
        newEvent.requiresChoice = true;
        newEvent.canRepeat = false;
        newEvent.isQuest = false;
        
        // 初始化数组字段
        newEvent.choices = new EventChoice[0];
        newEvent.automaticEffects = new EventEffect[0];
        newEvent.triggerConditions = new EventCondition[0];
        newEvent.questObjectives = new QuestObjective[0];
        newEvent.prerequisiteQuestIds = new string[0];
        newEvent.unlockQuestIds = new string[0];
        newEvent.tags = new string[0];
        newEvent.editorColor = Color.white;
        
        if (newEvent.nodePosition == null)
            newEvent.nodePosition = new EventNode();
        newEvent.nodePosition.position = position;
        
        AssetDatabase.CreateAsset(newEvent, fileName);
        AssetDatabase.SaveAssets();
        
        allEvents.Add(newEvent);
        
        EditorEventNode newNode = new EditorEventNode
        {
            eventData = newEvent,
            rect = new Rect(position.x, position.y, 200, 140),
            isSelected = false
        };
        eventNodes.Add(newNode);
        
        selectedEvent = newEvent;
        selectedNode = newNode;
        UpdateNodeSelection();
        
        EditorApplication.delayCall += () => {
            EnterRenameMode(newEvent);
        };
        
        Repaint();
        
        Debug.Log($"[ModernEventEditor] 创建新事件: {fileName}");
    }
    
    // 保持所有原有功能方法...
    void SaveAllEvents()
    {
        foreach (var eventData in allEvents)
        {
            if (eventData != null)
            {
                EditorUtility.SetDirty(eventData);
            }
        }
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("保存完成", "所有事件已保存完成!", "确定");
    }
    
    void ShowEventContextMenu(RandomEvent eventData)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent($"{IconRename} 重命名"), false, () => {
            EnterRenameMode(eventData);
        });
        
        menu.AddItem(new GUIContent($"{IconCopy} 复制"), false, () => {
            DuplicateEvent(eventData);
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent($"{IconDelete} 删除"), false, () => {
            if (EditorUtility.DisplayDialog("删除事件", 
                $"确定要删除 '{eventData.eventName}' 吗？", "删除", "取消"))
            {
                DeleteEvent(eventData);
            }
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("在项目中显示"), false, () => {
            EditorGUIUtility.PingObject(eventData);
        });
        
        menu.ShowAsContext();
    }
    
    void EnterRenameMode(RandomEvent eventData)
    {
        isRenamingEvent = true;
        renamingEvent = eventData;
        renamingText = eventData.eventName;
        EditorGUI.FocusTextInControl("RenameField");
    }
    
    void ExitRenameMode()
    {
        isRenamingEvent = false;
        renamingEvent = null;
        renamingText = "";
    }
    
    void RenameEvent(RandomEvent eventData, string newName)
    {
        string oldPath = AssetDatabase.GetAssetPath(eventData);
        eventData.eventName = newName;
        EditorUtility.SetDirty(eventData);
        
        string directory = System.IO.Path.GetDirectoryName(oldPath);
        string extension = System.IO.Path.GetExtension(oldPath);
        string newPath = System.IO.Path.Combine(directory, newName + extension);
        
        if (!System.IO.File.Exists(newPath))
        {
            AssetDatabase.RenameAsset(oldPath, newName);
        }
        
        AssetDatabase.SaveAssets();
    }
    
    void DuplicateEvent(RandomEvent original)
    {
        string originalPath = AssetDatabase.GetAssetPath(original);
        string newPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);
        
        if (AssetDatabase.CopyAsset(originalPath, newPath))
        {
            AssetDatabase.SaveAssets();
            
            RandomEvent duplicate = AssetDatabase.LoadAssetAtPath<RandomEvent>(newPath);
            if (duplicate != null)
            {
                duplicate.eventName += " (副本)";
                EditorUtility.SetDirty(duplicate);
                
                allEvents.Add(duplicate);
                selectedEvent = duplicate;
                
                var newNode = new EditorEventNode
                {
                    eventData = duplicate,
                    rect = new Rect(120, 120, 200, 140),
                    isSelected = false
                };
                eventNodes.Add(newNode);
                selectedNode = newNode;
                UpdateNodeSelection();
            }
        }
    }
    
    void DeleteEvent(RandomEvent eventData)
    {
        string path = AssetDatabase.GetAssetPath(eventData);
        AssetDatabase.DeleteAsset(path);
        
        allEvents.Remove(eventData);
        eventNodes.RemoveAll(n => n.eventData == eventData);
        
        if (selectedEvent == eventData)
        {
            selectedEvent = null;
            selectedNode = null;
        }
        
        AssetDatabase.SaveAssets();
        Repaint();
    }
    
    void HandleKeyboardInput()
    {
        Event currentEvent = Event.current;
        
        if (currentEvent.type == UnityEngine.EventType.KeyDown)
        {
            switch (currentEvent.keyCode)
            {
                case KeyCode.Delete:
                    if (selectedEvent != null && !isRenamingEvent)
                    {
                        if (EditorUtility.DisplayDialog("删除事件", 
                            $"删除 '{selectedEvent.eventName}'?", "删除", "取消"))
                        {
                            DeleteEvent(selectedEvent);
                        }
                        currentEvent.Use();
                    }
                    break;
                    
                case KeyCode.F2:
                    if (selectedEvent != null && !isRenamingEvent)
                    {
                        EnterRenameMode(selectedEvent);
                        currentEvent.Use();
                    }
                    break;
                    
                case KeyCode.Escape:
                    if (isRenamingEvent)
                    {
                        ExitRenameMode();
                        currentEvent.Use();
                    }
                    break;
                    
                case KeyCode.D:
                    if (currentEvent.control && selectedEvent != null && !isRenamingEvent)
                    {
                        DuplicateEvent(selectedEvent);
                        currentEvent.Use();
                    }
                    break;
                    
                case KeyCode.Return:
                    if (isRenamingEvent)
                    {
                        if (!string.IsNullOrEmpty(renamingText))
                        {
                            RenameEvent(renamingEvent, renamingText);
                        }
                        ExitRenameMode();
                        currentEvent.Use();
                    }
                    break;
            }
        }
    }
    
    void ValidateAllEvents()
    {
        var issues = new List<string>();
        
        foreach (var eventData in allEvents)
        {
            var eventIssues = ValidateEvent(eventData);
            if (eventIssues.Count > 0)
            {
                issues.Add($"{eventData.eventName}:");
                issues.AddRange(eventIssues.Select(i => $"  - {i}"));
            }
        }
        
        if (issues.Count > 0)
        {
            string message = $"发现问题:\n" + string.Join("\n", issues.Take(10));
            if (issues.Count > 10)
            {
                message += $"\n... 还有 {issues.Count - 10} 个问题";
            }
            EditorUtility.DisplayDialog("事件验证结果", message, "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("事件验证结果", "所有事件验证成功!", "确定");
        }
    }
    
    List<string> ValidateEvent(RandomEvent eventData)
    {
        var issues = new List<string>();
        
        if (string.IsNullOrEmpty(eventData.eventName))
            issues.Add("事件名称为空");
        
        if (string.IsNullOrEmpty(eventData.eventDescription))
            issues.Add("事件描述为空");
        
        if (eventData.minDay > eventData.maxDay)
            issues.Add("最小天数大于最大天数");
        
        if (eventData.baseTriggerChance <= 0 || eventData.baseTriggerChance > 1)
            issues.Add("触发概率无效 (应为0-1)");
        
        if (eventData.requiresChoice && (eventData.choices == null || eventData.choices.Length == 0))
            issues.Add("需要选择但未定义选择");
        
        return issues;
    }
    
    void TriggerEventInGame(RandomEvent eventData)
    {
        var gameEventManager = GameEventManager.Instance;
        if (gameEventManager != null)
        {
            gameEventManager.TriggerEventExternally(eventData);
            EditorUtility.DisplayDialog("事件已触发", $"触发: {eventData.eventName}", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "场景中未找到 GameEventManager", "确定");
        }
    }
    
    void ShowHelp()
    {
        string helpText = @"现代事件编辑器帮助:

🔧 快捷键:
• F2: 重命名选中的事件
• Delete: 删除选中的事件  
• Ctrl+D: 复制选中的事件
• Esc: 取消重命名
• Enter: 确认重命名

🔍 搜索与过滤:
• 支持按名称、描述、类型搜索
• 使用类型和优先级过滤器
• 实时过滤结果

🖱️ 右键菜单:
• 重命名、复制、删除事件
• 在项目中显示文件

📊 节点图操作:
• 拖拽移动节点
• 右键创建新事件
• 连接线显示事件关系
• 可视化事件流程

🎯 测试功能:
• 播放模式下触发事件
• 事件验证检查
• 实时统计信息";
        
        EditorUtility.DisplayDialog("帮助", helpText, "确定");
    }
}

// 现代化事件选择编辑器
public class ModernEventChoiceEditor : EditorWindow
{
    private RandomEvent targetEvent;
    private Vector2 scrollPos;
    
    // 现代样式（简化版）
    private GUIStyle modernCardStyle;
    private GUIStyle modernButtonStyle;
    private GUIStyle modernHeaderStyle;
    
    private static readonly Color PrimaryColor = new Color(0.3f, 0.7f, 1f);
    private static readonly Color CardColor = new Color(0.18f, 0.18f, 0.18f);
    private static readonly Color DangerColor = new Color(0.9f, 0.3f, 0.3f);
    
    public static void OpenWindow(RandomEvent eventData)
    {
        ModernEventChoiceEditor window = GetWindow<ModernEventChoiceEditor>("事件选择编辑器");
        window.targetEvent = eventData;
        window.minSize = new Vector2(700, 500);
        window.Show();
    }
    
    void OnEnable()
    {
        InitializeStyles();
    }
    
    void InitializeStyles()
    {
        modernCardStyle = new GUIStyle();
        modernCardStyle.normal.background = CreateRoundedTexture(CardColor, 8);
        modernCardStyle.border = new RectOffset(8, 8, 8, 8);
        modernCardStyle.padding = new RectOffset(12, 12, 12, 12);
        modernCardStyle.margin = new RectOffset(4, 4, 4, 4);
        
        modernButtonStyle = new GUIStyle();
        modernButtonStyle.normal.background = CreateRoundedTexture(PrimaryColor, 6);
        modernButtonStyle.hover.background = CreateRoundedTexture(new Color(PrimaryColor.r * 1.2f, PrimaryColor.g * 1.2f, PrimaryColor.b * 1.2f), 6);
        modernButtonStyle.normal.textColor = Color.white;
        modernButtonStyle.hover.textColor = Color.white;
        modernButtonStyle.border = new RectOffset(6, 6, 6, 6);
        modernButtonStyle.padding = new RectOffset(12, 12, 8, 8);
        modernButtonStyle.alignment = TextAnchor.MiddleCenter;
        modernButtonStyle.fontStyle = FontStyle.Bold;
        
        modernHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
        modernHeaderStyle.fontSize = 16;
        modernHeaderStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
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
    
    void OnGUI()
    {
        if (modernCardStyle == null) InitializeStyles();
        
        // 设置背景
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.08f, 0.08f, 0.08f));
        
        if (targetEvent == null)
        {
            GUILayout.Label("未选择事件");
            return;
        }
        
        EditorGUILayout.BeginVertical(modernCardStyle);
        
        GUILayout.Label($"🔀 编辑选择: {targetEvent.eventName}", modernHeaderStyle);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(8);
        
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        if (targetEvent.choices != null)
        {
            for (int i = 0; i < targetEvent.choices.Length; i++)
            {
                DrawModernChoiceEditor(i);
                GUILayout.Space(8);
            }
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.Space(8);
        
        // 底部按钮
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("➕ 添加选择", modernButtonStyle, GUILayout.Height(35)))
        {
            AddNewChoice();
        }
        
        if (GUILayout.Button("➖ 删除最后", modernButtonStyle, GUILayout.Height(35)) && 
            targetEvent.choices != null && targetEvent.choices.Length > 0)
        {
            RemoveLastChoice();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(targetEvent);
        }
    }
    
    void DrawModernChoiceEditor(int index)
    {
        if (targetEvent.choices == null || index >= targetEvent.choices.Length) return;
        
        var choice = targetEvent.choices[index];
        
        EditorGUILayout.BeginVertical(modernCardStyle);
        
        GUILayout.Label($"🔀 选择 {index + 1}", modernHeaderStyle);
        
        choice.choiceText = EditorGUILayout.TextField("选择文本", choice.choiceText ?? "");
        choice.resultDescription = EditorGUILayout.TextArea(choice.resultDescription ?? "", GUILayout.Height(40));
        
        EditorGUILayout.BeginHorizontal();
        choice.isRecommended = EditorGUILayout.Toggle("推荐选择", choice.isRecommended);
        choice.buttonColor = EditorGUILayout.ColorField("按钮颜色", choice.buttonColor);
        EditorGUILayout.EndHorizontal();
        
        // 需求条件
        GUILayout.Label("📋 需求条件:", EditorStyles.boldLabel);
        if (choice.requirements != null)
        {
            for (int r = 0; r < choice.requirements.Length; r++)
            {
                var req = choice.requirements[r];
                EditorGUILayout.BeginHorizontal();
                req.resourceType = EditorGUILayout.TextField("资源", req.resourceType ?? "");
                req.amount = EditorGUILayout.IntField("数量", req.amount);
                
                var deleteStyle = new GUIStyle(modernButtonStyle);
                deleteStyle.normal.background = CreateRoundedTexture(DangerColor, 6);
                
                if (GUILayout.Button("🗑", deleteStyle, GUILayout.Width(30)))
                {
                    RemoveRequirement(index, r);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        if (GUILayout.Button("➕ 添加需求", modernButtonStyle, GUILayout.Height(25)))
        {
            AddRequirement(index);
        }
        
        // 效果
        GUILayout.Label("⚡ 效果:", EditorStyles.boldLabel);
        if (choice.effects != null)
        {
            for (int e = 0; e < choice.effects.Length; e++)
            {
                var effect = choice.effects[e];
                EditorGUILayout.BeginVertical("box");
                
                effect.type = (EffectType)EditorGUILayout.EnumPopup("类型", effect.type);
                
                // 根据效果类型显示相关字段
                switch (effect.type)
                {
                    case EffectType.ModifyResource:
                        effect.resourceType = EditorGUILayout.TextField("资源类型", effect.resourceType ?? "");
                        effect.resourceAmount = EditorGUILayout.IntField("数量", effect.resourceAmount);
                        break;
                        
                    case EffectType.ModifyHealth:
                        effect.affectAllFamily = EditorGUILayout.Toggle("影响所有家庭", effect.affectAllFamily);
                        effect.healthChange = EditorGUILayout.IntField("健康变化", effect.healthChange);
                        effect.cureIllness = EditorGUILayout.Toggle("治愈疾病", effect.cureIllness);
                        effect.causeIllness = EditorGUILayout.Toggle("引起疾病", effect.causeIllness);
                        break;
                        
                    case EffectType.AddJournalEntry:
                        effect.customMessage = EditorGUILayout.TextField("消息", effect.customMessage ?? "");
                        break;
                        
                    case EffectType.UnlockContent:
                        effect.unlockMap = EditorGUILayout.Toggle("解锁地图", effect.unlockMap);
                        if (effect.unlockMap)
                        {
                            effect.mapToUnlock = EditorGUILayout.TextField("地图ID", effect.mapToUnlock ?? "");
                        }
                        break;
                }
                
                var deleteEffectStyle = new GUIStyle(modernButtonStyle);
                deleteEffectStyle.normal.background = CreateRoundedTexture(DangerColor, 6);
                
                if (GUILayout.Button("🗑 删除效果", deleteEffectStyle, GUILayout.Height(25)))
                {
                    RemoveEffect(index, e);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                    return;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        if (GUILayout.Button("➕ 添加效果", modernButtonStyle, GUILayout.Height(25)))
        {
            AddEffect(index);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // 保持所有原有的功能方法...
    void AddRequirement(int choiceIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var requirements = choice.requirements?.ToList() ?? new List<ResourceRequirement>();
        requirements.Add(new ResourceRequirement { resourceType = "food", amount = 1 });
        choice.requirements = requirements.ToArray();
        EditorUtility.SetDirty(targetEvent);
    }
    
    void RemoveRequirement(int choiceIndex, int reqIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var requirements = choice.requirements.ToList();
        requirements.RemoveAt(reqIndex);
        choice.requirements = requirements.ToArray();
        EditorUtility.SetDirty(targetEvent);
    }
    
    void AddEffect(int choiceIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var effects = choice.effects?.ToList() ?? new List<EventEffect>();
        effects.Add(new EventEffect { type = EffectType.ModifyResource, resourceType = "food", resourceAmount = 1 });
        choice.effects = effects.ToArray();
        EditorUtility.SetDirty(targetEvent);
    }
    
    void RemoveEffect(int choiceIndex, int effectIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var effects = choice.effects.ToList();
        effects.RemoveAt(effectIndex);
        choice.effects = effects.ToArray();
        EditorUtility.SetDirty(targetEvent);
    }
    
    void AddNewChoice()
    {
        var newChoice = new EventChoice
        {
            choiceText = "新选择",
            resultDescription = "结果描述",
            requirements = new ResourceRequirement[0],
            effects = new EventEffect[0],
            buttonColor = Color.white
        };
        
        var choices = targetEvent.choices?.ToList() ?? new List<EventChoice>();
        choices.Add(newChoice);
        targetEvent.choices = choices.ToArray();
        
        EditorUtility.SetDirty(targetEvent);
    }
    
    void RemoveLastChoice()
    {
        if (targetEvent.choices != null && targetEvent.choices.Length > 0)
        {
            var choices = targetEvent.choices.ToList();
            choices.RemoveAt(choices.Count - 1);
            targetEvent.choices = choices.ToArray();
            
            EditorUtility.SetDirty(targetEvent);
        }
    }
}
#endif