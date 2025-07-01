#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class EventEditorWindow : EditorWindow
{
    // 核心字段定义
    private Vector2 scrollPos;
    private RandomEvent selectedEvent;
    private List<RandomEvent> allEvents = new List<RandomEvent>();
    private Vector2 nodeScrollPos;
    private Rect nodeArea = new Rect(0, 0, 2000, 2000);
    
    // 节点编辑状态
    private bool isDragging = false;
    private Vector2 dragStartPos;
    private EventNode selectedNode;
    private List<EventNode> eventNodes = new List<EventNode>();
    
    // UI样式
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle connectionStyle;
    
    // 新增功能字段
    private bool showEventTesting = false;
    private bool showEventStatistics = false;
    private Vector2 statisticsScrollPos;
    private string eventSearchFilter = "";
    private EventType eventTypeFilter = (EventType)(-1);
    private EventPriority eventPriorityFilter = (EventPriority)(-1);
    
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
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.padding = new RectOffset(10, 10, 10, 10);
        nodeStyle.normal.textColor = Color.white;
        
        selectedNodeStyle = new GUIStyle(nodeStyle);
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        
        connectionStyle = new GUIStyle();
        connectionStyle.normal.background = EditorGUIUtility.whiteTexture;
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
            var node = new EventNode
            {
                eventData = eventData,
                rect = new Rect(100 + (i % 5) * 200, 100 + (i / 5) * 150, 180, 120)
            };
            eventNodes.Add(node);
        }
    }
    
    void OnGUI()
    {
        DrawToolbar();
        
        // 分割界面
        Rect leftPanel = new Rect(0, 30, 300, position.height - 30);
        Rect rightPanel = new Rect(300, 30, position.width - 300, position.height - 30);
        
        DrawLeftPanel(leftPanel);
        DrawRightPanel(rightPanel);
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
        
        // 新增：配置验证
        if (GUILayout.Button("Validate All", EditorStyles.toolbarButton))
        {
            ValidateAllEvents();
        }
        
        // 新增：批量操作
        if (GUILayout.Button("Batch Edit", EditorStyles.toolbarButton))
        {
            ShowBatchEditWindow();
        }
        
        GUILayout.FlexibleSpace();
        
        // 新增：视图选项
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
        
        // 新增：搜索和过滤
        DrawEventFilters();
        
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        foreach (var eventData in GetFilteredEvents())
        {
            DrawEventListItem(eventData);
        }
        
        GUILayout.EndScrollView();
        
        // 现有事件详情
        if (selectedEvent != null)
        {
            DrawEventDetails();
        }
        
        // 新增：事件测试面板
        if (showEventTesting)
        {
            DrawEventTestingPanel();
        }
        
        // 新增：事件统计面板
        if (showEventStatistics)
        {
            DrawEventStatisticsPanel();
        }
        
        GUILayout.EndArea();
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
        
        // 选择项编辑按钮
        if (GUILayout.Button("Edit Choices"))
        {
            EventChoiceEditor.OpenWindow(selectedEvent);
        }
    }
    
    void DrawRightPanel(Rect panel)
    {
        GUILayout.BeginArea(panel);
        
        GUILayout.Label("Event Flow Graph", EditorStyles.boldLabel);
        
        // 节点图编辑区域
        Rect graphArea = new Rect(0, 25, panel.width, panel.height - 25);
        DrawEventGraph(graphArea);
        
        GUILayout.EndArea();
    }
    
    void DrawEventGraph(Rect area)
    {
        // 背景网格
        DrawGrid(area);
        
        // 开始滚动视图
        nodeScrollPos = GUI.BeginScrollView(area, nodeScrollPos, nodeArea);
        
        // 绘制连接线
        DrawConnections();
        
        // 绘制节点
        DrawNodes();
        
        GUI.EndScrollView();
    }
    
    void DrawGrid(Rect area)
    {
        int gridSpacing = 20;
        Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
        Handles.BeginGUI();
        Handles.color = gridColor;
        
        // 垂直线
        for (int x = 0; x < area.width; x += gridSpacing)
        {
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, area.height));
        }
        
        // 水平线
        for (int y = 0; y < area.height; y += gridSpacing)
        {
            Handles.DrawLine(new Vector3(0, y), new Vector3(area.width, y));
        }
        
        Handles.EndGUI();
    }
    
    void DrawNodes()
    {
        foreach (var node in eventNodes)
        {
            DrawEventNode(node);
        }
    }
    
    void DrawEventNode(EventNode node)
    {
        bool isSelected = selectedNode == node;
        GUIStyle style = isSelected ? selectedNodeStyle : nodeStyle;
        
        GUI.Box(node.rect, "", style);
        
        GUILayout.BeginArea(node.rect);
        
        GUILayout.Label(node.eventData.eventName, EditorStyles.boldLabel);
        GUILayout.Label($"Type: {node.eventData.eventType}");
        GUILayout.Label($"Day: {node.eventData.minDay}-{node.eventData.maxDay}");
        GUILayout.Label($"Chance: {node.eventData.baseTriggerChance:P0}");
        
        GUILayout.EndArea();
        
        // 连接点
        DrawConnectionPoints(node);
    }
    
    void DrawConnectionPoints(EventNode node)
    {
        // 输入点
        Rect inputPoint = new Rect(node.rect.x - 10, node.rect.y + node.rect.height / 2 - 5, 10, 10);
        GUI.Box(inputPoint, "", EditorStyles.helpBox);
        
        // 输出点
        Rect outputPoint = new Rect(node.rect.x + node.rect.width, node.rect.y + node.rect.height / 2 - 5, 10, 10);
        GUI.Box(outputPoint, "", EditorStyles.helpBox);
    }
    
    void DrawConnections()
    {
        foreach (var node in eventNodes)
        {
            if (node.eventData.followupEvent != null)
            {
                EventNode targetNode = eventNodes.FirstOrDefault(n => n.eventData == node.eventData.followupEvent);
                if (targetNode != null)
                {
                    DrawConnection(node, targetNode);
                }
            }
        }
    }
    
    void DrawConnection(EventNode from, EventNode to)
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
    
    void CreateNewEvent()
    {
        CreateNewEventAt(new Vector2(100, 100));
    }
    
    void CreateNewEventAt(Vector2 position)
    {
        RandomEvent newEvent = CreateInstance<RandomEvent>();
        newEvent.eventName = "New Event";
        newEvent.eventDescription = "Enter event description here";
        newEvent.eventType = EventType.ResourceGain;
        newEvent.priority = EventPriority.Normal;
        newEvent.minDay = 1;
        newEvent.maxDay = 5;
        newEvent.baseTriggerChance = 0.3f;
        
        string path = $"Assets/GameData/Events/NewEvent_{System.DateTime.Now.Ticks}.asset";
        AssetDatabase.CreateAsset(newEvent, path);
        AssetDatabase.SaveAssets();
        
        allEvents.Add(newEvent);
        
        EventNode newNode = new EventNode
        {
            eventData = newEvent,
            rect = new Rect(position.x, position.y, 180, 120)
        };
        eventNodes.Add(newNode);
        
        selectedEvent = newEvent;
        selectedNode = newNode;
        
        Repaint();
    }
    
    void SaveAllEvents()
    {
        foreach (var eventData in allEvents)
        {
            EditorUtility.SetDirty(eventData);
        }
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Save Complete", "All events have been saved.", "OK");
    }
    
    void ShowHelp()
    {
        string helpText = @"Event Editor Help:

Left Click: Select node
Right Click: Context menu
Drag: Move nodes

Create new events with the toolbar button or right-click context menu.
Connect events by setting the followupEvent field in the inspector.
Edit event choices with the 'Edit Choices' button.

Event Types:
- ResourceGain: Provides resources
- ResourceLoss: Removes resources
- FamilyHealth: Affects family members
- StoryProgression: Advances story
- RadioReminder: Radio signal reminders";
        
        EditorUtility.DisplayDialog("Event Editor Help", helpText, "OK");
    }
    
    // 新增功能方法
    void DrawEventFilters()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Filters", EditorStyles.boldLabel);
        
        eventSearchFilter = EditorGUILayout.TextField("Search:", eventSearchFilter);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Type:", GUILayout.Width(40));
        eventTypeFilter = (EventType)EditorGUILayout.EnumPopup(eventTypeFilter);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Priority:", GUILayout.Width(40));
        eventPriorityFilter = (EventPriority)EditorGUILayout.EnumPopup(eventPriorityFilter);
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Clear Filters"))
        {
            eventSearchFilter = "";
            eventTypeFilter = (EventType)(-1);
            eventPriorityFilter = (EventPriority)(-1);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    List<RandomEvent> GetFilteredEvents()
    {
        var filtered = allEvents.AsEnumerable();
        
        if (!string.IsNullOrEmpty(eventSearchFilter))
        {
            filtered = filtered.Where(e => e.eventName.ToLower().Contains(eventSearchFilter.ToLower()) ||
                                          e.eventDescription.ToLower().Contains(eventSearchFilter.ToLower()));
        }
        
        if ((int)eventTypeFilter >= 0)
        {
            filtered = filtered.Where(e => e.eventType == eventTypeFilter);
        }
        
        if ((int)eventPriorityFilter >= 0)
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
        
        if (isSelected)
            GUI.backgroundColor = Color.cyan;
        
        if (GUILayout.Button($"{eventData.eventName}", EditorStyles.label))
        {
            selectedEvent = eventData;
            selectedNode = eventNodes.FirstOrDefault(n => n.eventData == eventData);
        }
        
        GUI.backgroundColor = Color.white;
        
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
        
        if (eventData.triggerConditions != null && eventData.triggerConditions.Length > 0)
        {
            GUILayout.Label("C", EditorStyles.miniLabel, GUILayout.Width(15));
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
                
                if (GUILayout.Button("Test Conditions"))
                {
                    TestEventConditions(selectedEvent);
                }
                
                if (GUILayout.Button("Validate Event"))
                {
                    ValidateEvent(selectedEvent);
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
        var withConditions = allEvents.Count(e => e.triggerConditions != null && e.triggerConditions.Length > 0);
        
        EditorGUILayout.LabelField("Other Statistics:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  Total Events: {allEvents.Count}");
        EditorGUILayout.LabelField($"  With Choices: {withChoices}");
        EditorGUILayout.LabelField($"  With Conditions: {withConditions}");
        
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
            EditorUtility.DisplayDialog("Event Validation Results", 
                $"Found issues in {issues.Count} events:\n" + string.Join("\n", issues.Take(10)), "OK");
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
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.TriggerEventExternally(eventData);
            EditorUtility.DisplayDialog("Event Triggered", $"Triggered: {eventData.eventName}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "GameEventManager not found in scene", "OK");
        }
    }
    
    void TestEventConditions(RandomEvent eventData)
    {
        if (eventData.triggerConditions == null || eventData.triggerConditions.Length == 0)
        {
            EditorUtility.DisplayDialog("Condition Test", "No conditions to test", "OK");
            return;
        }
        
        var results = new List<string>();
        foreach (var condition in eventData.triggerConditions)
        {
            bool result = condition.IsMet();
            results.Add($"{condition.type}: {(result ? "PASS" : "FAIL")}");
        }
        
        EditorUtility.DisplayDialog("Condition Test Results", string.Join("\n", results), "OK");
    }
    
    void ShowBatchEditWindow()
    {
        EventBatchEditWindow.OpenWindow(allEvents);
    }
}

[System.Serializable]
public class EventNode
{
    public RandomEvent eventData;
    public Rect rect;
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
        var choice = targetEvent.choices[index];
        
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"Choice {index + 1}", EditorStyles.boldLabel);
        
        choice.choiceText = EditorGUILayout.TextField("Choice Text", choice.choiceText);
        choice.resultDescription = EditorGUILayout.TextArea(choice.resultDescription, GUILayout.Height(40));
        choice.isRecommended = EditorGUILayout.Toggle("Is Recommended", choice.isRecommended);
        choice.buttonColor = EditorGUILayout.ColorField("Button Color", choice.buttonColor);
        
        GUILayout.Label("Requirements:", EditorStyles.boldLabel);
        if (choice.requirements != null)
        {
            for (int r = 0; r < choice.requirements.Length; r++)
            {
                var req = choice.requirements[r];
                GUILayout.BeginHorizontal();
                req.resourceType = EditorGUILayout.TextField("Resource", req.resourceType);
                req.amount = EditorGUILayout.IntField("Amount", req.amount);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveRequirement(index, r);
                }
                GUILayout.EndHorizontal();
            }
        }
        
        if (GUILayout.Button("Add Requirement"))
        {
            AddRequirement(index);
        }
        
        GUILayout.Label("Effects:", EditorStyles.boldLabel);
        if (choice.effects != null)
        {
            for (int e = 0; e < choice.effects.Length; e++)
            {
                var effect = choice.effects[e];
                GUILayout.BeginHorizontal();
                effect.type = (EffectType)EditorGUILayout.EnumPopup("Type", effect.type);
                effect.resourceType = EditorGUILayout.TextField("Resource", effect.resourceType);
                effect.resourceAmount = EditorGUILayout.IntField("Amount", effect.resourceAmount);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveEffect(index, e);
                }
                GUILayout.EndHorizontal();
            }
        }
        
        if (GUILayout.Button("Add Effect"))
        {
            AddEffect(index);
        }
        
        GUILayout.EndVertical();
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
    }
    
    void RemoveLastChoice()
    {
        if (targetEvent.choices != null && targetEvent.choices.Length > 0)
        {
            var choices = targetEvent.choices.ToList();
            choices.RemoveAt(choices.Count - 1);
            targetEvent.choices = choices.ToArray();
        }
    }
    
    void AddRequirement(int choiceIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var requirements = choice.requirements?.ToList() ?? new List<ResourceRequirement>();
        requirements.Add(new ResourceRequirement { resourceType = "food", amount = 1 });
        choice.requirements = requirements.ToArray();
    }
    
    void RemoveRequirement(int choiceIndex, int reqIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var requirements = choice.requirements.ToList();
        requirements.RemoveAt(reqIndex);
        choice.requirements = requirements.ToArray();
    }
    
    void AddEffect(int choiceIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var effects = choice.effects?.ToList() ?? new List<EventEffect>();
        effects.Add(new EventEffect { type = EffectType.ModifyResource, resourceType = "food", resourceAmount = 1 });
        choice.effects = effects.ToArray();
    }
    
    void RemoveEffect(int choiceIndex, int effectIndex)
    {
        var choice = targetEvent.choices[choiceIndex];
        var effects = choice.effects.ToList();
        effects.RemoveAt(effectIndex);
        choice.effects = effects.ToArray();
    }
}

// 事件批量编辑窗口
public class EventBatchEditWindow : EditorWindow
{
    private List<RandomEvent> events;
    private Vector2 scrollPos;
    private bool[] selectedEvents;
    
    private EventType batchEventType;
    private EventPriority batchPriority;
    private float batchTriggerChance = 0.3f;
    private int batchMinDay = 1;
    private int batchMaxDay = 5;
    
    public static void OpenWindow(List<RandomEvent> eventList)
    {
        var window = GetWindow<EventBatchEditWindow>("Batch Edit Events");
        window.events = eventList;
        window.selectedEvents = new bool[eventList.Count];
        window.Show();
    }
    
    private void OnGUI()
    {
        if (events == null) return;
        
        EditorGUILayout.LabelField("Batch Edit Events", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Select Events to Edit:");
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            for (int i = 0; i < selectedEvents.Length; i++)
                selectedEvents[i] = true;
        }
        if (GUILayout.Button("Select None"))
        {
            for (int i = 0; i < selectedEvents.Length; i++)
                selectedEvents[i] = false;
        }
        EditorGUILayout.EndHorizontal();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        for (int i = 0; i < events.Count; i++)
        {
            selectedEvents[i] = EditorGUILayout.ToggleLeft(events[i].eventName, selectedEvents[i]);
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Batch Edit Options:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        batchEventType = (EventType)EditorGUILayout.EnumPopup("Event Type:", batchEventType);
        batchPriority = (EventPriority)EditorGUILayout.EnumPopup("Priority:", batchPriority);
        batchTriggerChance = EditorGUILayout.Slider("Trigger Chance:", batchTriggerChance, 0f, 1f);
        
        EditorGUILayout.BeginHorizontal();
        batchMinDay = EditorGUILayout.IntField("Min Day:", batchMinDay);
        batchMaxDay = EditorGUILayout.IntField("Max Day:", batchMaxDay);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Changes"))
        {
            ApplyBatchChanges();
        }
    }
    
    void ApplyBatchChanges()
    {
        int changedCount = 0;
        
        for (int i = 0; i < events.Count; i++)
        {
            if (selectedEvents[i])
            {
                events[i].eventType = batchEventType;
                events[i].priority = batchPriority;
                events[i].baseTriggerChance = batchTriggerChance;
                events[i].minDay = batchMinDay;
                events[i].maxDay = batchMaxDay;
                
                EditorUtility.SetDirty(events[i]);
                changedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Batch Edit Complete", $"Modified {changedCount} events", "OK");
    }
}
#endif
