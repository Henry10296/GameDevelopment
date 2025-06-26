using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : Singleton<UIManager>
{
    [Header("UI界面面板")]
    public MainMenuUI mainMenuUI;
    public HomeUI homeUI;
    public ExplorationUI explorationUI;
    public InventoryUI inventoryUI;
    public EventChoiceUI eventChoiceUI;
    public JournalUI journalUI;
    public SettingsUI settingsUI;
    public PauseMenuUI pauseMenuUI;
    
    
    [Header("通用UI组件")]
    public MessageDisplay messageDisplay;
    public LoadingScreen loadingScreen;
    public CanvasGroup fadeOverlay;
    
    [Header("设置")]
    public float fadeSpeed = 2f;
    public float messageDuration = 3f;
    
    // 状态管理
    private GamePhase currentPhase = GamePhase.MainMenu;
    private Dictionary<GamePhase, IUIPanel> uiPanels = new Dictionary<GamePhase, IUIPanel>();
    private bool inventoryOpen = false;
    private bool pauseMenuOpen = false;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeUIPanels();
    }
    
    void Start()
    {
        // 初始化UI状态
        HideAllPanels();
        OnPhaseChanged(GamePhase.MainMenu);
        
        // 订阅输入事件
        SubscribeToInputs();
    }
    
    void Update()
    {
        HandleInput();
        UpdateDynamicUI();
    }
    
    void InitializeUIPanels()
    {
        // 初始化UI面板字典
        if (mainMenuUI) uiPanels[GamePhase.MainMenu] = mainMenuUI;
        if (homeUI) uiPanels[GamePhase.Home] = homeUI;
        if (explorationUI) uiPanels[GamePhase.Exploration] = explorationUI;
        
        // 初始化各个UI面板
        InitializeAllPanels();
    }
    
    void InitializeAllPanels()
    {
        mainMenuUI?.Initialize();
        homeUI?.Initialize();
        explorationUI?.Initialize();
        inventoryUI?.Initialize();
        eventChoiceUI?.Initialize();
        journalUI?.Initialize();
        settingsUI?.Initialize();
        pauseMenuUI?.Initialize();
        
        messageDisplay?.Initialize();
    }
    
    void SubscribeToInputs()
    {
        // 可以订阅输入管理器的事件
    }
    
    void HandleInput()
    {
        // Tab键切换背包
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
        
        // ESC键暂停菜单
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
        
        // J键日志
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleJournal();
        }
        
        // I键背包
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }
    
    void UpdateDynamicUI()
    {
        // 更新当前阶段的UI
        if (uiPanels.ContainsKey(currentPhase))
        {
            uiPanels[currentPhase].UpdateUI();
        }
        
        // 更新通用UI元素
        UpdateCommonUI();
    }
    
    void UpdateCommonUI()
    {
        // 更新生命值显示
        if (explorationUI && PlayerHealth.Instance && currentPhase == GamePhase.Exploration)
        {
            explorationUI.UpdateHealth(PlayerHealth.Instance.currentHealth, PlayerHealth.Instance.maxHealth);
        }
        
        // 更新时间显示
        if (GameManager.Instance)
        {
            UpdateTimeDisplay();
        }
    }
    
    void UpdateTimeDisplay()
    {
        if (currentPhase == GamePhase.Exploration && explorationUI)
        {
            float remainingTime = GameManager.Instance.PhaseTimer;
            explorationUI.UpdateTimeDisplay(remainingTime);
        }
    }
    
    public void OnPhaseChanged(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;
        
        // 隐藏当前UI
        HideCurrentPanel();
        
        // 更新当前阶段
        currentPhase = newPhase;
        
        // 显示新阶段UI
        ShowCurrentPanel();
        
        // 设置鼠标状态
        SetCursorState(GetCursorVisibilityForPhase(newPhase));
        
        Debug.Log($"[UIManager] UI phase changed to: {newPhase}");
    }
    
    void HideAllPanels()
    {
        foreach (var panel in uiPanels.Values)
        {
            panel?.Hide();
        }
        
        // 隐藏弹出式UI
        inventoryUI?.Hide();
        eventChoiceUI?.Hide();
        journalUI?.Hide();
        settingsUI?.Hide();
        pauseMenuUI?.Hide();
    }
    
    void HideCurrentPanel()
    {
        if (uiPanels.ContainsKey(currentPhase))
        {
            uiPanels[currentPhase]?.Hide();
        }
    }
    
    void ShowCurrentPanel()
    {
        if (uiPanels.ContainsKey(currentPhase))
        {
            uiPanels[currentPhase]?.Show();
        }
    }
    
    bool GetCursorVisibilityForPhase(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.MainMenu => true,
            GamePhase.Story => true,
            GamePhase.Home => true,
            GamePhase.MapSelection => true,
            GamePhase.Exploration => false,
            GamePhase.EventProcessing => true,
            GamePhase.GameEnd => true,
            _ => true
        };
    }
    
    void SetCursorState(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }
    
    // UI切换方法
    public void ToggleInventory()
    {
        inventoryOpen = !inventoryOpen;
        
        if (inventoryOpen)
        {
            inventoryUI?.Show();
            PauseGame();
        }
        else
        {
            inventoryUI?.Hide();
            ResumeGame();
        }
    }
    
    public void TogglePauseMenu()
    {
        pauseMenuOpen = !pauseMenuOpen;
        
        if (pauseMenuOpen)
        {
            pauseMenuUI?.Show();
            PauseGame();
        }
        else
        {
            pauseMenuUI?.Hide();
            ResumeGame();
        }
    }
    
    public void ToggleJournal()
    {
        bool isJournalOpen = journalUI?.IsVisible() ?? false;
        
        if (isJournalOpen)
        {
            journalUI?.Hide();
        }
        else
        {
            journalUI?.Show();
            journalUI?.RefreshEntries();
        }
    }
    
    public void ShowEventChoice(RandomEvent eventData)
    {
        eventChoiceUI?.ShowEvent(eventData);
        PauseGame();
    }
    
    public void HideEventChoice()
    {
        eventChoiceUI?.Hide();
        ResumeGame();
    }
    
    void PauseGame()
    {
        Time.timeScale = 0f;
        SetCursorState(true);
    }
    
    void ResumeGame()
    {
        Time.timeScale = 1f;
        SetCursorState(GetCursorVisibilityForPhase(currentPhase));
    }
    
    // 消息系统
    public void ShowMessage(string message, float duration = 0f)
    {
        float showDuration = duration > 0f ? duration : messageDuration;
        messageDisplay?.ShowMessage(message, showDuration);
    }
    
    public void ShowTimeWarning()
    {
        ShowMessage("时间不多了！赶紧回家！", 5f);
        
        if (explorationUI)
        {
            explorationUI.ShowTimeWarning();
        }
    }
    
    // 加载屏幕
    public void ShowLoadingScreen()
    {
        loadingScreen?.Show();
    }
    
    public void HideLoadingScreen()
    {
        loadingScreen?.Hide();
    }
    
    // 淡入淡出效果
    public void FadeIn(float duration = 1f)
    {
        if (fadeOverlay)
        {
            StartCoroutine(FadeCoroutine(1f, 0f, duration));
        }
    }
    
    public void FadeOut(float duration = 1f)
    {
        if (fadeOverlay)
        {
            StartCoroutine(FadeCoroutine(0f, 1f, duration));
        }
    }
    
    IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadeOverlay.alpha = alpha;
            yield return null;
        }
        
        fadeOverlay.alpha = endAlpha;
    }
    
    // 交互提示
    public void ShowInteractionPrompt(string text)
    {
        if (explorationUI)
        {
            explorationUI.ShowInteractionPrompt(text);
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (explorationUI)
        {
            explorationUI.HideInteractionPrompt();
        }
    }
    
    // UI数据更新方法
    public void UpdateAmmoDisplay(int current, int max)
    {
        if (explorationUI)
        {
            explorationUI.UpdateAmmo(current, max);
        }
    }
    
    public void UpdateHealthDisplay(float current, float max)
    {
        if (explorationUI)
        {
            explorationUI.UpdateHealth(current, max);
        }
    }
    
    public void RefreshHomeUI()
    {
        if (homeUI)
        {
            homeUI.RefreshAllData();
        }
    }
    
    public void RefreshInventoryUI()
    {
        if (inventoryUI)
        {
            inventoryUI.RefreshInventory();
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}