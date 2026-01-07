using TMPro;
using UnityEngine;

public class ScoreHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text currentHeightText;
    [SerializeField] private TMP_Text bestHeightText;

    [SerializeField] private string currentPrefix = "";
    [SerializeField] private string currentSuffix = "m";
    [SerializeField] private string bestPrefix = "";
    [SerializeField] private string bestSuffix = "m";

    void Update()
    {
        if (GameManager.Instance == null) return;

        // currentScore를 '현재 최고 높이(m)'로 쓰는 구조일 때
        currentHeightText.text = currentPrefix + GameManager.Instance.GetScore().ToString("F0") + currentSuffix;
        bestHeightText.text = bestPrefix + GameManager.Instance.GetBestScore().ToString("F0") + bestSuffix;
    }
}