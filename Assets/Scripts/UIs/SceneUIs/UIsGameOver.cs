using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIsGameOver : MonoBehaviour
{
    [SerializeField] private Text textGameResult;
    [SerializeField] private Text textBestLabel; // "best!" 텍스트 오브젝트 (작게)

    private void Awake()
    {
        transform.gameObject.SetActive(false); //게임이 시작되면 게임오버창 비활성화
    }
    

    public void Show()
    {
        float score = GameManager.Instance.GetScore(); // 현재 점수 불러오기
        bool isBest = GameManager.Instance.IsBestRecord(); //최고 기록인지 판별
        transform.gameObject.SetActive(true); // 게임오버창 활성화
        textGameResult.text = $"{score.ToString("F0")}m"; //
        textBestLabel.gameObject.SetActive(isBest); //최고기록이면 best! (아니면 안나타남)
    }

    public void onClick_Retry() // 재도전 
    {
        StartCoroutine(RestartGame());
    }

    private IEnumerator RestartGame()
    {
        

        // 점수를 서버(혹은 PlayerPrefs)에 저장
        //yield return GameManager.Instance.SaveScoreToServer(); // 시간이 걸리는 비동기 작업 -> 코루틴 필요

        // 점수 초기화
        GameManager.Instance.ResetScore();
        yield return null; // 1프레임 대기해서 GUI 이벤트 종료 기다림

        // 인게임 씬 다시 로드
        SceneManager.LoadScene("InGame");

    }
}

