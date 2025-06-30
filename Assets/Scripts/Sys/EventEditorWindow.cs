#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class EventEditorWindow : EditorWindow
{
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
        
        GUILayout.FlexibleSpace();
        
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
        
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        foreach (var eventData in allEvents)
        {
            bool isSelected = selectedEvent == eventData;
            
            if (isSelected)
                GUI.backgroundColor = Color.cyan;
            
            if (GUILayout.Button($"{eventData.eventName}\n[{eventData.eventType}]", 
                GUILayout.Height(40)))
            {
                selectedEvent = eventData;
                selectedNode = eventNodes.FirstOrDefault(n => n.eventData == eventData);
            }
            
            if (isSelected)
                GUI.backgroundColor = Color.white;
        }
        
        GUILayout.EndScrollView();
        
        // 事件详情
        if (selectedEvent != null)
        {
            DrawEventDetails();
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
        
        // 处理输入
        //HandleGraphInput();
        
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
    
    
    void HandleLeftClick(Vector2 mousePos)
    {
        // 检查是否点击了节点
        foreach (var node in eventNodes)
        {
            if (node.rect.Contains(mousePos))
            {
                selectedNode = node;
                selectedEvent = node.eventData;
                isDragging = true;
                dragStartPos = mousePos;
                Event.current.Use();
                return;
            }
        }
        
        // 点击空白区域，取消选择
        selectedNode = null;
        selectedEvent = null;
    }
    
    void HandleRightClick(Vector2 mousePos)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("Create New Event"), false, () => CreateNewEventAt(mousePos));
        
        if (selectedNode != null)
        {
            menu.AddItem(new GUIContent("Delete Event"), false, () => DeleteSelectedEvent());
            menu.AddItem(new GUIContent("Duplicate Event"), false, () => DuplicateSelectedEvent());
        }
        
        menu.ShowAsContext();
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
    
    void DeleteSelectedEvent()
    {
        if (selectedNode != null)
        {
            string path = AssetDatabase.GetAssetPath(selectedNode.eventData);
            AssetDatabase.DeleteAsset(path);
            
            allEvents.Remove(selectedNode.eventData);
            eventNodes.Remove(selectedNode);
            
            selectedNode = null;
            selectedEvent = null;
            
            Repaint();
        }
    }
    
    void DuplicateSelectedEvent()
    {
        if (selectedNode != null)
        {
            RandomEvent original = selectedNode.eventData;
            RandomEvent duplicate = Instantiate(original);
            duplicate.eventName = original.eventName + " (Copy)";
            
            string path = $"Assets/GameData/Events/{duplicate.eventName}_{System.DateTime.Now.Ticks}.asset";
            AssetDatabase.CreateAsset(duplicate, path);
            AssetDatabase.SaveAssets();
            
            allEvents.Add(duplicate);
            
            EventNode newNode = new EventNode
            {
                eventData = duplicate,
                rect = new Rect(selectedNode.rect.x + 200, selectedNode.rect.y, 180, 120)
            };
            eventNodes.Add(newNode);
            
            Repaint();
        }
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
        
        // 显示所有选择项
        if (targetEvent.choices != null)
        {
            for (int i = 0; i < targetEvent.choices.Length; i++)
            {
                DrawChoiceEditor(i);
                GUILayout.Space(10);
            }
        }
        
        GUILayout.EndScrollView();
        
        // 添加/删除按钮
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
        
        // 需求条件编辑
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
        
        // 效果编辑
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
#endif