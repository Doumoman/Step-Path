using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIsSceneInGame : MonoBehaviour
{
    // ──────────── HUD ────────────
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI BestScoreText;
    [SerializeField] private TextMeshProUGUI CurrentScoreText;
    [SerializeField] private Button OpenPauseButton;

    // ──────────── TimeBar ────────────
    [Header("TimeBar")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private float maxTime = 10f;            // 타임바 최대 시간
    [SerializeField] private float addTimePerScore = 1f;     // 점수 단위마다 회복할 시간
    [SerializeField] private int scoreInterval = 10;         // 점수 단위
    private float currentTime;
    private int lastScoreCheckpoint = 0;                     // 마지막으로 회복한 점수 단위

    // ──────────── Pause Panel ────────────
    [Header("PausePanel")]
    [SerializeField] private GameObject PausePanel;

    // ──────────── Game Over ────────────
    [Header("GameOverPopup")]
    [SerializeField] private GameObject GameOverPopup;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverBestScoreText;
    [SerializeField] private GameObject gameOverBestLabel;

    void Start()
    {
        // 타임바 초기화
        currentTime = maxTime;
        if (timeSlider != null)
        {
            timeSlider.maxValue = maxTime;
            timeSlider.minValue = 0f;
            timeSlider.value = currentTime;
        }

        // 필드 연결 경고
        if (CurrentScoreText == null) Debug.LogWarning("CurrentScoreText is NULL");
        if (BestScoreText == null) Debug.LogWarning("BestScoreText is NULL");
        if (PausePanel == null) Debug.LogWarning("PausePanel is NULL");
        if (GameOverPopup == null) Debug.LogWarning("GameOverPopup is NULL");
        if (gameOverScoreText == null) Debug.LogWarning("gameOverScoreText is NULL");
        if (gameOverBestScoreText == null) Debug.LogWarning("gameOverBestScoreText is NULL");
        if (gameOverBestLabel == null) Debug.LogWarning("gameOverBestLabel is NULL");
        if (GameManager.Instance == null) Debug.LogError("GameManager.Instance is NULL");

        // Pause 버튼 클릭 이벤트 연결
        if (OpenPauseButton != null)
            OpenPauseButton.onClick.AddListener(OpenPause);

        // 처음 PausePanel 숨김
        if (PausePanel != null)
            PausePanel.SetActive(false);
    }

    void Update()
    {
        UpdateInGameHUD();
        UpdateTimeBar();
    }

    // ──────────── HUD 갱신 ────────────
    private void UpdateInGameHUD()
    {
        if (GameManager.Instance == null) return;

        float score = GameManager.Instance.GetScore();

        if (CurrentScoreText != null)
            CurrentScoreText.text = $"{score:F0}m";

        if (BestScoreText != null)
            BestScoreText.text = $"{GameManager.Instance.GetBestScore():F0}m";

        // 점수 단위마다 타임바 회복
        int checkpoint = (int)score / scoreInterval;
        if (checkpoint > lastScoreCheckpoint)
        {
            AddTime(addTimePerScore);
            lastScoreCheckpoint = checkpoint;
        }
    }

    // ──────────── 타임바 감소 ────────────
    private void UpdateTimeBar()
    {
        if (currentTime <= 0) return;

        currentTime -= Time.deltaTime; // Time.timeScale = 0이면 자동 멈춤

        if (timeSlider != null)
            timeSlider.value = Mathf.Clamp(currentTime, 0f, maxTime);

        if (currentTime <= 0)
            ShowGameOver();
    }

    // ──────────── 타임바 회복 ────────────
    private void AddTime(float amount)
    {
        currentTime += amount;
        if (currentTime > maxTime) currentTime = maxTime;

        if (timeSlider != null)
            timeSlider.value = currentTime;
    }

    // ──────────── Pause ────────────
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

        ResetTimeBar();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ──────────── Game Over ────────────
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

    // ──────────── 타임바 초기화 ────────────
    public void ResetTimeBar()
    {
        currentTime = maxTime;
        lastScoreCheckpoint = 0;
        if (timeSlider != null)
            timeSlider.value = currentTime;
    }
}
