using UnityEngine;
using System.Collections;

public class BulletTrail : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float duration = 0.1f;
    
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        
        lineRenderer.material = Resources.Load<Material>("BulletTrailMaterial");
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false; // 修复：初始状态为禁用
    }
    
    public void InitializeTrail(Vector3 start, Vector3 end)
    {
        // 修复：添加安全检查
        if (lineRenderer == null) return;
        
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.enabled = true;
        
        StopAllCoroutines(); // 修复：停止之前的协程
        StartCoroutine(FadeOut());
    }
    
    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(duration);
        
        // 修复：检查对象是否仍然有效
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
    
    // 修复：添加重置方法供对象池使用
    public void ResetTrail()
    {
        StopAllCoroutines();
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
}