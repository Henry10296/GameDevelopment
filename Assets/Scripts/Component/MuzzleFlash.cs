using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            StartCoroutine(DisableLightAfterDelay(0.05f));
        }
        
        if (particles)
        {
            particles.Play();
        }
    }
    
    IEnumerator DisableLightAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        flashLight.enabled = false;
    }
}
