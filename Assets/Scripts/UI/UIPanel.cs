using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IUIPanel
{
    void Initialize();
    void Show();
    void Hide();
    void UpdateUI();
    bool IsVisible();
}

public abstract class UIPanel : MonoBehaviour, IUIPanel
{
    [Header("面板设置")]
    public GameObject panelObject;
    public CanvasGroup canvasGroup;
    public float fadeSpeed = 5f;
    
    protected bool isVisible = false;
    
    public virtual void Initialize()
    {
        if (panelObject) panelObject.SetActive(false);
        if (canvasGroup) canvasGroup.alpha = 0f;
    }
    
    public virtual void Show()
    {
        isVisible = true;
        if (panelObject) panelObject.SetActive(true);
        
        if (canvasGroup)
        {
            StartCoroutine(FadeIn());
        }
    }
    
    public virtual void Hide()
    {
        isVisible = false;
        
        if (canvasGroup)
        {
            StartCoroutine(FadeOut());
        }
        else if (panelObject)
        {
            panelObject.SetActive(false);
        }
    }
    
    public virtual void UpdateUI()
    {
        // 子类重写
    }
    
    public bool IsVisible()
    {
        return isVisible;
    }
    
    protected IEnumerator FadeIn()
    {
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
    
    protected IEnumerator FadeOut()
    {
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        if (panelObject) panelObject.SetActive(false);
    }
}

// 具体UI面板类（占位符）
public class MainMenuUI : UIPanel
{
    [Header("主菜单组件")]
    public Button newGameButton;
    public Button continueButton;
    public Button settingsButton;
    public Button exitButton;
    
    public override void Initialize()
    {
        base.Initialize();
        SetupButtons();
    }
    
    void SetupButtons()
    {
        newGameButton?.onClick.AddListener(() => GameManager.Instance.StartNewGame());
        continueButton?.onClick.AddListener(() => GameManager.Instance.LoadGame());
        settingsButton?.onClick.AddListener(() => UIManager.Instance.settingsUI?.Show());
        exitButton?.onClick.AddListener(() => GameManager.Instance.QuitGame());
    }
}

public class HomeUI : UIPanel
{
    [Header("家庭UI组件")]
    /*public FamilyStatusPanel familyStatusPanel;
    public ResourceDisplayPanel resourcePanel;*/
    public Button startExplorationButton;
    
    public override void Initialize()
    {
        base.Initialize();
        startExplorationButton?.onClick.AddListener(() => GameManager.Instance.StartMapSelection());
    }
    
    public override void UpdateUI()
    {
        RefreshAllData();
    }
    
    public void RefreshAllData()
    {
        /*if (FamilyManager.Instance)
        {
            familyStatusPanel?.UpdateFamilyStatus(FamilyManager.Instance.FamilyMembers);
            resourcePanel?.UpdateResources(
                FamilyManager.Instance.Food,
                FamilyManager.Instance.Water,
                FamilyManager.Instance.Medicine
            );
        }*/
    }
}


// 其他UI面板类的占位符

public class EventChoiceUI : UIPanel
{
    public void ShowEvent(RandomEvent eventData) { /* 实现事件显示 */ }
}

public class JournalUI : UIPanel
{
    public void RefreshEntries() { /* 实现日志刷新 */ }
}

public class SettingsUI : UIPanel { }

// 通用UI组件
