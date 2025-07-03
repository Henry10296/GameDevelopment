using System.Collections.Generic;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EventEditorWindow : EditorWindow
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
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    
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

    [MenuItem("Game Tools/Event Editor")]
    public static void OpenWindow()
    {
        EventEditorWindow window = GetWindow<EventEditorWindow>("Event Editor");
        window.minSize = new Vector2(1200, 800);
        window.Show();
    }
    
    void OnEnable()
    {
        LoadAllEvents();
        InitializeProfessionalStyles();
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
        
        // 节点样式
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = CreateSolidTexture(Surface);
        nodeStyle.border = new RectOffset(1, 1, 1, 1);
        nodeStyle.padding = new RectOffset(12, 12, 12, 12);
        nodeStyle.normal.textColor = TextPrimary;
        nodeStyle.alignment = TextAnchor.UpperLeft;
        nodeStyle.fontSize = 11;
        
        selectedNodeStyle = new GUIStyle(nodeStyle);
        selectedNodeStyle.normal.background = CreateBorderedTexture(Surface, Primary, 2);
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
        if (cardStyle == null) InitializeProfessionalStyles();
        
        // 设置背景色
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), Background);
        
        DrawToolbar();
        
        // 主要分割布局
        Rect leftPanel = new Rect(8, 60, 400, position.height - 68);
        Rect rightPanel = new Rect(416, 60, position.width - 424, position.height - 68);
        
        DrawLeftPanel(leftPanel);
        DrawRightPanel(rightPanel);
        
        HandleKeyboardInput();
        ProcessNodeEvents();
    }
    
    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(toolbarStyle, GUILayout.Height(55));
        
        // 主要操作按钮组
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("New Event", buttonPrimaryStyle, GUILayout.Height(35)))
        {
            CreateNewEvent();
        }
        
        GUILayout.Space(8);
        
        if (GUILayout.Button("Refresh", buttonSecondaryStyle, GUILayout.Width(80), GUILayout.Height(35)))
        {
            LoadAllEvents();
        }
        
        if (GUILayout.Button("Save All", buttonSecondaryStyle, GUILayout.Width(80), GUILayout.Height(35)))
        {
            SaveAllEvents();
        }
        
        if (GUILayout.Button("Validate", buttonSecondaryStyle, GUILayout.Width(80), GUILayout.Height(35)))
        {
            ValidateAllEvents();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.FlexibleSpace();
        
        // 搜索和过滤区域
        DrawSearchAndFilter();
        
        GUILayout.FlexibleSpace();
        
        // 工具切换区域
        EditorGUILayout.BeginHorizontal();
        
        var testingStyle = showEventTesting ? tabActiveStyle : tabInactiveStyle;
        if (GUILayout.Button("Testing", testingStyle, GUILayout.Height(35)))
        {
            showEventTesting = !showEventTesting;
        }
        
        var statsStyle = showEventStatistics ? tabActiveStyle : tabInactiveStyle;
        if (GUILayout.Button("Statistics", statsStyle, GUILayout.Height(35)))
        {
            showEventStatistics = !showEventStatistics;
        }
        
        if (GUILayout.Button("Help", buttonSecondaryStyle, GUILayout.Width(60), GUILayout.Height(35)))
        {
            ShowHelp();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndHorizontal();
        
        // 绘制工具栏分隔线
        EditorGUI.DrawRect(new Rect(0, 55, position.width, 2), Border);
    }
    
    void DrawSearchAndFilter()
    {
        EditorGUILayout.BeginVertical();
        
        // 搜索栏
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        
        var newSearchText = EditorGUILayout.TextField(eventSearchFilter, searchFieldStyle, GUILayout.Width(150));
        if (newSearchText != eventSearchFilter)
        {
            eventSearchFilter = newSearchText;
        }
        
        if (!string.IsNullOrEmpty(eventSearchFilter))
        {
            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                eventSearchFilter = "";
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 过滤器状态显示
        if (useEventTypeFilter || useEventPriorityFilter)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (useEventTypeFilter)
            {
                var typeColor = GetEventTypeColor(eventTypeFilter);
                GUI.color = typeColor;
                GUILayout.Label($"Type: {eventTypeFilter}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            
            if (useEventPriorityFilter)
            {
                var priorityColor = GetPriorityColor(eventPriorityFilter);
                GUI.color = priorityColor;
                GUILayout.Label($"Priority: {eventPriorityFilter}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            
            if (GUILayout.Button("Clear", EditorStyles.miniButton))
            {
                useEventTypeFilter = false;
                useEventPriorityFilter = false;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawLeftPanel(Rect panel)
    {
        GUILayout.BeginArea(panel);
        
        EditorGUILayout.BeginVertical(sectionStyle, GUILayout.ExpandHeight(true));
        
        // 标题栏
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Event Management", headerStyle);
        
        GUILayout.FlexibleSpace();
        
        var eventCount = GetFilteredEvents().Count();
        var countStyle = new GUIStyle(EditorStyles.miniLabel);
        countStyle.normal.textColor = Primary;
        GUILayout.Label($"{eventCount}/{allEvents.Count}", countStyle);
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(8);
        
        DrawEventFilters();
        
        GUILayout.Space(8);
        
        // 事件列表
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        var filteredEvents = GetFilteredEvents();
        foreach (var eventData in filteredEvents)
        {
            DrawEventListItem(eventData);
        }
        
        if (filteredEvents.Count() == 0)
        {
            EditorGUILayout.BeginVertical(cardStyle);
            var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            emptyStyle.normal.textColor = TextSecondary;
            GUILayout.Label("No matching events found", emptyStyle);
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
        
        // 详细信息面板
        if (selectedEvent != null)
        {
            GUILayout.Space(8);
            DrawEventDetails();
        }
        
        // 测试和统计面板
        if (showEventTesting)
        {
            GUILayout.Space(8);
            DrawEventTestingPanel();
        }
        
        if (showEventStatistics)
        {
            GUILayout.Space(8);
            DrawEventStatisticsPanel();
        }
        
        EditorGUILayout.EndVertical();
        
        GUILayout.EndArea();
    }
    
    void DrawEventFilters()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Filters", subHeaderStyle);
        
        GUILayout.FlexibleSpace();
        
        if (useEventTypeFilter || useEventPriorityFilter)
        {
            if (GUILayout.Button("Reset", EditorStyles.miniButton))
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
        GUILayout.Label("Type:", GUILayout.Width(40));
        eventTypeFilter = (EventType)EditorGUILayout.EnumPopup(eventTypeFilter);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        // 优先级过滤器
        EditorGUILayout.BeginHorizontal();
        useEventPriorityFilter = EditorGUILayout.Toggle(useEventPriorityFilter, GUILayout.Width(20));
        GUI.enabled = useEventPriorityFilter;
        GUILayout.Label("Priority:", GUILayout.Width(40));
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
    
    void DrawEventListItem(RandomEvent eventData)
    {
        bool isSelected = selectedEvent == eventData;
        
        var itemStyle = isSelected ? selectedCardStyle : cardStyle;
        var itemStyleCopy = new GUIStyle(itemStyle);
        itemStyleCopy.margin = new RectOffset(0, 0, 2, 2);
        
        EditorGUILayout.BeginVertical(itemStyleCopy);
        
        EditorGUILayout.BeginHorizontal();
        
        // 事件类型和优先级指示器
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
            if (GUILayout.Button(eventData.eventName, labelStyle))
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
            GUI.color = Success;
            GUILayout.Label($"Choices({eventData.choices.Length})", EditorStyles.miniLabel, GUILayout.Width(60));
            GUI.color = Color.white;
        }
        
        // 任务指示器
        if (eventData.isQuest)
        {
            GUI.color = Warning;
            GUILayout.Label("Quest", EditorStyles.miniLabel, GUILayout.Width(35));
            GUI.color = Color.white;
        }
        
        // 类型指示器
        var typeColor = GetEventTypeColor(eventData.eventType);
        GUI.color = typeColor;
        GUILayout.Label(eventData.eventType.ToString(), EditorStyles.miniLabel, GUILayout.Width(60));
        GUI.color = Color.white;
    }
    
    void DrawEventQuickInfo(RandomEvent eventData)
    {
        var miniStyle = new GUIStyle(EditorStyles.miniLabel);
        miniStyle.normal.textColor = TextSecondary;
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Type: {eventData.eventType}", miniStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Days: {eventData.minDay}-{eventData.maxDay}", miniStyle);
        EditorGUILayout.EndHorizontal();
        
        if (!string.IsNullOrEmpty(eventData.eventDescription))
        {
            var descStyle = new GUIStyle(EditorStyles.miniLabel);
            descStyle.normal.textColor = TextSecondary;
            descStyle.wordWrap = true;
            
            string shortDesc = eventData.eventDescription.Length > 60 ? 
                eventData.eventDescription.Substring(0, 60) + "..." : 
                eventData.eventDescription;
            GUILayout.Label(shortDesc, descStyle);
        }
    }
    
    void DrawEventDetails()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        
        EditorGUILayout.LabelField("Event Details", subHeaderStyle);
        
        EditorGUI.BeginChangeCheck();
        
        selectedEvent.eventName = EditorGUILayout.TextField("Event Name", selectedEvent.eventName);
        selectedEvent.eventType = (EventType)EditorGUILayout.EnumPopup("Event Type", selectedEvent.eventType);
        selectedEvent.priority = (EventPriority)EditorGUILayout.EnumPopup("Priority", selectedEvent.priority);
        
        GUILayout.Space(4);
        
        GUILayout.Label("Description:");
        selectedEvent.eventDescription = EditorGUILayout.TextArea(selectedEvent.eventDescription, GUILayout.Height(50));
        
        GUILayout.Space(4);
        
        EditorGUILayout.BeginHorizontal();
        selectedEvent.minDay = EditorGUILayout.IntField("Min Day", selectedEvent.minDay);
        selectedEvent.maxDay = EditorGUILayout.IntField("Max Day", selectedEvent.maxDay);
        EditorGUILayout.EndHorizontal();
        
        selectedEvent.baseTriggerChance = EditorGUILayout.Slider("Trigger Chance", selectedEvent.baseTriggerChance, 0f, 1f);
        
        EditorGUILayout.BeginHorizontal();
        selectedEvent.requiresChoice = EditorGUILayout.Toggle("Requires Choice", selectedEvent.requiresChoice);
        selectedEvent.canRepeat = EditorGUILayout.Toggle("Can Repeat", selectedEvent.canRepeat);
        EditorGUILayout.EndHorizontal();
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedEvent);
        }
        
        GUILayout.Space(8);
        
        if (GUILayout.Button("Edit Choices", buttonPrimaryStyle, GUILayout.Height(30)))
        {
            EventChoiceEditor.OpenWindow(selectedEvent);
        }
        
        // 任务设置
        selectedEvent.isQuest = EditorGUILayout.Toggle("Is Quest", selectedEvent.isQuest);
        
        if (selectedEvent.isQuest)
        {
            showQuestFields = EditorGUILayout.Foldout(showQuestFields, "Quest Settings");
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
        
        selectedEvent.isMainQuest = EditorGUILayout.Toggle("Main Quest", selectedEvent.isMainQuest);
        selectedEvent.isSideQuest = EditorGUILayout.Toggle("Side Quest", selectedEvent.isSideQuest);
        selectedEvent.questChain = EditorGUILayout.TextField("Quest Chain", selectedEvent.questChain ?? "");
        selectedEvent.questOrder = EditorGUILayout.IntField("Quest Order", selectedEvent.questOrder);
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawRightPanel(Rect panel)
    {
        GUILayout.BeginArea(panel);
        
        EditorGUILayout.BeginVertical(sectionStyle, GUILayout.ExpandHeight(true));
        
        EditorGUILayout.LabelField("Event Flow Graph", headerStyle);
        
        GUILayout.Space(8);
        
        Rect graphArea = new Rect(0, 40, panel.width - 32, panel.height - 72);
        DrawEventGraph(graphArea);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.EndArea();
    }
    
    void DrawEventGraph(Rect area)
    {
        DrawGrid(area);
        
        nodeScrollPos = GUI.BeginScrollView(area, nodeScrollPos, nodeArea);
        
        DrawConnections();
        DrawNodes();
        
        GUI.EndScrollView();
    }
    
    void DrawGrid(Rect area)
    {
        int gridSpacing = 25;
        Color gridColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);
        Color majorGridColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        
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
    
    void DrawNodes()
    {
        BeginWindows();
        
        for (int i = 0; i < eventNodes.Count; i++)
        {
            var node = eventNodes[i];
            if (node.eventData != null)
            {
                GUIStyle style = node.isSelected ? selectedNodeStyle : nodeStyle;
                node.rect = GUILayout.Window(i, node.rect, DrawNodeWindow, "", style);
            }
        }
        
        EndWindows();
    }
    
    void DrawNodeWindow(int id)
    {
        if (id < 0 || id >= eventNodes.Count) return;
        
        var node = eventNodes[id];
        var eventData = node.eventData;
        
        // 标题
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.normal.textColor = TextPrimary;
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
        GUILayout.Label("■", GUILayout.Width(15));
        GUI.color = Color.white;
        
        var typeStyle = new GUIStyle(EditorStyles.miniLabel);
        typeStyle.normal.textColor = TextSecondary;
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
        statsStyle.normal.textColor = TextSecondary;
        
        GUILayout.Label($"Days: {eventData.minDay}-{eventData.maxDay}", statsStyle);
        GUILayout.Label($"Chance: {eventData.baseTriggerChance:P0}", statsStyle);
        
        if (eventData.choices != null && eventData.choices.Length > 0)
        {
            GUILayout.Label($"Choices: {eventData.choices.Length}", statsStyle);
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
    
    void DrawConnections()
    {
        foreach (var node in eventNodes)
        {
            if (node.eventData.followupEvent != null)
            {
                EditorEventNode targetNode = eventNodes.FirstOrDefault(n => n.eventData == node.eventData.followupEvent);
                if (targetNode != null)
                {
                    DrawConnection(node, targetNode);
                }
            }
        }
    }
    
    void DrawConnection(EditorEventNode from, EditorEventNode to)
    {
        Vector3 startPos = new Vector3(from.rect.x + from.rect.width, from.rect.y + from.rect.height / 2);
        Vector3 endPos = new Vector3(to.rect.x, to.rect.y + to.rect.height / 2);
        
        Handles.BeginGUI();
        
        // 绘制阴影
        Handles.color = new Color(0, 0, 0, 0.2f);
        Handles.DrawBezier(
            startPos + Vector3.one * 2, 
            endPos + Vector3.one * 2, 
            startPos + Vector3.right * 60 + Vector3.one * 2, 
            endPos + Vector3.left * 60 + Vector3.one * 2, 
            Color.black, 
            null, 
            3f
        );
        
        // 绘制连接线
        Handles.color = Primary;
        Handles.DrawBezier(
            startPos, 
            endPos, 
            startPos + Vector3.right * 60, 
            endPos + Vector3.left * 60, 
            Primary, 
            null, 
            2f
        );
        
        // 绘制箭头
        Vector3 direction = (endPos - startPos).normalized;
        Vector3 arrowHead = endPos - direction * 15;
        Vector3 arrowSide1 = arrowHead + new Vector3(-direction.y, direction.x) * 6;
        Vector3 arrowSide2 = arrowHead + new Vector3(direction.y, -direction.x) * 6;
        
        Handles.DrawLine(endPos, arrowSide1);
        Handles.DrawLine(endPos, arrowSide2);
        
        Handles.EndGUI();
    }
    
    void DrawEventTestingPanel()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        
        EditorGUILayout.LabelField("Event Testing", subHeaderStyle);
        
        if (selectedEvent == null)
        {
            EditorGUILayout.HelpBox("Select an event to test", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Testing: {selectedEvent.eventName}");
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter play mode to test events", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Trigger Event", buttonPrimaryStyle, GUILayout.Height(28)))
                {
                    TriggerEventInGame(selectedEvent);
                }
                
                if (GUILayout.Button("Validate", buttonSecondaryStyle, GUILayout.Height(28)))
                {
                    var issues = ValidateEvent(selectedEvent);
                    string message = issues.Count > 0 ? 
                        "Issues found:\n" + string.Join("\n", issues) : 
                        "Event validation passed!";
                    EditorUtility.DisplayDialog("Validation Result", message, "OK");
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEventStatisticsPanel()
    {
        EditorGUILayout.BeginVertical(cardStyle);
        
        EditorGUILayout.LabelField("Event Statistics", subHeaderStyle);
        
        statisticsScrollPos = EditorGUILayout.BeginScrollView(statisticsScrollPos, GUILayout.Height(120));
        
        var typeGroups = allEvents.GroupBy(e => e.eventType);
        EditorGUILayout.LabelField("By Type:", labelStyle);
        foreach (var group in typeGroups)
        {
            var typeColor = GetEventTypeColor(group.Key);
            
            EditorGUILayout.BeginHorizontal();
            GUI.color = typeColor;
            GUILayout.Label("■", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label($"{group.Key}: {group.Count()}");
            EditorGUILayout.EndHorizontal();
        }
        
        GUILayout.Space(8);
        
        var priorityGroups = allEvents.GroupBy(e => e.priority);
        EditorGUILayout.LabelField("By Priority:", labelStyle);
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
        
        EditorGUILayout.LabelField("Other Stats:", labelStyle);
        EditorGUILayout.LabelField($"  Total Events: {allEvents.Count}");
        EditorGUILayout.LabelField($"  With Choices: {withChoices}");
        EditorGUILayout.LabelField($"  Quest Events: {questEvents}");
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    // 辅助方法
    Color GetPriorityColor(EventPriority priority)
    {
        return priority switch
        {
            EventPriority.Critical => Danger,
            EventPriority.High => Warning,
            EventPriority.Normal => Success,
            EventPriority.Low => Secondary,
            _ => TextSecondary
        };
    }
    
    Color GetEventTypeColor(EventType type)
    {
        return type switch
        {
            EventType.ResourceGain => Success,
            EventType.ResourceLoss => Danger,
            EventType.HealthEvent => Warning,
            EventType.Discovery => Primary,
            EventType.Encounter => new Color(0.6f, 0.4f, 0.8f),
            _ => TextSecondary
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
        
        menu.AddItem(new GUIContent("Create New Event"), false, () => {
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
            "Create New Event", 
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
            if (!EditorUtility.DisplayDialog("File Exists", 
                $"File {System.IO.Path.GetFileName(fileName)} already exists. Overwrite?", 
                "Overwrite", "Cancel"))
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
        
        newEvent.eventDescription = "Enter event description";
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
        
        Debug.Log($"[EventEditor] Created new event: {fileName}");
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
        
        EditorUtility.DisplayDialog("Save Complete", "All events saved successfully!", "OK");
    }
    
    void ShowEventContextMenu(RandomEvent eventData)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("Rename"), false, () => {
            EnterRenameMode(eventData);
        });
        
        menu.AddItem(new GUIContent("Duplicate"), false, () => {
            DuplicateEvent(eventData);
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("Delete"), false, () => {
            if (EditorUtility.DisplayDialog("Delete Event", 
                $"Are you sure you want to delete '{eventData.eventName}'?", "Delete", "Cancel"))
            {
                DeleteEvent(eventData);
            }
        });
        
        menu.AddSeparator("");
        
        menu.AddItem(new GUIContent("Show in Project"), false, () => {
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
                duplicate.eventName += " (Copy)";
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
                        if (EditorUtility.DisplayDialog("Delete Event", 
                            $"Delete '{selectedEvent.eventName}'?", "Delete", "Cancel"))
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
            string message = $"Issues found:\n" + string.Join("\n", issues.Take(10));
            if (issues.Count > 10)
            {
                message += $"\n... and {issues.Count - 10} more issues";
            }
            EditorUtility.DisplayDialog("Event Validation Results", message, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Event Validation Results", "All events validated successfully!", "OK");
        }
    }
    
    List<string> ValidateEvent(RandomEvent eventData)
    {
        var issues = new List<string>();
        
        if (string.IsNullOrEmpty(eventData.eventName))
            issues.Add("Event name is empty");
        
        if (string.IsNullOrEmpty(eventData.eventDescription))
            issues.Add("Event description is empty");
        
        if (eventData.minDay > eventData.maxDay)
            issues.Add("Min day is greater than max day");
        
        if (eventData.baseTriggerChance <= 0 || eventData.baseTriggerChance > 1)
            issues.Add("Trigger chance is invalid (should be 0-1)");
        
        if (eventData.requiresChoice && (eventData.choices == null || eventData.choices.Length == 0))
            issues.Add("Requires choice but no choices defined");
        
        return issues;
    }
    
    void TriggerEventInGame(RandomEvent eventData)
    {
        var gameEventManager = GameEventManager.Instance;
        if (gameEventManager != null)
        {
            gameEventManager.TriggerEventExternally(eventData);
            EditorUtility.DisplayDialog("Event Triggered", $"Triggered: {eventData.eventName}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "GameEventManager not found in scene", "OK");
        }
    }
    
    void ShowHelp()
    {
        string helpText = @"Event Editor Help:

Keyboard Shortcuts:
• F2: Rename selected event
• Delete: Delete selected event  
• Ctrl+D: Duplicate selected event
• Esc: Cancel rename
• Enter: Confirm rename

Search & Filter:
• Search by name, description, type
• Use type and priority filters
• Real-time filtering results

Right-click Menu:
• Rename, duplicate, delete events
• Show file in project

Node Graph Operations:
• Drag to move nodes
• Right-click to create new events
• Connection lines show event relationships
• Visual event flow representation

Testing Features:
• Trigger events in play mode
• Event validation checking
• Real-time statistics";
        
        EditorUtility.DisplayDialog("Help", helpText, "OK");
    }
}

// 现代化事件选择编辑器
public class EventChoiceEditor : EditorWindow
{
    private RandomEvent targetEvent;
    private Vector2 scrollPos;
    
    // 现代样式（简化版）
    private GUIStyle cardStyle;
    private GUIStyle buttonPrimaryStyle;
    private GUIStyle buttonSecondaryStyle;
    private GUIStyle buttonDangerStyle;
    private GUIStyle headerStyle;
    
    private static readonly Color Primary = new Color(0.26f, 0.54f, 0.96f);
    private static readonly Color Secondary = new Color(0.45f, 0.55f, 0.60f);
    private static readonly Color Danger = new Color(0.86f, 0.21f, 0.27f);
    private static readonly Color Surface = new Color(1f, 1f, 1f);
    private static readonly Color Background = new Color(0.94f, 0.94f, 0.96f);
    private static readonly Color TextPrimary = new Color(0.13f, 0.13f, 0.13f);
    
    public static void OpenWindow(RandomEvent eventData)
    {
        EventChoiceEditor window = GetWindow<EventChoiceEditor>("Event Choice Editor");
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
        cardStyle = new GUIStyle();
        cardStyle.normal.background = CreateSolidTexture(Surface);
        cardStyle.border = new RectOffset(1, 1, 1, 1);
        cardStyle.padding = new RectOffset(12, 12, 12, 12);
        cardStyle.margin = new RectOffset(4, 4, 4, 4);
        
        buttonPrimaryStyle = CreateButtonStyle(Primary, Color.white);
        buttonSecondaryStyle = CreateButtonStyle(Secondary, Color.white);
        buttonDangerStyle = CreateButtonStyle(Danger, Color.white);
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.normal.textColor = TextPrimary;
    }
    
    GUIStyle CreateButtonStyle(Color bgColor, Color textColor)
    {
        var style = new GUIStyle();
        style.normal.background = CreateSolidTexture(bgColor);
        style.hover.background = CreateSolidTexture(AdjustBrightness(bgColor, 1.1f));
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.padding = new RectOffset(12, 12, 8, 8);
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
    
    Color AdjustBrightness(Color color, float factor)
    {
        return new Color(
            Mathf.Clamp01(color.r * factor),
            Mathf.Clamp01(color.g * factor),
            Mathf.Clamp01(color.b * factor),
            color.a
        );
    }
    
    void OnGUI()
    {
        if (cardStyle == null) InitializeStyles();
        
        // 设置背景
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), Background);
        
        if (targetEvent == null)
        {
            GUILayout.Label("No event selected");
            return;
        }
        
        EditorGUILayout.BeginVertical(cardStyle);
        
        GUILayout.Label($"Edit Choices: {targetEvent.eventName}", headerStyle);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(8);
        
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        if (targetEvent.choices != null)
        {
            for (int i = 0; i < targetEvent.choices.Length; i++)
            {
                DrawChoiceEditor(i);
                GUILayout.Space(8);
            }
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.Space(8);
        
        // 底部按钮
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Add Choice", buttonPrimaryStyle, GUILayout.Height(35)))
        {
            AddNewChoice();
        }
        
        if (GUILayout.Button("Remove Last", buttonSecondaryStyle, GUILayout.Height(35)) && 
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
    
    void DrawChoiceEditor(int index)
    {
        if (targetEvent.choices == null || index >= targetEvent.choices.Length) return;
        
        var choice = targetEvent.choices[index];
        
        EditorGUILayout.BeginVertical(cardStyle);
        
        GUILayout.Label($"Choice {index + 1}", headerStyle);
        
        choice.choiceText = EditorGUILayout.TextField("Choice Text", choice.choiceText ?? "");
        choice.resultDescription = EditorGUILayout.TextArea(choice.resultDescription ?? "", GUILayout.Height(40));
        
        EditorGUILayout.BeginHorizontal();
        choice.isRecommended = EditorGUILayout.Toggle("Recommended", choice.isRecommended);
        choice.buttonColor = EditorGUILayout.ColorField("Button Color", choice.buttonColor);
        EditorGUILayout.EndHorizontal();
        
        // 需求条件
        GUILayout.Label("Requirements:", EditorStyles.boldLabel);
        if (choice.requirements != null)
        {
            for (int r = 0; r < choice.requirements.Length; r++)
            {
                var req = choice.requirements[r];
                EditorGUILayout.BeginHorizontal();
                req.resourceType = EditorGUILayout.TextField("Resource", req.resourceType ?? "");
                req.amount = EditorGUILayout.IntField("Amount", req.amount);
                
                if (GUILayout.Button("Remove", buttonDangerStyle, GUILayout.Width(60)))
                {
                    RemoveRequirement(index, r);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        if (GUILayout.Button("Add Requirement", buttonSecondaryStyle, GUILayout.Height(25)))
        {
            AddRequirement(index);
        }
        
        // 效果
        GUILayout.Label("Effects:", EditorStyles.boldLabel);
        if (choice.effects != null)
        {
            for (int e = 0; e < choice.effects.Length; e++)
            {
                var effect = choice.effects[e];
                EditorGUILayout.BeginVertical("box");
                
                effect.type = (EffectType)EditorGUILayout.EnumPopup("Type", effect.type);
                
                // 根据效果类型显示相关字段
                switch (effect.type)
                {
                    case EffectType.ModifyResource:
                        effect.resourceType = EditorGUILayout.TextField("Resource Type", effect.resourceType ?? "");
                        effect.resourceAmount = EditorGUILayout.IntField("Amount", effect.resourceAmount);
                        break;
                        
                    case EffectType.ModifyHealth:
                        effect.affectAllFamily = EditorGUILayout.Toggle("Affect All Family", effect.affectAllFamily);
                        effect.healthChange = EditorGUILayout.IntField("Health Change", effect.healthChange);
                        effect.cureIllness = EditorGUILayout.Toggle("Cure Illness", effect.cureIllness);
                        effect.causeIllness = EditorGUILayout.Toggle("Cause Illness", effect.causeIllness);
                        break;
                        
                    case EffectType.AddJournalEntry:
                        effect.customMessage = EditorGUILayout.TextField("Message", effect.customMessage ?? "");
                        break;
                        
                    case EffectType.UnlockContent:
                        effect.unlockMap = EditorGUILayout.Toggle("Unlock Map", effect.unlockMap);
                        if (effect.unlockMap)
                        {
                            effect.mapToUnlock = EditorGUILayout.TextField("Map ID", effect.mapToUnlock ?? "");
                        }
                        break;
                }
                
                if (GUILayout.Button("Remove Effect", buttonDangerStyle, GUILayout.Height(25)))
                {
                    RemoveEffect(index, e);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                    return;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        if (GUILayout.Button("Add Effect", buttonSecondaryStyle, GUILayout.Height(25)))
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
            choiceText = "New Choice",
            resultDescription = "Result description",
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