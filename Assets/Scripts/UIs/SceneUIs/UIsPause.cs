using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIsPause : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button HomeButton;
    [SerializeField] private Button RestartButton;
    [SerializeField] private Button ResumeButton;

    private bool _isLoading;

    private void Awake()
    {
        // 중복 방지: 기존 리스너 제거 후 다시 등록
        if (ResumeButton != null)
        {
            ResumeButton.onClick.RemoveAllListeners();
            ResumeButton.onClick.AddListener(Resume);
        }

        if (HomeButton != null)
        {
            HomeButton.onClick.RemoveAllListeners();
            HomeButton.onClick.AddListener(OnClickHome);
        }

        if (RestartButton != null)
        {
            RestartButton.onClick.RemoveAllListeners();
            RestartButton.onClick.AddListener(OnClickRetry);
        }
    }

    public void OnClickClose()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
    public void Resume()
    {
        if (_isLoading) return;
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    public void OnClickHome()
    {
        if (_isLoading) return;
        _isLoading = true;

        Time.timeScale = 1f;
        gameObject.SetActive(false);

        SceneManager.LoadScene("Main");
    }

    public void OnClickRetry()
    {
        if (_isLoading) return;
        _isLoading = true;

        Time.timeScale = 1f;
        gameObject.SetActive(false);

        string gameplayScene = SceneManager.GetActiveScene().name;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartFromClean(gameplaySceneName: gameplayScene, hubSceneName: "LoadingScene");
        }
        else
        {
            SceneManager.LoadScene(gameplayScene);
        }
    }

    public void OpenPause()
    {
        if (_isLoading) return;

        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }
}