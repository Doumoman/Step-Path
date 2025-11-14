using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIsSceneInGame : MonoBehaviour
{
    // ───────────────────────────────
    //   IN-GAME HUD
    // ───────────────────────────────
    [Header("InGameUI_HUD")]
    [SerializeField] public TextMeshProUGUI BestScoreText;    // 화면 상단 최고 기록
    [SerializeField] public TextMeshProUGUI CurrentScoreText; // 현재 점수
    // [SerializeField] private Image timeBar;               // ← 주석 처리 (타임바 제거)

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
    [SerializeField] private TextMeshProUGUI gameOverScoreText;     // 게임오버 순간 점수
    [SerializeField] private TextMeshProUGUI gameOverBestScoreText; // 최고기록 표시
    [SerializeField] private GameObject gameOverBestLabel;          // "BEST!" 표기

    void Start()
    {
        if (CurrentScoreText == null) Debug.LogWarning("CurrentScoreText is NULL");
        if (BestScoreText == null) Debug.LogWarning("BestScoreText is NULL");
        if (PausePanel == null) Debug.LogWarning("PausePanel is NULL");
        if (GameOverPopup == null) Debug.LogWarning("GameOverPopup is NULL");
        if (gameOverScoreText == null) Debug.LogWarning("gameOverScoreText is NULL");
        if (gameOverBestScoreText == null) Debug.LogWarning("gameOverBestScoreText is NULL");
        if (gameOverBestLabel == null) Debug.LogWarning("gameOverBestLabel is NULL");

        if (GameManager.Instance == null)
            Debug.LogError("GameManager.Instance is NULL");
    }

    void Update()
    {
        UpdateInGameHUD();
    }

    private void UpdateInGameHUD()
    {
        if (GameManager.Instance == null) return;

        if (CurrentScoreText != null)
            CurrentScoreText.text = $"{GameManager.Instance.GetScore():F0}m";

        if (BestScoreText != null)
            BestScoreText.text = $"{GameManager.Instance.GetBestScore():F0}m";

        // timeBar 관련 제거
        // if (timeBar != null)
        //     timeBar.fillAmount = GameManager.Instance.GetTimePercent();
    }

    // ───────────────────────────────
    //   PAUSE
    // ───────────────────────────────
    public void OpenPause()
    {
        if (PausePanel != null)
            PausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ClosePause()
    {
        if (PausePanel != null)
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
        if (PausePanel != null)
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

        if (GameManager.Instance == null) return;

        float score = GameManager.Instance.GetScore();
        float bestScore = GameManager.Instance.GetBestScore();
        bool isBest = GameManager.Instance.IsBestRecord();

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"{score:F0}m";

        if (gameOverBestScoreText != null)
            gameOverBestScoreText.text = $"{bestScore:F0}m";

        if (gameOverBestLabel != null)
            gameOverBestLabel.SetActive(isBest);

        if (PausePanel != null)
            PausePanel.SetActive(false);

        if (GameOverPopup != null)
            GameOverPopup.SetActive(true);
    }
}
