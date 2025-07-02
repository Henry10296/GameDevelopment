using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UITemplate", menuName = "UI/UI Template")]
public class UITemplate : ScriptableObject
{
    [Header("模板信息")]
    public string templateName;
    public GameObject uiPrefab;
    
    [Header("自动生成设置")]
    public string[] requiredComponents;
    public Vector2 defaultSize = new Vector2(400, 300);
}

