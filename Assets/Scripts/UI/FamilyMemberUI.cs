using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class FamilyMemberUI
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
    
    public void Initialize(int index)
    {
        memberIndex = index;
        
        // 设置按钮事件
        if (feedButton) feedButton.onClick.AddListener(() => FeedMember());
        if (waterButton) waterButton.onClick.AddListener(() => GiveWater());
        if (healButton) healButton.onClick.AddListener(() => HealMember());
    }
    
    public void UpdateDisplay(FamilyMember member)
    {
        if (nameText) nameText.text = member.name;
        
        // 更新血量
        if (healthSlider)
        {
            healthSlider.value = member.health / 100f;
            UpdateSliderColor(healthSlider, member.health / 100f);
        }
        
        // 更新饥饿
        if (hungerSlider)
        {
            hungerSlider.value = member.hunger / 100f;
            UpdateSliderColor(hungerSlider, member.hunger / 100f);
        }
        
        // 更新口渴
        if (thirstSlider)
        {
            thirstSlider.value = member.thirst / 100f;
            UpdateSliderColor(thirstSlider, member.thirst / 100f);
        }
        
        // 更新生病状态
        if (sickIcon) sickIcon.SetActive(member.isSick);
        
        // 更新按钮可用性
        UpdateButtonStates();
    }
    
    void UpdateSliderColor(Slider slider, float value)
    {
        if (slider.fillRect == null) return;
        
        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage == null) return;
        
        if (value > 0.6f)
            fillImage.color = healthyColor;
        else if (value > 0.3f)
            fillImage.color = warningColor;
        else
            fillImage.color = dangerColor;
    }
    
    void UpdateButtonStates()
    {
        if (FamilyManager.Instance == null) return;
        
        // 修复：使用公共属性访问器而不是私有字段
        if (feedButton) 
            feedButton.interactable = FamilyManager.Instance.Food > 0;
        if (waterButton) 
            waterButton.interactable = FamilyManager.Instance.Water > 0;
        if (healButton) 
            healButton.interactable = FamilyManager.Instance.Medicine > 0;
    }
    
    void FeedMember()
    {
        if (FamilyManager.Instance)
            FamilyManager.Instance.FeedMember(memberIndex);
    }
    
    void GiveWater()
    {
        if (FamilyManager.Instance)
            FamilyManager.Instance.GiveWaterToMember(memberIndex);
    }
    
    void HealMember()
    {
        if (FamilyManager.Instance)
            FamilyManager.Instance.HealMember(memberIndex);
    }
}