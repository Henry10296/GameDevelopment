using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
    
    public void InitializeTrail(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.enabled = true;
        
        StartCoroutine(FadeOut());
    }
    
    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(duration);
        lineRenderer.enabled = false;
    }
}