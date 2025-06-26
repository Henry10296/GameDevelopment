using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "GameEvent", menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<GameEventListener> listeners = new();
    
    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            if (listeners[i] != null)
                listeners[i].OnEventRaised();
            else
                listeners.RemoveAt(i);
        }
    }
    
    public void RegisterListener(GameEventListener listener)
    {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }
    
    public void UnregisterListener(GameEventListener listener) => listeners.Remove(listener);
    
    private void OnEnable() => listeners.Clear(); // 编辑器重置
}

[CreateAssetMenu(fileName = "IntGameEvent", menuName = "Events/Int Game Event")]
public class IntGameEvent : ScriptableObject
{
    private readonly List<IntGameEventListener> listeners = new();
    
    public void Raise(int value)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            if (listeners[i] != null)
                listeners[i].OnEventRaised(value);
            else
                listeners.RemoveAt(i);
        }
    }
    
    public void RegisterListener(IntGameEventListener listener)
    {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }
    
    public void UnregisterListener(IntGameEventListener listener) => listeners.Remove(listener);
}

public class GameEventListener : MonoBehaviour
{
    public GameEvent gameEvent;
    public UnityEvent response;
    
    private void OnEnable() => gameEvent?.RegisterListener(this);
    private void OnDisable() => gameEvent?.UnregisterListener(this);
    
    public void OnEventRaised() => response?.Invoke();
}

public class IntGameEventListener : MonoBehaviour
{
    public IntGameEvent gameEvent;
    public UnityEvent<int> response;
    
    private void OnEnable() => gameEvent?.RegisterListener(this);
    private void OnDisable() => gameEvent?.UnregisterListener(this);
    
    public void OnEventRaised(int value) => response?.Invoke(value);
}

