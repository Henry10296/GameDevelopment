using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UITextSettings", menuName = "Game/UI Text Settings")]
public class UITextSettings : ScriptableObject
{
    [System.Serializable]
    public class TextEntry
    {
        public string key;
        [TextArea(1, 3)]
        public string text;
    }
    
    [Header("交互提示文本")]
    public TextEntry[] interactionTexts = new TextEntry[]
    {
        new() { key = "PICKUP_PROMPT", text = "按 {0} 拾取" },
        new() { key = "INTERACT_PROMPT", text = "按 {0} 交互" },
        new() { key = "USE_PROMPT", text = "按 {0} 使用" }
    };
    
    [Header("系统消息文本")]
    public TextEntry[] systemMessages = new TextEntry[]
    {
        new() { key = "INVENTORY_FULL", text = "背包已满!" },
        new() { key = "TIME_WARNING", text = "时间不多了！赶紧回家！" },
        new() { key = "RADIO_FOUND", text = "找到了无线电设备!" },
        new() { key = "GAME_SAVED", text = "游戏已保存" },
        new() { key = "GAME_LOADED", text = "游戏已加载" },
        new() { key = "NO_AMMO", text = "没有弹药了!" }
    };
    
    [Header("成就文本")]
    public TextEntry[] achievementTexts = new TextEntry[]
    {
        new() { key = "RADIO_FINDER", text = "无线电专家" },
        new() { key = "COLLECTOR", text = "收集者" },
        new() { key = "SURVIVOR_3_DAYS", text = "三日生存者" }
    };
    
    private Dictionary<string, string> textDict;
    
    void OnEnable()
    {
        BuildTextDictionary();
    }
    
    void BuildTextDictionary()
    {
        textDict = new Dictionary<string, string>();
        
        AddTextsToDict(interactionTexts);
        AddTextsToDict(systemMessages);
        AddTextsToDict(achievementTexts);
    }
    
    void AddTextsToDict(TextEntry[] entries)
    {
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.key))
                textDict[entry.key] = entry.text;
        }
    }
    
    public string GetText(string key, params object[] args)
    {
        if (textDict == null) BuildTextDictionary();
        
        if (textDict.TryGetValue(key, out string text))
        {
            return args.Length > 0 ? string.Format(text, args) : text;
        }
        
        return $"[Missing: {key}]";
    }
}
