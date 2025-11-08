using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using Core;

public class GameManager : Singleton<GameManager>
{

    InputManager _input = new InputManager();
    ResourceManager _resource = new ResourceManager();
    SoundManager _sound = new SoundManager();


    public static InputManager Input { get { return Instance._input; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SoundManager Sound { get { return Instance._sound; } }

    private void Start()
    {
        Application.targetFrameRate = 60;
    }
    private void Update()
    {
        _input.OnUpdate();
    }
    
    //------------여기부터 추가

    private float currentScore = 0f; // 현재 플레이의 높이
    private float bestScore = 0f; // 서버에서 불러온 최고 기록

    private bool isBestRecord = false; // 최고기록 여부

    //현재 점수 갱신(플레이어 높이 기준)
    public void UpdateScore(float height)
    { 
        if (height > currentScore) 
            currentScore = height;
    } 
    //점수 리셋 (게임 재시작 시)
    public void ResetScore()
    { 
        currentScore = 0f; 
        isBestRecord = false; 
    } 
    //현재 점수 반환
    public float GetScore() 
    { 
        return currentScore; 
    }
    // 최고 점수 반환
    public float GetBestScore()
    {
        return bestScore;
    }

    // 서버에서 최고 점수 불러오기 (예시용, 실제로는 웹통신 코드로 교체 가능)
    /*
    public IEnumerator LoadBestScoreFromServer()
    {
        // TODO: 서버 API 연결
        yield return new WaitForSeconds(0.5f); // 예시 지연
        bestScore = PlayerPrefs.GetFloat("BestScore", 0f); // 임시로 로컬에 저장된 값 사용
    }

    // 서버에 점수 저장하기
    public IEnumerator SaveScoreToServer()
    {
        yield return new WaitForSeconds(0.3f);

        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            isBestRecord = true;
            PlayerPrefs.SetFloat("BestScore", bestScore); // 임시로 로컬 저장
            PlayerPrefs.Save();
        }
        else
        {
            isBestRecord = false;
        }

        yield break;
    }
    */

    // 최고 기록인지 여부 반환 (UI에서 "best!" 표시용)
    public bool IsBestRecord()
    {
        return isBestRecord;
    }

    // -------------------------- 
}

    




