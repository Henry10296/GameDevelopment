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
    
    // UI样式
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle connectionStyle;
    
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

    [MenuItem("Game Tools/Event Editor")]
    public static void OpenWindow()
    {
        EventEditorWindow window = GetWindow<EventEditorWindow>("Event Editor");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }
    
    void OnEnable()
    {
        LoadAllEvents();
        InitializeStyles();
    }
    
    void InitializeStyles()
    {
        if (nodeStyle == null)
        {
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 1f));
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
            nodeStyle.padding = new RectOffset(10, 10, 10, 10);
            nodeStyle.normal.textColor = Color.white;
            nodeStyle.alignment = TextAnchor.UpperLeft;
        }
        
        if (selectedNodeStyle == null)
        {
            selectedNodeStyle = new GUIStyle(nodeStyle);
            selectedNodeStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.5f, 0.8f, 1f));
        }
        
        if (connectionStyle == null)
        {
            connectionStyle = new GUIStyle();
            connectionStyle.normal.background = EditorGUIUtility.whiteTexture;
        }
    }
    
    Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
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
            
            // 使用保存的位置信息，如果没有则使用默认布局
            Vector2 nodePos = Vector2.zero;
            if (eventData.nodePosition != null && eventData.nodePosition.position != Vector2.zero)
            {
                nodePos = eventData.nodePosition.position;
            }
            else
            {
                nodePos = new Vector2(100 + (i % 5) * 200, 100 + (i / 5) * 150);
            }
            
            var node = new EditorEventNode
            {
                eventData = eventData,
                rect = new Rect(nodePos.x, nodePos.y, 180, 120),
                isSelected = false
            };
            eventNodes.Add(node);
        }
    }
    
    void OnGUI()
    {
        if (nodeStyle == null) InitializeStyles();
        
        DrawToolbar();
        
        // 分割界面
        Rect leftPanel = new Rect(0, 30, 300, position.height - 30);
        Rect rightPanel = new Rect(300, 30, position.width - 300, position.height - 30);
        
        DrawLeftPanel(leftPanel);
        DrawRightPanel(rightPanel);
        
        HandleKeyboardInput();
        ProcessNodeEvents();
    }
    
    void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("New Event", EditorStyles.toolbarButton))
        {
            CreateNewEvent();
        }
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
        {
            LoadAllEvents();
        }
        
        if (GUILayout.Button("Save All", EditorStyles.toolbarButton))
        {
            SaveAllEvents();
        }
        
        if (GUILayout.Button("Validate All", EditorStyles.toolbarButton))
        {
            ValidateAllEvents();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Search:", EditorStyles.miniLabel);
        string newFilter = EditorGUILayout.TextField(eventSearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
        if (newFilter != eventSearchFilter)
        {
            eventSearchFilter = newFilter;
        }
        
        if (!string.IsNullOrEmpty(eventSearchFilter) || useEventTypeFilter || useEventPriorityFilter)
        {
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                useEventTypeFilter = false;
                useEventPriorityFilter = false;
                eventSearchFilter = "";
            }
        }
        
        GUILayout.FlexibleSpace();
        
        showEventTesting = GUILayout.Toggle(showEventTesting, "Testing", EditorStyles.toolbarButton);
        showEventStatistics = GUILayout.Toggle(showEventStatistics, "Statistics", EditorStyles.toolbarButton);
        
        if (GUILayout.Button("Help", EditorStyles.toolbarButton))
        {
            ShowHelp();
        }
        
        GUILayout.EndHorizontal();
    }
    
    void DrawLeftPanel(Rect panel)
    {
        GUILayout.BeginArea(panel);
        
        GUILayout.Label("Event List", EditorStyles.boldLabel);
        
        DrawEventFilters();
        
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        var filteredEvents = GetFilteredEvents();
        foreach (var eventData in filteredEvents)
        {
            DrawEventListItem(eventData);
        }
        
        GUILayout.EndScrollView();
        
        if (selectedEvent != null)
        {
            DrawEventDetails();
        }
        
        if (showEventTesting)
        {
            DrawEventTestingPanel();
        }
        
        if (showEventStatistics)
        {
            DrawEventStatisticsPanel();
        }
        
        GUILayout.EndArea();
    }
    
    void DrawEventFilters()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Filters", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        useEventTypeFilter = EditorGUILayout.Toggle("Type:", useEventTypeFilter, GUILayout.Width(60));
        GUI.enabled = useEventTypeFilter;
        eventTypeFilter = (EventType)EditorGUILayout.EnumPopup(eventTypeFilter);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        useEventPriorityFilter = EditorGUILayout.Toggle("Priority:", useEventPriorityFilter, GUILayout.Width(60));
        GUI.enabled = useEventPriorityFilter;
        eventPriorityFilter = (EventPriority)EditorGUILayout.EnumPopup(eventPriorityFilter);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Clear Filters"))
        {
            eventSearchFilter = "";
            useEventTypeFilter = false;
            useEventPriorityFilter = false;
        }
        
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
        
        EditorGUILayout.BeginVertical(isSelected ? "selectionRect" : "box");
        
        EditorGUILayout.BeginHorizontal();
        
        if (isRenamingEvent && renamingEvent == eventData)
        {
            GUI.SetNextControlName("RenameField");
            renamingText = EditorGUILayout.TextField(renamingText);
            
            if (GUILayout.Button("✓", GUILayout.Width(20)))
            {
                if (!string.IsNullOrEmpty(renamingText))
                {
                    RenameEvent(eventData, renamingText);
                }
                ExitRenameMode();
            }
            
            if (GUILayout.Button("✗", GUILayout.Width(20)))
            {
                ExitRenameMode();
            }
            
            // 处理键盘输入
            if (Event.current.type == UnityEngine.EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return)
                {
                    if (!string.IsNullOrEmpty(renamingText))
                    {
                        RenameEvent(eventData, renamingText);
                    }
                    ExitRenameMode();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    ExitRenameMode();
                    Event.current.Use();
                }
            }
        }
        else
        {
            if (isSelected)
                GUI.backgroundColor = Color.cyan;
            
            if (GUILayout.Button($"{eventData.eventName}", EditorStyles.label))
            {
                selectedEvent = eventData;
                selectedNode = eventNodes.FirstOrDefault(n => n.eventData == eventData);
                UpdateNodeSelection();
            }
            
            GUI.backgroundColor = Color.white;
            
            // 右键菜单处理
            Event currentEvent = Event.current;
            if (currentEvent.type == UnityEngine.EventType.ContextClick && 
                GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                ShowEventContextMenu(eventData);
                currentEvent.Use();
            }
        }
        
        DrawEventStatusIndicators(eventData);
        
        EditorGUILayout.EndHorizontal();
        
        if (isSelected)
        {
            EditorGUILayout.LabelField($"Type: {eventData.eventType}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Priority: {eventData.priority}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Days: {eventData.minDay}-{eventData.maxDay}", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEventStatusIndicators(RandomEvent eventData)
    {
        GUILayout.FlexibleSpace();
        
        Color priorityColor = eventData.priority switch
        {
            EventPriority.Critical => Color.red,
            EventPriority.High => Color.yellow,
            EventPriority.Normal => Color.green,
            EventPriority.Low => Color.gray,
            _ => Color.white
        };
        
        GUI.color = priorityColor;
        GUILayout.Label("●", GUILayout.Width(15));
        GUI.color = Color.white;
        
        if (eventData.choices != null && eventData.choices.Length > 0)
        {
            GUILayout.Label($"[{eventData.choices.Length}]", EditorStyles.miniLabel, GUILayout.Width(25));
        }
    }
    
    void DrawEventDetails()
    {
        GUILayout.Space(10);
        GUILayout.Label("Event Details", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        selectedEvent.eventName = EditorGUILayout.TextField("Event Name", selectedEvent.eventName);
        selectedEvent.eventType = (EventType)EditorGUILayout.EnumPopup("Event Type", selectedEvent.eventType);
        selectedEvent.priority = (EventPriority)EditorGUILayout.EnumPopup("Priority", selectedEvent.priority);
        
        GUILayout.Label("Description:");
        selectedEvent.eventDescription = EditorGUILayout.TextArea(selectedEvent.eventDescription, GUILayout.Height(60));
        
        GUILayout.Label("Trigger Settings:");
        selectedEvent.minDay = EditorGUILayout.IntField("Min Day", selectedEvent.minDay);
        selectedEvent.maxDay = EditorGUILayout.IntField("Max Day", selectedEvent.maxDay);
        selectedEvent.baseTriggerChance = EditorGUILayout.Slider("Trigger Chance", selectedEvent.baseTriggerChance, 0f, 1f);
        
        selectedEvent.requiresChoice = EditorGUILayout.Toggle("Requires Choice", selectedEvent.requiresChoice);
        selectedEvent.canRepeat = EditorGUILayout.Toggle("Can Repeat", selectedEvent.canRepeat);
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedEvent);
        }
        
        if (GUILayout.Button("Edit Choices"))
        {
            EventChoiceEditor.OpenWindow(selectedEvent);
        }
        
        // 任务相关设置
        selectedEvent.isQuest = EditorGUILayout.Toggle("Is Quest", selectedEvent.isQuest);
        
        if (selectedEvent.isQuest)
        {
            showQuestFields = EditorGUILayout.Foldout(showQuestFields, "Quest Settings");
            if (showQuestFields)
            {
                DrawQuestFields();
            }
        }
        
        // 扩展字段
        GUILayout.Label("Advanced Settings:", EditorStyles.boldLabel);
        selectedEvent.isMainQuest = EditorGUILayout.Toggle("Is Main Quest", selectedEvent.isMainQuest);
        selectedEvent.isSideQuest = EditorGUILayout.Toggle("Is Side Quest", selectedEvent.isSideQuest);
        selectedEvent.questChain = EditorGUILayout.TextField("Quest Chain", selectedEvent.questChain ?? "");
        selectedEvent.questOrder = EditorGUILayout.IntField("Quest Order", selectedEvent.questOrder);
        
        // 事件标签
        GUILayout.Label("Tags:", EditorStyles.boldLabel);
        DrawEventTags();
        
        selectedEvent.editorColor = EditorGUILayout.ColorField("Editor Color", selectedEvent.editorColor);
    }
    
    void DrawEventTags()
    {
        if (selectedEvent.tags == null) selectedEvent.tags = new string[0];
        
        int newSize = EditorGUILayout.IntField("Tags Count", selectedEvent.tags.Length);
        if (newSize != selectedEvent.tags.Length)
        {
            Array.Resize(ref selectedEvent.tags, newSize);
        }
        
        for (int i = 0; i < selectedEvent.tags.Length; i++)
        {
            selectedEvent.tags[i] = EditorGUILayout.TextField($"Tag {i}", selectedEvent.tags[i] ?? "");
        }
    }
    
    void DrawQuestFields()
    {
        EditorGUILayout.BeginVertical("box");
        
        GUILayout.Label("Prerequisites:", EditorStyles.boldLabel);
        DrawStringArray(ref selectedEvent.prerequisiteQuestIds, "Prerequisite Quest IDs");
        
        GUILayout.Label("Unlocks:", EditorStyles.boldLabel);
        DrawStringArray(ref selectedEvent.unlockQuestIds, "Unlock Quest IDs");
        
        GUILayout.Label("Objectives:", EditorStyles.boldLabel);
        DrawQuestObjectives();
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawStringArray(ref string[] array, string label)
    {
        if (array == null) array = new string[0];
        
        int newSize = EditorGUILayout.IntField("Size", array.Length);
        if (newSize != array.Length)
        {
            Array.Resize(ref array, newSize);
        }
        
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = EditorGUILayout.TextField($"Element {i}", array[i] ?? "");
        }
    }
    
    void DrawQuestObjectives()
    {
        if (selectedEvent.questObjectives == null) selectedEvent.questObjectives = new QuestObjective[0];
        
        int newSize = EditorGUILayout.IntField("Objectives Count", selectedEvent.questObjectives.Length);
        if (newSize != selectedEvent.questObjectives.Length)
        {
            Array.Resize(ref selectedEvent.questObjectives, newSize);
            for (int i = 0; i < selectedEvent.questObjectives.Length; i++)
            {
                if (selectedEvent.questObjectives[i] == null)
                    selectedEvent.questObjectives[i] = new QuestObjective();
            }
        }
        
        for (int i = 0; i < selectedEvent.questObjectives.Length; i++)
        {
            var obj = selectedEvent.questObjectives[i];
            EditorGUILayout.BeginVertical("box");
            
            obj.objectiveId = EditorGUILayout.TextField("Objective ID", obj.objectiveId ?? "");
            obj.description = EditorGUILayout.TextField("Description", obj.description ?? "");
            obj.type = (QuestObjectiveType)EditorGUILayout.EnumPopup("Type", obj.type);
            obj.targetAmount = EditorGUILayout.IntField("Target Amount", obj.targetAmount);
            obj.targetId = EditorGUILayout.TextField("Target ID", obj.targetId ?? "");
            
            EditorGUILayout.EndVertical();
        }
    }
    
    void DrawRightPanel(Rect panel)
    {
        GUILayout.BeginArea(panel);
        
        GUILayout.Label("Event Flow Graph", EditorStyles.boldLabel);
        
        Rect graphArea = new Rect(0, 25, panel.width, panel.height - 25);
        DrawEventGraph(graphArea);
        
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
        int gridSpacing = 20;
        Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
        Handles.BeginGUI();
        Handles.color = gridColor;
        
        for (int x = 0; x < area.width; x += gridSpacing)
        {
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, area.height));
        }
        
        for (int y = 0; y < area.height; y += gridSpacing)
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
                node.rect = GUILayout.Window(i, node.rect, DrawNodeWindow, node.eventData.eventName, style);
            }
        }
        
        EndWindows();
    }
    
    void DrawNodeWindow(int id)
    {
        if (id < 0 || id >= eventNodes.Count) return;
        
        var node = eventNodes[id];
        
        GUILayout.Label($"Type: {node.eventData.eventType}");
        GUILayout.Label($"Day: {node.eventData.minDay}-{node.eventData.maxDay}");
        GUILayout.Label($"Chance: {node.eventData.baseTriggerChance:P0}");
        
        Event currentEvent = Event.current;
        if (currentEvent.type == UnityEngine.EventType.MouseDown)
        {
            selectedEvent = node.eventData;
            selectedNode = node;
            UpdateNodeSelection();
            Repaint();
        }
        
        // 保存节点位置到事件数据
        if (currentEvent.type == UnityEngine.EventType.MouseDrag || 
            currentEvent.type == UnityEngine.EventType.MouseUp)
        {
            SaveNodePosition(node);
        }
        
        GUI.DragWindow();
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
        Handles.color = Color.cyan;
        Handles.DrawBezier(startPos, endPos, 
            startPos + Vector3.right * 50, 
            endPos + Vector3.left * 50, 
            Color.cyan, null, 3f);
        Handles.EndGUI();
    }
    
    void ProcessNodeEvents()
    {
        Event currentEvent = Event.current;
        
        if (currentEvent.type == UnityEngine.EventType.MouseDown && currentEvent.button == 1)
        {
            Vector2 mousePos = currentEvent.mousePosition;
            mousePos.y -= 30; // 调整工具栏高度
            
            if (mousePos.x > 300) // 在右侧面板
            {
                Vector2 graphMousePos = mousePos - new Vector2(300, 25) + nodeScrollPos;
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
        // 弹出文件名输入对话框
        string fileName = EditorUtility.SaveFilePanel(
            "创建新事件", 
            "Assets/GameData/Events", 
            "NewEvent", 
            "asset");
            
        if (string.IsNullOrEmpty(fileName))
        {
            return; // 用户取消了
        }
        
        // 转换为相对路径
        if (fileName.StartsWith(Application.dataPath))
        {
            fileName = "Assets" + fileName.Substring(Application.dataPath.Length);
        }
        
        // 检查文件是否已存在
        if (System.IO.File.Exists(fileName))
        {
            if (!EditorUtility.DisplayDialog("文件已存在", 
                $"文件 {System.IO.Path.GetFileName(fileName)} 已存在，是否覆盖？", 
                "覆盖", "取消"))
            {
                return;
            }
        }
        
        // 确保目录存在
        string directory = System.IO.Path.GetDirectoryName(fileName);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // 创建事件
        RandomEvent newEvent = CreateInstance<RandomEvent>();
        
        // 从文件名推导事件名（移除扩展名和路径）
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
        
        // 初始化节点位置（使用GameEventManager中定义的EventNode）
        if (newEvent.nodePosition == null)
            newEvent.nodePosition = new EventNode();
        newEvent.nodePosition.position = position;
        
        // 创建资源文件
        AssetDatabase.CreateAsset(newEvent, fileName);
        AssetDatabase.SaveAssets();
        
        allEvents.Add(newEvent);
        
        EditorEventNode newNode = new EditorEventNode
        {
            eventData = newEvent,
            rect = new Rect(position.x, position.y, 180, 120),
            isSelected = false
        };
        eventNodes.Add(newNode);
        
        selectedEvent = newEvent;
        selectedNode = newNode;
        UpdateNodeSelection();
        
        // 自动进入重命名模式，让用户可以立即修改事件名
        EditorApplication.delayCall += () => {
            EnterRenameMode(newEvent);
        };
        
        Repaint();
        
        Debug.Log($"[EventEditor] 创建新事件: {fileName}");
    }
    
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
        
        EditorUtility.DisplayDialog("Save Complete", "All events have been saved.", "OK");
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
                    rect = new Rect(100, 100, 180, 120),
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
            }
        }
    }
    
    void DrawEventTestingPanel()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Event Testing", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (selectedEvent == null)
        {
            EditorGUILayout.HelpBox("Select an event to test", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Testing: {selectedEvent.eventName}");
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test events", MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Trigger Event"))
                {
                    TriggerEventInGame(selectedEvent);
                }
                
                if (GUILayout.Button("Validate Event"))
                {
                    var issues = ValidateEvent(selectedEvent);
                    string message = issues.Count > 0 ? 
                        "Issues found:\n" + string.Join("\n", issues) : 
                        "Event is valid!";
                    EditorUtility.DisplayDialog("Validation Result", message, "OK");
                }
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawEventStatisticsPanel()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Event Statistics", EditorStyles.boldLabel);
        
        statisticsScrollPos = EditorGUILayout.BeginScrollView(statisticsScrollPos, "box", GUILayout.Height(150));
        
        var typeGroups = allEvents.GroupBy(e => e.eventType);
        EditorGUILayout.LabelField("Events by Type:", EditorStyles.boldLabel);
        foreach (var group in typeGroups)
        {
            EditorGUILayout.LabelField($"  {group.Key}: {group.Count()}");
        }
        
        EditorGUILayout.Space();
        
        var priorityGroups = allEvents.GroupBy(e => e.priority);
        EditorGUILayout.LabelField("Events by Priority:", EditorStyles.boldLabel);
        foreach (var group in priorityGroups)
        {
            EditorGUILayout.LabelField($"  {group.Key}: {group.Count()}");
        }
        
        EditorGUILayout.Space();
        
        var withChoices = allEvents.Count(e => e.choices != null && e.choices.Length > 0);
        
        EditorGUILayout.LabelField("Other Statistics:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  Total Events: {allEvents.Count}");
        EditorGUILayout.LabelField($"  With Choices: {withChoices}");
        
        EditorGUILayout.EndScrollView();
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
            string message = $"Found issues in events:\n" + string.Join("\n", issues.Take(10));
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
            issues.Add("Invalid trigger chance (should be 0-1)");
        
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

快捷键:
- F2: 重命名选中的事件
- Delete: 删除选中的事件  
- Ctrl+D: 复制选中的事件
- Esc: 取消重命名

搜索:
- 支持按名称、描述、类型搜索
- 使用类型和优先级过滤器

右键菜单:
- 重命名、复制、删除事件
- 在项目中显示文件

节点图:
- 拖拽移动节点
- 右键创建新事件
- 连接线显示事件关系";
        
        EditorUtility.DisplayDialog("Event Editor Help", helpText, "OK");
    }
}

// 事件选择编辑器
public class EventChoiceEditor : EditorWindow
{
    private RandomEvent targetEvent;
    private Vector2 scrollPos;
    
    public static void OpenWindow(RandomEvent eventData)
    {
        EventChoiceEditor window = GetWindow<EventChoiceEditor>("Event Choice Editor");
        window.targetEvent = eventData;
        window.minSize = new Vector2(600, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        if (targetEvent == null)
        {
            GUILayout.Label("No event selected");
            return;
        }
        
        GUILayout.Label($"Editing Choices for: {targetEvent.eventName}", EditorStyles.boldLabel);
        
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        if (targetEvent.choices != null)
        {
            for (int i = 0; i < targetEvent.choices.Length; i++)
            {
                DrawChoiceEditor(i);
                GUILayout.Space(10);
            }
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Choice"))
        {
            AddNewChoice();
        }
        if (GUILayout.Button("Remove Last Choice") && targetEvent.choices != null && targetEvent.choices.Length > 0)
        {
            RemoveLastChoice();
        }
        GUILayout.EndHorizontal();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(targetEvent);
        }
    }
    
    void DrawChoiceEditor(int index)
    {
        if (targetEvent.choices == null || index >= targetEvent.choices.Length) return;
        
        var choice = targetEvent.choices[index];
        
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"Choice {index + 1}", EditorStyles.boldLabel);
        
        choice.choiceText = EditorGUILayout.TextField("Choice Text", choice.choiceText ?? "");
        choice.resultDescription = EditorGUILayout.TextArea(choice.resultDescription ?? "", GUILayout.Height(40));
        choice.isRecommended = EditorGUILayout.Toggle("Is Recommended", choice.isRecommended);
        choice.buttonColor = EditorGUILayout.ColorField("Button Color", choice.buttonColor);
        
        // 需求条件
        GUILayout.Label("Requirements:", EditorStyles.boldLabel);
        if (choice.requirements != null)
        {
            for (int r = 0; r < choice.requirements.Length; r++)
            {
                var req = choice.requirements[r];
                GUILayout.BeginHorizontal();
                req.resourceType = EditorGUILayout.TextField("Resource", req.resourceType ?? "");
                req.amount = EditorGUILayout.IntField("Amount", req.amount);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveRequirement(index, r);
                    return;
                }
                GUILayout.EndHorizontal();
            }
        }
        
        if (GUILayout.Button("Add Requirement"))
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
                GUILayout.BeginVertical("box");
                
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
                
                if (GUILayout.Button("Remove Effect"))
                {
                    RemoveEffect(index, e);
                    GUILayout.EndVertical();
                    return;
                }
                
                GUILayout.EndVertical();
            }
        }
        
        if (GUILayout.Button("Add Effect"))
        {
            AddEffect(index);
        }
        
        GUILayout.EndVertical();
    }
    
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