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
    public GameConfig gameConfig;
    
    [Header("当前状态")]
    [SerializeField] private GamePhase currentPhase = GamePhase.MainMenu;
    [SerializeField] private int currentDay = 1;
    [SerializeField] private float phaseTimer = 0f;
    [SerializeField] private bool gameEnded = false;
    
    [Header("ScriptableObject配置")]
    public SceneSettings sceneSettings;
    public InputSettings inputSettings;
    public UITextSettings uiTextSettings;
    public GameValues gameValues;
    public ResourcePaths resourcePaths;
    
    [Header("事件")]
    public GameEvent onPhaseChanged;
    public IntGameEvent onDayChanged;
    public GameEvent onGameEnd;
    public IntGameEvent onDayChangedSO;
    
    private float systemUpdateInterval = 0.5f;
    private float lastSystemUpdate;
    private bool isInitialized = false;
    
    // 销毁顺序：GameManager应该最后销毁
    protected override int DestroyOrder => 1000;
    
    // 属性访问器
    public GamePhase CurrentPhase => currentPhase;
    public int CurrentDay => currentDay;
    public float PhaseTimer => phaseTimer;
    public bool GameEnded => gameEnded;
    public GameConfig Config => gameConfig;
    public bool IsInitialized => isInitialized;
    
    protected override void OnSingletonAwake()
    {
        // 确保音频监听器存在
        EnsureAudioListener();
        
        // 加载默认配置
        LoadDefaultConfigs();
        
        // 等待所有管理器初始化完成
        StartCoroutine(WaitForManagersInitialization());
    }
    
    private void EnsureAudioListener()
    {
        if (FindObjectOfType<AudioListener>() == null)
        {
            // 在主摄像机上添加AudioListener
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // 如果没有主摄像机，创建一个临时的
                GameObject audioObj = new GameObject("AudioListener");
                audioObj.AddComponent<AudioListener>();
                DontDestroyOnLoad(audioObj);
                Debug.Log("[GameManager] Created AudioListener");
            }
            else
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
                Debug.Log("[GameManager] Added AudioListener to main camera");
            }
        }
    }
    
    private void LoadDefaultConfigs()
    {
        // 如果配置为空，尝试从Resources加载
        if (gameConfig == null)
        {
            gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig == null)
            {
                Debug.LogWarning("[GameManager] GameConfig not found in Resources, creating default config");
                CreateDefaultGameConfig();
            }
            else
            {
                Debug.Log("[GameManager] Loaded GameConfig from Resources");
            }
        }
        
        // 加载其他配置
        LoadOtherConfigs();
    }
    
    private void CreateDefaultGameConfig()
    {
        // 创建运行时默认配置
        gameConfig = ScriptableObject.CreateInstance<GameConfig>();
        gameConfig.maxDays = 5;
        gameConfig.explorationTimeLimit = 900f;
        gameConfig.timeWarningThreshold = 150f;
        gameConfig.currentDifficulty = DifficultyLevel.Normal;
        
        Debug.Log("[GameManager] Created default GameConfig");
    }
    
    private void LoadOtherConfigs()
    {
        if (sceneSettings == null)
            sceneSettings = Resources.Load<SceneSettings>("Configs/SceneSettings");
            
        if (inputSettings == null)
            inputSettings = Resources.Load<InputSettings>("Configs/InputSettings");
            
        if (uiTextSettings == null)
            uiTextSettings = Resources.Load<UITextSettings>("Configs/UITextSettings");
            
        if (gameValues == null)
            gameValues = Resources.Load<GameValues>("Configs/GameValues");
            
        if (resourcePaths == null)
            resourcePaths = Resources.Load<ResourcePaths>("Configs/ResourcePaths");
    }

    IEnumerator WaitForManagersInitialization()
    {
        var singletonManager = FindObjectOfType<ImprovedSingletonManager>();
        if (singletonManager != null)
        {
            while (!singletonManager.AreAllManagersInitialized())
            {
                yield return null;
            }
        }
        else
        {
            // 等待几帧让其他管理器初始化
            yield return new WaitForSeconds(1f);
        }
        
        InitializeSystems();
        isInitialized = true;
    }
    
    void Update()
    {
        if (!isInitialized || gameEnded) return;
        
        if (currentPhase == GamePhase.Exploration)
        {
            UpdatePhaseTimer();
        }
        
        if (Time.time - lastSystemUpdate > systemUpdateInterval)
        {
            UpdateSystems();
            lastSystemUpdate = Time.time;
        }
        
#if UNITY_EDITOR
        HandleDebugInput();
#endif
    }
    
    void InitializeSystems()
    {
        if (AudioManager.Instance)
        {
            AudioManager.Instance.SetMusicForGamePhase(currentPhase);
        }
        
        Debug.Log("[GameManager] Systems initialized");
    }
    
    public void StartGame()
    {
        currentDay = 1;
        phaseTimer = 0f;
        gameEnded = false;
        
        ChangePhase(GamePhase.MainMenu);
        Debug.Log("[GameManager] Game started");
    }
    
    public void StartNewGame()
    {
        // 安全地重置所有数据
        if (FamilyManager.HasInstance) FamilyManager.Instance.ResetFamily();
        if (InventoryManager.HasInstance) InventoryManager.Instance.ClearInventory();
        if (GameData.HasInstance) GameData.Instance.ResetData();
        
        ChangePhase(GamePhase.Story);
        LoadScene("1_Home");
        
        Debug.Log("[GameManager] New game started");
    }
    
    public void LoadGame()
    {
        if (SaveManager.HasInstance)
        {
            SaveManager.Instance.LoadGame(0);
        }
    }
    
    public void ChangePhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;
        
        GamePhase previousPhase = currentPhase;
        currentPhase = newPhase;
        phaseTimer = 0f;
        
        HandlePhaseTransition(previousPhase, newPhase);//
        
        if (UIManager.HasInstance)
        {
            UIManager.Instance.OnPhaseChanged(newPhase);
        }
        
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.SetMusicForGamePhase(newPhase);
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
    public void HandleStoryPhase()
    {
        if (StoryManager.Instance)
        {
            StoryManager.Instance.StartStory();
        }
    
        phaseTimer = float.MaxValue; // 故事阶段不限时
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
        phaseTimer = float.MaxValue;
    }
    
    void HandleExplorationPhase()
    {
        float timeLimit = gameValues?.explorationTimeLimit ?? 900f;
        phaseTimer = timeLimit;
        StartCoroutine(ExplorationCountdown());
    }
    
    void HandleEventPhase()
    {
        if (HomeEventManager.HasInstance)
        {
            HomeEventManager.Instance.ProcessDailyEvents();
        }
        
        StartCoroutine(ProcessEventPhaseDelay());
    }
    
    void HandleGameEndPhase()
    {
        gameEnded = true;
        onGameEnd?.Raise();
        
        bool goodEnding = CalculateGoodEnding();
        if (EndGameManager.HasInstance)
        {
            EndGameManager.Instance.ShowEnding(goodEnding);
        }
    }
    
    IEnumerator ExplorationCountdown()
    {
        float warningThreshold = gameValues?.timeWarningThreshold ?? 300f;
        
        while (phaseTimer > 0 && currentPhase == GamePhase.Exploration)
        {
            phaseTimer -= Time.deltaTime;
            
            if (phaseTimer <= warningThreshold && UIManager.HasInstance)
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
        if (FamilyManager.HasInstance)
        {
            FamilyManager.Instance.ProcessDailyNeeds();
        }
    }
    
    bool CalculateGoodEnding()
    {
        if (RadioManager.HasInstance)
        {
            return RadioManager.Instance.GetGoodEndingAchieved();
        }
        return false;
    }
    
    public void StartExploration(int mapIndex)
    {
        string sceneName = sceneSettings?.GetExplorationScene(mapIndex) ?? "";
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[GameManager] Invalid map index: {mapIndex}");
            return;
        }
        
        if (ExplorationManager.HasInstance)
        {
            ExplorationManager.Instance.SetSelectedMap(mapIndex);
        }
        
        ChangePhase(GamePhase.Exploration);
        LoadScene(sceneName);
        
        Debug.Log($"[GameManager] Starting exploration: {sceneName}");
    }
    
    public void ReturnHomeFromExploration(bool isTimeout = false)
    {
        if (isTimeout && InventoryManager.HasInstance)
        {
            // 超时惩罚处理
        }
        
        LoadScene("1_Home");
        ChangePhase(GamePhase.EventProcessing);
    }
    
    public void AdvanceToNextDay()
    {
        currentDay++;
        onDayChanged?.Raise(currentDay);
        onDayChangedSO?.Raise(currentDay);
        
        if (GameData.HasInstance)
        {
            GameData.Instance.SetDayCompleted(currentDay);
        }
        
        if (currentDay > gameConfig.maxDays)
        {
            ChangePhase(GamePhase.GameEnd);
        }
        else
        {
            ChangePhase(GamePhase.Home);
        }
        
        Debug.Log($"[GameManager] Advanced to day {currentDay}");
    }
    
    public void LoadScene(string sceneName)
    {
        if (SceneTransitionManager.HasInstance)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    void UpdatePhaseTimer()
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
    
    void UpdateSystems()
    {
        CheckGameEndConditions();
    }
    
    void CheckGameEndConditions()
    {
        if (gameEnded) return;
        
        if (FamilyManager.HasInstance && FamilyManager.Instance.AliveMembers == 0)
        {
            ChangePhase(GamePhase.GameEnd);
        }
        
        if (PlayerHealth.HasInstance && !PlayerHealth.Instance.IsAlive())
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

        if (evt != null && evt.CanTrigger() && GameEventManager.HasInstance)
        {
            GameEventManager.Instance.TriggerEventExternally(evt);
        }
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
        
        if (Input.GetKeyDown(inputSettings.debugAddResources) && FamilyManager.HasInstance)
        {
            FamilyManager.Instance.DebugAddResources();
        }
        
        if (Input.GetKeyDown(inputSettings.debugFindRadio) && RadioManager.HasInstance)
        {
            RadioManager.Instance.FindRadio();
        }
    }
    
    public void CompleteStoryPhase()
    {
        ChangePhase(GamePhase.Home);
    }
    
    public void StartMapSelection()
    {
        ChangePhase(GamePhase.MapSelection);
    }
    
    public void QuitToMainMenu()
    {
        ChangePhase(GamePhase.MainMenu);
        LoadScene("0_MainMenu");
    }
    
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game");
        
        // 安全保存游戏
        if (SaveManager.HasInstance)
        {
            SaveManager.Instance.SaveGame(0);
        }
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    public GameStateInfo GetGameState()
    {
        return new GameStateInfo
        {
            currentPhase = currentPhase,
            currentDay = currentDay,
            phaseTimer = phaseTimer,
            gameEnded = gameEnded,
            aliveMembers = FamilyManager.HasInstance ? FamilyManager.Instance.AliveMembers : 0,
            totalResources = GetTotalResources()
        };
    }
    
    private int GetTotalResources()
    {
        if (FamilyManager.HasInstance)
        {
            var fm = FamilyManager.Instance;
            return fm.Food + fm.Water + fm.Medicine;
        }
        return 0;
    }
    
    // 应用退出时的清理
    protected override void OnSingletonApplicationQuit()
    {
        gameEnded = true;
        
        // 停止所有协程
        StopAllCoroutines();
        
        // 安全保存（不依赖其他管理器）
        try
        {
            if (SaveManager.HasInstance)
            {
                // 简化保存，避免依赖其他管理器
                PlayerPrefs.SetInt("LastDay", currentDay);
                PlayerPrefs.SetString("LastPhase", currentPhase.ToString());
                PlayerPrefs.Save();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] Error during quit save: {e.Message}");
        }
        
        Debug.Log("[GameManager] Application quit cleanup completed");
    }
    
    protected override void OnSingletonDestroy()
    {
        // 清理事件订阅
        if (onPhaseChanged != null) onPhaseChanged = null;
        if (onDayChanged != null) onDayChanged = null;
        if (onGameEnd != null) onGameEnd = null;
        if (onDayChangedSO != null) onDayChangedSO = null;
        
        base.OnSingletonDestroy();
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
