using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ExplorationUI : UIPanel
{
    [Header("=== 时间系统 ===")]
    public GameObject timePanel;
    public TextMeshProUGUI timeText;
    public Slider timeSlider;
    public GameObject timeWarning;
    public Image timeBarFill;
    
    [Header("=== 任务信息 ===")]
    public GameObject objectivePanel;
    public TextMeshProUGUI objectiveText;
    
    [Header("=== 地图信息 ===")]
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI locationText;
    
    private float totalTime;
    private float currentTime;
    
    public void Initialize(float explorationTime)
    {
        totalTime = explorationTime;
        currentTime = explorationTime;
        
        if (timeSlider) timeSlider.maxValue = totalTime;
        if (timeWarning) timeWarning.SetActive(false);
        
        // 设置地图信息
        if (ExplorationManager.Instance?.CurrentMap != null)
        {
            var map = ExplorationManager.Instance.CurrentMap;
            if (mapNameText) mapNameText.text = map.mapName;
            if (locationText) locationText.text = $"风险等级: {map.riskLevel}/5";
        }
    }
    
    void Update()
    {
        UpdateTimeDisplay();
    }

    public void UpdateTimeDisplay()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Exploration) return;
        
        currentTime = GameManager.Instance.PhaseTimer;
        float timePercent = currentTime / totalTime;
        
        // 更新时间文本
        if (timeText)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timeText.text = $"{minutes:00}:{seconds:00}";
            
            // 时间颜色警告
            if (timePercent <= 0.1f)
                timeText.color = Color.red;
            else if (timePercent <= 0.3f)
                timeText.color = Color.yellow;
            else
                timeText.color = Color.white;
        }
        
        // 更新时间条
        if (timeSlider) timeSlider.value = currentTime;
        if (timeBarFill)
        {
            if (timePercent <= 0.1f)
                timeBarFill.color = Color.red;
            else if (timePercent <= 0.3f)
                timeBarFill.color = Color.yellow;
            else
                timeBarFill.color = Color.green;
        }
        
        // 时间警告
        if (timeWarning)
        {
            timeWarning.SetActive(timePercent <= 0.3f);
        }
    }
    
    public void SetObjective(string objective)
    {
        if (objectiveText) objectiveText.text = objective;
        if (objectivePanel) objectivePanel.SetActive(!string.IsNullOrEmpty(objective));
    }
}