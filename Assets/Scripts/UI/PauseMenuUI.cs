using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PauseMenuUI : UIPanel
{
    [Header("暂停菜单组件")]
    public Button resumeButton;
    public Button settingsButton;
    public Button saveGameButton;
    public Button loadGameButton;
    public Button mainMenuButton;
    public Button quitGameButton;
    
    [Header("确认对话框")]
    public GameObject confirmDialog;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    
    private System.Action currentConfirmAction;
    
    public override void Initialize()
    {
        base.Initialize();
        SetupButtons();
        if (confirmDialog) confirmDialog.SetActive(false);
    }
    
    void SetupButtons()
    {
        resumeButton?.onClick.AddListener(Resume);
        settingsButton?.onClick.AddListener(OpenSettings);
        saveGameButton?.onClick.AddListener(SaveGame);
        loadGameButton?.onClick.AddListener(LoadGame);
        mainMenuButton?.onClick.AddListener(() => ConfirmAction("返回主菜单？", ReturnToMainMenu));
        quitGameButton?.onClick.AddListener(() => ConfirmAction("退出游戏？", QuitGame));
        
        confirmYesButton?.onClick.AddListener(ConfirmYes);
        confirmNoButton?.onClick.AddListener(ConfirmNo);
    }
    
    void Resume()
    {
        Hide();
    }
    
    void OpenSettings()
    {
        if (UIManager.Instance?.settingsUI != null)
        {
            UIManager.Instance.settingsUI.Show();
        }
    }
    
    void SaveGame()
    {
        if (SaveManager.Instance)
        {
            SaveManager.Instance.SaveGame(0, $"快速存档_{System.DateTime.Now:HH_mm}");
            UIManager.Instance?.ShowMessage("游戏已保存", 2f);
        }
    }
    
    void LoadGame()
    {
        if (SaveManager.Instance)
        {
            SaveManager.Instance.LoadGame(0);
        }
    }
    
    void ReturnToMainMenu()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.QuitToMainMenu();
        }
    }
    
    void QuitGame()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.QuitGame();
        }
    }
    
    void ConfirmAction(string message, System.Action action)
    {
        if (confirmDialog && confirmText)
        {
            confirmText.text = message;
            confirmDialog.SetActive(true);
            currentConfirmAction = action;
        }
    }
    
    void ConfirmYes()
    {
        currentConfirmAction?.Invoke();
        if (confirmDialog) confirmDialog.SetActive(false);
        currentConfirmAction = null;
    }
    
    void ConfirmNo()
    {
        if (confirmDialog) confirmDialog.SetActive(false);
        currentConfirmAction = null;
    }
}