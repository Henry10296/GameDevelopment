using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// UI快速开发工具
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class UIBindingData
{
    public string propertyName;
    public UnityEngine.Object targetComponent;
    public string componentProperty;
}

// UI自动绑定工具
public class UIAutoBinding : MonoBehaviour
{
    [Header("自动绑定设置")]
    public UIBindingData[] bindings;
    
    [Header("调试")]
    public bool showDebugInfo = true;
    
    void Start()
    {
        foreach (var binding in bindings)
        {
            AutoBind(binding);
        }
    }
    
    void AutoBind(UIBindingData binding)
    {
        // 自动绑定逻辑
        if (showDebugInfo)
        {
            Debug.Log($"绑定 {binding.propertyName} 到 {binding.targetComponent}");
        }
    }
}

// UI快速生成器

// 编辑器工具
public class UIQuickTools : EditorWindow
{
    private Vector2 scrollPos;
    private UITemplate selectedTemplate;
    
    [MenuItem("Game Tools/UI Quick Tools")]
    public static void OpenWindow()
    {
        GetWindow<UIQuickTools>("UI快速工具").Show();
    }
    
    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Label("UI快速生成", EditorStyles.boldLabel);
        
        selectedTemplate = EditorGUILayout.ObjectField("UI模板", selectedTemplate, typeof(UITemplate), false) as UITemplate;
        
        if (GUILayout.Button("生成UI") && selectedTemplate != null)
        {
            GenerateUI();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("快速绑定工具", EditorStyles.boldLabel);
        
        if (GUILayout.Button("自动查找UI组件"))
        {
            AutoFindUIComponents();
        }
        
        if (GUILayout.Button("生成UI数据绑定代码"))
        {
            GenerateDataBindingCode();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void GenerateUI()
    {
        GameObject uiObject = Instantiate(selectedTemplate.uiPrefab);
        uiObject.name = selectedTemplate.templateName;
        
        // 添加必需组件
        foreach (string componentName in selectedTemplate.requiredComponents)
        {
            System.Type componentType = System.Type.GetType(componentName);
            if (componentType != null && uiObject.GetComponent(componentType) == null)
            {
                uiObject.AddComponent(componentType);
            }
        }
        
        Selection.activeGameObject = uiObject;
        EditorGUIUtility.PingObject(uiObject);
    }
    
    void AutoFindUIComponents()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;
        
        // 自动查找并绑定UI组件
        var buttons = selected.GetComponentsInChildren<UnityEngine.UI.Button>();
        var texts = selected.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        var images = selected.GetComponentsInChildren<UnityEngine.UI.Image>();
        
        Debug.Log($"找到 {buttons.Length} 个按钮, {texts.Length} 个文本, {images.Length} 个图片");
        
        // 生成绑定代码建议
        string bindingCode = GenerateBindingCodeSuggestion(buttons, texts, images);
        Debug.Log("建议的绑定代码:\n" + bindingCode);
    }
    
    string GenerateBindingCodeSuggestion(UnityEngine.UI.Button[] buttons, TMPro.TextMeshProUGUI[] texts, UnityEngine.UI.Image[] images)
    {
        string code = "[Header(\"UI组件\")]\n";
        
        foreach (var button in buttons)
        {
            string varName = button.name.ToLower().Replace(" ", "").Replace("(", "").Replace(")", "") + "Button";
            code += $"public Button {varName};\n";
        }
        
        foreach (var text in texts)
        {
            string varName = text.name.ToLower().Replace(" ", "").Replace("(", "").Replace(")", "") + "Text";
            code += $"public TextMeshProUGUI {varName};\n";
        }
        
        return code;
    }
    
    void GenerateDataBindingCode()
    {
        // 生成数据绑定相关代码
        string template = @"
// 自动生成的数据绑定代码
public class {0}DataBinding : MonoBehaviour
{
    [Header(""数据源"")]
    public {1} dataSource;
    
    [Header(""UI组件"")]
    // TODO: 添加UI组件引用
    
    void Start()
    {
        BindData();
    }
    
    public void BindData()
    {
        if (dataSource == null) return;
        // TODO: 实现数据绑定逻辑
    }
}";
        
        Debug.Log("数据绑定代码模板:\n" + template);
    }
}

// UI数据绑定组件
public class UIDataBinder : MonoBehaviour
{
    [System.Serializable]
    public class DataBinding
    {
        public string dataPath;
        public UnityEngine.Object uiComponent;
        public string uiProperty;
        public bool autoUpdate = true;
    }
    
    [Header("数据绑定")]
    public DataBinding[] bindings;
    
    [Header("更新设置")]
    public float updateInterval = 0.1f;
    public bool updateOnStart = true;
    
    void Start()
    {
        if (updateOnStart)
        {
            UpdateAllBindings();
        }
        
        if (updateInterval > 0)
        {
            InvokeRepeating(nameof(UpdateAllBindings), 0f, updateInterval);
        }
    }
    
    public void UpdateAllBindings()
    {
        foreach (var binding in bindings)
        {
            if (binding.autoUpdate)
            {
                UpdateBinding(binding);
            }
        }
    }
    
    void UpdateBinding(DataBinding binding)
    {
        // 实现具体的数据绑定逻辑
        object value = GetValueFromPath(binding.dataPath);
        SetUIValue(binding.uiComponent, binding.uiProperty, value);
    }
    
    object GetValueFromPath(string path)
    {
        // 解析数据路径并获取值
        string[] parts = path.Split('.');
        
        // 示例：FamilyManager.Food
        if (parts.Length >= 2)
        {
            switch (parts[0])
            {
                case "FamilyManager":
                    return GetFamilyManagerValue(parts[1]);
                case "GameManager":
                    return GetGameManagerValue(parts[1]);
                // 添加更多管理器
            }
        }
        
        return null;
    }
    
    object GetFamilyManagerValue(string property)
    {
        if (FamilyManager.Instance == null) return null;
        
        return property switch
        {
            "Food" => FamilyManager.Instance.Food,
            "Water" => FamilyManager.Instance.Water,
            "Medicine" => FamilyManager.Instance.Medicine,
            "AliveMembers" => FamilyManager.Instance.AliveMembers,
            _ => null
        };
    }
    
    object GetGameManagerValue(string property)
    {
        if (GameManager.Instance == null) return null;
        
        return property switch
        {
            "CurrentDay" => GameManager.Instance.CurrentDay,
            "CurrentPhase" => GameManager.Instance.CurrentPhase.ToString(),
            _ => null
        };
    }
    
    void SetUIValue(UnityEngine.Object component, string property, object value)
    {
        if (component == null || value == null) return;
        
        switch (component)
        {
            case TMPro.TextMeshProUGUI text when property == "text":
                text.text = value.ToString();
                break;
            case UnityEngine.UI.Slider slider when property == "value":
                if (value is float f) slider.value = f;
                else if (value is int i) slider.value = i;
                break;
            case UnityEngine.UI.Image image when property == "fillAmount":
                if (value is float f2) image.fillAmount = f2;
                break;
        }
    }
}
#endif
