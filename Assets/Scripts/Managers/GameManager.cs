using System;
using UnityEngine;
using Core;
using UnityEngine.SceneManagement;

// 이 GameManager는 Singleton<T>를 상속받아
// "게임 내에서 단 하나만 존재하는 전역 매니저" 역할을 함
public class GameManager : Singleton<GameManager>
{
    // ───────────────────────────────
    //   SUB MANAGERS (입력/리소스/사운드)
    // ───────────────────────────────
    // 다른 매니저들을 GameManager 안에서 생성해두고,
    // 어디서든 GameManager.Input 처럼 접근 가능하게 만듦
    InputManager _input = new InputManager();
    ResourceManager _resource = new ResourceManager();
    SoundManager _sound = new SoundManager();

    // 외부에서 쉽게 접근할 수 있는 프로퍼티들
    public static InputManager Input => Instance._input;
    public static ResourceManager Resource => Instance._resource;
    public static SoundManager Sound => Instance._sound;


    // ───────────────────────────────
    //   TIME (게임 제한 시간)
    // ───────────────────────────────
    private float maxTime = 600f;   // 게임 최대 시간 (300초 = 5분)
    private float currentTime;      // 현재 남은 시간


    // ───────────────────────────────
    //   SCORE (점수 및 최고기록)
    // ───────────────────────────────
    private float currentScore = 0f;  // 캐릭터가 올라간 최대 높이
    private float bestScore = 0f;     // 저장된 최고 기록

    public Action OnGameOver;         // 게임오버 시 호출되는 이벤트

    private bool isGameOver = false;
    public bool IsGameOver => isGameOver;
    // ───────────────────────────────
    //   UNITY LIFECYCLE
    // ───────────────────────────────
    protected override void Awake()
    {
        // Singleton<T>의 Awake 실행 (Instance 설정)
        base.Awake();

        currentTime = maxTime; // 처음 시작하면 시간을 풀로 채움

        // PlayerPrefs에서 이전 최고기록 로드
        // 저장된 값이 없으면 0으로 시작
        bestScore = PlayerPrefs.GetFloat("BestScore", 0f);
    }

    private void Start()
    {
        // 게임 프레임 고정 (60FPS)
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        if (isGameOver) return;

        _input.OnUpdate();  // 매 프레임 입력 업데이트

        UpdateTime();       // 매 프레임 남은 시간 감소 처리
    }
    public void ResetRunState()
    {
        isGameOver = false;
        currentScore = 0f;
        currentTime = maxTime;
    }
    public void RestartFromClean(string gameplaySceneName, string hubSceneName = "LoadingScene")
    {
        Time.timeScale = 1f;

        ResetRunState();

        //허브/빈 씬으로 이동
        //다음 프레임 또는 씬 로드 완료 후 gameplayScene 로드
        StartCoroutine(RestartRoutine(gameplaySceneName, hubSceneName));
    }
    private System.Collections.IEnumerator RestartRoutine(string gameplaySceneName, string hubSceneName)
    {
        // hub로 먼저 이동 (씬 리셋 효과)
        SceneManager.LoadScene(hubSceneName);

        // 씬 전환 1초 대기 (오브젝트 파괴/초기화 정리)
        yield return new WaitForSeconds(1f);

        // 실제 게임플레이 씬 진입
        SceneManager.LoadScene(gameplaySceneName);
    }

    // ───────────────────────────────
    //   TIME HANDLING
    // ───────────────────────────────
    private void UpdateTime()
    {
        // 시간이 이미 끝났으면 더 이상 감소시키지 않음
        if (currentTime <= 0f) return;

        // 매 프레임 시간 감소
        currentTime -= Time.deltaTime;

        // 시간이 0 이하가 되면 게임오버
        if (currentTime <= 0f)
        {
            currentTime = 0f;

            SaveScore();        // 종료 시 점수 저장 (최고기록 갱신 가능)
            OnGameOver?.Invoke();  // 등록된 GameOver 함수들 모두 호출
        }
    }

    // UI에서 사용 — "남은 시간 퍼센트" 리턴
    public float GetTimePercent()
    {
        return currentTime / maxTime;
    }


    // ───────────────────────────────
    //   SCORE METHODS
    // ───────────────────────────────

    // 플레이어가 올라간 높이를 받아서
    // 현재 점수보다 크면 갱신
    public void UpdateScore(float height)
    {
        if (height > currentScore)
            currentScore = height;
    }

    // 새 스테이지 시작 시 점수 리셋
    public void ResetScore()
    {
        currentScore = 0f;
    }

    // 현재 점수 가져오기
    public float GetScore()
    {
        return currentScore;
    }

    // 현재 기록이 최고기록인지 확인
    public bool IsBestRecord()
    {
        return currentScore >= bestScore;
    }

    // 최고기록 저장하기
    public void SaveScore()
    {
        if (IsBestRecord())
        {
            bestScore = currentScore;
            PlayerPrefs.SetFloat("BestScore", bestScore);
            PlayerPrefs.Save();
        }
    }

    // 최고기록 가져오기
    public float GetBestScore()
    {
        return bestScore;
    }
    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        currentTime = 0f;
        SaveScore();
        OnGameOver?.Invoke();
    }
    public void BeginRun()
    {
        isGameOver = false;
        currentTime = maxTime;
        currentScore = 0f;
    }
    // 거리 1m 단위 점수 추가
    public void AddScore(int amount)
    {
        currentScore += amount;
    }

}





