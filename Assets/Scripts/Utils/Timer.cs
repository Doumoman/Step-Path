using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class Timer : MonoBehaviour //제한 시간 안에 300m 미달 시 게임오버
{
    [SerializeField] private TextMeshProUGUI timerText;  //2D Text
    [SerializeField] private float timeRemaining = 600f;

    int seconds;

    public bool TimeActive = true; // 시간 작동 여부
    private void Awake() //처음에 한 번 실행(초기화될 때)
    {
        timeRemaining = 600f; //600초
        StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        while (TimeActive && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime; // 시간 감소
            int seconds = Mathf.FloorToInt(timeRemaining); // 소수점 제거
            timerText.text = seconds.ToString(); // “267”처럼 숫자만 표시
            yield return null; // 다음 프레임까지 대기
        }

        if (timeRemaining <= 0)
        {
            TimeActive = false;
            GameOver();
        }
    }
     private void GameOver()
    {
        Debug.Log("게임 오버!");
        // GameOverUI popup
    }

}
        