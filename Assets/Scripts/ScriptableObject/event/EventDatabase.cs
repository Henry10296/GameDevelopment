using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EventDatabase", menuName = "Game/Event Database")]
public class EventDatabase : ScriptableObject
{
    [Header("所有事件")]
    public RandomEvent[] allEvents;
    
    [Header("分类事件")]
    public RandomEvent[] dailyEvents;        // 日常事件
    public RandomEvent[] emergencyEvents;    // 紧急事件
    public RandomEvent[] storyEvents;        // 剧情事件
    public RandomEvent[] resourceEvents;     // 资源相关事件
    
    public RandomEvent[] GetEventsForDay(int day)
    {
        return allEvents.Where(e => e.minDay <= day && day <= e.maxDay).ToArray();
    }
    
    public RandomEvent[] GetEventsByType(GameEventType eventType)
    {
        // 这里需要在RandomEvent中添加eventType字段
        // 暂时返回所有事件
        return allEvents;
    }
}