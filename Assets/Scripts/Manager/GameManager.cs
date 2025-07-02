using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum GamePhase//用来管理游戏的全局状态
{
    MainMenu,
    Story,
    Home,
    MapSelection,
    Exploration,
    EventProcessing,//？
    GameEnd
}



public class GameManager : Singleton<GameManager>
{
    [Header("游戏配置")]
    public GameConfig gameConfig;//游戏的配置
    
    [Header("当前状态")]
    [SerializeField] private GamePhase currentPhase = GamePhase.MainMenu;
    [SerializeField] private int currentDay = 1;
    [SerializeField] private float phaseTimer = 0f;
    [SerializeField] private bool gameEnded = false;
    [Header("ScriptableObject配置")] // 添加到现有字段后
    public SceneSettings sceneSettings;
    public InputSettings inputSettings;
    public UITextSettings uiTextSettings;
    public GameValues gameValues;
    public ResourcePaths resourcePaths;
    [Header("事件")]
    public GameEvent onPhaseChanged;
    public IntGameEvent onDayChanged;
    public GameEvent onGameEnd;
    [Header("事件系统")]
    public IntGameEvent onDayChangedSO;
    
    private float systemUpdateInterval = 0.5f;
    private float lastSystemUpdate;
    // 系统管理器引用
    public UIManager UIManager => UIManager.Instance;
    public AudioManager AudioManager => AudioManager.Instance;
    public SaveManager SaveManager => SaveManager.Instance;
    public FamilyManager FamilyManager => FamilyManager.Instance;
    public InventoryManager InventoryManager => InventoryManager.Instance;
    
    // 属性访问器
    public GamePhase CurrentPhase => currentPhase;
    public int CurrentDay => currentDay;
    public float PhaseTimer => phaseTimer;
    public bool GameEnded => gameEnded;
    public GameConfig Config => gameConfig;
    
    protected override void Awake()
    {
        base.Awake();
    
        // 等待所有管理器初始化完成
        StartCoroutine(WaitForManagersInitialization());
    }

    IEnumerator WaitForManagersInitialization()
    {
        var singletonManager = FindObjectOfType<ImprovedSingletonManager>();
        if (singletonManager != null)
        {
            // 等待所有管理器初始化完成
            while (!singletonManager.AreAllManagersInitialized())
            {
                yield return null;
            }
        }
    
        // 确保配置有效
        if (gameConfig == null)
        {
            Debug.LogError("[GameManager] GameConfig not assigned!");
            enabled = false;
            yield break;
        }
    
        // 初始化系统
        InitializeSystems();
    }
    void Start()
    {
        
        StartGame();
    }
    
    void Update()
    {
        if (gameEnded) return;
        
        // 只在探索阶段才每帧更新时间
        if (currentPhase == GamePhase.Exploration)
        {
            UpdatePhaseTimer();
        }
        
        // 降低系统检查频率
        if (Time.time - lastSystemUpdate > systemUpdateInterval)
        {
            UpdateSystems();
            lastSystemUpdate = Time.time;
        }
        
#if UNITY_EDITOR
        HandleDebugInput(); // 只在编辑器模式下执行
#endif
    }
    
    void InitializeSystems()
    {
        // 初始化音频系统
        if (AudioManager)
        {
            AudioManager.SetMusicForGamePhase(currentPhase);
        }
        
        Debug.Log("[GameManager] Systems initialized");
    }
    
    public void StartGame()//开始游戏
    {
        // 重置游戏状态
        currentDay = 1;
        phaseTimer = 0f;
        gameEnded = false;
        
        // 切换到主菜单
        ChangePhase(GamePhase.MainMenu);
        
        Debug.Log("[GameManager] Game started");
    }
    
    public void StartNewGame()//开始新的存档
    {
        // 重置所有数据
        if (FamilyManager) FamilyManager.ResetFamily();
        if (InventoryManager) InventoryManager.ClearInventory();
        if (GameData.Instance) GameData.Instance.ResetData();
        
        // 开始故事选择
        ChangePhase(GamePhase.Story);
        LoadScene("1_Home");
        
        Debug.Log("[GameManager] New game started");
    }
    
    public void LoadGame()//读取
    {
        if (SaveManager)
        {
            SaveManager.LoadGame(0); // 默认存档槽
        }
    }
    
    public void ChangePhase(GamePhase newPhase)//切换状态
    {
        if (currentPhase == newPhase) return;
        
        GamePhase previousPhase = currentPhase;
        currentPhase = newPhase;
        phaseTimer = 0f;
        
        // 执行阶段切换逻辑
        HandlePhaseTransition(previousPhase, newPhase);
        
        // 通知UI系统
        if (UIManager)
        {
            UIManager.OnPhaseChanged(newPhase);
        }
        
        // 更新音乐
        if (AudioManager)
        {
            AudioManager.SetMusicForGamePhase(newPhase);
        }
        
        onPhaseChanged?.Raise();
        
        Debug.Log($"[GameManager] Phase: {previousPhase} → {newPhase}");
    }
    
    void HandlePhaseTransition(GamePhase from, GamePhase to)
    {
        switch (to)
        {
            case GamePhase.MainMenu:
                HandleMainMenuPhase();
                break;
                
            case GamePhase.Story:
                HandleStoryPhase();
                break;
                
            case GamePhase.Home:
                HandleHomePhase();
                break;
                
            case GamePhase.MapSelection:
                HandleMapSelectionPhase();
                break;
                
            case GamePhase.Exploration:
                HandleExplorationPhase();
                break;
                
            case GamePhase.EventProcessing:
                HandleEventPhase();
                break;
                
            case GamePhase.GameEnd:
                HandleGameEndPhase();
                break;
        }
    }
    
    void HandleMainMenuPhase()
    {
        string sceneName = sceneSettings?.GetSceneName(GamePhase.MainMenu) ?? "0_MainMenu";
        LoadScene(sceneName);
        Time.timeScale = 1f;
    }
    
    void HandleStoryPhase()//故事这里还没做
    {
        // 显示开局故事选择
        phaseTimer = float.MaxValue; // 等待玩家选择
    }
    
    void HandleHomePhase()
    {
        string sceneName = sceneSettings?.GetSceneName(GamePhase.Home) ?? "1_Home";
        LoadScene(sceneName);
        if (currentDay > 1)
        {
            ProcessDailyNeeds();
        }
        phaseTimer = float.MaxValue;
    }
    
    void HandleMapSelectionPhase()
    {
        phaseTimer = float.MaxValue; // 等待玩家选择
    }
    
    void HandleExplorationPhase()
    {
        float timeLimit = gameValues?.explorationTimeLimit ?? 900f;//TODO:xiugai 
        phaseTimer = timeLimit;
        StartCoroutine(ExplorationCountdown());
    }
    
    void HandleEventPhase()
    {
        // 处理随机事件
        if (HomeEventManager.Instance)
        {
            HomeEventManager.Instance.ProcessDailyEvents();
        }
        
        // 短暂延迟后推进到下一天
        StartCoroutine(ProcessEventPhaseDelay());
    }
    
    void HandleGameEndPhase()
    {
        gameEnded = true;
        onGameEnd?.Raise();
        
        // 计算结局
        bool goodEnding = CalculateGoodEnding();
        if (EndGameManager.Instance)
        {
            EndGameManager.Instance.ShowEnding(goodEnding);
        }
    }
    public void StartExploration(int mapIndex)
    {
        string sceneName = sceneSettings?.GetExplorationScene(mapIndex) ?? "";
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[GameManager] Invalid map index: {mapIndex}");
            return;
        }
        
        if (ExplorationManager.Instance)
        {
            ExplorationManager.Instance.SetSelectedMap(mapIndex);
        }
        
        ChangePhase(GamePhase.Exploration);
        LoadScene(sceneName);
        
        Debug.Log($"[GameManager] Starting exploration: {sceneName}");
    }
    
    // 修改现有ExplorationCountdown方法
    IEnumerator ExplorationCountdown()
    {
        float warningThreshold = gameValues?.timeWarningThreshold ?? 300f;
        
        while (phaseTimer > 0 && currentPhase == GamePhase.Exploration)
        {
            phaseTimer -= Time.deltaTime;
            
            if (phaseTimer <= warningThreshold && UIManager.Instance)
            {
                UIManager.Instance.ShowTimeWarning();
            }
            
            yield return null;
        }
        
        if (currentPhase == GamePhase.Exploration)
        {
            ReturnHomeFromExploration(true);
        }
    }
   
    
    IEnumerator ProcessEventPhaseDelay()
    {
        yield return new WaitForSeconds(2f);
        AdvanceToNextDay();
    }
    
    void ProcessDailyNeeds()
    {
        if (FamilyManager)
        {
            FamilyManager.ProcessDailyNeeds();
        }
    }
    
    bool CalculateGoodEnding()
    {
        if (RadioManager.Instance)
        {
            return RadioManager.Instance.GetGoodEndingAchieved();
        }
        return false;
    }
    
    /*public void StartExploration(int mapIndex)
    {
        if (mapIndex < 0 || mapIndex >= sceneNames.Length - 2)
        {
            Debug.LogError($"[GameManager] Invalid map index: {mapIndex}");
            return;
        }
        
        string sceneName = sceneNames[mapIndex + 2]; // +2 跳过主菜单和家
        
        // 设置选择的地图
        if (ExplorationManager.Instance)
        {
            ExplorationManager.Instance.SetSelectedMap(mapIndex);
        }
        
        // 切换阶段和场景
        ChangePhase(GamePhase.Exploration);
        LoadScene(sceneName);
        
        Debug.Log($"[GameManager] Starting exploration: {sceneName}");
    }*/
    
    public void ReturnHomeFromExploration(bool isTimeout = false)
    {
        // 应用返回逻辑
        if (isTimeout && InventoryManager)
        {
            // 超时惩罚：丢失部分物品
            //InventoryManager.ApplyOvertimeLoss(0.3f);
        }
        
        // 切换回家庭阶段
        LoadScene("1_Home");
        ChangePhase(GamePhase.EventProcessing);
    }
    
    public void AdvanceToNextDay()
    {
        currentDay++;
        onDayChanged?.Raise(currentDay);
        
        // 记录统计
        if (GameData.Instance)
        {
            //GameData.Instance.SetDayCompleted(currentDay);
        }
        
        // 检查游戏结束
        if (currentDay > gameConfig.maxDays)
        {
            ChangePhase(GamePhase.GameEnd);
        }
        else
        {
            ChangePhase(GamePhase.Home);
        }
        onDayChangedSO?.Raise(currentDay);
        Debug.Log($"[GameManager] Advanced to day {currentDay}");
    }
    
    public void LoadScene(string sceneName)//加载
    {
        if (SceneTransitionManager.Instance)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    void UpdatePhaseTimer()//放在exploration
    {
        if (phaseTimer == float.MaxValue) return;
        
        phaseTimer -= Time.deltaTime;
        
        if (phaseTimer <= 0)
        {
            HandlePhaseTimeout();
        }
    }
    
    void HandlePhaseTimeout()
    {
        switch (currentPhase)
        {
            case GamePhase.Exploration:
                ReturnHomeFromExploration(true);
                break;
        }
    }
    
    void UpdateSystems()//杂糅
    {
        // 检查游戏结束条件
        CheckGameEndConditions();
    }
    
    void CheckGameEndConditions()
    {
        if (gameEnded) return;
        
        // 检查家庭成员全部死亡
        if (FamilyManager && FamilyManager.AliveMembers == 0)
        {
            ChangePhase(GamePhase.GameEnd);
        }
        
        // 检查玩家死亡
        if (PlayerHealth.Instance && !PlayerHealth.Instance.IsAlive())
        {
            ChangePhase(GamePhase.GameEnd);
        }
    }
    public void ScheduleEvent(RandomEvent evt, float delayDays)
    {
        StartCoroutine(ScheduleEventCoroutine(evt, delayDays));
    }

    private IEnumerator ScheduleEventCoroutine(RandomEvent evt, float delayDays)
    {
        int targetDay = CurrentDay + Mathf.RoundToInt(delayDays);
        yield return new WaitUntil(() => CurrentDay >= targetDay);

        if (evt != null && evt.CanTrigger())
        {
            GameEventManager.Instance?.TriggerEventExternally(evt); // 调用外部暴露接口
        }
    }

    private float GetPhaseTimeLimit(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Exploration => gameConfig.explorationTimeLimit,
            _ => float.MaxValue
        };
    }
    void HandleDebugInput()
    {
        if (!Debug.isDebugBuild || inputSettings == null) return;
        
        if (Input.GetKeyDown(inputSettings.debugNextDay))
        {
            AdvanceToNextDay();
        }
        
        if (Input.GetKeyDown(inputSettings.debugEndGame))
        {
            ChangePhase(GamePhase.GameEnd);
        }
        
        if (Input.GetKeyDown(inputSettings.debugAddResources) && FamilyManager.Instance)
        {
            FamilyManager.Instance.DebugAddResources();
        }
        
        if (Input.GetKeyDown(inputSettings.debugFindRadio) && RadioManager.Instance)
        {
            RadioManager.Instance.FindRadio();
        }
    }
    
    // 公共接口方法
    public void CompleteStoryPhase()//故事之后
    {
        ChangePhase(GamePhase.Home);
    }
    
    public void StartMapSelection()//地图选择
    {
        ChangePhase(GamePhase.MapSelection);
    }
    
    public void QuitToMainMenu()//返回菜单
    {
        ChangePhase(GamePhase.MainMenu);
        LoadScene("0_MainMenu");
    }
    
    public void QuitGame()//退游戏
    {
        Debug.Log("[GameManager] Quitting game");
        
        // 保存游戏
        if (SaveManager)
        {
            SaveManager.SaveGame(0);
        }
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    // 获取游戏状态信息
    public GameStateInfo GetGameState()
    {
        return new GameStateInfo
        {
            currentPhase = currentPhase,
            currentDay = currentDay,
            phaseTimer = phaseTimer,
            gameEnded = gameEnded,
            aliveMembers = FamilyManager?.AliveMembers ?? 0,
            totalResources = FamilyManager ? FamilyManager.Food + FamilyManager.Water + FamilyManager.Medicine : 0
        };
    }
    
    protected override void OnDestroy()
    {
        // 保存游戏状态
        if (SaveManager)
        {
            SaveManager.SaveGame(0);
        }
        
        base.OnDestroy();
    }
}

[System.Serializable]
public class GameStateInfo
{
    public GamePhase currentPhase;
    public int currentDay;
    public float phaseTimer;
    public bool gameEnded;
    public int aliveMembers;
    public int totalResources;
}
