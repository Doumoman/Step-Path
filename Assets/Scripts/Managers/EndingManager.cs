using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingManager : MonoBehaviour
    {
        [Header("연출용 오브젝트")]
        public GameObject endingTile;    // 300m 타일 발판
        public GameObject vine;          // 301m 덩굴
        public GameObject silhouette;    // 주인 실루엣
    
        [Header("UI 및 애니메이션")]
        public Animator poriAnimator;    // 포리 애니메이션 컨트롤러
        public CanvasGroup endingIllust; // 엔딩 일러스트 (페이드용)
        public CanvasGroup fadePanel; //검정화면 패널, 장면 전환 페이드에 사용
        public RectTransform creditsContent; //크레딧 내용이 담긴 RectTransform (스크롤 대상)
        public CanvasGroup creditsCanvasGroup; //크레딧 전체를 감싸는 CanvasGroup (페이드용)
        public CanvasGroup thankYouText; // "플레이해주셔서 감사합니다!" 텍스트
        
        [Header("연출 설정")]
        public float illustFadeDuration = 2f;       // 엔딩 일러스트가 나타나는 데 걸리는 시간
        public float illustHoldDuration = 4f;       // 일러스트를 보여주는 유지 시간
        public float creditsScrollSpeed = 60f;      // 크레딧이 위로 올라가는 속도 (픽셀/초)
        public float creditsEndY = 1500f;           // 크레딧 스크롤이 끝나는 Y 위치
        public float thankYouFadeDuration = 2f;     // 감사 메시지 페이드인 시간
        
    
        [Header("사운드")]
        public AudioClip pianoBGM; // 엔딩에서 재생할 피아노 BGM 클립
        
        //   이벤트 구독
        //   GameManager의 OnGameClear 이벤트에 연결
        //   300m 도달 시 자동으로 StartEnding이 호출됨
        // ───────────────────────────────
        void OnEnable()
        {
            // 이 오브젝트가 활성화될 때 클리어 이벤트 구독
            GameManager.Instance.OnGameClear += StartEnding;
        }

        void OnDisable()
        {
            // 이 오브젝트가 비활성화될 때 구독 해제 (메모리 누수 방지)
            GameManager.Instance.OnGameClear -= StartEnding;
        }


        // ───────────────────────────────
        //   엔딩 시작 진입점
        //   OnGameClear 이벤트가 발동하면 이 함수가 호출됨
        // ───────────────────────────────
        public void StartEnding()
        {
            StartCoroutine(EndingSequenceRoutine());
        }
        // ───────────────────────────────
        //   엔딩 시퀀스 코루틴
        //   전체 엔딩 흐름을 순서대로 실행
        // ───────────────────────────────
          IEnumerator EndingSequenceRoutine() 
          { 
              // ── 1단계: 환경 조성 ──
              // // 300m 지점에 엔딩용 오브젝트들을 화면에 표시
              endingTile.SetActive(true);
              vine.SetActive(true);
              silhouette.SetActive(true);

              // BGM을 피아노 곡으로 전환 (사운드 매니저에 맞게 주석 해제)
              // BGMManager.instance.ChangeBGM(pianoBGM);

              // 플레이어 조작 중지 (조작 스크립트에 맞게 주석 해제)
              // // PlayerControl.instance.DisableInput();


              // ── 2단계: 포리가 주인에게 달려감 ──
              // 애니메이터에서 "RunToMaster" 트리거를 발동시켜
              // 포리가 실루엣 쪽으로 뛰어가는 애니메이션 재생
              poriAnimator.SetTrigger("RunToMaster");

              // 달려가는 애니메이션이 끝날 때까지 3초 대기
              yield return new WaitForSeconds(3f);


              // ── 3단계: 화면 페이드아웃 → 엔딩 일러스트 표시 ──
              // 검정 화면으로 서서히 덮음 (1초)
              yield return StartCoroutine(Fade(fadePanel, 0f, 1f, 1f));

              // 검정 화면인 동안 게임 오브젝트들 숨김
              endingTile.SetActive(false);
              vine.SetActive(false);
              silhouette.SetActive(false);

              // 엔딩 일러스트 준비 (아직 투명한 상태로 활성화)
              endingIllust.gameObject.SetActive(true);
              endingIllust.alpha = 0f;

              // 검정 화면 걷어냄 (1초)
              yield return StartCoroutine(Fade(fadePanel, 1f, 0f, 1f));

              // 엔딩 일러스트를 서서히 보여줌
              yield return StartCoroutine(Fade(endingIllust, 0f, 1f, illustFadeDuration));

              // 일러스트를 일정 시간 동안 유지 (감상 시간)
              yield return new WaitForSeconds(illustHoldDuration);


              // ── 4단계: 크레딧 스크롤 ──
              // 일러스트를 서서히 사라지게 함
              yield return StartCoroutine(Fade(endingIllust, 1f, 0f, 1f));

              // 크레딧 UI 활성화 및 표시
              creditsContent.gameObject.SetActive(true);
              creditsCanvasGroup.alpha = 1f;

              // 크레딧을 아래에서 위로 스크롤
              yield return StartCoroutine(ScrollCredits());


              // ── 5단계: 감사 메시지 ──
              // 크레딧을 서서히 사라지게 함
              yield return StartCoroutine(Fade(creditsCanvasGroup, 1f, 0f, 1f));

              // "플레이해주셔서 감사합니다!" 텍스트 페이드인
              thankYouText.gameObject.SetActive(true);
              yield return StartCoroutine(Fade(thankYouText, 0f, 1f, thankYouFadeDuration));

              // 감사 메시지를 3초간 보여줌
              yield return new WaitForSeconds(3f);


              // ── 6단계: 마무리 ──
              // 최종 페이드아웃 (검정 화면으로 마무리)
              yield return StartCoroutine(Fade(fadePanel, 0f, 1f, 2f));

              // 타이틀 씬으로 돌아감 (씬 이름에 맞게 수정)
              // SceneManager.LoadScene("TitleScene");
          }


          // ───────────────────────────────
          //   공용 페이드 함수
          //   CanvasGroup의 alpha 값을 from → to로
          //   duration 초에 걸쳐 부드럽게 변경
          // ───────────────────────────────
          IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
          {
              // CanvasGroup이 할당 안 되어 있으면 건너뜀
              if (cg == null) yield break;

              // 페이드 대상을 활성화
              cg.gameObject.SetActive(true);

              float elapsed = 0f;

              while (elapsed < duration)
              {
                  // 경과 시간 누적
                  elapsed += Time.deltaTime;

                  // Lerp로 alpha를 부드럽게 보간
                  // elapsed/duration이 0→1로 가면서 from→to로 변화
                  cg.alpha = Mathf.Lerp(from, to, elapsed / duration);

                  // 다음 프레임까지 대기
                  yield return null;
              }

              // 최종값을 정확히 맞춰줌 (부동소수점 오차 방지)
              cg.alpha = to;
          }


        // ───────────────────────────────
        //   크레딧 스크롤 함수
        //   creditsContent의 anchoredPosition.y를
        //   매 프레임 위로 이동시켜 스크롤 효과 구현
        // ───────────────────────────────
        IEnumerator ScrollCredits()
        {
            // Y 위치가 목표값(creditsEndY)에 도달할 때까지 반복
            while (creditsContent.anchoredPosition.y < creditsEndY)
            {
                // 매 프레임 위로 이동 (속도 × 프레임 시간)
                creditsContent.anchoredPosition += Vector2.up * creditsScrollSpeed * Time.deltaTime;

                // 다음 프레임까지 대기
                yield return null;
            }
        }
    } 