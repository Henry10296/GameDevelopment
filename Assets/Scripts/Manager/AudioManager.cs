using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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
        // 创建音频源对象池
        audioSourcePool = new ObjectPool<AudioSource>(CreatePooledAudioSource, audioSourcePoolSize);
        
        // 创建专用音频源
        musicSource = CreateDedicatedAudioSource("MusicSource", musicMixer);
        ambientSource = CreateDedicatedAudioSource("AmbientSource", sfxMixer);
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
        
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
    }
    
    // 音效播放方法
    public void PlaySFX(string clipName, Vector3 position = default, float volume = 1f)
    {
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) return;
        
        AudioSource source = audioSourcePool.Get();
        source.clip = clip;
        source.volume = volume * sfxVolume;
        source.transform.position = position;
        source.pitch = Random.Range(0.95f, 1.05f); // 轻微音调变化
        
        source.Play();
        
        StartCoroutine(ReturnAudioSourceAfterPlay(source, clip.length));
    }
    
    public void PlaySFX(AudioClip clip, Vector3 position = default, float volume = 1f)
    {
        if (clip == null) return;
        
        AudioSource source = audioSourcePool.Get();
        source.clip = clip;
        source.volume = volume * sfxVolume;
        source.transform.position = position;
        source.pitch = Random.Range(0.95f, 1.05f);
        
        source.Play();
        
        StartCoroutine(ReturnAudioSourceAfterPlay(source, clip.length));
    }
    
    // 音乐播放方法
    public void PlayMusic(string clipName, bool loop = true, float fadeTime = 2f)
    {
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) return;
        
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
    
    // 环境音效
    public void PlayAmbientSound(string clipName, bool loop = true, float volume = 0.5f)
    {
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) return;
        
        ambientSource.clip = clip;
        ambientSource.loop = loop;
        ambientSource.volume = volume * sfxVolume;
        ambientSource.Play();
    }
    
    public void StopAmbientSound(float fadeTime = 1f)
    {
        if (ambientSource.isPlaying)
        {
            StartCoroutine(FadeOutAmbient(fadeTime));
        }
    }
    
    IEnumerator FadeOutAmbient(float fadeTime)
    {
        float startVolume = ambientSource.volume;
        
        while (ambientSource.volume > 0)
        {
            ambientSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        
        ambientSource.volume = 0f;
        ambientSource.Stop();
    }
    
    // 3D音效播放
    public void Play3DSFX(string clipName, Vector3 position, float maxDistance = 20f, float volume = 1f)
    {
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) return;
        
        AudioSource source = audioSourcePool.Get();
        source.clip = clip;
        source.volume = volume * sfxVolume;
        source.transform.position = position;
        
        // 3D音频设置
        source.spatialBlend = 1f; // 完全3D
        source.rolloffMode = AudioRolloffMode.Linear;
        source.maxDistance = maxDistance;
        source.minDistance = 1f;
        
        source.Play();
        
        StartCoroutine(ReturnAudioSourceAfterPlay(source, clip.length));
    }
    
    // 音量控制方法
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        masterMixer.audioMixer.SetFloat("MasterVolume", LinearToDecibel(volume));
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
        musicMixer.audioMixer.SetFloat("MusicVolume", LinearToDecibel(volume));
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxMixer.audioMixer.SetFloat("SFXVolume", LinearToDecibel(volume));
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }
    
    float LinearToDecibel(float linear)
    {
        return linear > 0 ? 20f * Mathf.Log10(linear) : -80f;
    }
    
    // 获取音频剪辑
    AudioClip GetAudioClip(string clipName)
    {
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
        
        source.Stop();
        source.clip = null;
        source.volume = 1f;
        source.pitch = 1f;
        source.spatialBlend = 0f; // 重置为2D
        
        audioSourcePool.Return(source);
    }
    
    // 动态音乐系统
    public void SetMusicForGamePhase(GamePhase phase)
    {
        string musicTrack = phase switch
        {
            GamePhase.HomeManagement => "HomeBGM",
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
    
    // 紧张度系统
    public void SetTensionLevel(float tensionLevel)
    {
        tensionLevel = Mathf.Clamp01(tensionLevel);
        
        // 根据紧张度调整音乐
        if (tensionLevel > 0.8f)
        {
            PlayMusic("HighTensionBGM");
        }
        else if (tensionLevel > 0.5f)
        {
            PlayMusic("MediumTensionBGM");
        }
        else
        {
            PlayMusic("LowTensionBGM");
        }
    }
    
    // 调试方法
    [ContextMenu("Test All Audio")]
    public void DebugTestAllAudio()
    {
        foreach (var clip in audioCache.Values)
        {
            PlaySFX(clip, transform.position, 0.5f);
        }
    }
    
    protected override void OnDestroy()
    {
        PlayerPrefs.Save();
        base.OnDestroy();
    }
}