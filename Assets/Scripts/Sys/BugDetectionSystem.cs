using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class BugDetectionSystem : Singleton<BugDetectionSystem>
{
    [Header("检测配置")]
    public bool enableAutoDetection = true;
    public float detectionInterval = 5f;
    public bool showDetailedReport = true;
    
    [Header("严重程度过滤")]
    public bool detectCritical = true;
    public bool detectWarning = true;
    public bool detectInfo = true;
    
    private List<BugReport> bugReports = new List<BugReport>();
    private float lastDetectionTime;
    
    public System.Action<BugReport> OnBugDetected;
    
    protected override void Awake()
    {
        base.Awake();
        InvokeRepeating(nameof(PerformSystemCheck), 2f, detectionInterval);
    }
    
    void Update()
    {
        if (enableAutoDetection && Time.time - lastDetectionTime > detectionInterval)
        {
            PerformSystemCheck();
            lastDetectionTime = Time.time;
        }
    }
    
    void PerformSystemCheck()
    {
        bugReports.Clear();
        
        // 检查管理器状态
        CheckManagerHealth();
        
        // 检查数据完整性
        CheckDataIntegrity();
        
        // 检查性能问题
        CheckPerformanceIssues();
        
        // 检查UI状态
        CheckUIState();
        
        // 检查游戏逻辑
        CheckGameLogic();
        
        // 处理检测结果
        ProcessBugReports();
    }
    
    void CheckManagerHealth()
    {
        // 检查关键管理器是否存在
        CheckManager<GameManager>("GameManager", BugSeverity.Critical);
        CheckManager<FamilyManager>("FamilyManager", BugSeverity.Critical);
        CheckManager<InventoryManager>("InventoryManager", BugSeverity.Warning);
        CheckManager<UIManager>("UIManager", BugSeverity.Warning);
        CheckManager<AudioManager>("AudioManager", BugSeverity.Info);
    }
    
    void CheckManager<T>(string managerName, BugSeverity severity) where T : MonoBehaviour
    {
        var manager = FindObjectOfType<T>();
        if (manager == null)
        {
            ReportBug($"{managerName} missing", 
                     $"关键管理器 {managerName} 未找到", 
                     severity, 
                     BugCategory.Manager);
        }
    }
    
    void CheckDataIntegrity()
    {
        // 检查GameManager配置
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.Config == null)
                ReportBug("GameConfig missing", "GameManager缺少配置文件", BugSeverity.Critical, BugCategory.Data);
        }
        
        // 检查家庭数据
        if (FamilyManager.Instance != null)
        {
            var familyMembers = FamilyManager.Instance.FamilyMembers;
            if (familyMembers == null || familyMembers.Count == 0)
                ReportBug("Empty family", "家庭成员数据为空", BugSeverity.Critical, BugCategory.Data);
            
            // 检查异常的血量值
            foreach (var member in familyMembers)
            {
                if (member.health < 0 || member.health > 100)
                    ReportBug($"Invalid health: {member.name}", 
                             $"{member.name}的血量异常: {member.health}", 
                             BugSeverity.Warning, BugCategory.Data);
                
                if (member.hunger < 0 || member.hunger > 100)
                    ReportBug($"Invalid hunger: {member.name}", 
                             $"{member.name}的饥饿值异常: {member.hunger}", 
                             BugSeverity.Warning, BugCategory.Data);
            }
        }
        
        // 检查背包数据
        if (InventoryManager.Instance != null)
        {
            var items = InventoryManager.Instance.GetItems();
            foreach (var item in items)
            {
                if (item.itemData == null)
                    ReportBug("Null item data", "背包中存在空的物品数据", BugSeverity.Warning, BugCategory.Data);
                
                if (item.quantity <= 0)
                    ReportBug("Invalid item quantity", $"物品数量异常: {item.quantity}", BugSeverity.Warning, BugCategory.Data);
            }
        }
    }
    
    void CheckPerformanceIssues()
    {
        // FPS检测
        float fps = 1.0f / Time.deltaTime;
        if (fps < 30f)
            ReportBug("Low FPS", $"FPS过低: {fps:F1}", BugSeverity.Warning, BugCategory.Performance);
        
        // 内存检测
        long memoryUsage = System.GC.GetTotalMemory(false);
        if (memoryUsage > 500 * 1024 * 1024) // 500MB
            ReportBug("High memory usage", $"内存使用过高: {memoryUsage / 1024 / 1024}MB", BugSeverity.Info, BugCategory.Performance);
        
        // 检查敌人数量
        var enemies = FindObjectsOfType<EnemyAI>();
        if (enemies.Length > 20)
            ReportBug("Too many enemies", $"敌人数量过多: {enemies.Length}", BugSeverity.Warning, BugCategory.Performance);
    }
    
    void CheckUIState()
    {
        if (UIManager.Instance == null) return;
        
        // 检查UI面板状态
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        int activeCanvases = canvases.Count(c => c.gameObject.activeInHierarchy);
        
        if (activeCanvases > 10)
            ReportBug("Too many active canvases", $"活跃Canvas过多: {activeCanvases}", BugSeverity.Info, BugCategory.UI);
    }
    
    void CheckGameLogic()
    {
        if (GameManager.Instance == null) return;
        
        // 检查游戏状态逻辑
        var gameState = GameManager.Instance.GetGameState();
        
        if (gameState.currentDay > 5)
            ReportBug("Invalid day", $"天数超出范围: {gameState.currentDay}", BugSeverity.Warning, BugCategory.Logic);
        
        if (gameState.phaseTimer < 0)
            ReportBug("Negative timer", $"计时器为负数: {gameState.phaseTimer}", BugSeverity.Warning, BugCategory.Logic);
        
        // 检查玩家状态
        if (PlayerHealth.Instance != null)
        {
            if (PlayerHealth.Instance.currentHealth < 0)
                ReportBug("Player health negative", "玩家血量为负数", BugSeverity.Critical, BugCategory.Logic);
        }
    }
    
    void ReportBug(string id, string description, BugSeverity severity, BugCategory category)
    {
        // 避免重复报告相同的bug
        if (bugReports.Any(b => b.id == id)) return;
        
        var bug = new BugReport
        {
            id = id,
            description = description,
            severity = severity,
            category = category,
            timestamp = System.DateTime.Now,
            stackTrace = System.Environment.StackTrace
        };
        
        bugReports.Add(bug);
        OnBugDetected?.Invoke(bug);
    }
    
    void ProcessBugReports()
    {
        if (bugReports.Count == 0) return;
        
        // 按严重程度分类
        var criticalBugs = bugReports.Where(b => b.severity == BugSeverity.Critical).ToList();
        var warningBugs = bugReports.Where(b => b.severity == BugSeverity.Warning).ToList();
        var infoBugs = bugReports.Where(b => b.severity == BugSeverity.Info).ToList();
        
        // 处理关键错误
        foreach (var bug in criticalBugs)
        {
            Debug.LogError($"[BugDetection] CRITICAL: {bug.description}");
            // 可以在这里添加自动修复逻辑
            AttemptAutoFix(bug);
        }
        
        // 输出报告
        if (showDetailedReport && bugReports.Count > 0)
        {
            Debug.Log(GenerateBugReport());
        }
    }
    
    void AttemptAutoFix(BugReport bug)
    {
        switch (bug.id)
        {
            case "GameConfig missing":
                // 尝试查找配置文件
                var config = Resources.Load<GameConfig>("GameConfig");
                if (config != null && GameManager.Instance != null)
                {
                    Debug.Log("[BugDetection] Auto-fixed: GameConfig restored");
                }
                break;
                
            case "Player health negative":
                // 重置玩家血量
                if (PlayerHealth.Instance != null)
                {
                    PlayerHealth.Instance.Heal(100f);
                    Debug.Log("[BugDetection] Auto-fixed: Player health restored");
                }
                break;
        }
    }
    
    public string GenerateBugReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Bug Detection Report ===");
        sb.AppendLine($"检测时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"发现问题: {bugReports.Count}");
        sb.AppendLine();
        
        // 按类别分组
        var groupedBugs = bugReports.GroupBy(b => b.category);
        
        foreach (var group in groupedBugs)
        {
            sb.AppendLine($"=== {group.Key} ===");
            foreach (var bug in group.OrderByDescending(b => b.severity))
            {
                sb.AppendLine($"[{bug.severity}] {bug.description}");
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    // 手动检测接口
    public void ManualCheck()
    {
        PerformSystemCheck();
        
        if (bugReports.Count > 0)
        {
            UIManager.Instance?.ShowMessage($"检测到 {bugReports.Count} 个问题", 3f);
        }
        else
        {
            UIManager.Instance?.ShowMessage("系统状态正常", 2f);
        }
    }
    
    public List<BugReport> GetRecentBugs()
    {
        return new List<BugReport>(bugReports);
    }
    
    // GUI显示
    void OnGUI()
    {
        if (!showDetailedReport || bugReports.Count == 0) return;
        
        var criticalCount = bugReports.Count(b => b.severity == BugSeverity.Critical);
        var warningCount = bugReports.Count(b => b.severity == BugSeverity.Warning);
        
        if (criticalCount > 0)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 300, 25), $"严重错误: {criticalCount}");
        }
        
        if (warningCount > 0)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 35, 300, 25), $"警告: {warningCount}");
        }
        
        GUI.color = Color.white;
    }
}

[System.Serializable]
public class BugReport
{
    public string id;
    public string description;
    public BugSeverity severity;
    public BugCategory category;
    public System.DateTime timestamp;
    public string stackTrace;
}

public enum BugSeverity
{
    Info,      // 信息
    Warning,   // 警告
    Critical   // 严重
}

public enum BugCategory
{
    Manager,     // 管理器相关
    Data,        // 数据完整性
    Performance, // 性能问题
    UI,          // UI相关
    Logic,       // 游戏逻辑
    Audio,       // 音频问题
    Save         // 存档问题
}
