using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UIInputSettings
{
    [Header("UI快捷键")]
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode journalKey = KeyCode.J;
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode interactionKey = KeyCode.F;
    public KeyCode reloadKey = KeyCode.R;
    
    [Header("调试快捷键")]
    public KeyCode debugNextDay = KeyCode.F1;
    public KeyCode debugEndGame = KeyCode.F2;
    public KeyCode debugAddResources = KeyCode.F3;
    public KeyCode debugFindRadio = KeyCode.F4;
}

// ============ UI状态管理器 ============
public class UIStateManager : MonoBehaviour
{
    public static UIStateManager Instance { get; private set; }
    
    [Header("UI状态")]
    public bool isAnyUIOpen = false;
    public string currentOpenUI = "";
    
    private Dictionary<string, IUIPanel> registeredPanels = new Dictionary<string, IUIPanel>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterPanel(string name, IUIPanel panel)
    {
        registeredPanels[name] = panel;
    }
    
    public void OnPanelOpened(string panelName)
    {
        isAnyUIOpen = true;
        currentOpenUI = panelName;
        
        // 关闭其他面板
        foreach (var kvp in registeredPanels)
        {
            if (kvp.Key != panelName && kvp.Value.IsVisible())
            {
                kvp.Value.Hide();
            }
        }
    }
    
    public void OnPanelClosed(string panelName)
    {
        if (currentOpenUI == panelName)
        {
            isAnyUIOpen = false;
            currentOpenUI = "";
        }
    }
    
    public bool IsUIOpen() => isAnyUIOpen;
    public string GetCurrentUI() => currentOpenUI;
}