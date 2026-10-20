using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject audioManagerObj = new GameObject("AudioManager");
                _instance = audioManagerObj.AddComponent<AudioManager>();
                DontDestroyOnLoad(audioManagerObj);
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        public bool loop = false;

        [HideInInspector]
        public AudioSource source;
    }

    [Header("音效设置")]
    public SoundEffect[] soundEffects;

    [Header("脚步声设置")]
    public AudioClip[] footstepSounds;
    [Range(0f, 1f)]
    public float footstepVolume = 0.5f;

    [Header("传送音效设置")]
    public AudioClip teleportSound;
    [Range(0f, 1f)]
    public float teleportVolume = 0.8f;

    [Header("交互提示音效设置")]
    public AudioClip interactionPromptSound;
    [Range(0f, 1f)]
    public float interactionPromptVolume = 0.5f;

    private Dictionary<string, AudioSource> soundDict = new Dictionary<string, AudioSource>();
    private AudioSource footstepSource;
    private AudioSource teleportSource;
    private AudioSource interactionSource;

    private void Awake()
    {
        // 单例模式检查
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化所有音效
        InitializeSounds();
    }

    private void InitializeSounds()
    {
        // 初始化普通音效
        foreach (SoundEffect sound in soundEffects)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = sound.clip;
            source.volume = sound.volume;
            source.pitch = sound.pitch;
            source.loop = sound.loop;
            source.playOnAwake = false;

            sound.source = source;
            soundDict[sound.name] = source;
        }

        // 初始化脚步声音效
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.volume = footstepVolume;
        footstepSource.playOnAwake = false;

        // 初始化传送音效
        teleportSource = gameObject.AddComponent<AudioSource>();
        teleportSource.clip = teleportSound;
        teleportSource.volume = teleportVolume;
        teleportSource.playOnAwake = false;

        // 初始化交互提示音效
        interactionSource = gameObject.AddComponent<AudioSource>();
        interactionSource.clip = interactionPromptSound;
        interactionSource.volume = interactionPromptVolume;
        interactionSource.playOnAwake = false;
    }

    // 播放普通音效
    public void PlaySound(string name)
    {
        if (soundDict.TryGetValue(name, out AudioSource source))
        {
            source.Play();
        }
        else
        {
            Debug.LogWarning($"音效 {name} 不存在!");
        }
    }

    // 停止普通音效
    public void StopSound(string name)
    {
        if (soundDict.TryGetValue(name, out AudioSource source))
        {
            source.Stop();
        }
    }

    // 播放随机脚步声
    public void PlayFootstep()
    {
        if (footstepSounds.Length == 0) return;

        int index = Random.Range(0, footstepSounds.Length);
        footstepSource.clip = footstepSounds[index];
        footstepSource.Play();
    }

    // 播放传送音效
    public void PlayTeleportSound()
    {
        if (teleportSound != null)
        {
            teleportSource.Play();
        }
    }

    // 播放交互提示音效
    public void PlayInteractionPromptSound()
    {
        if (interactionPromptSound != null)
        {
            interactionSource.Play();
        }
    }
}
