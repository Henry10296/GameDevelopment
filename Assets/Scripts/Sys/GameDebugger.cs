using UnityEngine;

public class GameDebugger : MonoBehaviour
{
    [Header("调试显示")]
    public bool showDebugInfo = true;
    public bool showGameState = true;
    public bool showFamilyStatus = true;
    public bool showInventory = true;
    public bool showPerformance = true;
    public bool showAIStatus = true;
    
    private GUIStyle guiStyle;
    private float deltaTime = 0.0f;
    
    void Start()
    {
        // 初始化GUI样式
        guiStyle = new GUIStyle();
        guiStyle.alignment = TextAnchor.UpperLeft;
        guiStyle.fontSize = 14;
        guiStyle.normal.textColor = Color.white;
        
        // 添加背景
        Texture2D backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
        backgroundTexture.Apply();
        guiStyle.normal.background = backgroundTexture;
        
        guiStyle.padding = new RectOffset(5, 5, 5, 5);
    }
    
    void Update()
    {
        // 计算FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        // 快捷键调试
        HandleDebugKeys();
    }
    
    void HandleDebugKeys()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebugInfo = !showDebugInfo;
        }
        
        if (Input.GetKeyDown(KeyCode.F2) && GameStateManager.Instance)
        {
            GameStateManager.Instance.DebugNextDay();
        }
        
        if (Input.GetKeyDown(KeyCode.F3) && FamilyManager.Instance)
        {
            FamilyManager.Instance.DebugAddResources();
        }
        
        if (Input.GetKeyDown(KeyCode.F4) && RadioManager.Instance)
        {
            RadioManager.Instance.FindRadio();
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        float yPos = 10f;
        float panelWidth = 350f;
        
        // 性能信息
        if (showPerformance)
        {
            yPos = DrawPerformanceInfo(10f, yPos, panelWidth);
        }
        
        // 游戏状态信息
        if (showGameState && GameStateManager.Instance != null)
        {
            yPos = DrawGameStateInfo(10f, yPos, panelWidth);
        }
        
        // 家庭状态信息
        if (showFamilyStatus && FamilyManager.Instance != null)
        {
            yPos = DrawFamilyStatusInfo(10f, yPos, panelWidth);
        }
        
        // 背包信息
        if (showInventory && InventoryManager.Instance != null)
        {
            yPos = DrawInventoryInfo(10f, yPos, panelWidth);
        }
        
        // AI状态信息
        if (showAIStatus)
        {
            yPos = DrawAIStatusInfo(10f, yPos, panelWidth);
        }
        
        // 调试按钮
        DrawDebugButtons(10f, yPos);
    }
    
    float DrawPerformanceInfo(float x, float y, float width)
    {
        float fps = 1.0f / deltaTime;
        string perfText = $"=== 性能信息 ===\n";
        perfText += $"FPS: {fps:0.0}\n";
        perfText += $"内存: {System.GC.GetTotalMemory(false) / 1024 / 1024:F1} MB\n";
        perfText += $"时间缩放: {Time.timeScale:F2}\n";
        
        float height = guiStyle.CalcHeight(new GUIContent(perfText), width);
        GUI.Label(new Rect(x, y, width, height), perfText, guiStyle);
        
        return y + height + 5f;
    }
    
    float DrawGameStateInfo(float x, float y, float width)
    {
        var gm = GameStateManager.Instance;
        string gameText = $"=== 游戏状态 ===\n";
        gameText += $"当前阶段: {gm.CurrentPhase}\n";
        gameText += $"天数: {gm.CurrentDay} / {gm.config.maxDays}\n";
        gameText += $"剩余时间: {gm.RemainingTime:F1}s\n";
        gameText += $"是否探索中: {gm.IsExploring}\n";
        gameText += $"游戏是否结束: {gm.GameEnded}\n";
        
        if (RadioManager.Instance)
        {
            gameText += $"拥有无线电: {RadioManager.Instance.hasRadio}\n";
            gameText += $"信号已发送: {RadioManager.Instance.radioBroadcasted}\n";
        }
        
        float height = guiStyle.CalcHeight(new GUIContent(gameText), width);
        GUI.Label(new Rect(x, y, width, height), gameText, guiStyle);
        
        return y + height + 5f;
    }
    
    float DrawFamilyStatusInfo(float x, float y, float width)
    {
        var fm = FamilyManager.Instance;
        string familyText = $"=== 家庭状态 ===\n";
        familyText += $"资源 - 食物: {fm.Food}, 水: {fm.Water}, 药品: {fm.Medicine}\n";
        familyText += $"活着的成员: {fm.AliveMembers}/{fm.FamilyMembers.Count}\n";
        familyText += $"整体状况: {fm.GetOverallStatus()}\n\n";
        
        foreach (var member in fm.FamilyMembers)
        {
            string status = member.IsAlive ? "存活" : "死亡";
            if (member.isSick) status += " [生病]";
            if (member.isInjured) status += " [受伤]";
            
            Color originalColor = GUI.color;
            
            if (!member.IsAlive)
                GUI.color = Color.red;
            else if (member.IsInDanger)
                GUI.color = Color.green;
            else if (!member.IsHealthy)
                GUI.color = Color.yellow;
            
            familyText += $"{member.name}: H{member.health:0} F{member.hunger:0} T{member.thirst:0} M{member.mood:0} [{status}]\n";
            
            GUI.color = originalColor;
        }
        
        float height = guiStyle.CalcHeight(new GUIContent(familyText), width);
        GUI.Label(new Rect(x, y, width, height), familyText, guiStyle);
        
        return y + height + 5f;
    }
    
    float DrawInventoryInfo(float x, float y, float width)
    {
        var im = InventoryManager.Instance;
        string invText = $"=== 背包信息 ===\n";
        invText += $"使用槽位: {im.GetItems().Count} / {im.maxSlots}\n";
        
        var items = im.GetItems();
        if (items.Count > 0)
        {
            invText += "物品列表:\n";
            foreach (var item in items)
            {
                invText += $"  {item.itemData.itemName} x{item.quantity}\n";
            }
        }
        else
        {
            invText += "背包为空\n";
        }
        
        float height = guiStyle.CalcHeight(new GUIContent(invText), width);
        GUI.Label(new Rect(x, y, width, height), invText, guiStyle);
        
        return y + height + 5f;
    }
    
    float DrawAIStatusInfo(float x, float y, float width)
    {
        string aiText = $"=== AI状态 ===\n";
        
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        if (enemies.Length > 0)
        {
            aiText += $"敌人数量: {enemies.Length}\n";
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    string enemyInfo = $"{enemy.name}: {enemy.EnemyType}";
                    if (enemy.GetComponent<EnemyHealth>() != null)
                    {
                        var health = enemy.GetComponent<EnemyHealth>();
                        enemyInfo += $" HP:{health.CurrentHealth:0}/{health.MaxHealth:0}";
                    }
                    aiText += enemyInfo + "\n";
                }
            }
        }
        else
        {
            aiText += "场景中没有敌人\n";
        }
        
        float height = guiStyle.CalcHeight(new GUIContent(aiText), width);
        GUI.Label(new Rect(x, y, width, height), aiText, guiStyle);
        
        return y + height + 5f;
    }
    
    void DrawDebugButtons(float x, float y)
    {
        float buttonWidth = 120f;
        float buttonHeight = 25f;
        float spacing = 5f;
        
        // 第一行按钮
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "下一天 (F2)"))
        {
            GameStateManager.Instance?.DebugNextDay();
        }
        
        if (GUI.Button(new Rect(x + buttonWidth + spacing, y, buttonWidth, buttonHeight), "添加资源 (F3)"))
        {
            FamilyManager.Instance?.DebugAddResources();
        }
        
        if (GUI.Button(new Rect(x + (buttonWidth + spacing) * 2, y, buttonWidth, buttonHeight), "结束游戏"))
        {
            GameStateManager.Instance?.DebugGameEnd();
        }
        
        // 第二行按钮
        y += buttonHeight + spacing;
        
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "找到无线电 (F4)"))
        {
            RadioManager.Instance?.FindRadio();
        }
        
        if (GUI.Button(new Rect(x + buttonWidth + spacing, y, buttonWidth, buttonHeight), "治愈所有人"))
        {
            FamilyManager.Instance?.DebugHealAll();
        }
        
        if (GUI.Button(new Rect(x + (buttonWidth + spacing) * 2, y, buttonWidth, buttonHeight), "所有人生病"))
        {
            FamilyManager.Instance?.DebugMakeAllSick();
        }
        
        // 第三行按钮
        y += buttonHeight + spacing;
        
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "跳到探索"))
        {
            GameStateManager.Instance?.DebugSkipToExploration();
        }
        
        if (GUI.Button(new Rect(x + buttonWidth + spacing, y, buttonWidth, buttonHeight), "显示消息"))
        {
            UIManager.Instance?.ShowMessage("这是一条调试消息！", 3f);
        }
        
        // 显示快捷键提示
        y += buttonHeight + spacing * 2;
        string keyText = "快捷键: F1-切换调试 F2-下一天 F3-加资源 F4-找无线电";
        GUI.Label(new Rect(x, y, 400f, 20f), keyText, guiStyle);
    }
}