using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Core; // Singleton<T>를 사용하는 경우

public class SoundManager : Singleton<SoundManager>
{
    // =====================
    // AudioSources
    // =====================
    private AudioSource audioSourceBGM; // 페이드용 BGM
    private AudioSource audioSourceSFX; // 효과음

    // =====================
    // BGM Clips
    // =====================
    private AudioClip bgmMain;
    private AudioClip bgmInGame1;
    private AudioClip bgmInGame12;
    private AudioClip bgmInGame2;
    private AudioClip bgmInGame3;
    private AudioClip gameOver;
    private AudioClip bgmEnding;
    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    // =====================
    // 볼륨
    // =====================
    public float currentBGMVolume { get; private set; }
    public float currentSFXVolume { get; private set; }

    // =====================
    // SFX Clips
    // =====================
    public enum SFXType
    {
        Button_Click,
        Start_Game,
        Jump,
        Clear,
        GameOver
    }
    private Dictionary<SFXType, AudioClip> sfxClips = new Dictionary<SFXType, AudioClip>();
    private Dictionary<string, List<AudioClip>> playerSFX = new Dictionary<string, List<AudioClip>>();
    private Dictionary<string, AudioClip> itemSFX = new Dictionary<string, AudioClip>();
    // =====================
    // Awake: 초기화
    // =====================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitOnGameStart()
    {
        var instance = Instance;
    }

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void AfterAwake()
    {
        base.Awake();

        // PlayerPrefs 초기값
        if (!PlayerPrefs.HasKey("BGMVolume")) PlayerPrefs.SetFloat("BGMVolume", 1f);
        if (!PlayerPrefs.HasKey("SFXVolume")) PlayerPrefs.SetFloat("SFXVolume", 1f);

        currentBGMVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // AudioSource 생성
        audioSourceBGM = gameObject.AddComponent<AudioSource>();
        audioSourceBGM.loop = true;

        audioSourceSFX = gameObject.AddComponent<AudioSource>();

        // 리소스 로드
        LoadBGM();
        LoadAllSFX();
        LoadItemSFX();
    }

    // =====================
    // Start: 게임 시작 시 메인 BGM
    // =====================
    private void Start()
    {
        if (bgmMain != null)
        {
            StartCoroutine(FadeInBGM(bgmMain, 1f));
        }
    }

    // =====================
    // BGM 로드
    // =====================
    private void LoadBGM()
    {
        bgmMain = Resources.Load<AudioClip>("Sound/BGM/BGM_Main");
        bgmInGame1 = Resources.Load<AudioClip>("Sound/BGM/BGM_Play1");
        bgmInGame12 = Resources.Load<AudioClip>("Sound/BGM/BGM_Play1-2");
        bgmInGame2 = Resources.Load<AudioClip>("Sound/BGM/BGM_Play2");
        bgmInGame3 = Resources.Load<AudioClip>("Sound/BGM/BGM_Play3");
        bgmEnding = Resources.Load<AudioClip>("Sound/BGM/BGM_Play_Ending");

        bgmClips.Clear();

        bgmClips["Main"] = bgmMain;
        bgmClips["Play1"] = bgmInGame1;
        bgmClips["Play1-2"] = bgmInGame12;
        bgmClips["Play2"] = bgmInGame2;
        bgmClips["Play3"] = bgmInGame3;
        bgmClips["Ending"] = bgmEnding;
    }

    // =====================
    // SFX 로드
    // =====================
    private void LoadAllSFX()
    {
        sfxClips.Clear();

        sfxClips[SFXType.Button_Click] = Resources.Load<AudioClip>("Sound/SFX/UI/Button_Click");
        sfxClips[SFXType.Start_Game] = Resources.Load<AudioClip>("Sound/SFX/UI/Start_Game");
        //sfxClips[SFXType.Jump] = Resources.Load<AudioClip>("Sound/SFX/Character/Jump");
        //sfxClips[SFXType.Clear] = Resources.Load<AudioClip>("Sound/SFX/System/Clear");
        sfxClips[SFXType.GameOver] = Resources.Load<AudioClip>("Sound/SFX/character/Character_Fall");

        Debug.Log("[SoundManager] SFX 로드 완료: " + sfxClips.Count + "개");
    }
    private void LoadPlayerSFX()
    {
        playerSFX.Clear();

        string basePath = "Sound/SFX/character";

        string[] folders =
        {
        "Climb_Vine",
        "Walk_Step",
        "Hit_Obstacle"
    };

        foreach (var folder in folders)
        {
            string path = basePath + "/" + folder;

            AudioClip[] clips = Resources.LoadAll<AudioClip>(path);

            if (clips.Length > 0)
            {
                playerSFX[folder] = new List<AudioClip>(clips);
                Debug.Log($"[SoundManager] {folder} 로드 완료: {clips.Length}개");
            }
            else
            {
                Debug.LogWarning($"[SoundManager] {folder} 사운드 없음");
            }
        }
    }
    private void LoadItemSFX()
    {
        itemSFX.Clear();

        // item 폴더 내의 사운드 로드 (하나만 있어도 배열 형태로 가져옴)
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Sound/item");

        if (clips.Length > 0)
        {
            foreach (AudioClip clip in clips)
            {
                itemSFX[clip.name] = clip;
            }

            Debug.Log($"[SoundManager] Item SFX 로드 완료: 총 {clips.Length}개");
        }
        else
        {
            Debug.LogWarning("[SoundManager] item 폴더에 사운드 없음!");
        }
    }

    // =====================
    // SFX 재생
    // =====================
    public void EffectSoundOn(SFXType type)
    {
        if (audioSourceSFX == null) return;

        if (sfxClips.TryGetValue(type, out AudioClip clip) && clip != null)
        {
            audioSourceSFX.volume = currentSFXVolume;
            audioSourceSFX.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("SFX 없음: " + type);
        }
    }

    public void EffectSoundOff()
    {
        if (audioSourceSFX != null)
            audioSourceSFX.Stop();
    }

    public void PlayPlayerSound(string type)
    {
        if (!playerSFX.ContainsKey(type))
        {
            Debug.LogWarning("[SoundManager] Player SFX 없음: " + type);
            return;
        }

        List<AudioClip> clips = playerSFX[type];

        if (clips.Count == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Count)];

        audioSourceSFX.volume = currentSFXVolume;
        audioSourceSFX.PlayOneShot(clip);
    }
    public void PlayObjectSound(string clipName)
    {
        string path = "Sound/SFX/object/" + clipName;

        AudioClip clip = Resources.Load<AudioClip>(path);

        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] Object SFX 없음: " + path);
            return;
        }

        audioSourceSFX.volume = currentSFXVolume;
        audioSourceSFX.PlayOneShot(clip);
    }

    public void PlayItemSound(string clipName)
    {
        // 딕셔너리에 해당 파일명의 사운드가 있는지 확인
        if (!itemSFX.ContainsKey(clipName))
        {
            Debug.LogWarning("[SoundManager] Item SFX 없음: " + clipName);
            return;
        }

        // 볼륨 설정 후 재생 (기존 작동 방식과 완벽히 동일)
        audioSourceSFX.volume = currentSFXVolume;
        audioSourceSFX.PlayOneShot(itemSFX[clipName]);
    }

    // =====================
    // BGM 페이드 인
    // =====================
    public IEnumerator FadeInBGM(AudioClip clip, float duration)
    {
        if (audioSourceBGM == null || clip == null) yield break;

        audioSourceBGM.clip = clip;
        audioSourceBGM.volume = 0f;
        audioSourceBGM.Play();

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            audioSourceBGM.volume = Mathf.Lerp(0f, currentBGMVolume, time / duration);
            yield return null;
        }

        audioSourceBGM.volume = currentBGMVolume;
    }

    // =====================
    // BGM 페이드 아웃
    // =====================
    public IEnumerator FadeOutBGM(float duration)
    {
        if (audioSourceBGM == null) yield break;

        float startVol = audioSourceBGM.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            audioSourceBGM.volume = Mathf.Lerp(startVol, 0f, time / duration);
            yield return null;
        }

        audioSourceBGM.volume = 0f;
        audioSourceBGM.Stop();
    }
    // =====================
    // 게임 시작 시 메인 BGM → 스타트 SFX → 인게임 BGM
    // =====================
    public IEnumerator PlayStartSequence()
    {
        // 메인 BGM 페이드 아웃
        yield return StartCoroutine(FadeOutBGM(1f));

        // 스타트 SFX 재생
        EffectSoundOn(SFXType.Start_Game);
        yield return new WaitForSeconds(0.5f);

        // 인게임 BGM 페이드 인
        yield return StartCoroutine(FadeInBGM(bgmInGame1, 1f));
    }

    public void PlayBgm(string bgmName)
    {
        if (!bgmClips.TryGetValue(bgmName, out AudioClip clip) || clip == null)
        {
            Debug.LogWarning("[SoundManager] BGM 없음: " + bgmName);
            return;
        }

        StartCoroutine(PlayBgmCoroutine(clip));
    }

    private IEnumerator PlayBgmCoroutine(AudioClip clip)
    {
        yield return StartCoroutine(FadeOutBGM(1f));
        yield return StartCoroutine(FadeInBGM(clip, 1f));
    }

    // =====================
    // 볼륨 설정 (슬라이더용)
    // =====================
    public void SetBGMVolume(float volume)
    {
        currentBGMVolume = volume;
        PlayerPrefs.SetFloat("BGMVolume", volume);

        if (audioSourceBGM != null)
            audioSourceBGM.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);

        if (audioSourceSFX != null)
            audioSourceSFX.volume = volume;
    }

    public float GetBGMVolume()
    {
        return PlayerPrefs.GetFloat("BGMVolume", 1f);
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat("SFXVolume", 1f);
    }
}
