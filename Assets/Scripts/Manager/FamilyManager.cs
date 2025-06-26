using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class FamilyMember
{
    [Header("基本信息")]
    public string name;
    public CharacterRole role;
    
    [Header("状态值")]
    [Range(0, 100)] public float health = 100f;
    [Range(0, 100)] public float hunger = 100f;
    [Range(0, 100)] public float thirst = 100f;
    [Range(0, 100)] public float mood = 100f;
    
    [Header("疾病状态")]
    public bool isSick = false;
    public int sickDaysLeft = 0;
    public string illnessType = "";
    
    [Header("特殊状态")]
    public bool isInjured = false;
    public List<string> statusEffects = new();
    
    // 状态检查属性
    public bool IsAlive => health > 0;
    public bool IsHealthy => health > 70f && hunger > 50f && thirst > 50f && !isSick && !isInjured;
    public bool IsInDanger => health < 30f || hunger < 20f || thirst < 20f;
    public bool NeedsAttention => IsInDanger || isSick || isInjured;
    
    public void ProcessDailyNeeds(GameConfiguration config)
    {
        // 每日基础消耗
        hunger -= Random.Range(18f, 25f); // 轻微随机化
        thirst -= Random.Range(20f, 30f);
        
        // 资源不足的惩罚
        if (hunger <= 0)
        {
            health -= config.hungerDamageRate;
            hunger = 0f;
            mood -= 15f;
        }
        
        if (thirst <= 0)
        {
            health -= config.thirstDamageRate;
            thirst = 0f;
            mood -= 20f;
        }
        
        // 生病处理
        if (isSick)
        {
            health -= Random.Range(8f, 15f);
            mood -= 10f;
            sickDaysLeft--;
            
            if (sickDaysLeft <= 0)
            {
                isSick = false;
                illnessType = "";
                mood += 20f; // 康复带来的心情提升
            }
        }
        
        // 受伤处理
        if (isInjured)
        {
            health -= 5f;
            mood -= 5f;
        }
        
        // 心情影响健康
        if (mood < 30f)
        {
            health -= 3f; // 抑郁影响健康
        }
        else if (mood > 80f)
        {
            health += 2f; // 好心情促进恢复
        }
        
        // 随机生病 (基础概率 + 健康状态影响)
        float sicknessChance = config.sicknessProbability;
        if (health < 50f) sicknessChance *= 2f;
        if (hunger < 30f || thirst < 30f) sicknessChance *= 1.5f;
        
        if (!isSick && Random.Range(0f, 1f) < sicknessChance)
        {
            GetSick();
        }
        
        // 限制数值范围
        health = Mathf.Clamp(health, 0f, 100f);
        hunger = Mathf.Clamp(hunger, 0f, 100f);
        thirst = Mathf.Clamp(thirst, 0f, 100f);
        mood = Mathf.Clamp(mood, 0f, 100f);
    }
    
    public void GetSick()
    {
        isSick = true;
        sickDaysLeft = Random.Range(2, 4);
        illnessType = GetRandomIllness();
        mood -= 25f;
        
        // 记录到日志
        JournalManager.Instance?.AddEntry($"{name}生病了", 
            $"{name}患上了{illnessType}，需要药物治疗。");
    }
    
    string GetRandomIllness()
    {
        string[] illnesses = { "感冒", "发烧", "腹泻", "头痛", "疲劳综合症" };
        return illnesses[Random.Range(0, illnesses.Length)];
    }
    
    public void Heal(float amount)
    {
        health = Mathf.Min(100f, health + amount);
        mood += amount * 0.3f; // 治疗提升心情
    }
    
    public void Feed(float amount)
    {
        hunger = Mathf.Min(100f, hunger + amount);
        mood += amount * 0.2f;
    }
    
    public void GiveWater(float amount)
    {
        thirst = Mathf.Min(100f, thirst + amount);
        mood += amount * 0.2f;
    }
    
    public void CureSickness()
    {
        isSick = false;
        sickDaysLeft = 0;
        illnessType = "";
        mood += 30f; // 康复的喜悦
    }
    
    public void TreatInjury()
    {
        isInjured = false;
        mood += 15f;
    }
    
    public string GetStatusDescription()
    {
        List<string> status = new();
        
        if (health < 30f) status.Add("危重");
        else if (health < 60f) status.Add("虚弱");
        
        if (hunger < 30f) status.Add("饥饿");
        if (thirst < 30f) status.Add("口渴");
        if (mood < 30f) status.Add("沮丧");
        
        if (isSick) status.Add($"患{illnessType}");
        if (isInjured) status.Add("受伤");
        
        return status.Count > 0 ? string.Join(", ", status) : "状态良好";
    }
}

public enum CharacterRole
{
    Father,   // 父亲 - 唯一可以外出探索
    Mother,   // 母亲 
    Child     // 孩子
}

public class FamilyManager : Singleton<FamilyManager>
{
    [Header("家庭成员")]
    [SerializeField] private List<FamilyMember> familyMembers = new();
    
    [Header("资源")]
    [SerializeField] private int food = 15;
    [SerializeField] private int water = 15;
    [SerializeField] private int medicine = 2;
    
    [Header("事件")]
    public GameEvent onResourceChanged;
    public GameEvent onMemberStatusChanged;
    public GameEvent onFamilyDeath;
    
    // 属性访问器
    public List<FamilyMember> FamilyMembers => familyMembers;
    public int Food => food;
    public int Water => water;
    public int Medicine => medicine;
    public int AliveMembers => familyMembers.Count(m => m.IsAlive);
    
    protected override void Awake()
    {
        base.Awake();
        InitializeFamily();
    }
    
    void Start()
    {
        SubscribeToEvents();
    }
    
    void SubscribeToEvents()
    {
        if (GameStateManager.Instance)
        {
            GameStateManager.Instance.onDayChanged.RegisterListener(
                GetComponent<IntGameEventListener>());
        }
    }
    
    void InitializeFamily()
    {
        if (familyMembers.Count == 0)
        {
            familyMembers = new List<FamilyMember>
            {
                new FamilyMember 
                { 
                    name = "约翰", 
                    role = CharacterRole.Father,
                    health = 100f,
                    hunger = 100f,
                    thirst = 100f,
                    mood = 80f
                },
                new FamilyMember 
                { 
                    name = "玛丽", 
                    role = CharacterRole.Mother,
                    health = 100f,
                    hunger = 100f,
                    thirst = 100f,
                    mood = 75f
                },
                new FamilyMember 
                { 
                    name = "小汤姆", 
                    role = CharacterRole.Child,
                    health = 100f,
                    hunger = 100f,
                    thirst = 100f,
                    mood = 90f
                }
            };
        }
        
        // 应用初始资源配置
        if (GameStateManager.Instance?.config)
        {
            var config = GameStateManager.Instance.config;
            food = 15;  // 固定初始值，可调整
            water = 15;
            medicine = 2;
        }
    }
    
    public void ProcessDailyNeeds()
    {
        var config = GameStateManager.Instance.config;
        
        // 消耗每日资源
        ConsumeResources(config.dailyFoodConsumption, config.dailyWaterConsumption);
        
        // 处理家庭成员日常需求
        foreach (var member in familyMembers)
        {
            member.ProcessDailyNeeds(config);
            onMemberStatusChanged?.Raise();
        }
        
        // 检查死亡情况
        CheckForDeaths();
        
        onResourceChanged?.Raise();
        
        // 记录到日志
        RecordDailyStatus();
    }
    
    void ConsumeResources(int foodConsumption, int waterConsumption)
    {
        int aliveMemberCount = AliveMembers;
        
        food = Mathf.Max(0, food - foodConsumption);
        water = Mathf.Max(0, water - waterConsumption);
        
        // 如果资源不足，影响所有活着的成员
        if (food == 0)
        {
            foreach (var member in familyMembers.Where(m => m.IsAlive))
            {
                member.hunger = Mathf.Max(0, member.hunger - 20f);
                member.mood -= 10f;
            }
        }
        
        if (water == 0)
        {
            foreach (var member in familyMembers.Where(m => m.IsAlive))
            {
                member.thirst = Mathf.Max(0, member.thirst - 25f);
                member.mood -= 15f;
            }
        }
    }
    
    void CheckForDeaths()
    {
        bool someoneDied = false;
        
        foreach (var member in familyMembers)
        {
            if (member.IsAlive && member.health <= 0)
            {
                member.health = 0;
                someoneDied = true;
                
                // 记录死亡
                JournalManager.Instance?.AddEntry($"{member.name}去世了", 
                    $"我们失去了{member.name}。这对家庭来说是巨大的打击。");
                
                // 影响其他成员心情
                foreach (var otherMember in familyMembers)
                {
                    if (otherMember != member && otherMember.IsAlive)
                    {
                        otherMember.mood -= 40f; // 失去家人的痛苦
                    }
                }
            }
        }
        
        if (someoneDied)
        {
            onFamilyDeath?.Raise();
        }
    }
    
    void RecordDailyStatus()
    {
        int day = GameStateManager.Instance.CurrentDay;
        string statusReport = $"第{day}天结束。";
        
        // 资源状况
        statusReport += $" 剩余食物: {food}, 水: {water}, 药品: {medicine}。";
        
        // 家庭状况
        var concernedMembers = familyMembers.Where(m => m.IsAlive && m.NeedsAttention).ToList();
        if (concernedMembers.Any())
        {
            statusReport += " 需要关注: ";
            statusReport += string.Join(", ", concernedMembers.Select(m => 
                $"{m.name}({m.GetStatusDescription()})"));
        }
        else
        {
            statusReport += " 家人状况良好。";
        }
        
        JournalManager.Instance?.AddEntry($"第{day}天总结", statusReport);
    }
    
    // 资源操作方法
    public bool UseFood(int amount = 1)
    {
        if (food >= amount)
        {
            food -= amount;
            onResourceChanged?.Raise();
            return true;
        }
        return false;
    }
    
    public bool UseWater(int amount = 1)
    {
        if (water >= amount)
        {
            water -= amount;
            onResourceChanged?.Raise();
            return true;
        }
        return false;
    }
    
    public bool UseMedicine(int amount = 1)
    {
        if (medicine >= amount)
        {
            medicine -= amount;
            onResourceChanged?.Raise();
            return true;
        }
        return false;
    }
    
    public void AddResource(string resourceType, int amount)
    {
        switch (resourceType.ToLower())
        {
            case "food":
                food += amount;
                break;
            case "water":
                water += amount;
                break;
            case "medicine":
                medicine += amount;
                break;
        }
        onResourceChanged?.Raise();
        
        // 记录资源获得
        if (amount > 0)
        {
            JournalManager.Instance?.AddEntry("资源获得", 
                $"获得了{amount}个{GetResourceDisplayName(resourceType)}");
        }
    }
    
    string GetResourceDisplayName(string resourceType)
    {
        return resourceType.ToLower() switch
        {
            "food" => "食物",
            "water" => "水",
            "medicine" => "药品",
            _ => resourceType
        };
    }
    
    // 照顾家庭成员的方法
    public void FeedMember(int memberIndex)
    {
        if (memberIndex >= 0 && memberIndex < familyMembers.Count && UseFood())
        {
            familyMembers[memberIndex].Feed(30f);
            onMemberStatusChanged?.Raise();
            
            JournalManager.Instance?.AddEntry("照顾家人", 
                $"给{familyMembers[memberIndex].name}喂食。");
        }
    }
    
    public void GiveWaterToMember(int memberIndex)
    {
        if (memberIndex >= 0 && memberIndex < familyMembers.Count && UseWater())
        {
            familyMembers[memberIndex].GiveWater(35f);
            onMemberStatusChanged?.Raise();
            
            JournalManager.Instance?.AddEntry("照顾家人", 
                $"给{familyMembers[memberIndex].name}喝水。");
        }
    }
    
    public void HealMember(int memberIndex)
    {
        if (memberIndex >= 0 && memberIndex < familyMembers.Count && UseMedicine())
        {
            var member = familyMembers[memberIndex];
            member.Heal(50f);
            
            if (member.isSick)
                member.CureSickness();
            
            if (member.isInjured)
                member.TreatInjury();
            
            onMemberStatusChanged?.Raise();
            
            JournalManager.Instance?.AddEntry("治疗家人", 
                $"用药品治疗了{member.name}。");
        }
    }
    
    // 获取家庭整体状况
    public FamilyStatus GetOverallStatus()
    {
        if (AliveMembers == 0) return FamilyStatus.AllDead;
        
        bool hasUrgentNeeds = familyMembers.Any(m => m.IsAlive && m.IsInDanger);
        if (hasUrgentNeeds) return FamilyStatus.Critical;
        
        bool hasMinorIssues = familyMembers.Any(m => m.IsAlive && !m.IsHealthy);
        if (hasMinorIssues) return FamilyStatus.Concerning;
        
        if (food < 5 || water < 5) return FamilyStatus.ResourceShortage;
        
        return FamilyStatus.Stable;
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    public void DebugMakeAllSick()
    {
        foreach (var member in familyMembers)
        {
            if (member.IsAlive && !member.isSick)
            {
                member.GetSick();
            }
        }
        onMemberStatusChanged?.Raise();
        Debug.Log("[FamilyManager] All family members made sick for debugging");
    }

    public void DebugAddResources()
    {
        AddResource("food", 5);
        AddResource("water", 5);
        AddResource("medicine", 3);
        Debug.Log("[FamilyManager] Added debug resources: +5 food, +5 water, +3 medicine");
    }

    public void DebugHealAll()
    {
        foreach (var member in familyMembers)
        {
            if (member.IsAlive)
            {
                member.Heal(50f);
                member.Feed(50f);
                member.GiveWater(50f);
                if (member.isSick) member.CureSickness();
                if (member.isInjured) member.TreatInjury();
            }
        }
        onMemberStatusChanged?.Raise();
        Debug.Log("[FamilyManager] All family members healed for debugging");
    }
}

public enum FamilyStatus
{
    Stable,           // 状况良好
    Concerning,       // 有些担忧
    ResourceShortage, // 资源短缺
    Critical,         // 危急状况
    AllDead          // 全部死亡
}
