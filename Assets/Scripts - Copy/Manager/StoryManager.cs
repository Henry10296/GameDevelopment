using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class StoryChoice
{
    public string choiceText;
    public string description;
    public int foodBonus;
    public int waterBonus;
    public int medicineBonus;
    public bool hasWeapon;
    public string consequenceText;
}

[System.Serializable]
public class StorySegment
{
    public string storyText;
    public List<StoryChoice> choices;
    public float displayDuration = 3f;
}

public class StoryManager : Singleton<StoryManager>
{
    /*public static StoryManager Instance;*/
    
    [Header("故事配置")]
    public List<StorySegment> storySegments;
    
    [Header("UI引用")]
    public GameObject storyPanel;
    public TMPro.TextMeshProUGUI storyText;
    public Transform choiceButtonParent;
    public GameObject choiceButtonPrefab;
    
    public static event Action OnStoryCompleted;
    
    private int currentSegmentIndex = 0;
    private bool storyInProgress = false;
    void InitializeStory()
    {
        // 初始化故事段落
        storySegments = new List<StorySegment>
        {
            new StorySegment
            {
                storyText = "核战争爆发了！你们一家三口被困在地下室中。外面的世界变得危险而混乱。你必须做出选择来决定如何度过这场灾难...",
                displayDuration = 4f,
                choices = new List<StoryChoice>
                {
                    new StoryChoice
                    {
                        choiceText = "储备食物",
                        description = "你之前囤积了大量食物",
                        foodBonus = 10,
                        waterBonus = 5,
                        medicineBonus = 0,
                        hasWeapon = false,
                        consequenceText = "你们有足够的食物，但缺乏其他物资"
                    },
                    new StoryChoice
                    {
                        choiceText = "准备医疗用品",
                        description = "你预见到了医疗的重要性",
                        foodBonus = 5,
                        waterBonus = 5,
                        medicineBonus = 8,
                        hasWeapon = false,
                        consequenceText = "你们有充足的医疗用品，但食物紧张"
                    },
                    new StoryChoice
                    {
                        choiceText = "武装自己",
                        description = "你准备了武器来保护家人",
                        foodBonus = 3,
                        waterBonus = 3,
                        medicineBonus = 1,
                        hasWeapon = true,
                        consequenceText = "你有武器，但物资稀缺，需要外出寻找"
                    }
                }
            }
        };
    }
    
    public void StartStory()
    {
        if (storySegments.Count == 0) return;
        
        storyInProgress = true;
        currentSegmentIndex = 0;
        
        if (storyPanel) storyPanel.SetActive(true);
        
        StartCoroutine(DisplayStorySegment(storySegments[currentSegmentIndex]));
    }
    
    System.Collections.IEnumerator DisplayStorySegment(StorySegment segment)
    {
        // 显示故事文本
        if (storyText)
        {
            storyText.text = segment.storyText;
        }
        
        // 清空选择按钮
        foreach (Transform child in choiceButtonParent)
        {
            Destroy(child.gameObject);
        }
        
        // 等待文本显示时间
        yield return new WaitForSeconds(segment.displayDuration);
        
        // 显示选择按钮
        foreach (var choice in segment.choices)
        {
            CreateChoiceButton(choice);
        }
    }
    
    void CreateChoiceButton(StoryChoice choice)
    {
        GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonParent);
        UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        TMPro.TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        
        if (buttonText)
        {
            buttonText.text = choice.choiceText + "\n<size=12>" + choice.description + "</size>";
        }
        
        if (button)
        {
            button.onClick.AddListener(() => OnChoiceSelected(choice));
        }
    }
    
    void OnChoiceSelected(StoryChoice choice)
    {
        // 应用选择结果
        ApplyChoiceConsequences(choice);
        
        // 显示结果
        StartCoroutine(ShowChoiceResult(choice));
    }
    
    void ApplyChoiceConsequences(StoryChoice choice)
    {
        if (FamilyManager.Instance)
        {
            FamilyManager.Instance.AddResource("food", choice.foodBonus);
            FamilyManager.Instance.AddResource("water", choice.waterBonus);
            FamilyManager.Instance.AddResource("medicine", choice.medicineBonus);
        }
        
        if (choice.hasWeapon && InventoryManager.Instance)
        {
            // 添加手枪到背包
            // 这里需要手枪的ItemData引用
            Debug.Log("获得了手枪!");
        }
    }
    
    System.Collections.IEnumerator ShowChoiceResult(StoryChoice choice)
    {
        // 清空选择按钮
        foreach (Transform child in choiceButtonParent)
        {
            Destroy(child.gameObject);
        }
        
        // 显示结果文本
        if (storyText)
        {
            storyText.text = choice.consequenceText;
        }
        
        yield return new WaitForSeconds(3f);
        
        // 完成故事
        CompleteStory();
    }
    
    void CompleteStory()
    {
        storyInProgress = false;
        
        if (storyPanel) storyPanel.SetActive(false);
        
        OnStoryCompleted?.Invoke();
        
        // 切换到家庭场景
        if (GameManager.Instance)
        {
            GameManager.Instance.ChangePhase(GamePhase.Home);
        }
    }
}
