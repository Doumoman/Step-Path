using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using TMPro.Examples;
public class SceneLoader : MonoBehaviour
{
    public void LoadMainSceneNoChange() // 메인 씬 로드
    {
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click); //효과음 재생
        SceneManager.LoadScene("Main"); 
        Time.timeScale = 1.0f; // 시간 정상화
    }
    
    public void LoadInGameScene()
    {
        StartCoroutine(StartGameSequence());
    }
    

// 씬 전환 + 사운드 시퀀스 코루틴
    private IEnumerator StartGameSequence()
    {
        // 스타트 SFX 재생 후 메인 BGM 페이드 아웃 → 인게임 BGM 페이드 인
        yield return StartCoroutine(SoundManager.Instance.PlayStartSequence());

        // 인게임 씬 로드
        SceneManager.LoadScene("InGame");
        Time.timeScale = 1.0f;
    }
    public GameObject settingsPopup; // 설정 팝업

    // 설정 팝업 열기
    public void OpenSettings()
    {
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click);
        settingsPopup.SetActive(true);
    }

    // 설정 팝업 닫기
    public void CloseSettings()
    {
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click);
        settingsPopup.SetActive(false);
    }

}
