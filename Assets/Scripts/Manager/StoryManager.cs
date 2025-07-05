using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : Singleton<StoryManager>
{
    [Header("Story配置")]
    public StoryData currentStory;
    
    [Header("UI组件")]
    public GameObject storyPanel;
    public TextMeshProUGUI storyText;
    public Image backgroundImage;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;
    public CanvasGroup storyCanvasGroup;
    
    [Header("动画设置")]
    public float fadeSpeed = 2f;
    public float typewriterSpeed = 50f;
    
    private int currentSegmentIndex = 0;
    private bool storyActive = false;
    private Coroutine currentAnimation;
    
    public void StartStory()
    {
        if (currentStory == null || currentStory.segments.Length == 0)
        {
            Debug.LogError("No story data configured!");
            return;
        }
        
        storyActive = true;
        currentSegmentIndex = 0;
        
        // 显示故事面板
        if (storyPanel) storyPanel.SetActive(true);
        
        // 播放背景音乐
        if (currentStory.backgroundMusic && AudioManager.Instance)
        {
            AudioManager.Instance.PlayMusic(currentStory.backgroundMusic.name);
        }
        
        // 开始第一段故事
        StartCoroutine(PlayStorySegment(currentStory.segments[currentSegmentIndex]));
    }
    
    IEnumerator PlayStorySegment(StorySegment segment)
    {
        // 清空选择按钮
        ClearChoices();
        
        // 设置背景图片
        if (backgroundImage && segment.segmentImage)
        {
            backgroundImage.sprite = segment.segmentImage;
        }
        
        // 淡入效果
        yield return StartCoroutine(FadeIn());
        
        // 打字机效果显示文本
        yield return StartCoroutine(TypewriterEffect(segment.storyText, segment.textSpeed));
        
        // 等待暂停时间
        yield return new WaitForSeconds(segment.pauseAfterText);
        
        // 显示选择项
        if (segment.choices != null && segment.choices.Length > 0)
        {
            CreateChoiceButtons(segment.choices);
        }
        else
        {
            // 没有选择，等待输入或自动继续
            if (segment.waitForInput)
            {
                yield return new WaitUntil(() => Input.anyKeyDown);
            }
            
            ContinueStory();
        }
    }
    
    IEnumerator TypewriterEffect(string text, float speed)
    {
        if (storyText == null) yield break;
        
        storyText.text = "";
        float characterDelay = 1f / speed;
        
        for (int i = 0; i < text.Length; i++)
        {
            storyText.text += text[i];
            yield return new WaitForSeconds(characterDelay);
            
            // 空格和标点符号稍微快一些
            if (char.IsPunctuation(text[i]) || char.IsWhiteSpace(text[i]))
            {
                yield return new WaitForSeconds(characterDelay * 0.5f);
            }
        }
    }
    
    IEnumerator FadeIn()
    {
        if (storyCanvasGroup == null) yield break;
        
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += fadeSpeed * Time.deltaTime;
            storyCanvasGroup.alpha = alpha;
            yield return null;
        }
        storyCanvasGroup.alpha = 1f;
    }
    
    void CreateChoiceButtons(StoryChoice[] choices)
    {
        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
            
            // 设置按钮文本
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText)
            {
                buttonText.text = $"{choice.choiceText}\n<size=14><color=#CCCCCC>{choice.description}</color></size>";
            }
            
            // 绑定点击事件
            var button = buttonObj.GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(() => OnChoiceSelected(choice));
            }
        }
    }
    
    void OnChoiceSelected(StoryChoice choice)
    {
        // 应用选择效果
        ApplyChoiceEffects(choice);
        
        // 显示结果
        StartCoroutine(ShowChoiceResult(choice));
    }
    
    void ApplyChoiceEffects(StoryChoice choice)
    {
        if (FamilyManager.Instance)
        {
            if (choice.foodBonus != 0)
                FamilyManager.Instance.AddResource("food", choice.foodBonus);
            if (choice.waterBonus != 0)
                FamilyManager.Instance.AddResource("water", choice.waterBonus);
            if (choice.medicineBonus != 0)
                FamilyManager.Instance.AddResource("medicine", choice.medicineBonus);
        }
        
        if (choice.grantWeapon && InventoryManager.Instance)
        {
            // 根据weaponType添加武器到背包
            // 需要武器的ItemData引用
            Debug.Log($"获得了{choice.weaponType}!");
        }
        
        // 记录到日志
        if (JournalManager.Instance)
        {
            JournalManager.Instance.AddEntry("故事选择", 
                $"选择了：{choice.choiceText}。{choice.resultText}", 
                JournalEntryType.Important);
        }
    }
    
    IEnumerator ShowChoiceResult(StoryChoice choice)
    {
        ClearChoices();
        
        // 显示结果文本
        yield return StartCoroutine(TypewriterEffect(choice.resultText, typewriterSpeed));
        
        // 等待显示时间
        yield return new WaitForSeconds(choice.resultDisplayTime);
        
        // 继续故事或结束
        ContinueStory();
    }
    
    void ClearChoices()
    {
        if (choiceContainer == null) return;
        
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    void ContinueStory()
    {
        currentSegmentIndex++;
        
        if (currentSegmentIndex < currentStory.segments.Length)
        {
            // 继续下一段
            StartCoroutine(PlayStorySegment(currentStory.segments[currentSegmentIndex]));
        }
        else
        {
            // 故事结束
            CompleteStory();
        }
    }
    
    void CompleteStory()
    {
        storyActive = false;
        
        StartCoroutine(FadeOutAndFinish());
    }
    
    IEnumerator FadeOutAndFinish()
    {
        // 淡出效果
        if (storyCanvasGroup)
        {
            float alpha = 1f;
            while (alpha > 0f)
            {
                alpha -= fadeSpeed * Time.deltaTime;
                storyCanvasGroup.alpha = alpha;
                yield return null;
            }
        }
        
        // 隐藏故事面板
        if (storyPanel) storyPanel.SetActive(false);
        
        // 切换到家庭场景
        if (GameManager.Instance)
        {
            GameManager.Instance.CompleteStoryPhase();
        }
    }
    
    // 跳过故事（调试用）
    [ContextMenu("Skip Story")]
    public void SkipStory()
    {
        if (storyActive)
        {
            StopAllCoroutines();
            CompleteStory();
        }
    }
}
