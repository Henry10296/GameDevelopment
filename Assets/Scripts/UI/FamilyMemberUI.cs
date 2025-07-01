using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class FixedFamilyMemberUI : MonoBehaviour
{
    [Header("UI组件")]
    public TextMeshProUGUI nameText;
    public Slider healthSlider;
    public Slider hungerSlider;
    public Slider thirstSlider;
    public GameObject sickIcon;
    public Button feedButton;
    public Button waterButton;
    public Button healButton;
    
    [Header("颜色设置")]
    public Color healthyColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    
    private int memberIndex;
    private FamilyMember cachedMember;
    
    void Start()
    {
        // 订阅家庭管理器事件
        if (FamilyManager.Instance != null)
        {
            FamilyManager.Instance.onMemberStatusChanged?.RegisterListener(
                GetComponent<GameEventListener>() ?? gameObject.AddComponent<GameEventListener>());
        }
        
        // 设置按钮事件
        SetupButtons();
    }
    
    public void Initialize(int index)
    {
        memberIndex = index;
        RefreshDisplay();
    }
    
    void SetupButtons()
    {
        feedButton?.onClick.AddListener(() => FeedMember());
        waterButton?.onClick.AddListener(() => GiveWater());
        healButton?.onClick.AddListener(() => HealMember());
    }
    
    // 响应事件的公共方法
    public void OnFamilyStatusChanged()
    {
        RefreshDisplay();
    }
    
    void RefreshDisplay()
    {
        if (FamilyManager.Instance?.FamilyMembers == null || 
            memberIndex >= FamilyManager.Instance.FamilyMembers.Count)
        {
            return;
        }
        
        cachedMember = FamilyManager.Instance.FamilyMembers[memberIndex];
        UpdateDisplay(cachedMember);
    }
    
    void UpdateDisplay(FamilyMember member)
    {
        if (member == null) return;
        
        // 更新文本
        if (nameText) nameText.text = member.name;
        
        // 更新滑条
        UpdateSlider(healthSlider, member.health / 100f);
        UpdateSlider(hungerSlider, member.hunger / 100f);
        UpdateSlider(thirstSlider, member.thirst / 100f);
        
        // 更新状态图标
        if (sickIcon) sickIcon.SetActive(member.isSick);
        
        // 更新按钮状态
        UpdateButtonStates();
    }
    
    void UpdateSlider(Slider slider, float value)
    {
        if (slider == null) return;
        
        slider.value = value;
        
        // 更新颜色
        if (slider.fillRect != null)
        {
            var fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = GetHealthColor(value);
            }
        }
    }
    
    Color GetHealthColor(float value)
    {
        if (value > 0.6f) return healthyColor;
        if (value > 0.3f) return warningColor;
        return dangerColor;
    }
    
    void UpdateButtonStates()
    {
        if (FamilyManager.Instance == null) return;
        
        // 使用公共属性访问器
        bool canFeed = FamilyManager.Instance.Food > 0;
        bool canGiveWater = FamilyManager.Instance.Water > 0;
        bool canHeal = FamilyManager.Instance.Medicine > 0;
        
        if (feedButton) feedButton.interactable = canFeed;
        if (waterButton) waterButton.interactable = canGiveWater;
        if (healButton) healButton.interactable = canHeal;
    }
    
    void FeedMember()
    {
        FamilyManager.Instance?.FeedMember(memberIndex);
    }
    
    void GiveWater()
    {
        FamilyManager.Instance?.GiveWaterToMember(memberIndex);
    }
    
    void HealMember()
    {
        FamilyManager.Instance?.HealMember(memberIndex);
    }
}