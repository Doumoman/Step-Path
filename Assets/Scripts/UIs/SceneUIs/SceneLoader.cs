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
        SoundManager.Instance.EffectSoundOn("3"); //효과음 재생
        SceneManager.LoadScene("Main"); 
        Time.timeScale = 1.0f; // 시간 정상화
    }

    // Update is called once per frame
    public void LoadInGameScene() // 음악 변경 없이 메인씬 로드
    {
        //GameManager.Instance.SetNowInGame(1);
        SoundManager.Instance.EffectSoundOn("3");
        SceneManager.LoadScene("InGame");
        SoundManager.Instance.Stage1BgmOn();
        Time.timeScale = 1.0f;
    }

    public GameObject settingsPopup; // 설정 팝업

    // 설정 팝업 열기
    public void OpenSettings()
    {
        SoundManager.Instance.EffectSoundOn("3");
        settingsPopup.SetActive(true);
    }

    // 설정 팝업 닫기
    public void CloseSettings()
    {
        SoundManager.Instance.EffectSoundOn("3");
        settingsPopup.SetActive(false);
    }

}
