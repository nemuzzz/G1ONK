using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CombatAudioManager : MonoBehaviour
{
    private static CombatAudioManager _instance;
    public static CombatAudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CombatAudioManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("CombatAudioManager");
                    _instance = obj.AddComponent<CombatAudioManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    [Header("战斗音效")]
    [SerializeField] private AudioClip shootSound; // 发射子弹音效
    [SerializeField] private AudioClip playerHitSound; // 玩家受击音效
    [SerializeField] private AudioClip enemyDeathSound; // 敌人死亡音效
    [SerializeField] private AudioClip arrowInputSound; // 输入Arrow指令音效
    [SerializeField] private AudioClip wrongInputSound; // 输入错误音效
    [SerializeField] private AudioClip bulletAddedSound; // 子弹增加音效
    [SerializeField] private AudioClip gameOverSound; // 游戏失败音效
    [SerializeField] private AudioClip victorySound; // 游戏胜利音效
    [SerializeField] private AudioClip buttonClickSound; // 按钮点击音效

    [Header("音量设置")]
    [Range(0f, 1f)]
    [SerializeField] private float shootVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float playerHitVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float enemyDeathVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float arrowInputVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float wrongInputVolume = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float bulletAddedVolume = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float gameOverVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float victoryVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float buttonClickVolume = 0.5f;

    private AudioSource _audioSource;
    private Dictionary<string, AudioSource> _audioSources = new Dictionary<string, AudioSource>();

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

        // 初始化主音频源
        _audioSource = gameObject.AddComponent<AudioSource>();

        // 创建多个音频源以便同时播放多个音效
        CreateAudioSource("shoot");
        CreateAudioSource("playerHit");
        CreateAudioSource("enemyDeath");
        CreateAudioSource("arrowInput");
        CreateAudioSource("wrongInput");
        CreateAudioSource("bulletAdded");
        CreateAudioSource("gameOver");
        CreateAudioSource("victory");
        CreateAudioSource("buttonClick");
    }

    private void CreateAudioSource(string name)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        _audioSources[name] = source;
    }

    // 播放发射子弹音效
    public void PlayShootSound()
    {
        PlaySound(_audioSources["shoot"], shootSound, shootVolume);
    }

    // 播放玩家受击音效
    public void PlayPlayerHitSound()
    {
        PlaySound(_audioSources["playerHit"], playerHitSound, playerHitVolume);
    }

    // 播放敌人死亡音效
    public void PlayEnemyDeathSound()
    {
        PlaySound(_audioSources["enemyDeath"], enemyDeathSound, enemyDeathVolume);
    }

    // 播放输入Arrow指令音效
    public void PlayArrowInputSound()
    {
        PlaySound(_audioSources["arrowInput"], arrowInputSound, arrowInputVolume);
    }

    // 播放输入错误音效
    public void PlayWrongInputSound()
    {
        PlaySound(_audioSources["wrongInput"], wrongInputSound, wrongInputVolume);
    }

    // 播放子弹增加音效
    public void PlayBulletAddedSound()
    {
        PlaySound(_audioSources["bulletAdded"], bulletAddedSound, bulletAddedVolume);
    }

    // 播放游戏失败音效
    public void PlayGameOverSound()
    {
        PlaySound(_audioSources["gameOver"], gameOverSound, gameOverVolume);
    }

    // 播放游戏胜利音效
    public void PlayVictorySound()
    {
        PlaySound(_audioSources["victory"], victorySound, victoryVolume);
    }

    // 播放按钮点击音效
    public void PlayButtonClickSound()
    {
        PlaySound(_audioSources["buttonClick"], buttonClickSound, buttonClickVolume);
    }

    // 通用播放音效方法
    private void PlaySound(AudioSource source, AudioClip clip, float volume)
    {
        if (clip == null || source == null) return;

        source.clip = clip;
        source.volume = volume;
        source.Play();
    }

    // 注册UI按钮点击事件
    public void RegisterButtonClickSound(UIDocument uiDocument)
    {
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        var buttons = root.Query<Button>().ToList();

        foreach (var button in buttons)
        {
            button.clicked += PlayButtonClickSound;
        }

        Debug.Log($"已为{buttons.Count}个UI按钮注册点击音效");
    }
}
