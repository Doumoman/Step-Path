using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIsGameOver : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textGameResult;
    [SerializeField] private TextMeshProUGUI textBestLabel; 
    [SerializeField] private Button HomeButton;
    [SerializeField] private Button RetryButton;
    private bool _isLoading;
    private void Awake()
    {
    }

    private void Start()
    {
        // 버튼 이벤트 연결
        HomeButton.onClick.AddListener(OnClick_Home);
        RetryButton.onClick.AddListener(OnClick_Retry);
    }

    public void Show()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.SaveScore();

        float score = GameManager.Instance.GetScore();
        float best = GameManager.Instance.GetBestScore();

        bool isBest = score >= best;

        gameObject.SetActive(true);

        textGameResult.text = $"{score:F0}m";

        // 베스트 라벨 표시
        if (textBestLabel != null)
            textBestLabel.gameObject.SetActive(true);
    }

    private void OnClick_Retry()
    {
        if (_isLoading) return;
        _isLoading = true;

        Time.timeScale = 1f;

        // 현재 게임 씬 이름
        string gameplayScene = SceneManager.GetActiveScene().name;

        // "Main"으로 나갔다가 다시 게임씬으로
        GameManager.Instance.RestartFromClean(gameplaySceneName: gameplayScene, hubSceneName: "LoadingScene");
    }

    private void OnClick_Home()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }
}



