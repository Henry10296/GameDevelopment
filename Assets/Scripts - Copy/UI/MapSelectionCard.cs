using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class MapSelectionCard
{
    [Header("地图信息")]
    public string mapName;
    public string sceneName;
    public int riskLevel = 1; // 1-5星
    public bool isUnlocked = true;
    
    [Header("UI组件")]
    public GameObject cardObject;
    public Button selectButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI riskText;
    public TextMeshProUGUI descriptionText;
    public Image backgroundImage;
    public GameObject lockIcon;
    public Image[] riskStars;
    
    [Header("视觉设置")]
    public Sprite previewImage;
    public Color unlockedColor = Color.white;
    public Color lockedColor = Color.gray;
    
    public void Initialize()
    {
        if (titleText) titleText.text = mapName;
        if (backgroundImage && previewImage) backgroundImage.sprite = previewImage;
        
        UpdateRiskDisplay();
        UpdateLockState();
        SetupButton();
    }
    
    void UpdateRiskDisplay()
    {
        if (riskText) riskText.text = $"风险等级: {riskLevel}/5";
        
        // 更新星级显示
        if (riskStars != null)
        {
            for (int i = 0; i < riskStars.Length; i++)
            {
                if (riskStars[i] != null)
                {
                    riskStars[i].color = i < riskLevel ? Color.yellow : Color.gray;
                }
            }
        }
    }
    
    void UpdateLockState()
    {
        if (lockIcon) lockIcon.SetActive(!isUnlocked);
        if (selectButton) selectButton.interactable = isUnlocked;
        
        // 更新视觉效果
        if (cardObject)
        {
            Image cardImage = cardObject.GetComponent<Image>();
            if (cardImage)
            {
                cardImage.color = isUnlocked ? unlockedColor : lockedColor;
            }
        }
    }
    
    void SetupButton()
    {
        if (selectButton)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(SelectMap);
        }
    }
    
    void SelectMap()
    {
        if (!isUnlocked) return;
        
        Debug.Log($"选择地图: {mapName}");
        
        // 根据场景名称获取地图索引
        int mapIndex = GetMapIndex(sceneName);
        if (mapIndex >= 0)
        {
            GameManager.Instance.StartExploration(mapIndex);
        }
    }
    
    int GetMapIndex(string sceneName)
    {
        string[] scenes = {"2_Hospital", "3_School", "4_Supermarket", "5_Park"};
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i] == sceneName) return i;
        }
        return -1;
    }
    
    public void UpdateDescription(string description)
    {
        if (descriptionText) descriptionText.text = description;
    }
}