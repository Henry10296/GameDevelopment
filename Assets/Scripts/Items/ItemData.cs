using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基础信息")]
    public string itemName;
    public Sprite icon;
    public ItemType itemType;

    [Header("堆叠设置")]
    public int maxStackSize = 99;

    [Header("数值")]
    public int value = 1; // 回复量或价值

    [Header("描述")]
    [TextArea(3, 5)]
    public string description;
}

