using UnityEngine;
using System.Collections;

public class MuzzleFlash : MonoBehaviour
{
    private Light flashLight;
    private ParticleSystem particles;
    
    void Awake()
    {
        flashLight = GetComponent<Light>();
        if (flashLight == null)
            flashLight = gameObject.AddComponent<Light>();
        
        flashLight.type = LightType.Point;
        flashLight.color = Color.yellow;
        flashLight.intensity = 2f;
        flashLight.range = 5f;
        flashLight.enabled = false;
        
        particles = GetComponent<ParticleSystem>();
    }
    
    public void PlayEffect()
    {
        if (flashLight)
        {
            flashLight.enabled = true;
            StopAllCoroutines(); // 修复：停止之前的协程
            StartCoroutine(DisableLightAfterDelay(0.05f));
        }
        
        if (particles)
        {
            particles.Stop(); // 修复：先停止再播放
            particles.Play();
        }
    }
    
    IEnumerator DisableLightAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 修复：检查对象是否仍然有效
        if (flashLight != null)
            flashLight.enabled = false;
    }
    
    // 修复：添加重置方法供对象池使用
    public void ResetEffect()
    {
        StopAllCoroutines();
        if (flashLight != null)
            flashLight.enabled = false;
        if (particles != null)
            particles.Stop();
    }
}