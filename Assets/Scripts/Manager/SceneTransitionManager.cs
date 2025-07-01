using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    [Header("转换效果")]
    public Image fadeImage;
    public float fadeSpeed = 1f;
    public bool fadeOnStart = true;
    
    private Coroutine currentTransition; // 修复：跟踪当前转换
    
    void Start()
    {
        if (fadeOnStart)
        {
            FadeIn();
        }
    }
    
    public void LoadSceneWithFade(string sceneName)
    {
        // 修复：停止之前的转换
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        currentTransition = StartCoroutine(LoadSceneCoroutine(sceneName));
    }
    
    IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return StartCoroutine(FadeOutCoroutine());
        
        // 修复：检查是否仍然有效
        if (this != null && gameObject != null)
        {
            SceneManager.LoadScene(sceneName);
        }
        
        currentTransition = null; // 清理引用
    }
    
    public void FadeIn()
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(FadeInCoroutine());
    }
    
    public void FadeOut()
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(FadeOutCoroutine());
    }
    
    IEnumerator FadeInCoroutine()
    {
        if (fadeImage == null) yield break;
        
        float alpha = 1f;
        while (alpha > 0f && this != null)
        {
            alpha -= fadeSpeed * Time.deltaTime;
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(0f);
        currentTransition = null;
    }
    
    IEnumerator FadeOutCoroutine()
    {
        if (fadeImage == null) yield break;
        
        float alpha = 0f;
        while (alpha < 1f && this != null)
        {
            alpha += fadeSpeed * Time.deltaTime;
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(1f);
        currentTransition = null;
    }
    
    void SetFadeAlpha(float alpha)
    {
        if (fadeImage)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}