using UnityEngine;

[CreateAssetMenu(fileName = "InputSettings", menuName = "Game/Input Settings")]
public class InputSettings : ScriptableObject
{
    [Header("交互按键")]
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode useItemKey = KeyCode.E;
    
    [Header("UI按键")]
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode journalKey = KeyCode.J;
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Header("武器按键")]
    public KeyCode reloadKey = KeyCode.R;
    public KeyCode weapon1Key = KeyCode.Alpha1;
    public KeyCode weapon2Key = KeyCode.Alpha2;
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode aimKey = KeyCode.Mouse1;
    
    [Header("移动按键")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode jumpKey = KeyCode.Space;
    
    [Header("调试按键")]
    public KeyCode debugNextDay = KeyCode.F1;
    public KeyCode debugEndGame = KeyCode.F2;
    public KeyCode debugAddResources = KeyCode.F3;
    public KeyCode debugFindRadio = KeyCode.F4;
}