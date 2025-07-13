// 完整的 ExplorationUI.cs 实现
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ExplorationUI : UIPanel
{
    [Header("=== 时间显示 ===")]
    public TextMeshProUGUI timeRemainingText;
    public Image timeBar;
    public GameObject timeWarningPanel;
    public TextMeshProUGUI timeWarningText;
    
    [Header("=== 交互提示 ===")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI interactionText;
    public Image interactionIcon;
    
    [Header("=== 消息显示 ===")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    public Image messageBackground;
    
    [Header("=== 目标提示 ===")]
    public GameObject objectivePanel;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI objectiveCounter;
    
    [Header("=== 地图信息 ===")]
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI riskLevelText;
    public Image riskLevelIcon;
    
    [Header("=== 返回家园 ===")]
    public Button returnHomeButton;
    public GameObject returnPrompt;
    
    [Header("=== 颜色配置 ===")]
    public Color normalTimeColor = Color.white;
    public Color warningTimeColor = Color.yellow;
    public Color criticalTimeColor = Color.red;
    public Color[] riskLevelColors = new Color[5];
    
    private float maxExplorationTime;
    private bool isTimeWarningShown = false;
    private Coroutine messageCoroutine;
    
    public override void Initialize()
    {
        base.Initialize();
        
        // 初始化UI状态
        HideInteractionPrompt();
        HideTimeWarning();
        if (messagePanel) messagePanel.SetActive(false);
        
        // 按钮事件
        returnHomeButton?.onClick.AddListener(ReturnHome);
        
        // 获取探索时间限制
        maxExplorationTime = GameManager.Instance?.gameConfig?.explorationTimeLimit ?? 900f;
        
        // 设置地图信息
        //SetupMapInfo();
        
        Debug.Log("[ExplorationUI] 初始化完成");
    }
    
    public override void Show()
    {
        base.Show();
        
        // 显示探索相关的UI元素
        if (timeRemainingText) timeRemainingText.gameObject.SetActive(true);
        if (mapNameText) mapNameText.gameObject.SetActive(true);
        if (objectivePanel) objectivePanel.SetActive(true);
        
        // 重置时间警告状态
        isTimeWarningShown = false;
    }
    
    public override void Hide()
    {
        base.Hide();
        
        // 隐藏所有提示
        HideInteractionPrompt();
        HideTimeWarning();
        if (messagePanel) messagePanel.SetActive(false);
    }
    
    public override void UpdateUI()
    {
        if (GameManager.Instance && GameManager.Instance.CurrentPhase == GamePhase.Exploration)
        {
            UpdateTimeDisplay();
            CheckTimeWarning();
        }
    }
    
    /*void SetupMapInfo()
    {
        // 从ExplorationManager获取当前地图信息
        if (ExplorationManager.HasInstance)
        {
            var mapData = ExplorationManager.Instance.GetCurrentMapData();
            if (mapData != null)
            {
                if (mapNameText) mapNameText.text = mapData.mapName;
                if (riskLevelText) riskLevelText.text = $"风险等级: {mapData.riskLevel}";
                
                // 设置风险等级颜色
                if (riskLevelIcon && mapData.riskLevel <= riskLevelColors.Length)
                {
                    riskLevelIcon.color = riskLevelColors[mapData.riskLevel - 1];
                }
            }
        }
        else
        {
            // 默认信息
            if (mapNameText) mapNameText.text = "探索区域";
            if (riskLevelText) riskLevelText.text = "风险等级: 未知";
        }
    }*/
    
    public void UpdateTimeDisplay()
    {
        if (!GameManager.Instance) return;
        
        float remainingTime = GameManager.Instance.PhaseTimer;
        
        // 更新时间文本
        if (timeRemainingText)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timeRemainingText.text = $"{minutes:00}:{seconds:00}";
            
            // 根据剩余时间改变颜色
            float timeRatio = remainingTime / maxExplorationTime;
            if (timeRatio <= 0.1f)
            {
                timeRemainingText.color = criticalTimeColor;
            }
            else if (timeRatio <= 0.3f)
            {
                timeRemainingText.color = warningTimeColor;
            }
            else
            {
                timeRemainingText.color = normalTimeColor;
            }
        }
        
        // 更新时间条
        if (timeBar)
        {
            float timeRatio = remainingTime / maxExplorationTime;
            timeBar.fillAmount = timeRatio;
            
            // 时间条颜色
            if (timeRatio <= 0.1f)
            {
                timeBar.color = criticalTimeColor;
            }
            else if (timeRatio <= 0.3f)
            {
                timeBar.color = warningTimeColor;
            }
            else
            {
                timeBar.color = normalTimeColor;
            }
        }
    }
    
    void CheckTimeWarning()
    {
        if (!GameManager.Instance) return;
        
        float remainingTime = GameManager.Instance.PhaseTimer;
        float warningThreshold = GameManager.Instance.gameConfig?.timeWarningThreshold ?? 150f;
        
        if (remainingTime <= warningThreshold && !isTimeWarningShown)
        {
            ShowTimeWarning();
            isTimeWarningShown = true;
        }
    }
    
    public void ShowTimeWarning()
    {
        if (timeWarningPanel)
        {
            timeWarningPanel.SetActive(true);
            if (timeWarningText) timeWarningText.text = "时间不多了！赶紧回家！";
            
            // 3秒后自动隐藏
            StartCoroutine(HideTimeWarningAfterDelay(3f));
        }
    }
    
    public void HideTimeWarning()
    {
        if (timeWarningPanel) timeWarningPanel.SetActive(false);
    }
    
    IEnumerator HideTimeWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTimeWarning();
    }
    
    // 交互提示系统
    public void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt)
        {
            interactionPrompt.SetActive(true);
            if (interactionText) interactionText.text = text;
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (interactionPrompt) interactionPrompt.SetActive(false);
    }
    
    // 消息显示系统
    public void ShowMessage(string message, float duration = 3f)
    {
        if (messagePanel && messageText)
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
            }
            
            messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
        }
    }
    
    IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        messagePanel.SetActive(true);
        messageText.text = message;
        
        // 淡入
        if (messageBackground)
        {
            Color color = messageBackground.color;
            color.a = 0f;
            messageBackground.color = color;
            
            float fadeTime = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 0.8f, elapsed / fadeTime);
                messageBackground.color = color;
                yield return null;
            }
        }
        
        // 等待显示时间
        yield return new WaitForSeconds(duration);
        
        // 淡出
        if (messageBackground)
        {
            Color color = messageBackground.color;
            float fadeTime = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0.8f, 0f, elapsed / fadeTime);
                messageBackground.color = color;
                yield return null;
            }
        }
        
        messagePanel.SetActive(false);
        messageCoroutine = null;
    }
    
    // 目标显示系统
    public void UpdateObjective(string objective, int current = -1, int total = -1)
    {
        if (objectiveText) objectiveText.text = objective;
        
        if (objectiveCounter && current >= 0 && total >= 0)
        {
            objectiveCounter.text = $"{current}/{total}";
            objectiveCounter.gameObject.SetActive(true);
        }
        else if (objectiveCounter)
        {
            objectiveCounter.gameObject.SetActive(false);
        }
    }
    
    // 返回家园
    void ReturnHome()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.ReturnHomeFromExploration(false);
        }
    }
    
    public void ShowReturnPrompt(bool show)
    {
        if (returnPrompt) returnPrompt.SetActive(show);
    }
    
    // 特殊效果
    public void PlayScreenShake(float intensity = 1f)
    {
        StartCoroutine(ScreenShakeCoroutine(intensity));
    }
    
    IEnumerator ScreenShakeCoroutine(float intensity)
    {
        Vector3 originalPos = transform.localPosition;
        
        for (int i = 0; i < 10; i++)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * intensity * 5f;
            yield return new WaitForSeconds(0.05f);
        }
        
        transform.localPosition = originalPos;
    }
    
    void OnDestroy()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
    }
}

// 改进后的 ExplorationHUD.cs
public class ExplorationHUD : MonoBehaviour
{
    [Header("HUD组件")]
    public ExplorationUI explorationUI;
    public PlayerUI playerUI;
    
    [Header("快捷信息")]
    public TextMeshProUGUI currentMapText;
    public TextMeshProUGUI objectivesText;
    public Button quickReturnButton;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // 初始化HUD组件
        if (explorationUI) explorationUI.Initialize();
        if (playerUI) playerUI.Initialize();
        
        // 设置按钮事件
        if (quickReturnButton)
        {
            quickReturnButton.onClick.AddListener(() => {
                GameManager.Instance?.ReturnHomeFromExploration(false);
            });
        }
        
        // 设置当前地图信息
        //UpdateMapInfo();
    }
    
    void Update()
    {
        // 处理输入
        HandleInput();
        
        // 更新UI
        if (explorationUI) explorationUI.UpdateUI();
    }
    
    void HandleInput()
    {
        // ESC键快速返回
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UIManager.Instance)
            {
                UIManager.Instance.TogglePauseMenu();
            }
        }
        
        // M键显示地图（如果有）
        if (Input.GetKeyDown(KeyCode.M))
        {
            // TODO: 显示小地图
        }
    }
    
    /*void UpdateMapInfo()
    {
        if (ExplorationManager.HasInstance)
        {
            var mapData = ExplorationManager.Instance.GetCurrentMapData();
            if (mapData != null && currentMapText)
            {
                currentMapText.text = mapData.mapName;
            }
        }
        
        // 更新目标
        if (objectivesText)
        {
            objectivesText.text = "目标: 搜寻物资并安全返回";
        }
    }*/
    
    public void ShowInteractionPrompt(string text)
    {
        explorationUI?.ShowInteractionPrompt(text);
    }
    
    public void HideInteractionPrompt()
    {
        explorationUI?.HideInteractionPrompt();
    }
}