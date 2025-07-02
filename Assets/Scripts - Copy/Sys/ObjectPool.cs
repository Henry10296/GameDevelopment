using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly Stack<T> pool = new Stack<T>();
    private readonly Func<T> createFunc;
    private readonly int maxSize;
    
    public ObjectPool(Func<T> createFunc, int maxSize = 100)
    {
        this.createFunc = createFunc;
        this.maxSize = maxSize;
    }
    
    public T Get()
    {
        if (pool.Count > 0)
        {
            var item = pool.Pop();
            item.gameObject.SetActive(true);
            return item;
        }
        
        return createFunc();
    }
    
    public void Return(T item)
    {
        if (item == null) return;
        
        item.gameObject.SetActive(false);
        
        if (pool.Count < maxSize)
            pool.Push(item);
        else
            GameObject.Destroy(item.gameObject);
    }
}