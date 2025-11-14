using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIsSceneInGame : MonoBehaviour
{
    // ───────────────────────────────
    //   IN-GAME HUD
    // ───────────────────────────────
    [Header("InGameUI_HUD")]
    public Text BestScoreText;        //화면 상단 최고 기록
    public Text CurrentScoreText;     //현재 점수
    public Image timeBar;             //타임바

    // ───────────────────────────────
    //   PAUSE PANEL
    // ───────────────────────────────
    [Header("PausePanel")]
    public GameObject PausePanel;

    // ───────────────────────────────
    //   GAME OVER POPUP
    // ───────────────────────────────
    [Header("GameOverPopup")]
    public GameObject GameOverPopup;
    public Text gameOverScoreText;        //게임오버 순간 점수
    public Text gameOverBestScoreText;    //최고기록 표시
    public GameObject gameOverBestLabel;  //"BEST!" 표기


    // ───────────────────────────────
    //   UPDATE (HUD 업데이트)
    // ───────────────────────────────
    void Update() 
    {
        UpdateInGameHUD();
    }

    private void UpdateInGameHUD()
    {
        CurrentScoreText.text = $"{GameManager.Instance.GetScore():F0}m";
        BestScoreText.text = $"{GameManager.Instance.GetBestScore():F0}m";
        timeBar.fillAmount = GameManager.Instance.GetTimePercent();
    }


    // ───────────────────────────────
    //   PAUSE
    // ───────────────────────────────
    public void OpenPause()
    {
        PausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ClosePause()
    {
        PausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }

    public void RestartGame()
    {
        PausePanel.SetActive(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    // ───────────────────────────────
    //   GAME OVER
    // ───────────────────────────────
    public void ShowGameOver()
    {
        Time.timeScale = 0f;

        // 현재 점수 가져오기
        float score = GameManager.Instance.GetScore();
        float bestScore = GameManager.Instance.GetBestScore();
        bool isBest = GameManager.Instance.IsBestRecord();

        // 게임오버 UI에 점수 반영
        gameOverScoreText.text = $"{score:F0}m";         //게임오버 순간 점수
        gameOverBestScoreText.text = $"{bestScore:F0}m"; //기록된 최고점수
        gameOverBestLabel.SetActive(isBest);             //최고기록일 때만 BEST! 표시

        // 팝업 표시
        PausePanel.SetActive(false);
        GameOverPopup.SetActive(true);
    }
}



