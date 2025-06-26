using UnityEngine;
using System;

public class SoundManager : Singleton<SoundManager>
{
    public event Action<Vector3, float> OnSoundHeard;

    public void AlertEnemies(Vector3 position, float radius)
    {
        OnSoundHeard?.Invoke(position, radius);
        Debug.Log($"声音传播: 位置{position}, 范围{radius}米");
    }
}