using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIsGameOver : MonoBehaviour
{
    public Text gameOverText;

    private void Awake()
    {
        transform.gameObject.SetActive(false); //게임이 시작되면 게임오버창 비활성화
    }

    public void Show()
    {
        int score = FindFirstObjectByType<ScoreText>().GetScore(); //현재 점수(높이)를 불러옴
        transform.gameObject.SetActive(true); //게임오버창 활성화
        gameOverText.text = score.ToString();
    }

    public void onClick_Retry() // 재도전 
    {
        SceneManager.LoadScene("InGame"); //인게임 화면으로 들어감
    }
}

