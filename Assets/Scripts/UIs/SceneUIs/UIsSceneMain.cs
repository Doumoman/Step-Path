using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIsSceneMain : MonoBehaviour
{
    public float soundEffectDelayTime = 0.5f; // 효과음 후 씬 전환까지 대기 시간

    // 버튼에서 직접 연결할 함수
    public void OnClickPlayButton()
    {
        Debug.Log("Play 버튼 클릭됨");
        StartCoroutine(LoadSceneInGame());
    }

    private IEnumerator LoadSceneInGame()
    {
        // 효과음 재생
        SoundManager.Instance.EffectSoundOn("2");

        // 효과음이 끝나길 잠시 대기
        yield return new WaitForSeconds(soundEffectDelayTime);

        // 인게임 씬으로 전환
        SceneManager.LoadScene("STEPPATH");
    }
    
    public GameObject settingsPopup; // 설정 팝업
  
    // 설정 팝업 열기
    public void OpenSettings()
    {
        settingsPopup.SetActive(true);
    }

    // 설정 팝업 닫기
    public void CloseSettings()
    {
        settingsPopup.SetActive(false);
    }

    
}