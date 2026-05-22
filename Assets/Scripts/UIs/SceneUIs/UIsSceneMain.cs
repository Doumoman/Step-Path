using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIsSceneMain : MonoBehaviour
{
    [Header("Scene")]
    private string loadingSceneName = "LoadingScene";
    private string gameplaySceneName = "STEPPATH";
    public float soundEffectDelayTime = 0.5f; // 효과음 후 씬 전환까지 대기 시간
    public GameObject settingsPopup; // 설정 팝업

    // =====================
    // 버튼 클릭
    // =====================
    public void OnClickPlayButton()
    {
        Debug.Log("Play 버튼 클릭됨");
        StartCoroutine(StartGameRoutine());
    }

    public void OpenSettings()
    {
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click);
        settingsPopup.SetActive(true);
    }

    public void CloseSettings()
    {
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click);
        settingsPopup.SetActive(false);
    }

    // =====================
    // 씬 전환
    // =====================
    private IEnumerator StartGameRoutine()
    {
        Time.timeScale = 1f;

        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Start_Game);

        yield return new WaitForSecondsRealtime(soundEffectDelayTime);

        // 이전 플레이 기록 초기화 + 로딩씬 경유 + 게임씬 진입
        GameManager.Instance.RestartFromClean(gameplaySceneName, loadingSceneName);
    }

    // =====================
    // 슬라이더 연동
    // =====================
    public void OnBGMVolumeChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        SoundManager.Instance.SetSFXVolume(value);
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click); // 테스트용
    }
}