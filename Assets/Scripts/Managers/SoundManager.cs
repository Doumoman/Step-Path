using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Core;

public class SoundManager : Singleton<SoundManager>
{
    private void Awake()
    {
        // 게임이 꺼졌다 켜져도 이전 설정 유지
        if (!PlayerPrefs.HasKey("BGMVolume")) PlayerPrefs.SetFloat("BGMVolume", 1.0f);
        if (!PlayerPrefs.HasKey("SFXVolume")) PlayerPrefs.SetFloat("SFXVolume", 1.0f);
        Init();
    }

    AudioClip bgmMain;
    AudioClip bgmInGame;
    AudioClip clear;
    AudioClip GameOver;
    AudioClip ending;

    private AudioSource audioSource1; // BGM
    private AudioSource audioSource2; // SFX

    AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];
    Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    public AudioMixer audioMixer;
    public float currentBGMVolume { get; set; }
    public float currentSFXVolume { get; set; }

    public void Init()
    {
        currentBGMVolume = PlayerPrefs.GetFloat("BGMVolume");
        currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume");

        audioMixer = Resources.Load<AudioMixer>("NewMixer");
        AudioMixerGroup[] audioMixerGroups = audioMixer.FindMatchingGroups("Master");

        GameObject root = this.gameObject;

        string[] soundNames = System.Enum.GetNames(typeof(Sound));
        for (int i = 0; i < soundNames.Length - 1; i++)
        {
            GameObject go = new GameObject { name = soundNames[i] };
            _audioSources[i] = go.AddComponent<AudioSource>();
            go.transform.parent = root.transform;
            go.GetComponent<AudioSource>().outputAudioMixerGroup = audioMixerGroups[i + 1];
        }

        _audioSources[(int)Sound.Bgm].loop = true;
        _audioSources[(int)Sound.LoopEffect].loop = true;
    }

    void Start()
    {
        audioSource1 = gameObject.AddComponent<AudioSource>();
        audioSource2 = gameObject.AddComponent<AudioSource>();
        audioSource1.loop = true;

        bgmMain = Resources.Load<AudioClip>("Sounds/main");
        bgmInGame = Resources.Load<AudioClip>("Sounds/1stageTheme_first_dream");
        clear = Resources.Load<AudioClip>("Sounds/win");
        GameOver = Resources.Load<AudioClip>("Sounds/lose");
        ending = Resources.Load<AudioClip>("Sounds/EndingBGM");

        MainBgmOn();
    }

    //  UIsSettings 슬라이더 연동용 — 볼륨 조절
    public void SetBGMVolume(float volume)
    {
        audioSource1.volume = volume;
        PlayerPrefs.SetFloat("BGMVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        audioSource2.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    //  UIsSettings에서 초기값 읽기용
    public float GetBGMVolume()
    {
        return PlayerPrefs.GetFloat("BGMVolume", 1f);
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    // 기존 BGM 관련 로직
    public void MainBgmOn()
    {
        audioSource1.clip = bgmMain;
        audioSource1.volume = GetBGMVolume();
        audioSource1.Play();
    }

    public void InGameBgmOn()
    {
        audioSource1.clip = bgmInGame;
        audioSource1.volume = GetBGMVolume();
        audioSource1.Play();
    
    }

    public void ClearBgmOn()
    {
        audioSource1.clip = clear;
        audioSource1.volume = GetBGMVolume();
        audioSource1.Play();
    }

    public void GameOverBgmOn()
    {
        audioSource1.clip = GameOver;
        audioSource1.volume = GetBGMVolume();
        audioSource1.Play();
    }

    public void EndingBgmOn()
    {
        audioSource1.clip = ending;
        audioSource1.volume = GetBGMVolume();
        audioSource1.Play();
    }

    // 효과음 재생
    public void EffectSoundOn(string effectName)
    {
        string effectPath = "Sounds/" + effectName;
        AudioClip effectClip = Resources.Load<AudioClip>(effectPath);
        audioSource2.volume = GetSFXVolume();
        audioSource2.PlayOneShot(effectClip);
    }

    public void EffectSoundOff()
    {
        audioSource2.Stop();
    }
}
