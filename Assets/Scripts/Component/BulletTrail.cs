using UnityEngine;
using System.Collections;

public class BulletTrail : MonoBehaviour
{
    [Header("配置")]
    public float duration = 0.1f;
    public string materialPath = "BulletTrailMaterial";
    public float startWidth = 0.02f;
    public float endWidth = 0.01f;
    
    private LineRenderer lineRenderer;
    private Coroutine fadeCoroutine;
    private bool isInitialized = false;
    
    void Awake()
    {
        InitializeLineRenderer();
    }
    
    void InitializeLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // 安全加载材质
        Material trailMaterial = null;
        if (!string.IsNullOrEmpty(materialPath))
        {
            trailMaterial = Resources.Load<Material>(materialPath);
            if (trailMaterial == null)
            {
                Debug.LogWarning($"[BulletTrail] Material not found at path: {materialPath}. Using default material.");
            }
        }
        
        // 配置LineRenderer
        lineRenderer.material = trailMaterial;
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;
        
        // 设置其他默认属性
        lineRenderer.sortingOrder = 100; // 确保在前景
        
        isInitialized = true;
    }
    
    public void InitializeTrail(Vector3 start, Vector3 end)
    {
        if (!isInitialized || lineRenderer == null) 
        {
            Debug.LogError("[BulletTrail] LineRenderer not initialized!");
            return;
        }
        
        // 停止之前的协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        // 设置轨迹位置
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.enabled = true;
        
        // 开始淡出
        fadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }
    
    IEnumerator FadeOutCoroutine()
    {
        yield return new WaitForSeconds(duration);
        
        // 检查对象是否仍然有效
        if (this != null && lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        fadeCoroutine = null;
    }
    
    public void ResetTrail()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
    
    void OnDestroy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }
    
    // 对象池支持
    public void OnReturnToPool()
    {
        ResetTrail();
    }
    
    public void OnGetFromPool()
    {
        if (!isInitialized)
        {
            InitializeLineRenderer();
        }
    }
}