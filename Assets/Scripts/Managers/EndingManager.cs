using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndingManager : MonoBehaviour
{
    // ───────────────────────────────
    //   엔딩 조건
    // ───────────────────────────────
    [Header("엔딩 조건")]
    [SerializeField] private float endingGoalScore = 100f;

    private bool endingTriggered = false;

    // ───────────────────────────────
    //   연출용 오브젝트
    // ───────────────────────────────
    [Header("연출용 오브젝트")]
    public GameObject vine;                 // 덩굴 오브젝트
    public GameObject silhouette;           // 주인 실루엣
    public GameObject exclamationMark;      // 느낌표, Player 자식 권장

    // ───────────────────────────────
    //   플레이어
    // ───────────────────────────────
    [Header("플레이어")]
    public PlayerAutoRunner player;

    // ───────────────────────────────
    //   게임 UI
    // ───────────────────────────────
    [Header("게임 UI")]
    [SerializeField] private GameObject gameplayUICanvas;
    [SerializeField] private GameObject slotUICanvas;

    // ───────────────────────────────
    //   엔딩 고정 좌표
    // ───────────────────────────────
    [Header("엔딩 고정 좌표")]
    [SerializeField] private float endingLockedY = 96.44f;
    [SerializeField] private float vineX = -1f;
    [SerializeField] private float vineTopY = 97.44f;
    [SerializeField] private float meetTargetX = 1.25f;
    // ───────────────────────────────
    //   이동 설정
    // ───────────────────────────────
    [Header("이동 설정")]
    public float walkSpeed = 2f;
    public float climbSpeed = 1.5f;

    // ───────────────────────────────
    //   엔딩 UI
    // ───────────────────────────────
    [Header("엔딩 UI")]
    public CanvasGroup endingIllust;
    public CanvasGroup fadePanel;

    [Header("크레딧 슬라이드")]
    public Image[] creditSlides;
    public float slideDisplayTime = 1f;
    public float slideTransitionTime = 0.5f;

    [Header("추가 엔딩 시퀀스")]
    [SerializeField] private GameObject endingSequenceFirstSprite;
    [SerializeField] private Transform endingTapeRoot;
    [SerializeField] private GameObject[] endingTapeSprites;

    [SerializeField] private float firstSpriteFadeDuration = 1f;
    [SerializeField] private float spriteFadeDuration = 0.5f;

    [SerializeField] private float tapeSpriteSpacingY = 7.1f;
    [SerializeField] private int tapeStackDirectionY = -1;
    [SerializeField] private int tapeMoveDirectionY = 1;

    [SerializeField] private float tapeMoveAmountY = 7.1f;
    [SerializeField] private float tapeMoveDuration = 1f;
    [SerializeField] private float tapeHoldDuration = 3f;
    [SerializeField] private float tapeFadeDuration = 0.5f;

    [SerializeField] private AnimationCurve tapeMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // ───────────────────────────────
    //   연출 타이밍 설정
    // ───────────────────────────────
    [Header("연출 설정")]
    public float illustFadeDuration = 2f;
    public float illustHoldDuration = 4f;

    // ───────────────────────────────
    //   GameManager 이벤트 연결
    // ───────────────────────────────
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameClear += StartEnding;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameClear -= StartEnding;
        }
    }

    private void Update()
    {
        if (endingTriggered)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsCleared)
            return;

        if (GameManager.Instance.GetScore() >= endingGoalScore)
        {
            endingTriggered = true;
            GameManager.Instance.TriggerGameClear();
        }
    }

    // ───────────────────────────────
    //   엔딩 시작 진입점
    // ───────────────────────────────
    public void StartEnding()
    {
        if (player == null)
        {
            Debug.LogWarning("EndingManager: player 참조가 없습니다.");
            return;
        }

        Debug.Log("엔딩 시작 / 포리 위치: " + player.transform.position);

        StopAllCoroutines();
        StartCoroutine(EndingSequenceRoutine());
    }

    // ───────────────────────────────
    //   엔딩 시퀀스 전체 흐름
    // ───────────────────────────────
    private IEnumerator EndingSequenceRoutine()
    {
        // ══════════════════════════════
        //  1단계: 플레이어 상태 정지 + 환경 정리
        // ══════════════════════════════

        player.EnterEndingState();

        if (gameplayUICanvas != null)
        {
            gameplayUICanvas.SetActive(false);
            slotUICanvas.SetActive(false);
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        MonsterGridSpawner spawner = FindObjectOfType<MonsterGridSpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBgm("Ending");
        }

        // ══════════════════════════════
        //  2단계: 플레이어 Y를 96.44로 강제 고정
        // ══════════════════════════════

        Vector3 startPos = player.transform.position;
        startPos.y = endingLockedY;
        player.transform.position = startPos;

        player.vyPixels = 0f;
        player.pixelAccum = Vector2.zero;
        player.onGround = false;

        // ══════════════════════════════
        //  3단계: 덩굴 활성화 + x = -1, y = 96.44로 이동
        // ══════════════════════════════

        if (vine != null)
        {
            vine.SetActive(true);
        }

        player.SetFacing(-1);
        player.PlayAnim(player.WalkHash);

        yield return StartCoroutine(
            MovePlayerXToWithFixedY(vineX, endingLockedY, walkSpeed)
        );

        // 정확히 덩굴 아래 위치로 고정
        Vector3 vineBottom = player.transform.position;
        vineBottom.x = vineX;
        vineBottom.y = endingLockedY;
        player.transform.position = vineBottom;

        // ══════════════════════════════
        //  4단계: Climb 애니메이션 + y = 97.44까지 상승
        // ══════════════════════════════

        player.PlayAnim(player.ClimbHash);

        yield return StartCoroutine(
            MovePlayerYToWithFixedX(vineX, vineTopY, climbSpeed)
        );

        // ══════════════════════════════
        //  5단계: 실루엣 활성화
        // ══════════════════════════════

        if (silhouette != null)
        {
            silhouette.SetActive(true);
        }

        // ══════════════════════════════
        //  6단계: 오른쪽 바라보기 + Idle + 느낌표
        // ══════════════════════════════

        player.SetFacing(1);
        player.PlayAnim(player.IdleHash);

        if (exclamationMark != null)
        {
            exclamationMark.SetActive(true);
        }

        yield return new WaitForSeconds(1f);

        if (exclamationMark != null)
        {
            exclamationMark.SetActive(false);
        }

        // ══════════════════════════════
        //  7단계: 실루엣으로 이동
        // ══════════════════════════════

        player.PlayAnim(player.JumpHash);

        yield return StartCoroutine(
            MovePlayerXToWithFixedY(meetTargetX, vineTopY, walkSpeed)
        );

        // ══════════════════════════════
        //  8단계: 만남 연출
        // ══════════════════════════════

        player.SetFacing(-1);
        player.transform.localScale = new Vector3(
            0.2f,
            0.2f,
            player.transform.localScale.z
        );

        player.PlayAnim(player.MeetHash);

        yield return new WaitForSeconds(2f);
        yield return new WaitForSeconds(1f);

        // ══════════════════════════════
        //  9단계: 페이드아웃 → 엔딩 일러스트
        // ══════════════════════════════

        // 추가 엔딩 시퀀스 실행
        yield return StartCoroutine(PlayAdditionalEndingSequence());

// 이후 기존 페이드아웃
        yield return StartCoroutine(Fade(fadePanel, 0f, 1f, 1f));

        yield return new WaitForSeconds(10f);
        SceneManager.LoadScene("Main");
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBgm("Main");
        }
    }

    // ───────────────────────────────
    //   X축 이동 + Y축 완전 고정
    // ───────────────────────────────
    private IEnumerator MovePlayerXToWithFixedY(float targetX, float fixedY, float speed)
    {
        while (Mathf.Abs(player.transform.position.x - targetX) > 0.001f)
        {
            Vector3 pos = player.transform.position;

            pos.x = Mathf.MoveTowards(
                pos.x,
                targetX,
                speed * Time.deltaTime
            );

            // Y는 무조건 고정
            pos.y = fixedY;

            player.transform.position = pos;

            yield return null;
        }

        Vector3 finalPos = player.transform.position;
        finalPos.x = targetX;
        finalPos.y = fixedY;
        player.transform.position = finalPos;
    }
    // ───────────────────────────────
    //   Y축 이동 + X축 완전 고정
    // ───────────────────────────────
    private IEnumerator MovePlayerYToWithFixedX(float fixedX, float targetY, float speed)
    {
        while (Mathf.Abs(player.transform.position.y - targetY) > 0.001f)
        {
            Vector3 pos = player.transform.position;

            // X는 무조건 고정
            pos.x = fixedX;

            pos.y = Mathf.MoveTowards(
                pos.y,
                targetY,
                speed * Time.deltaTime
            );

            player.transform.position = pos;

            yield return null;
        }

        Vector3 finalPos = player.transform.position;
        finalPos.x = fixedX;
        finalPos.y = targetY;
        player.transform.position = finalPos;
    }

    // ───────────────────────────────
    //   일반 목표 위치 이동
    // ───────────────────────────────
    private IEnumerator MovePlayerTo(Vector3 target, float speed)
    {
        while (Vector3.Distance(player.transform.position, target) > 0.001f)
        {
            player.transform.position = Vector3.MoveTowards(
                player.transform.position,
                target,
                speed * Time.deltaTime
            );

            yield return null;
        }

        player.transform.position = target;
    }

    // ───────────────────────────────
    //   CanvasGroup 페이드
    // ───────────────────────────────
    private IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

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
    //   크레딧 슬라이드
    // ───────────────────────────────
    private IEnumerator PlayCreditSlides()
    {
        if (creditSlides == null || creditSlides.Length == 0)
            yield break;

        foreach (Image slide in creditSlides)
        {
            if (slide == null)
                continue;

            slide.gameObject.SetActive(false);
            slide.color = new Color(1f, 1f, 1f, 0f);
        }

        for (int i = 0; i < creditSlides.Length; i++)
        {
            Image slide = creditSlides[i];

            if (slide == null)
                continue;

            slide.gameObject.SetActive(true);

            float elapsed = 0f;

            while (elapsed < slideTransitionTime)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(0f, 1f, elapsed / slideTransitionTime);
                slide.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }

            slide.color = new Color(1f, 1f, 1f, 1f);

            yield return new WaitForSeconds(slideDisplayTime);

            if (i < creditSlides.Length - 1)
            {
                elapsed = 0f;

                while (elapsed < slideTransitionTime)
                {
                    elapsed += Time.deltaTime;
                    float a = Mathf.Lerp(1f, 0f, elapsed / slideTransitionTime);
                    slide.color = new Color(1f, 1f, 1f, a);
                    yield return null;
                }

                slide.color = new Color(1f, 1f, 1f, 0f);
                slide.gameObject.SetActive(false);
            }
        }
    }
   private IEnumerator PlayAdditionalEndingSequence()
{
    // 1. 첫 번째 배경/연출 스프라이트 페이드인
    if (endingSequenceFirstSprite != null)
    {
        endingSequenceFirstSprite.SetActive(true);
        SetObjectAlpha(endingSequenceFirstSprite, 0f);

        yield return StartCoroutine(
            FadeObjectAlpha(endingSequenceFirstSprite, 0f, 1f, firstSpriteFadeDuration)
        );
    }
    PrepareEndingTapeSprites();
    yield return new WaitForSeconds(5f);
    if (endingSequenceFirstSprite != null)
    {
        yield return StartCoroutine(
            FadeObjectAlpha(endingSequenceFirstSprite, 1f, 0f, firstSpriteFadeDuration)
        );
        endingSequenceFirstSprite.SetActive(false);
    }
    if (endingTapeRoot != null && endingTapeSprites != null && endingTapeSprites.Length > 0)
    {

        yield return StartCoroutine(PlayEndingTapeSequence());
    }
}
private void PrepareEndingTapeSprites()
{
    if (endingTapeRoot == null || endingTapeSprites == null)
        return;

    endingTapeRoot.gameObject.SetActive(true);

    for (int i = 0; i < endingTapeSprites.Length; i++)
    {
        GameObject obj = endingTapeSprites[i];

        if (obj == null)
            continue;

        obj.SetActive(true);
        SetObjectAlpha(obj, 1f);

        obj.transform.SetParent(endingTapeRoot, false);

        Vector3 localPos = Vector3.zero;
        localPos.x = 0f;
        localPos.y = i * tapeSpriteSpacingY * tapeStackDirectionY;
        localPos.z = 0f;

        obj.transform.localPosition = localPos;
    }
}
private void SetObjectAlpha(GameObject obj, float alpha)
{
    if (obj == null)
        return;

    CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
    if (canvasGroup != null)
    {
        canvasGroup.alpha = alpha;
    }

    Image image = obj.GetComponent<Image>();
    if (image != null)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
    if (spriteRenderer != null)
    {
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }
}
private IEnumerator FadeObjectAlpha(GameObject obj, float from, float to, float duration)
{
    if (obj == null)
        yield break;

    obj.SetActive(true);

    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        float alpha = Mathf.Lerp(from, to, t);

        SetObjectAlpha(obj, alpha);

        yield return null;
    }

    SetObjectAlpha(obj, to);
}
private IEnumerator PlayEndingTapeSequence()
{
    if (endingTapeRoot == null || endingTapeSprites == null || endingTapeSprites.Length == 0)
        yield break;

    Vector3 rootStartPos = endingTapeRoot.position;

    // Element 0 먼저 보여주고 정지
    endingTapeRoot.position = rootStartPos;
    yield return new WaitForSeconds(tapeHoldDuration);

    // Element 1, 2, 3... 순서대로 보여주기
    for (int i = 1; i < endingTapeSprites.Length; i++)
    {
        Vector3 from = endingTapeRoot.position;

        Vector3 to = rootStartPos
            + Vector3.up * (tapeMoveAmountY * tapeMoveDirectionY * i);

        yield return StartCoroutine(
            MoveTapeRoot(from, to, tapeMoveDuration)
        );

        yield return new WaitForSeconds(tapeHoldDuration);
    }
}
private IEnumerator MoveTapeRoot(Vector3 from, Vector3 to, float duration)
{
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        float easedT = tapeMoveCurve.Evaluate(t);

        endingTapeRoot.position = Vector3.Lerp(from, to, easedT);

        yield return null;
    }

    endingTapeRoot.position = to;
}
}