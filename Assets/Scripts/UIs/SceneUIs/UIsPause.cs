using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIsPause : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button HomeButton;
    [SerializeField] private Button RestartButton;
    [SerializeField] private Button PauseCloseButton;

    private void Awake()
    {
        // 처음엔 숨겨둠
        gameObject.SetActive(false);
    }

    private void Start()
    {
        PauseCloseButton.onClick.AddListener(OnClickClose);
        HomeButton.onClick.AddListener(OnClickHome);
        RestartButton.onClick.AddListener(OnClickRetry);
    }

    // ====== 버튼 기능 ======
    private void OnClickClose()
    {
        // 창 닫기 + 게임 재개
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    private void OnClickHome()
    {
        // 메인으로
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }

    private void OnClickRetry()
    {
        // 재시작
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ====== 외부에서 호출해서 Pause 열기 ======
    public void OpenPause()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }
}