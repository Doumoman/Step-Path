using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIsSceneMain : MonoBehaviour
{
    public float soundEffectDelayTime = 0.5f; // 효과음 후 씬 전환까지 대기 시간
    public GameObject settingsPopup; // 설정 팝업

    // =====================
    // 버튼 클릭
    // =====================
    public void OnClickPlayButton()
    {
        Debug.Log("Play 버튼 클릭됨");
        StartCoroutine(LoadSceneInGame());
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
    private IEnumerator LoadSceneInGame()
    {
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Start_Game);
        yield return new WaitForSeconds(soundEffectDelayTime);
        SceneManager.LoadScene("STEPPATH");
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