using DG.Tweening.Core.Easing;
using Lean.Pool;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;


public enum AudioType
{
    None,
    Click,
    Win,
    Lose,
    CellPop,
    CellSelect,
    CoinCollect,
    CoinBlast,
    Confetti,
    RegionMatch
}

public class AudioManager : SingletonMono<AudioManager>
{
    public const string KEY_MUSIC = "MUSIC";
    public const string KEY_SOUND = "SOUND";
    public AudioMixer audioMixer;
    [SerializeField] float m_maxMusicVolume = .8f;
    [SerializeField] float m_intervalPlayOverlap = 0.05f;

    private float lastTimePlayOverlap;

    public bool IsSoundEnabled
    {
        get
        {
            return AudioManager.Instance.GetSoundVolumn() > 0.001f;
        }
        set
        {
            if (value)
            {
                AudioManager.Instance.SetSoundVolumn(1f);
            }
            else
            {
                AudioManager.Instance.SetSoundVolumn(0f);
            }
        }
    }

    public bool IsMusicEnabled
    {
        get
        {
            return AudioManager.Instance.GetMusicVolumn() > 0.001f;
        }
        set
        {
            if (value)
            {
                AudioManager.Instance.SetMusicVolumn(1f);
            }
            else
            {
                AudioManager.Instance.SetMusicVolumn(0f);
            }
        }
    }

    [SerializeField] AudioSource audioMain;
    [SerializeField] AudioSource audioGamePlay;
    [SerializeField] AudioSource audioSoundEffect;
    [SerializeField] AudioSource audioClick;

    bool isPlayingMainMusic;

    [System.Serializable]
    public class SoundClips
    {
        public AudioType type;
        public AudioClip clip;
        public float defaultVolume = 1f;
    }

    [SerializeField] List<SoundClips> soundClips;
    Dictionary<AudioType, AudioSource> _dictClips = new();
    Dictionary<AudioType, float> _dictClipsVolume = new();
    [SerializeField] GameObject _audioFXContainer;

    private void Awake()
    {
        if (soundClips != null)
        {
            for (int i = 0; i < soundClips.Count; i++)
            {
                var audioSource = _audioFXContainer.AddComponent<AudioSource>();
                audioSource.clip = soundClips[i].clip;
                audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Sound")[0];
                _dictClips.Add(soundClips[i].type, audioSource);
            }

            for (int i = 0; i < soundClips.Count; i++)
            {
                _dictClipsVolume[soundClips[i].type] = soundClips[i].defaultVolume;
            }
        }
    }

    private void Start()
    {
        IsSoundEnabled = IsSoundEnabled;
        IsMusicEnabled = IsMusicEnabled;
    }

    public void PlayAudioFX(AudioType type, bool loop = false)
    {
        if (_dictClips.ContainsKey(type))
        {
            _dictClips[type].loop = loop;
            _dictClips[type].volume = _dictClipsVolume[type];
            _dictClips[type].Play();
        }

    }

    public void PlayAudioFxOverLap(AudioType type)
    {
        if (!_dictClips.ContainsKey(type)) return;
        var clip = _dictClips[type];

        if (audioSoundEffect.isPlaying)
        {
            if (Time.time - lastTimePlayOverlap < m_intervalPlayOverlap) return;
            lastTimePlayOverlap = Time.time;
        }

        PlayOneShot(_dictClips[type].clip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        audioSoundEffect.PlayOneShot(clip);
    }

    public void PlayAudioFX(AudioType type, bool loop, float volume = 1)
    {
        if (_dictClips.ContainsKey(type))
        {
            _dictClips[type].loop = loop;
            _dictClips[type].volume = volume;
            _dictClips[type].Play();
        }
    }

    public async void PlayAudioFxOnNewSource(AudioClip audio)
    {
        var audioSource = _audioFXContainer.AddComponent<AudioSource>();
        audioSource.clip = audio;
        audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Sound")[0];
        audioSource.Play();
        await Task.Delay((int)(audio.length * 1000));
        Destroy(audioSource);
    }

    public void StopAudioFX(AudioType type)
    {
        if (_dictClips.ContainsKey(type))
        {
            _dictClips[type].Stop();
        }
    }

    float lastTimeCollision { get; set; }

    GameObject _prefabAudioCollision;
    GameObject prefabAudioCollision
    {
        get
        {
            if (_prefabAudioCollision == null)
            {
                _prefabAudioCollision = Resources.Load<GameObject>("Prefabs/AudioCollision");
            }
            return _prefabAudioCollision;
        }
    }

    public void PlayAudioForce(float force)
    {
        if (Time.realtimeSinceStartup - lastTimeCollision > 0.1f && force > 1)
        {
            lastTimeCollision = Time.realtimeSinceStartup;
            var audio = LeanPool.Spawn(prefabAudioCollision, Vector3.zero, Quaternion.identity).GetComponent<AudioSource>();
            audio.volume = force / 8;
            audio.Play();
        }
    }

    public void PlaySoundEffect(AudioClip _audio)
    {
        audioSoundEffect.PlayOneShot(_audio);
    }

    public void SetMusic(float volume)
    {
        SetStatusAudio("Music", volume);
    }

    public void SetSound(float volume)
    {
        SetStatusAudio("Sound", volume);
    }

    private void SetStatusAudio(string nameAudio, float volume)
    {
        if (audioMixer != null)
        {
            // audioMixer.SetFloat(nameAudio, Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1)) * 20);
            if (volume > 0)
            {
                audioMixer.SetFloat(nameAudio, 1);
            }
            else
            {
                audioMixer.SetFloat(nameAudio, 0);
            }
        }
    }

    public void PlayMusicMain()
    {
        if (!isPlayingMainMusic)
        {
            audioMain.Play();
            isPlayingMainMusic = true;
        }
        audioGamePlay.Stop();
    }

    public void PlayMusicGamePlay()
    {
        audioGamePlay.Play();
        audioMain.Stop();
        isPlayingMainMusic = false;
    }

    public void SetMusicVolumn(float volume)
    {
        PlayerPrefs.SetFloat(KEY_MUSIC, volume);
        PlayerPrefs.Save();
        SetMusic(GetMusicVolumn());
    }

    public void SetSoundVolumn(float volume)
    {
        volume = Mathf.Clamp(volume, 0, m_maxMusicVolume);
        PlayerPrefs.SetFloat(KEY_SOUND, volume);
        PlayerPrefs.Save();
        SetSound(GetSoundVolumn());
    }

    public float GetMusicVolumn()
    {
        return PlayerPrefs.GetFloat(KEY_MUSIC, 1);
    }

    public float GetSoundVolumn()
    {
        return PlayerPrefs.GetFloat(KEY_SOUND, 1);
    }

    public void PlayAudioClick()
    {
        if(audioClick != null)
        audioClick.Play();
    }

#if UNITY_EDITOR
    [Header("Test")]
    [SerializeField] bool t_playAudio;
    [SerializeField] bool t_loop;
    [SerializeField] AudioType t_aType;
    [Space(10f)]
    [SerializeField] bool t_stopAudio;
    private void OnValidate()
    {
        if (t_playAudio)
        {
            t_playAudio = false;
            PlayAudioFX(t_aType, t_loop);
        }
        if (t_stopAudio)
        {
            t_stopAudio = false;
            StopAudioFX(t_aType);
        }
    }
#endif
}