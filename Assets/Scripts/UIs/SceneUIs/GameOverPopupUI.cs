using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TMP_Text currentHeightText;
    [SerializeField] private TMP_Text bestHeightText;

    [Header("Format")]
    [SerializeField] private string suffix = "m";

    private void Awake()
    {
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        /*
        var sm = ScoreManager.Instance;
        if (sm == null) return;

        if (currentHeightText != null)
            currentHeightText.text = sm.GetCurrentText() + suffix;

        if (bestHeightText != null)
            bestHeightText.text = sm.GetBestText() + suffix;

        // 필요하면 게임 일시정지
        // Time.timeScale = 0f;
        */
    }

    public void Hide()
    {
        // Time.timeScale = 1f;
        if (root != null) root.SetActive(false);
    }

    public void OnClickHome()
    {
        // Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }
}