using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    public static SceneTransitionManager Instance;
    
    [Header("转换效果")]
    public Image fadeImage;
    public float fadeSpeed = 1f;
    public bool fadeOnStart = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (fadeOnStart)
        {
            FadeIn();
        }
    }
    
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }
    
    IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // 淡出
        yield return StartCoroutine(FadeOutCoroutine());
        
        // 加载场景
        SceneManager.LoadScene(sceneName);
        
        // 淡入
        yield return StartCoroutine(FadeInCoroutine());
    }
    
    public void FadeIn()
    {
        StartCoroutine(FadeInCoroutine());
    }
    
    public void FadeOut()
    {
        StartCoroutine(FadeOutCoroutine());
    }
    
    IEnumerator FadeInCoroutine()
    {
        if (fadeImage == null) yield break;
        
        float alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= fadeSpeed * Time.deltaTime;
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(0f);
    }
    
    IEnumerator FadeOutCoroutine()
    {
        if (fadeImage == null) yield break;
        
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += fadeSpeed * Time.deltaTime;
            SetFadeAlpha(alpha);
            yield return null;
        }
        SetFadeAlpha(1f);
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