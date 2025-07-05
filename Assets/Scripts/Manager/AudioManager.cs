using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public class AudioManager : Singleton<AudioManager>
{
    [Header("音频配置")]
    public AudioMixerGroup masterMixer;
    public AudioMixerGroup musicMixer;
    public AudioMixerGroup sfxMixer;
    public AudioMixerGroup voiceMixer;
    
    [Header("预加载音频")]
    public AudioClip[] preloadedClips;
    
    [Header("对象池设置")]
    public int audioSourcePoolSize = 20;
    
    private readonly Dictionary<string, AudioClip> audioCache = new();
    private ObjectPool<AudioSource> audioSourcePool;
    private AudioSource musicSource;
    private AudioSource ambientSource;
    
    // 当前播放状态
    private string currentMusicTrack;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private float masterVolume = 1f;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeAudioSystem();
    }
    
    void Start()
    {
        LoadAudioSettings();
        PreloadAudioClips();
    }
    
    void InitializeAudioSystem()
    {
        try
        {
            // 创建音频源对象池
            audioSourcePool = new ObjectPool<AudioSource>(CreatePooledAudioSource, audioSourcePoolSize);
            
            // 创建专用音频源
            musicSource = CreateDedicatedAudioSource("MusicSource", musicMixer);
            ambientSource = CreateDedicatedAudioSource("AmbientSource", sfxMixer);
            
            Debug.Log("[AudioManager] Audio system initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AudioManager] Failed to initialize audio system: {e.Message}");
        }
    }
    
    AudioSource CreatePooledAudioSource()
    {
        GameObject audioObj = new GameObject("PooledAudioSource");
        audioObj.transform.SetParent(transform);
        
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixer;
        source.playOnAwake = false;
        
        return source;
    }
    
    AudioSource CreateDedicatedAudioSource(string name, AudioMixerGroup mixerGroup)
    {
        GameObject audioObj = new GameObject(name);
        audioObj.transform.SetParent(transform);
        
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = mixerGroup;
        source.playOnAwake = false;
        source.loop = true; // 音乐通常需要循环
        
        return source;
    }
    
    void PreloadAudioClips()
    {
        foreach (var clip in preloadedClips)
        {
            if (clip != null)
            {
                audioCache[clip.name] = clip;
            }
        }
        
        Debug.Log($"[AudioManager] Preloaded {audioCache.Count} audio clips");
    }
    
    void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        
        // 安全地设置音量
        SetMasterVolumeSafe(masterVolume);
        SetMusicVolumeSafe(musicVolume);
        SetSFXVolumeSafe(sfxVolume);
    }
    
    // 音效播放方法
    public void PlaySFX(string clipName, Vector3 position = default, float volume = 1f)
    {
        if (audioSourcePool == null)
        {
            Debug.LogWarning("[AudioManager] Audio pool not initialized");
            return;
        }
        
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) return;
        
        AudioSource source = audioSourcePool.Get();
        source.clip = clip;
        source.volume = volume * sfxVolume;
        source.transform.position = position;
        source.pitch = Random.Range(0.95f, 1.05f);
        
        source.Play();
        
        StartCoroutine(ReturnAudioSourceAfterPlay(source, clip.length));
    }
    
    // 音乐播放方法 - 修复版本
    public void PlayMusic(string clipName, bool loop = true, float fadeTime = 2f)
    {
        if (musicSource == null)
        {
            Debug.LogWarning("[AudioManager] Music source not initialized");
            return;
        }
        
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) 
        {
            Debug.LogWarning($"[AudioManager] Music clip '{clipName}' not found, skipping");
            return;
        }
        
        StartCoroutine(FadeToNewMusic(clip, loop, fadeTime));
        currentMusicTrack = clipName;
    }
    
    IEnumerator FadeToNewMusic(AudioClip newClip, bool loop, float fadeTime)
    {
        // 淡出当前音乐
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeOutMusic(fadeTime * 0.5f));
        }
        
        // 切换到新音乐
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();
        
        // 淡入新音乐
        yield return StartCoroutine(FadeInMusic(fadeTime * 0.5f));
    }
    
    IEnumerator FadeOutMusic(float fadeTime)
    {
        float startVolume = musicSource.volume;
        
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        
        musicSource.volume = 0f;
        musicSource.Stop();
    }
    
    IEnumerator FadeInMusic(float fadeTime)
    {
        float targetVolume = musicVolume;
        
        while (musicSource.volume < targetVolume)
        {
            musicSource.volume += targetVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        
        musicSource.volume = targetVolume;
    }
    
    // 音量控制方法 - 安全版本
    public void SetMasterVolumeSafe(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        if (masterMixer?.audioMixer != null)
        {
            masterMixer.audioMixer.SetFloat("MasterVolume", LinearToDecibel(volume));
        }
        else
        {
            AudioListener.volume = masterVolume;
        }
        
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }
    
    public void SetMusicVolumeSafe(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        
        if (musicMixer?.audioMixer != null)
        {
            musicMixer.audioMixer.SetFloat("MusicVolume", LinearToDecibel(volume));
        }
        
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
    
    public void SetSFXVolumeSafe(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        
        if (sfxMixer?.audioMixer != null)
        {
            sfxMixer.audioMixer.SetFloat("SFXVolume", LinearToDecibel(volume));
        }
        
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }
    
    // 旧方法保持兼容性
    public void SetMasterVolume(float volume) => SetMasterVolumeSafe(volume);
    public void SetMusicVolume(float volume) => SetMusicVolumeSafe(volume);
    public void SetSFXVolume(float volume) => SetSFXVolumeSafe(volume);
    
    float LinearToDecibel(float linear)
    {
        return linear > 0 ? 20f * Mathf.Log10(linear) : -80f;
    }
    
    // 获取音频剪辑 - 改进版本
    AudioClip GetAudioClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("[AudioManager] Empty clip name provided");
            return null;
        }
        
        if (audioCache.TryGetValue(clipName, out AudioClip cachedClip))
        {
            return cachedClip;
        }
        
        // 尝试从Resources加载
        AudioClip loadedClip = Resources.Load<AudioClip>($"Audio/{clipName}");
        if (loadedClip != null)
        {
            audioCache[clipName] = loadedClip;
            return loadedClip;
        }
        
        Debug.LogWarning($"[AudioManager] Audio clip not found: {clipName}");
        return null;
    }
    
    IEnumerator ReturnAudioSourceAfterPlay(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.volume = 1f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            
            audioSourcePool?.Return(source);
        }
    }
    
    // 动态音乐系统 - 安全版本
    public void SetMusicForGamePhase(GamePhase phase)
    {
        string musicTrack = phase switch
        {
            GamePhase.Home => "HomeBGM",
            GamePhase.Exploration => "ExplorationBGM",
            GamePhase.EventProcessing => "EventBGM",
            GamePhase.GameEnd => "EndingBGM",
            _ => "MenuBGM"
        };
        
        if (currentMusicTrack != musicTrack)
        {
            PlayMusic(musicTrack);
        }
    }
    
    // 应用退出时清理
    protected override void OnSingletonApplicationQuit()
    {
        PlayerPrefs.Save();
        base.OnSingletonApplicationQuit();
    }
}