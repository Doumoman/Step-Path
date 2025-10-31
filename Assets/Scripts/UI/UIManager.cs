using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject StartUI;
    public GameObject GameUI;
    public GameObject GameOverUI;

    void Start()
    {
        ShowStartUI();

    }
    public void ShowStartUI()
    {
        StartUI.SetActive(true);
        GameUI.SetActive(false);
        GameOverUI.SetActive(false);
    }

    public void ShowGameUI()
    {
        StartUI.SetActive(false);
        GameUI.SetActive(true);
        GameOverUI.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        StartUI.SetActive(false);
        GameUI.SetActive(false);
        GameOverUI.SetActive(true);
    }
}
