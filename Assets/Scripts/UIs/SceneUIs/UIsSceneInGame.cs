using UnityEngine;
using UnityEngine.SceneManagement;

public class UIsSceneInGame : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void LoadMain()
    {
        //SoundManager.Instance.StopLoopEffect();
        //SoundManager.Instance.EffectSoundOn("3");
        SceneManager.LoadScene("Main");
        //SoundManager.Instance.MainBgmOn();
        Time.timeScale = 1.0f;
    }

    public void LoadInGame()
    {
        //SoundManager.Instance.StopLoopEffect();
        //SoundManager.Instance.EffectSoundOn("3");
        SceneManager.LoadScene("InGame");
        //SoundManager.Instance.InGameBgmOn();
        Time.timeScale = 1.0f;
    }
    
    public GameObject pausePopup; // 일시정지 팝업


   
    // 일시정지 팝업 열기(게임 일시정지)
    public void OpenPause()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        pausePopup.SetActive(true);
        Time.timeScale = 0.0f;
    }

    //홈 버튼 //왼쪽 버튼
    public void OpenHome()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        SceneManager.LoadScene("Main");
    }

    // 일시정지 팝업 닫기(게임 재게) //가운데 버튼
    public void ClosePause()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        pausePopup.SetActive(false);
        Time.timeScale = 1.0f;
    }

    //재시작 버튼 //오른쪽 버튼
    public void RestartGame()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        pausePopup.SetActive(false);
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); //씬 다시 로드
    }
}


