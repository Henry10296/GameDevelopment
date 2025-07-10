// 修改后的InputSettings.cs - 整合所有输入设置
using UnityEngine;

[CreateAssetMenu(fileName = "InputSettings", menuName = "Game/Input Settings")]
public class InputSettings : BaseGameConfig
{
    [Header("移动控制")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode leanLeftKey = KeyCode.Q;
    public KeyCode leanRightKey = KeyCode.E;
    
    [Header("交互控制")]
    public KeyCode interactionKey = KeyCode.F;
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode useItemKey = KeyCode.E;
    
    [Header("武器控制")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode aimKey = KeyCode.Mouse1;
    public KeyCode reloadKey = KeyCode.R;
    public KeyCode weapon1Key = KeyCode.Alpha1;
    public KeyCode weapon2Key = KeyCode.Alpha2;
    
    [Header("UI控制")]
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode journalKey = KeyCode.J;
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Header("鼠标设置")]
    [Range(0.1f, 10f)] public float mouseSensitivity = 2f;
    [Range(0.1f, 10f)] public float aimSensitivity = 1f;
    public bool invertMouseY = false;
    
    [Header("调试按键")]
    public KeyCode debugNextDay = KeyCode.F1;
    public KeyCode debugEndGame = KeyCode.F2;
    public KeyCode debugAddResources = KeyCode.F3;
    public KeyCode debugFindRadio = KeyCode.F4;
    
    public override bool ValidateConfig()
    {
        bool isValid = base.ValidateConfig();
        
        // 检查按键冲突
        var keyMappings = new[]
        {
            ("Interaction", interactionKey),
            ("Pickup", pickupKey),
            ("UseItem", useItemKey),
            ("Run", runKey),
            ("Crouch", crouchKey),
            ("Jump", jumpKey)
        };
        
        for (int i = 0; i < keyMappings.Length; i++)
        {
            for (int j = i + 1; j < keyMappings.Length; j++)
            {
                if (keyMappings[i].Item2 == keyMappings[j].Item2)
                {
                    Debug.LogWarning($"[InputSettings] Key conflict: {keyMappings[i].Item1} and {keyMappings[j].Item1} both use {keyMappings[i].Item2}");
                }
            }
        }
        
        return isValid;
    }
}