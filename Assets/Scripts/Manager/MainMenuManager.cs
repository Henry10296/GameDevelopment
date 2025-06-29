using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : Singleton<MainMenuManager>
{
    [Header("UI组件")]
    public Button newGameButton;
    public Button continueButton;
    public Button settingsButton;
    public Button exitButton;
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    
    [Header("游戏信息")]
    public TextMeshProUGUI versionText;
    
    void Start()
    {
        SetupButtons();
        LoadSettings();
        
        if (versionText)
            versionText.text = $"v{Application.version}";
        if (settingsPanel) settingsPanel.SetActive(false);
    }
    
    void SetupButtons()
    {
        if (newGameButton) newGameButton.onClick.AddListener(StartNewGame);
        if (continueButton) continueButton.onClick.AddListener(ContinueGame);
        if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);
        
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        
        // 检查是否有存档
        bool hasSaveGame = PlayerPrefs.HasKey("SaveGame");
        if (continueButton) continueButton.interactable = hasSaveGame;
    }
    
    void StartNewGame()
    {
        // 清除旧存档
        PlayerPrefs.DeleteKey("SaveGame");
        
        // 开始新游戏
        GameManager.Instance.StartNewGame();
 
    }
    
    void ContinueGame()//无法加载
    {
        // 加载存档
        if (PlayerPrefs.HasKey("SaveGame"))
        {
            // 这里可以实现存档加载逻辑
            GameManager.Instance.ChangePhase(GamePhase.Home);
            GameManager.Instance.LoadScene("1_Home");
        }
    }
    
    void OpenSettings()//打开设置
    {
        if (settingsPanel)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
    
    void ExitGame()//退出游戏
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnVolumeChanged(float value)//改音量
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
    }
    
    void OnFullscreenChanged(bool isFullscreen)//屏幕
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }
    
    void LoadSettings()//加载
    {
        // 加载音量设置
        float volume = PlayerPrefs.GetFloat("Volume", 1f);
        AudioListener.volume = volume;
        if (volumeSlider) volumeSlider.value = volume;
        
        // 加载全屏设置
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = fullscreen;
        if (fullscreenToggle) fullscreenToggle.isOn = fullscreen;
    }
}