using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager>
{
    [Header("UI界面面板")]
    public HomeUI homeUI;
    public ExplorationUI explorationUI;
    public InventoryUI inventoryUI;
    public EventChoiceUI eventChoiceUI;
    public JournalUI journalUI;
    public SettingsUI settingsUI;
    public PauseMenuUI pauseMenuUI;
    
    [Header("游戏内HUD")]
    public PlayerUI playerHUD;
    
    [Header("通用UI组件")]
    public MessageDisplay messageDisplay;
    public LoadingScreen loadingScreen;
    public CanvasGroup fadeOverlay;
    
    [Header("设置")]
    public float fadeSpeed = 2f;
    public float messageDuration = 3f;
    
    // 状态管理
    private GamePhase currentPhase;
    private Dictionary<GamePhase, IUIPanel> uiPanels = new Dictionary<GamePhase, IUIPanel>();
    private bool inventoryOpen = false;
    private bool pauseMenuOpen = false;
    
    [Header("自动更新设置")] 
    public bool enableAutoUpdate = true;
    public float updateInterval = 0.1f;
    
    [Header("文本配置")]
    public UITextSettings textSettings;
    public GameValues gameValues;
    public InputSettings inputSettings;
    
    
    
    private bool gameIsPaused = false;
    private float previousTimeScale = 1f;
    protected override void OnSingletonApplicationQuit()
    {
        StopAllCoroutines();
        if (enableAutoUpdate)
            CancelInvoke(nameof(AutoUpdateUI));
        Debug.Log("[UIManager] Application quit cleanup completed");
    }
    
    protected override void Awake()
    {
        base.Awake();
        InitializeUIPanels();
    }
    
    void Start()
    {
        HideAllPanels();
        OnPhaseChanged(GamePhase.MainMenu);
        SubscribeToInputs();
        
        if (enableAutoUpdate)
        {
            InvokeRepeating(nameof(AutoUpdateUI), 0f, updateInterval);
        }
        
        InitializePlayerHUD();
    }
    
    void InitializePlayerHUD()
    {
        if (playerHUD == null)
        {
            playerHUD = FindObjectOfType<PlayerUI>();
        }
        
        if (playerHUD != null)
        {
            // 安全初始化PlayerUI
            try
            {
                playerHUD.Initialize();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIManager] PlayerUI initialization failed: {e.Message}");
            }
        }
    }
    
    void Update()
    {
        HandleInput();
        UpdateDynamicUI();
    }
    
    private void AutoUpdateUI()
    {
        try
        {
            if (currentPhase == GamePhase.Home && homeUI != null)
            {
                // 安全调用RefreshAllData
                if (HasMethod(homeUI, "RefreshAllData"))
                {
                    homeUI.RefreshAllData();
                }
            }
            else if (currentPhase == GamePhase.Exploration && explorationUI != null)
            {
                UpdateCommonUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] AutoUpdateUI error: {e.Message}");
        }
    }
    
    void InitializeUIPanels()
    {
        // 安全添加UI面板到字典
        try
        {
            if (homeUI != null && homeUI is IUIPanel) 
                uiPanels[GamePhase.Home] = homeUI as IUIPanel;
            if (explorationUI != null && explorationUI is IUIPanel) 
                uiPanels[GamePhase.Exploration] = explorationUI as IUIPanel;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] InitializeUIPanels error: {e.Message}");
        }
        
        InitializeAllPanels();
    }
    
    void InitializeAllPanels()
    {
        // 安全初始化所有面板
        SafeInitialize(homeUI, "HomeUI");
        SafeInitialize(explorationUI, "ExplorationUI");
        SafeInitialize(inventoryUI, "InventoryUI");
        SafeInitialize(eventChoiceUI, "EventChoiceUI");
        SafeInitialize(journalUI, "JournalUI");
        SafeInitialize(settingsUI, "SettingsUI");
        SafeInitialize(pauseMenuUI, "PauseMenuUI");
        SafeInitialize(messageDisplay, "MessageDisplay");
    }
    
    void SafeInitialize(object uiComponent, string componentName)
    {
        try
        {
            if (uiComponent != null && HasMethod(uiComponent, "Initialize"))
            {
                var method = uiComponent.GetType().GetMethod("Initialize");
                method?.Invoke(uiComponent, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] {componentName} initialization failed: {e.Message}");
        }
    }
    
    bool HasMethod(object obj, string methodName)
    {
        if (obj == null) return false;
        return obj.GetType().GetMethod(methodName) != null;
    }
    
    void SubscribeToInputs()
    {
        // 可以订阅输入管理器的事件
    }
    
    void HandleInput()
    {
        if (inputSettings == null) return;
        
        try
        {
            // 背包切换
            if (Input.GetKeyDown(inputSettings.inventoryKey))
            {
                ToggleInventory();
            }
            
            // 暂停菜单
            if (Input.GetKeyDown(inputSettings.pauseKey))
            {
                TogglePauseMenu();
            }
            
            // 日志
            if (Input.GetKeyDown(inputSettings.journalKey))
            {
                ToggleJournal();
            }
            
            // ESC键处理 - 关闭当前打开的面板或显示暂停菜单
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] HandleInput 错误: {e.Message}");
        }
    }
    
    void UpdateDynamicUI()
    {
        try
        {
            // 更新当前阶段的UI
            if (uiPanels.ContainsKey(currentPhase))
            {
                var panel = uiPanels[currentPhase];
                if (panel != null && HasMethod(panel, "UpdateUI"))
                {
                    panel.UpdateUI();
                }
            }
            
            // 更新通用UI元素
            UpdateCommonUI();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] UpdateDynamicUI error: {e.Message}");
        }
    }
    
    void UpdateCommonUI()
    {
        // 只负责通用UI，玩家HUD由PlayerUI自己处理
        if (GameManager.Instance)
        {
            UpdateTimeDisplay();
        }
    }
    
    void UpdateTimeDisplay()
    {
        try
        {
            if (currentPhase == GamePhase.Exploration && explorationUI != null)
            {
                float remainingTime = GameManager.Instance.PhaseTimer;
                
                // 安全调用UpdateTimeDisplay方法
                var method = explorationUI.GetType().GetMethod("UpdateTimeDisplay");
                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        // 无参数版本
                        method.Invoke(explorationUI, null);
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
                    {
                        // 有float参数版本
                        method.Invoke(explorationUI, new object[] { remainingTime });
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] UpdateTimeDisplay error: {e.Message}");
        }
    }
    
    public void OnPhaseChanged(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;
        
        try
        {
            HideCurrentPanel();
            currentPhase = newPhase;
            ShowCurrentPanel();
            SetCursorState(GetCursorVisibilityForPhase(newPhase));
            
            // 通知PlayerHUD阶段变化
            if (playerHUD != null && HasMethod(playerHUD, "OnPhaseChanged"))
            {
                var method = playerHUD.GetType().GetMethod("OnPhaseChanged");
                method?.Invoke(playerHUD, new object[] { newPhase });
            }
            
            Debug.Log($"[UIManager] UI phase changed to: {newPhase}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] OnPhaseChanged error: {e.Message}");
        }
    }
    
    void HideAllPanels()
    {
        try
        {
            foreach (var panel in uiPanels.Values)
            {
                if (panel != null && HasMethod(panel, "Hide"))
                {
                    panel.Hide();
                }
            }
            
            SafeHide(inventoryUI, "InventoryUI");
            SafeHide(eventChoiceUI, "EventChoiceUI");
            SafeHide(journalUI, "JournalUI");
            SafeHide(settingsUI, "SettingsUI");
            SafeHide(pauseMenuUI, "PauseMenuUI");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] HideAllPanels error: {e.Message}");
        }
    }
    
    void SafeHide(object uiComponent, string componentName)
    {
        try
        {
            if (uiComponent != null && HasMethod(uiComponent, "Hide"))
            {
                var method = uiComponent.GetType().GetMethod("Hide");
                method?.Invoke(uiComponent, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] {componentName} hide failed: {e.Message}");
        }
    }
    
    void SafeShow(object uiComponent, string componentName)
    {
        try
        {
            if (uiComponent != null && HasMethod(uiComponent, "Show"))
            {
                var method = uiComponent.GetType().GetMethod("Show");
                method?.Invoke(uiComponent, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] {componentName} show failed: {e.Message}");
        }
    }
    
    void HideCurrentPanel()
    {
        if (uiPanels.ContainsKey(currentPhase))
        {
            var panel = uiPanels[currentPhase];
            if (panel != null && HasMethod(panel, "Hide"))
            {
                panel.Hide();
            }
        }
    }
    
    void ShowCurrentPanel()
    {
        if (uiPanels.ContainsKey(currentPhase))
        {
            var panel = uiPanels[currentPhase];
            if (panel != null && HasMethod(panel, "Show"))
            {
                panel.Show();
            }
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
        try
        {
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] SetCursorState error: {e.Message}");
        }
    }
    
    // UI切换方法
    public void ToggleInventory()
    {
        if (inventoryUI == null)
        {
            Debug.LogError("[UIManager] InventoryUI is null! Please assign it in inspector.");
            return;
        }
    
        Debug.Log($"[UIManager] ToggleInventory called, current state: {inventoryUI.IsVisible()}");
    
        if (inventoryUI.IsVisible())
        {
            inventoryUI.Hide();
        }
        else
        {
            // 确保背包UI已初始化
            if (!inventoryUI.IsVisible())
            {
                inventoryUI.Show();
            }
        }
    }
    
    
    public void ShowInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.Show();
        }
        else
        {
            Debug.LogError("[UIManager] InventoryUI is null!");
        }
    }
    public void TogglePauseMenu()
    {
        try
        {
            pauseMenuOpen = !pauseMenuOpen;
            
            if (pauseMenuOpen)
            {
                SafeShow(pauseMenuUI, "PauseMenuUI");
                PauseGame();
            }
            else
            {
                SafeHide(pauseMenuUI, "PauseMenuUI");
                ResumeGame();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] TogglePauseMenu error: {e.Message}");
        }
    }
    
    public void ToggleJournal()
    {
        try
        {
            bool isJournalOpen = false;
            
            // 安全检查IsVisible方法
            if (journalUI != null && HasMethod(journalUI, "IsVisible"))
            {
                var method = journalUI.GetType().GetMethod("IsVisible");
                var result = method?.Invoke(journalUI, null);
                isJournalOpen = result is bool && (bool)result;
            }
            
            if (isJournalOpen)
            {
                SafeHide(journalUI, "JournalUI");
            }
            else
            {
                SafeShow(journalUI, "JournalUI");
                
                // 安全调用RefreshEntries
                if (HasMethod(journalUI, "RefreshEntries"))
                {
                    var method = journalUI.GetType().GetMethod("RefreshEntries");
                    method?.Invoke(journalUI, null);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] ToggleJournal error: {e.Message}");
        }
    }
    
    public void ShowEventChoice(RandomEvent eventData)
    {
        try
        {
            if (eventChoiceUI != null && HasMethod(eventChoiceUI, "ShowEvent"))
            {
                var method = eventChoiceUI.GetType().GetMethod("ShowEvent");
                method?.Invoke(eventChoiceUI, new object[] { eventData });
            }
            PauseGame();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] ShowEventChoice error: {e.Message}");
        }
    }
    
    public void HideEventChoice()
    {
        try
        {
            SafeHide(eventChoiceUI, "EventChoiceUI");
            ResumeGame();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] HideEventChoice error: {e.Message}");
        }
    }
    
    public void PauseGame()
    {
        if (!gameIsPaused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            gameIsPaused = true;
            
            // 显示鼠标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log("[UIManager] 游戏已暂停");
        }
    }
    
    public void ResumeGame()
    {
        if (gameIsPaused)
        {
            Time.timeScale = previousTimeScale;
            gameIsPaused = false;
            
            // 根据当前阶段设置鼠标状态
            SetCursorState(GetCursorVisibilityForPhase(currentPhase));
            
            Debug.Log("[UIManager] 游戏已恢复");
        }
    }
    
    // 消息系统
    public void ShowMessage(string message, float duration = 0f)
    {
        try
        {
            float showDuration = duration > 0f ? duration : messageDuration;
            
            if (messageDisplay != null && HasMethod(messageDisplay, "ShowMessage"))
            {
                var method = messageDisplay.GetType().GetMethod("ShowMessage");
                var parameters = method?.GetParameters();
                
                if (parameters != null && parameters.Length >= 2)
                {
                    method.Invoke(messageDisplay, new object[] { message, showDuration });
                }
                else if (parameters != null && parameters.Length == 1)
                {
                    method.Invoke(messageDisplay, new object[] { message });
                }
            }
            else
            {
                // 备用方案：在控制台显示消息
                Debug.Log($"[UIManager] Message: {message}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] ShowMessage error: {e.Message}");
            Debug.Log($"[UIManager] Message (fallback): {message}");
        }
    }
    
    public void ShowTimeWarning()
    {
        try
        {
            string warningText = "时间不多了！赶紧回家！";
            if (textSettings != null && HasMethod(textSettings, "GetText"))
            {
                var method = textSettings.GetType().GetMethod("GetText");
                var result = method?.Invoke(textSettings, new object[] { "TIME_WARNING" });
                if (result is string text && !string.IsNullOrEmpty(text))
                {
                    warningText = text;
                }
            }
            
            float duration = gameValues?.defaultMessageDuration ?? 5f;
            ShowMessage(warningText, duration);
            
            if (explorationUI != null && HasMethod(explorationUI, "ShowTimeWarning"))
            {
                var method = explorationUI.GetType().GetMethod("ShowTimeWarning");
                method?.Invoke(explorationUI, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] ShowTimeWarning error: {e.Message}");
        }
    }
    
    // 加载屏幕
    public void ShowLoadingScreen()
    {
        SafeShow(loadingScreen, "LoadingScreen");
    }
    
    public void HideLoadingScreen()
    {
        SafeHide(loadingScreen, "LoadingScreen");
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
    public void ShowInteractionPrompt(string customText = null)
    {
        try
        {
            if (explorationUI != null)
            {
                string promptText = customText ?? "按 E 交互";
                
                if (string.IsNullOrEmpty(promptText) && textSettings != null && inputSettings != null)
                {
                    if (HasMethod(textSettings, "GetText"))
                    {
                        var method = textSettings.GetType().GetMethod("GetText");
                        var result = method?.Invoke(textSettings, new object[] { "INTERACT_PROMPT", inputSettings.interactionKey });
                        if (result is string text)
                        {
                            promptText = text;
                        }
                    }
                }
                
                if (HasMethod(explorationUI, "ShowInteractionPrompt"))
                {
                    var method = explorationUI.GetType().GetMethod("ShowInteractionPrompt");
                    method?.Invoke(explorationUI, new object[] { promptText });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] ShowInteractionPrompt error: {e.Message}");
        }
    }
    
    public void HideInteractionPrompt()
    {
        try
        {
            if (explorationUI != null && HasMethod(explorationUI, "HideInteractionPrompt"))
            {
                var method = explorationUI.GetType().GetMethod("HideInteractionPrompt");
                method?.Invoke(explorationUI, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] HideInteractionPrompt error: {e.Message}");
        }
    }
    
    // PlayerUI委托方法 - 保持向后兼容
    public void UpdateAmmoDisplay(int current, int max)
    {
        try
        {
            if (playerHUD != null && HasMethod(playerHUD, "UpdateAmmoDisplay"))
            {
                var method = playerHUD.GetType().GetMethod("UpdateAmmoDisplay");
                method?.Invoke(playerHUD, new object[] { current, max });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] UpdateAmmoDisplay error: {e.Message}");
        }
    }
    
    public void UpdateHealthDisplay(float current, float max)
    {
        try
        {
            if (playerHUD != null && HasMethod(playerHUD, "UpdateHealthDisplay"))
            {
                var method = playerHUD.GetType().GetMethod("UpdateHealthDisplay");
                method?.Invoke(playerHUD, new object[] { current, max });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] UpdateHealthDisplay error: {e.Message}");
        }
    }
    
    public void RefreshHomeUI()
    {
        try
        {
            if (homeUI != null && HasMethod(homeUI, "RefreshAllData"))
            {
                var method = homeUI.GetType().GetMethod("RefreshAllData");
                method?.Invoke(homeUI, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] RefreshHomeUI error: {e.Message}");
        }
    }
    
    public void RefreshInventoryUI()
    {
        try
        {
            if (inventoryUI != null && HasMethod(inventoryUI, "RefreshInventory"))
            {
                var method = inventoryUI.GetType().GetMethod("RefreshInventory");
                method?.Invoke(inventoryUI, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] RefreshInventoryUI error: {e.Message}");
        }
    }
    
    public string GetText(string key, params object[] args)
    {
        try
        {
            if (textSettings != null && HasMethod(textSettings, "GetText"))
            {
                var method = textSettings.GetType().GetMethod("GetText");
                var result = method?.Invoke(textSettings, new object[] { key, args });
                return result as string ?? key;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] GetText error: {e.Message}");
        }
        return key;
    }
    
    void CloseOtherPanels()
    {
        // 关闭其他面板，确保同时只有一个面板打开
        if (journalUI != null && journalUI.IsVisible())
            journalUI.Hide();
        if (settingsUI != null && settingsUI.IsVisible())
            settingsUI.Hide();
        if (pauseMenuUI != null && pauseMenuUI.IsVisible())
            pauseMenuUI.Hide();
    }
    
    void HandleEscapeKey()
    {
        // 优先级：先关闭打开的面板，没有面板时显示暂停菜单
        if (inventoryUI != null && inventoryUI.IsVisible())
        {
            inventoryUI.Hide();
        }
        else if (journalUI != null && journalUI.IsVisible())
        {
            journalUI.Hide();
        }
        else if (settingsUI != null && settingsUI.IsVisible())
        {
            settingsUI.Hide();
        }
        else if (pauseMenuUI != null && pauseMenuUI.IsVisible())
        {
            pauseMenuUI.Hide();
        }
        else if (currentPhase == GamePhase.Exploration || currentPhase == GamePhase.Home)
        {
            TogglePauseMenu();
        }
    }
 
    /*void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 根据场景重新初始化UI
        RefreshUIForScene(scene.name);
    }*/
    
}


