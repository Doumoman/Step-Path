using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingManager : MonoBehaviour
{
    // ───────────────────────────────
    //   연출용 오브젝트
    // ───────────────────────────────
    [Header("연출용 오브젝트")]
    public GameObject vine;                 // 덩굴 (300m 지점 왼쪽에 배치, 비활성화 상태)
    public GameObject silhouette;           // 주인 실루엣 (301m 타일 위에 배치, 비활성화 상태)
    public GameObject endingTilemapPrefab;  // 301m 긴 타일맵 프리팹
    public GameObject exclamationMark;      // 느낌표 (Player 자식, 비활성화)

    [Header("플레이어")]
    public PlayerAutoRunner player;         // 포리 (PlayerAutoRunner 컴포넌트 직접 연결)

    // ───────────────────────────────
    //   301m 타일맵 생성 위치
    //   덩굴 꼭대기 위쪽에 배치될 위치
    // ───────────────────────────────
    [Header("301m 타일맵 설정")]
    public Vector3 endingTileSpawnPos;     // Inspector에서 직접 설정

    // ───────────────────────────────
    //   이동 목표 지점
    //   덩굴 아래/위, 주인 위치는 오브젝트 Transform에서 가져오지만
    //   세밀한 보정이 필요하면 여기서 오프셋 조절
    // ───────────────────────────────
    [Header("이동 설정")]
    public float vineClimbOffsetY = 5f;     // 덩굴 아래에서 위까지의 높이 (월드 유닛)
    public float walkSpeed = 2f;            // 주인에게 걸어가는 속도 (월드 유닛/초)
    public float climbSpeed = 1.5f;         // 덩굴 올라가는 속도 (월드 유닛/초)

    // ───────────────────────────────
    //   UI
    // ───────────────────────────────
    [Header("UI")]
    public CanvasGroup endingIllust;        // 엔딩 일러스트 (소녀+포리)
    public CanvasGroup fadePanel;           // 검정 페이드 패널
    public CanvasGroup thankYouText;        // "플레이해주셔서 감사합니다!"
    
    [Header("크레딧 슬라이드")]
    public Image[] creditSlides;              // 6개의 크레딧 이미지
    public float slideDisplayTime = 1f;       // 각 슬라이드 표시 시간
    public float slideTransitionTime = 0.5f;  // 전환(슬라이드) 시간
    

    // ───────────────────────────────
    //   연출 타이밍 설정
    // ───────────────────────────────
    [Header("연출 설정")]
    public float illustFadeDuration = 2f;
    public float illustHoldDuration = 4f;
    public float thankYouFadeDuration = 2f;

    [Header("사운드")]
    public AudioClip BGM_Ending;


    // ───────────────────────────────
    //   GameManager 이벤트 연결
    //   OnGameClear 발동 시 자동으로 엔딩 시작  → StartEnding 자동 호출
    // ───────────────────────────────
    void OnEnable()
    {
        GameManager.Instance.OnGameClear += StartEnding;
    }

    void OnDisable()
    {
        GameManager.Instance.OnGameClear -= StartEnding;
    }


    // ───────────────────────────────
    //   엔딩 시작 진입점
    // ───────────────────────────────
    public void StartEnding()
    {
        Debug.Log("포리 위치: " + player.transform.position);
        StartCoroutine(EndingSequenceRoutine());
    }


    // ───────────────────────────────
    //   엔딩 시퀀스 전체 흐름
    // ───────────────────────────────
    IEnumerator EndingSequenceRoutine()
    {
        // ══════════════════════════════
        //  1단계: 조작 중지 + 환경 정리
        // ══════════════════════════════

        // 포리의 자동 달리기 & 상태머신 비활성화
        // enabled = false로 Update 루프 자체를 멈춤
        player.enabled = false;

        // 포리의 Rigidbody2D 속도도 제거 (있을 경우)
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 몬스터 스포너 중지 (더 이상 몬스터 생성 안 함)
        var spawner = FindObjectOfType<MonsterGridSpawner>();
        if (spawner != null) spawner.enabled = false;

        // BGM 전환 
        SoundManager.Instance.PlayBgm("Ending");


        // ══════════════════════════════
        //  2단계: 덩굴 활성화 + 포리가 덩굴로 이동
        // ══════════════════════════════

        // 300m 지점 왼쪽에 덩굴 표시
        vine.SetActive(true);

        // 포리 방향을 왼쪽(덩굴 쪽)으로 전환
        player.dir = -1;
        player.Sprite.flipX = false;

        // 걷기 애니메이션 재생
        player.PlayAnim(player.WalkHash);

        // 덩굴 아래쪽까지 걸어감
        player.PlayAnim(player.WalkHash);
        Vector3 vineBottom = vine.transform.position;
        yield return StartCoroutine(MovePlayerTo(vineBottom, walkSpeed));


        // ══════════════════════════════
        //  3단계: 301m 타일맵 생성 + 주인 배치
        // ══════════════════════════════

        // 완전한 형태의 긴 타일맵을 덩굴 위에 생성
        Instantiate(endingTilemapPrefab, endingTileSpawnPos, Quaternion.identity);

        // 주인 실루엣 활성화
        silhouette.SetActive(true);
        
        // ══════════════════════════════
        //  4단계: 덩굴 타고 올라감
        // ══════════════════════════════

        // 올라가기 애니메이션으로 전환
        player.PlayAnim(player.ClimbHash);

        // 덩굴 꼭대기까지 위로 이동
        Vector3 vineTop = vineBottom + Vector3.up * vineClimbOffsetY;
        yield return StartCoroutine(MovePlayerTo(vineTop, climbSpeed));
        
        // ══════════════════════════════
        //  5단계: 오른쪽 바라보기 + Idle(흔들기) + 느낌표
        // ══════════════════════════════
 
        // 오른쪽(주인 쪽) 바라보기
        player.dir = 1;
        player.Sprite.flipX = true;
 
        // Idle 스프라이트 애니메이션 재생 (몸 흔들기)
        player.PlayAnim(player.IdleHash);
 
        // 느낌표 등장
        exclamationMark.SetActive(true);
 
        // 1초 유지
        yield return new WaitForSeconds(1f);
 
        // 느낌표 사라짐
        exclamationMark.SetActive(false);
        
        // ══════════════════════════════
        //  6단계: 주인에게 달려감 (Jump 스프라이트 애니메이션)
        // ══════════════════════════════
 
        // Jump 스프라이트 애니메이션 재생 (달리기 동작)
        player.PlayAnim(player.JumpHash);
 
        // 주인 위치까지 이동
        Vector3 masterPos = silhouette.transform.position;
        yield return StartCoroutine(MovePlayerTo(masterPos, walkSpeed));


        // ══════════════════════════════
        //  7단계: 만남 연출 (Scale 축소 + Meet 스프라이트 애니메이션)
        // ══════════════════════════════

        // Scale 0.6으로 축소
        player.transform.localScale = new Vector3(0.6f, 0.6f, player.transform.localScale.z);
 
        // Meet 스프라이트 애니메이션 재생 (만남 + 꼬리 흔들기)
        player.PlayAnim(player.MeetHash);
 
        // 꼬리 흔들기 재생 시간 대기
        yield return new WaitForSeconds(2f);
        
        // 마지막 스프라이트 유지한 채 1초 추가 대기
        yield return new WaitForSeconds(1f);



        // ══════════════════════════════
        //  8단계: 페이드아웃 → 엔딩 일러스트
        // ══════════════════════════════

        // 화면을 검정으로 덮음
        yield return StartCoroutine(Fade(fadePanel, 0f, 1f, 1f));

        // 검정 화면인 동안 게임 오브젝트 숨김
        vine.SetActive(false);
        silhouette.SetActive(false);
        player.gameObject.SetActive(false);

        // 일러스트를 투명 상태로 활성화
        endingIllust.gameObject.SetActive(true);
        endingIllust.alpha = 0f;

        // 검정 화면 걷어냄
        yield return StartCoroutine(Fade(fadePanel, 1f, 0f, 1f));

        // 일러스트를 서서히 보여줌
        yield return StartCoroutine(Fade(endingIllust, 0f, 1f, illustFadeDuration));

        // 일러스트 감상 시간
        yield return new WaitForSeconds(illustHoldDuration);


        // ══════════════════════════════
        //  9단계: 크레딧 스크롤
        // ══════════════════════════════

        // 일러스트 페이드아웃
        yield return StartCoroutine(Fade(endingIllust, 1f, 0f, 1f));

        // 크레딧 슬라이드쇼 재생
        yield return StartCoroutine(PlayCreditSlides());
        

        // ══════════════════════════════
        //  10단계: 마무리
        // ══════════════════════════════

        // 최종 페이드아웃
        yield return StartCoroutine(Fade(fadePanel, 0f, 1f, 2f));

        // 타이틀 씬으로 (씬 이름에 맞게 수정)
        // SceneManager.LoadScene("TitleScene");
    }


    // ───────────────────────────────
    //   포리 이동 함수
    //   PlayerAutoRunner의 픽셀 스냅 방식과 동일하게
    //   Transform.position을 직접 이동
    //   MoveTowards로 목표 지점까지 부드럽게 이동
    // ───────────────────────────────
    IEnumerator MovePlayerTo(Vector3 target, float speed)
    {
        // 픽셀 단위 스냅을 위한 값
        float step = player.unitPerPixel;

        while (Vector3.Distance(player.transform.position, target) > step)
        {
            // 목표를 향해 일정 속도로 이동
            player.transform.position = Vector3.MoveTowards(
                player.transform.position,
                target,
                speed * Time.deltaTime
            );

            // 픽셀 스냅 (기존 게임과 동일한 느낌 유지)
            Vector3 pos = player.transform.position;
            pos.x = Mathf.Round(pos.x / step) * step;
            pos.y = Mathf.Round(pos.y / step) * step;
            player.transform.position = pos;

            yield return null;
        }

        // 최종 위치 정확히 맞춤
        player.transform.position = target;
    }


    // ───────────────────────────────
    //   공용 페이드 함수
    //   CanvasGroup alpha를 from → to로 duration초에 걸쳐 변경
    // ───────────────────────────────
    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        cg.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
    }


    // ───────────────────────────────
    //   크레딧 스크롤 함수
    // ───────────────────────────────
    
    IEnumerator PlayCreditSlides()
    {
        // 모든 슬라이드 초기화
        foreach (var slide in creditSlides)
        {
            slide.gameObject.SetActive(false);
            slide.color = new Color(1f, 1f, 1f, 0f);
        }

        for (int i = 0; i < creditSlides.Length; i++)
        {
            creditSlides[i].gameObject.SetActive(true);

            // 페이드인
            float elapsed = 0f;
            while (elapsed < slideTransitionTime)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(0f, 1f, elapsed / slideTransitionTime);
                creditSlides[i].color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
            creditSlides[i].color = new Color(1f, 1f, 1f, 1f);

            // 표시 유지
            yield return new WaitForSeconds(slideDisplayTime);

            // 마지막이 아니면 페이드아웃
            if (i < creditSlides.Length - 1)
            {
                elapsed = 0f;
                while (elapsed < slideTransitionTime)
                {
                    elapsed += Time.deltaTime;
                    float a = Mathf.Lerp(1f, 0f, elapsed / slideTransitionTime);
                    creditSlides[i].color = new Color(1f, 1f, 1f, a);
                    yield return null;
                }
                creditSlides[i].color = new Color(1f, 1f, 1f, 0f);
                creditSlides[i].gameObject.SetActive(false);
            }
        }
    }
}