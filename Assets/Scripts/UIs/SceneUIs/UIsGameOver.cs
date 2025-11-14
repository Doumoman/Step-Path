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

    private void Awake()
    {
        transform.gameObject.SetActive(false);
    }

    private void Start()
    {
        // 버튼 이벤트 연결
        HomeButton.onClick.AddListener(OnClick_Home);
        RetryButton.onClick.AddListener(OnClick_Retry);
    }

    public void Show()
    {
        float score = GameManager.Instance.GetScore();
        bool isBest = GameManager.Instance.IsBestRecord();

        transform.gameObject.SetActive(true);
        textGameResult.text = $"{score:F0}m";
        textBestLabel.gameObject.SetActive(isBest);
    }

    private void OnClick_Retry()
    {
        StartCoroutine(RestartGame());
    }

    private IEnumerator RestartGame()
    {
        GameManager.Instance.ResetScore();
        yield return null;

        // 현재 활성화된 씬 이름을 가져와서 다시 로드 가능
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    private void OnClick_Home()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }
}



