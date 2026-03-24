using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class PlayerAutoRunner : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private int animLayer = 0;
    public SpriteRenderer Sprite => sr;
    [HideInInspector] public bool overrideFlip;
    public const string ANIM_WALK = "Walk";
    public const string ANIM_IDLE = "Idle";
    public const string ANIM_CLIMB = "Climb";
    public const string ANIM_JUMP = "Jump";
    public const string ANIM_FALL = "Fall";
    public const string ANIM_STAIRCLIMB = "StairClimb";
    public const string ANIM_ROCKET = "Rocket";
    public const string ANIM_GAMEOVER = "GameOver";
    private int _walkHash, _idleHash, _climbHash, _jumpHash, _fallHash, _stairclimbHash, _rocketHash, _gameOverHash;
    private Action _onGameOverAnimationFinished;

    [Header("Speed By Height")]
    [SerializeField] private bool useHeightSpeed = true;
    [SerializeField] private float baseHeightY = 0f;

    [SerializeField, Min(0.01f)] private float heightStepUnits = 5f;

    [SerializeField] private float speedAddPerStepPixels = 2f;

    [SerializeField] private float minRunSpeedPixels = 10f;
    [SerializeField] private float maxRunSpeedPixels = 20f;

    private float _baseRunSpeedPixels;
    [Header("Pixel")]
    public int pixelsPerUnit = 16;
    public float runSpeedPixelsPerSec = 40f;

    [Header("Enter By X")]
    [SerializeField, Min(0f)] public float enterXTolerancePixels = 2f; // 목표X 도달 허용 오차(픽셀)
    public float EnterXTolUnits => enterXTolerancePixels * unitPerPixel;
    [Header("Gravity")]
    public float gravityPixelsPerSec2 = 480f;
    public float maxFallSpeedPixelsPerSec = 360f;

    [Header("Air Smoothing")]
    [SerializeField, Range(1, 8)] private int airYSubDiv = 2;   // 2~4 권장 (1이면 기존과 동일)
    [SerializeField] private bool smoothAirY = true;

    [Header("Ground Snap")]
    [SerializeField, Min(0f)] public float groundSnapToleranceUnits = 0.03f;

    [Header("Layers/Maps")]
    public LayerMask groundMask;

    [Header("Climb (Ladder / Sprout)")]
    public LayerMask ladderMask;

    // 버섯처럼 "앞"에 프로브로 감지
    [SerializeField, Min(0.01f)] public float ladderProbe = 0.12f;

    // “잡고 잠깐 대기했다가” 오르기 시작 (요청한 ‘시간’)
    [SerializeField, Min(0f)] public float climbEnterDelaySec = 0.12f;

    // 오르는 속도(외부에서 조절)
    [SerializeField, Min(1f)] public float climbSpeedPixelsPerSec = 24f;

    // 어디까지 오를지: 콜라이더 top 기준으로 약간 더 올리는 보정
    [SerializeField, Min(0f)] public float ladderTopExtra = 0.05f;

    [SerializeField, Min(0f)] public float climbCooldownSec = 1f; // 0이면 쿨타임 없음
    [HideInInspector] public int climbCD;

    [Header("Wall Reverse (Inspector에서 선택)")]
    public LayerMask reverseOnMask;
    public float wallProbe = 0.10f;
    public bool reverseAlsoInAir = true;
    public int reverseCooldownFrames = 6;

    [Header("Cast Stabilize")]
    [SerializeField] private Collider2D bodyCol;
    [SerializeField] private bool useColliderBoundsForCasts = true;
    [SerializeField, Min(0f)] private float castSkin = 0.01f;
    [SerializeField, Min(0f)] private float stepUpMaxUnits = 0.25f;
    [SerializeField, Min(0f)] private float groundProbeUnits = 0.40f;
    [SerializeField, Range(0.3f, 1f)] private float groundCastWidthScale = 0.85f; // 바닥 판정 BoxCast 폭 축소(모서리 오판정 감소)
    [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.5f;       // 바닥으로 인정할 노말 y(0.5면 대략 60도 이상을 바닥으로 인정)

    [Header("Stairs")]
    [Header("Stairs")]
    public LayerMask stairsMask;
    [SerializeField, Min(0.01f)] public float stairsProbe = 0.12f;
    [SerializeField, Min(0.01f)] public float stairsRiseUnits = 1.0f;
    [SerializeField, Range(0.1f, 1.5f)] public float stairsShapeEase = 1.0f;
    [SerializeField] public bool invertStairsDirection = false;
    [SerializeField, Min(0f)] public float stairsCooldownSec = 0.2f;

    [Header("Rocket Lift")]
    public LayerMask rocketMask;
    [SerializeField, Min(0.01f)] public float rocketProbe = 0.12f;
    // 올라가는 속도 (px/s)
    [SerializeField, Min(1f)] public float rocketLiftSpeedPixelsPerSec = 64f;
    // 얼마나 올릴지(월드 유닛). "고정 상승" 모드
    [SerializeField, Min(0f)] public float rocketRiseUnits = 3.0f;
    // 또는 로켓 콜라이더 top까지 올리고 싶으면 사용(선택)
    [SerializeField] public bool rocketUseColliderTop = false;
    [SerializeField, Min(0f)] public float rocketTopExtra = 0.05f;
    [SerializeField, Min(0f)] public float rocketCooldownSec = 0.2f;

    [Header("Mushroom Jump")]
    public LayerMask mushroomMask;
    public float mushroomProbe = 0.12f;
    public int mushroomCooldownFrames = 8;
    public float jumpPeakHeightPixels = 64f;
    [Range(0.5f, 3f)]
    public float jumpHorizSpeedScale = 1.25f;
    public float mushroomJumpDelaySec = 0.25f;
    public bool requireGroundedAtLaunch = true;

    [Header("Big Mushroom Jump")]
    public LayerMask bigMushroomMask;

    // big 버섯 전용 점프 파라미터 (값만 다르게)
    public float bigJumpPeakHeightPixels = 96f;
    [SerializeField, Min(0.05f)] private float bigJumpTimeToApexSec = 0.26f;
    [Range(0.5f, 3f)] public float bigJumpHorizSpeedScale = 1.35f;
    public int bigMushroomCooldownFrames = 12;

    [Header("Jump Tuning")]
    [SerializeField, Min(0.05f)]
    private float jumpTimeToApexSec = 0.22f;          // 정점까지 걸리는 시간(작을수록 더 빠르게 상승)
    [SerializeField, Range(1f, 6f)]
    private float fallGravityMultiplier = 2.0f;       // 낙하 가속 배수(클수록 더 빨리 떨어짐)

    [Header("Game Over")]
    [SerializeField] private LayerMask monsterLayerMask;
    [SerializeField] private UIsGameOver gameOverPopup;

    private bool _isGameOverRequested;

    #region 런타임 필드
    [HideInInspector] public float pendingJumpTimer = -1f;
    [HideInInspector] public int pendingJumpDir = 1;
    [HideInInspector] public int mushroomCD;
    [HideInInspector] public float jumpHorizSpeedPixelsPerSec;

    [HideInInspector] public float unitPerPixel;
    [HideInInspector] public Vector2 pixelAccum;
    [HideInInspector] public float vyPixels;
    [HideInInspector] public float lockedY;
    [HideInInspector] public bool onGround;
    [HideInInspector] public int reverseCD;
    [HideInInspector] public int dir = 1;
    [HideInInspector] public bool lastHorizontalBlocked;

    // ───────── Climb 런타임 ─────────
    [HideInInspector] public bool pendingClimb;          // 예약 여부
    [HideInInspector] public float pendingClimbTargetX;  // 목표 X (centerX)
    [HideInInspector] public float pendingClimbTargetCenterY; // 목표 높이

    [HideInInspector] public float climbCenterX;
    [HideInInspector] public float climbTargetCenterY;

    // 상태머신
    private PlayerStateMachine stateMachine;
    // ───────── Mushroom 런타임 ─────────
    public enum MushroomKind { Normal, Big }

    [HideInInspector] public MushroomKind pendingMushroomKind;
    [HideInInspector] public bool pendingMushroom;       // 예약 여부
    [HideInInspector] public float pendingMushroomTargetX;


    // ───────── Stair 런타임 ─────────
    [HideInInspector] public int stairsCD;
    [HideInInspector] public bool pendingStairs;
    [HideInInspector] public float pendingStairsTargetX;
    [HideInInspector] public Vector2 stairStart;   // (x,y) : 선분의 시작(월드)
    [HideInInspector] public Vector2 stairEnd;     // (x,y) : 선분의 끝(월드)
    [HideInInspector] public float stairSlope;     // dy/dx
    [HideInInspector] public int stairMoveDir;     // +1 또는 -1 (계단 진행 방향)

    // ───────── Rocket 런타임 ─────────
    [HideInInspector] public int rocketCD;
    [HideInInspector] public bool pendingRocket;
    [HideInInspector] public float pendingRocketTargetX;
    [HideInInspector] public float rocketCenterX;
    [HideInInspector] public float rocketStartCenterY;
    [HideInInspector] public float rocketTargetCenterY;
    [HideInInspector] public Collider2D pendingRocketCol;
    #endregion

    // ───────────────── 캐스트 기준 ─────────────────
    public Vector2 CastSize
    {
        get
        {
            if (useColliderBoundsForCasts && bodyCol) return bodyCol.bounds.size;
            // 예외적으로 bodyCol이 없을 때 최소값 (충돌 불안정 방지)
            return new Vector2(0.5f, 0.9f);
        }
    }

    private Vector2 CastOrigin
    {
        get
        {
            if (useColliderBoundsForCasts && bodyCol) return bodyCol.bounds.center;
            return (Vector2)transform.position;
        }
    }
    void Awake()
    {
        unitPerPixel = 1f / Mathf.Max(1, pixelsPerUnit);
        _baseRunSpeedPixels = runSpeedPixelsPerSec;
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (!anim) anim = GetComponent<Animator>();

        _walkHash = Animator.StringToHash(ANIM_WALK);
        _idleHash = Animator.StringToHash(ANIM_IDLE);
        _climbHash = Animator.StringToHash(ANIM_CLIMB);
        _jumpHash = Animator.StringToHash(ANIM_JUMP);
        _fallHash = Animator.StringToHash(ANIM_FALL);
        _stairclimbHash = Animator.StringToHash(ANIM_STAIRCLIMB);
        _rocketHash = Animator.StringToHash(ANIM_ROCKET);
        _gameOverHash = Animator.StringToHash(ANIM_GAMEOVER);
        stateMachine = new PlayerStateMachine();
    }

    void Start()
    {
        ChangeState(new PlayerRunState(this, stateMachine));
    }

    void Update()
    {
        if (reverseCD > 0) reverseCD--;
        if (mushroomCD > 0) mushroomCD--;
        if (climbCD > 0) climbCD--;
        if (stairsCD > 0) stairsCD--;
        if (rocketCD > 0) rocketCD--;
        stateMachine.Update();
        if (useHeightSpeed)
        {
            float h = transform.position.y - baseHeightY;
            int step = Mathf.Max(0, Mathf.FloorToInt(h / Mathf.Max(0.0001f, heightStepUnits)));

            float speed = _baseRunSpeedPixels + step * speedAddPerStepPixels;
            runSpeedPixelsPerSec = Mathf.Clamp(speed, minRunSpeedPixels, maxRunSpeedPixels);
        }
    }
    void LateUpdate()
    {
        if (sr && !overrideFlip)
            sr.flipX = (dir > 0);
    }
    public void ChangeState(IPlayerState newState)
    {
        stateMachine.ChangeState(newState);

        // 카메라에 현재 상태 알려주기
        if (CameraMover.Instance != null)
        {
            CameraMover.Instance.OnPlayerStateChanged(newState);
        }
    }
    public void SetGameOver(Action onAnimationFinished = null)
    {
        _onGameOverAnimationFinished = onAnimationFinished;
        ChangeState(new PlayerGameOverState(this, stateMachine));
    }

    public void InvokeGameOverAnimationFinished()
    {
        Action callback = _onGameOverAnimationFinished;
        _onGameOverAnimationFinished = null;
        callback?.Invoke();
    }
    public void RequestGameOver()
    {
        if (_isGameOverRequested)
            return;

        _isGameOverRequested = true;

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();

        SetGameOver(OnGameOverAnimationFinished);
    }

    private void OnGameOverAnimationFinished()
    {
        if (gameOverPopup != null)
            gameOverPopup.Show();
    }

    // ───────────────── 애니메이션 로직 ──────────────────
    public void PlayAnim(int stateHash, bool restart = false)
    {
        if (!anim) return;

        // 일시정지 상태면 자동 재개
        anim.speed = 1f;

        var cur = anim.GetCurrentAnimatorStateInfo(animLayer);
        if (!restart && cur.shortNameHash == stateHash) return;

        anim.Play(stateHash, animLayer, 0f);
    }
    public void PauseAnim(bool pause)
    {
        if (!anim) return;
        anim.speed = pause ? 0f : 1f;
    }
    public int WalkHash => _walkHash;
    public int IdleHash => _idleHash;
    public int ClimbHash => _climbHash;
    public int JumpHash => _jumpHash;
    public int FallHash => _fallHash;
    public int StairClimbHash => _stairclimbHash;
    public int RocketHash => _rocketHash;
    public int GameOverHash => _gameOverHash;
    public Animator Animator => anim;
    #region 사다리 로직
    public bool DetectClimbableAhead(int dirSign, out Collider2D col, out float centerX, out float targetCenterY)
    {
        col = null; centerX = 0f; targetCenterY = 0f;
        if (climbCD > 0) return false;
        if (ladderMask == 0) return false;

        float halfX = CastSize.x * 0.5f;

        // 버섯과 동일: 캐릭터 “앞쪽”에 얇은 박스를 둠
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + ladderProbe * 0.5f), 0f);
        Vector2 size = new Vector2(ladderProbe, CastSize.y - castSkin * 2f);

        col = Physics2D.OverlapBox(center, size, 0f, ladderMask);
        if (!col) return false;

        float step = unitPerPixel;
        centerX = Mathf.Round(col.bounds.center.x / step) * step;

        // 목표 높이: “덩쿨/사다리 콜라이더 top”까지 올라가게
        float halfH = CastSize.y * 0.5f;
        float topY = col.bounds.max.y + ladderTopExtra;
        targetCenterY = topY + halfH;
        targetCenterY = Mathf.Round(targetCenterY / step) * step;

        return true;
    }

    public bool IsStillOnClimbableAhead(int dirSign, Collider2D expected)
    {
        if (!expected) return false;
        float halfX = CastSize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + ladderProbe * 0.5f), 0f);
        Vector2 size = new Vector2(ladderProbe, CastSize.y - castSkin * 2f);

        var col = Physics2D.OverlapBox(center, size, 0f, ladderMask);
        return col == expected;
    }

    public void SnapXTo(float worldX)
    {
        float step = unitPerPixel;
        var pos = transform.position;
        pos.x = Mathf.Round(worldX / step) * step;
        transform.position = pos;
    }
    public void StartClimbCooldown()
    {
        if (climbCooldownSec <= 0f) { climbCD = 0; return; }
        climbCD = Mathf.CeilToInt(climbCooldownSec / Mathf.Max(0.0001f, Time.deltaTime));
    }
    #endregion
    // ───────────────── Movement helpers ──────────────────

    public void MoveHorizontalWithCast(float dx, ref float lockedYRef)
    {
        lastHorizontalBlocked = false;
        if (dx == 0) return;

        Vector2 size = CastSize - new Vector2(castSkin * 2f, castSkin * 2f);
        Vector2 origin = CastOrigin;
        Vector2 dir2 = new Vector2(Mathf.Sign(dx), 0f);
        float dist = Mathf.Abs(dx) + castSkin;

        var hit = Physics2D.BoxCast(origin, size, 0f, dir2, dist, groundMask);

        if (hit.collider == null || hit.distance > Mathf.Abs(dx))
        {
            MovePixelSnapped(new Vector2(dx, 0));

            if (onGround && stepUpMaxUnits > 0f && SampleGroundY(transform.position, out float newGY))
            {
                if (newGY > lockedYRef && newGY - lockedYRef <= stepUpMaxUnits)
                {
                    lockedYRef = newGY;
                    SnapYToLocked();
                }
            }
        }
        else
        {
            lastHorizontalBlocked = true;
            float allow = Mathf.Max(0f, hit.distance - castSkin);
            if (allow > 0) MovePixelSnapped(new Vector2(Mathf.Sign(dx) * allow, 0));
        }
    }

    public void MoveVerticalWithCast(float dy)
    {
        if (dy == 0) return;

        Vector2 size = CastSize - new Vector2(castSkin * 2f, castSkin * 2f);
        Vector2 origin = CastOrigin;
        Vector2 dir2 = new Vector2(0f, Mathf.Sign(dy));
        float dist = Mathf.Abs(dy) + castSkin;

        var hit = Physics2D.BoxCast(origin, size, 0f, dir2, dist, groundMask);

        if (hit.collider == null || hit.distance > Mathf.Abs(dy))
        {
            MovePixelSnapped(new Vector2(0, dy));
        }
        else
        {
            float allow = Mathf.Max(0f, hit.distance - castSkin);
            if (allow > 0f)
                MovePixelSnapped(new Vector2(0, Mathf.Sign(dy) * allow));

            // 충돌이면 속도 끊기
            vyPixels = 0f;

            // 겹침이 남으면 바로 밀어내서 다음 프레임에 떨어질 수 있게 함
            DepenetrateIfOverlapped(hit.collider);

            // 천장에 박혔을 때 위로 남아있는 누적값 제거(계속 위로 "밀기" 방지)
            if (dy > 0f)
                pixelAccum.y = Mathf.Min(0f, pixelAccum.y);
        }
    }
    private void DepenetrateIfOverlapped(Collider2D other)
    {
        if (!bodyCol || !other) return;

        var d = Physics2D.Distance(bodyCol, other);
        if (!d.isOverlapped) return;

        // 겹친 깊이(양수)
        float depth = -d.distance;
        if (depth <= 0f) return;

        // "조금만" 밀어냄: (겹친 깊이 * strength) + skin 보정
        float pushDist = depth * 0.15f + castSkin;

        // 한 프레임 최대 밀림 제한
        pushDist = Mathf.Min(pushDist, 0.02f);

        Vector2 push = d.normal * pushDist;
        transform.position += (Vector3)push;
    
    }

    public void SnapYToLocked()
    {
        float step = unitPerPixel;
        var p = transform.position;
        p.y = Mathf.Round(lockedY / step) * step;
        transform.position = p;
    }

    public bool SampleGroundY(Vector3 worldPos, out float groundY)
    {
        // 발 위치(콜라이더 아래쪽)에서 얇은 박스를 아래로 캐스트
        Vector2 full = CastSize;

        float halfH = full.y * 0.5f;
        Vector2 feetOrigin = CastOrigin + Vector2.down * (halfH - castSkin);

        // 얇은 바닥 체크 박스: 높이는 아주 얇게, 폭은 살짝 줄여 모서리 오판정 줄임
        Vector2 checkSize = new Vector2(full.x * groundCastWidthScale, castSkin * 2f);

        float dist = groundProbeUnits + castSkin;

        var hit = Physics2D.BoxCast(feetOrigin, checkSize, 0f, Vector2.down, dist, groundMask);

        if (hit.collider && hit.normal.y >= minGroundNormalY)
        {
            // 바닥의 접점 y + 내 반높이 = 내 중심(=lockedY 기준)
            groundY = hit.point.y + halfH;
            return true;
        }

        groundY = 0f;
        return false;
    }
    public void MovePixelSnapped(Vector2 worldDelta)
    {
        pixelAccum += worldDelta;

        float stepX = unitPerPixel;
        float stepY = unitPerPixel;

        // 공중에서 Y 이동 간격을 더 촘촘하게(더 자연스러운 낙하)
        if (smoothAirY && !onGround && airYSubDiv > 1)
            stepY = unitPerPixel / airYSubDiv;

        int px = 0, py = 0;

        if (Mathf.Abs(pixelAccum.x) >= stepX)
        {
            px = Mathf.FloorToInt(Mathf.Abs(pixelAccum.x) / stepX) * (int)Mathf.Sign(pixelAccum.x);
            pixelAccum.x -= px * stepX;
        }
        if (Mathf.Abs(pixelAccum.y) >= stepY)
        {
            py = Mathf.FloorToInt(Mathf.Abs(pixelAccum.y) / stepY) * (int)Mathf.Sign(pixelAccum.y);
            pixelAccum.y -= py * stepY;
        }

        if (px != 0 || py != 0)
        {
            transform.position += new Vector3(px * stepX, py * stepY, 0);

            // X는 항상 픽셀 스냅 유지
            var p = transform.position;
            p.x = Mathf.Round(p.x / stepX) * stepX;

            // Y는 공중일 때는 더 촘촘한 그리드로 스냅, 지상일 때는 기존 픽셀 스냅
            p.y = Mathf.Round(p.y / stepY) * stepY;

            transform.position = p;
        }
    }

    

    #region 점프 로직
    // 원하는 높이(H)와 정점시간(T)로 "상승 중 중력"을 결정: g = 2H / T^2
    public float JumpGravityUpPixelsPerSec2
    {
        get
        {
            float T = Mathf.Max(0.05f, jumpTimeToApexSec);
            return 2f * jumpPeakHeightPixels / (T * T);
        }
    }
    // 하강은 더 빠르게: gDown = gUp * multiplier
    public float JumpGravityDownPixelsPerSec2 => JumpGravityUpPixelsPerSec2 * fallGravityMultiplier;
    // 정점시간 T에 맞추는 초기 속도: v0 = gUp * T = 2H / T
    public float JumpStartVyPixels
    {
        get
        {
            float T = Mathf.Max(0.05f, jumpTimeToApexSec);
            return JumpGravityUpPixelsPerSec2 * T;
        }
    }
    public bool DetectMushroomAheadX(int dirSign, out float centerX, out MushroomKind kind)
    {
        centerX = 0f;
        kind = MushroomKind.Normal;

        float halfX = CastSize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + mushroomProbe * 0.5f), 0f);
        Vector2 size = new Vector2(mushroomProbe, CastSize.y - castSkin * 2f);

        // 큰 버섯 먼저
        if (bigMushroomMask != 0)
        {
            var colBig = Physics2D.OverlapBox(center, size, 0f, bigMushroomMask);
            if (colBig)
            {
                float step = unitPerPixel;
                centerX = Mathf.Round(colBig.bounds.center.x / step) * step;
                kind = MushroomKind.Big;
                return true;
            }
        }

        // 그 다음 일반 버섯
        if (mushroomMask == 0) return false;

        var col = Physics2D.OverlapBox(center, size, 0f, mushroomMask);
        if (!col) return false;

        {
            float step = unitPerPixel;
            centerX = Mathf.Round(col.bounds.center.x / step) * step;
            kind = MushroomKind.Normal;
            return true;
        }
    }
    private float CalcJumpStartVy(float peakPixels, float timeToApexSec)
    {
        float T = Mathf.Max(0.05f, timeToApexSec);
        float gUp = 2f * peakPixels / (T * T);
        return gUp * T; // = 2H/T
    }
    public void StartMushroomJump(int jumpDir, MushroomKind kind)
    {
        float peak = (kind == MushroomKind.Big) ? bigJumpPeakHeightPixels : jumpPeakHeightPixels;
        float apex = (kind == MushroomKind.Big) ? bigJumpTimeToApexSec : jumpTimeToApexSec;
        float horizScale = (kind == MushroomKind.Big) ? bigJumpHorizSpeedScale : jumpHorizSpeedScale;
        int cdFrames = (kind == MushroomKind.Big) ? bigMushroomCooldownFrames : mushroomCooldownFrames;

        vyPixels = CalcJumpStartVy(peak, apex);

        onGround = false;
        dir = jumpDir;
        jumpHorizSpeedPixelsPerSec = runSpeedPixelsPerSec * horizScale;

        mushroomCD = cdFrames;
    }
    #endregion

    #region 계단 로직
    public bool DetectStairsAhead(int dirSign, out Collider2D col, out float enterX)
    {
        col = null; enterX = 0f;
        if (stairsCD > 0) return false;
        if (stairsMask == 0) return false;

        float halfX = CastSize.x * 0.5f;

        // 앞에 얇은 박스
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + stairsProbe * 0.5f), 0f);
        Vector2 size = new Vector2(stairsProbe, CastSize.y - castSkin * 2f);

        col = Physics2D.OverlapBox(center, size, 0f, stairsMask);
        if (!col) return false;

        // 계단이 PolygonCollider2D라고 가정
        var poly = col as PolygonCollider2D;
        if (!poly) return false;

        // 월드 좌표로 변환된 점들
        int n = poly.points.Length;
        if (n < 2) return false;

        Vector2[] w = new Vector2[n];
        Transform t = poly.transform;
        for (int i = 0; i < n; i++)
            w[i] = t.TransformPoint(poly.points[i]);

        // bottom/top 후보 찾기 (y 기준)
        float minY = w[0].y, maxY = w[0].y;
        for (int i = 1; i < n; i++)
        {
            if (w[i].y < minY) minY = w[i].y;
            if (w[i].y > maxY) maxY = w[i].y;
        }

        float epsY = unitPerPixel * 2f; // 2px 정도를 같은 높이로 취급
                                        // bottom: minY 근처 점들 중 좌/우 극값
        Vector2 bL = w[0], bR = w[0];
        bool bInit = false;

        // top: maxY 근처 점들 중 좌/우 극값
        Vector2 tL = w[0], tR = w[0];
        bool tInit = false;

        for (int i = 0; i < n; i++)
        {
            if (Mathf.Abs(w[i].y - minY) <= epsY)
            {
                if (!bInit) { bL = bR = w[i]; bInit = true; }
                else
                {
                    if (w[i].x < bL.x) bL = w[i];
                    if (w[i].x > bR.x) bR = w[i];
                }
            }

            if (Mathf.Abs(w[i].y - maxY) <= epsY)
            {
                if (!tInit) { tL = tR = w[i]; tInit = true; }
                else
                {
                    if (w[i].x < tL.x) tL = w[i];
                    if (w[i].x > tR.x) tR = w[i];
                }
            }
        }

        if (!bInit || !tInit) return false;

        bool flipX = false;
        var sr = col.GetComponentInParent<SpriteRenderer>();
        if (sr) flipX ^= sr.flipX;
        if (col.transform.lossyScale.x < 0f) flipX ^= true;
        flipX ^= invertStairsDirection;

        // "오른쪽으로 올라가는 계단"을 기본으로 보고,
        // flip이면 "왼쪽으로 올라가는 계단"으로 인식
        bool ascendRight = !flipX;

        Vector2 start = ascendRight ? bL : bR; // 아래 시작점
        Vector2 end = ascendRight ? tR : tL; // 위 끝점
        // 진행 방향
        int moveDir = (end.x >= start.x) ? +1 : -1;
        if (moveDir != dirSign) return false;

        stairStart = start;
        stairEnd = end;

        float dx = (end.x - start.x);
        if (Mathf.Abs(dx) < 0.0001f) return false;

        stairStart = start;
        stairEnd = end;
        stairMoveDir = moveDir;

        // 진입 목표 X(계단 시작쪽 x 근처로 맞추기)
        enterX = Mathf.Round(start.x / unitPerPixel) * unitPerPixel;

        return true;
    }
    public void StartStairsCooldown()
    {
        if (stairsCooldownSec <= 0f) { stairsCD = 0; return; }
        stairsCD = Mathf.CeilToInt(stairsCooldownSec / Mathf.Max(0.0001f, Time.deltaTime));
    }
    #endregion

    #region 로켓 로직
    public void StartRocketCooldown()
    {
        if (rocketCooldownSec <= 0f) { rocketCD = 0; return; }
        rocketCD = Mathf.CeilToInt(rocketCooldownSec / Mathf.Max(0.0001f, Time.deltaTime));
    }
    public bool DetectRocketAhead(int dirSign, out Collider2D col, out float centerX, out float targetCenterY)
    {
        col = null; centerX = 0f; targetCenterY = 0f;
        if (rocketCD > 0) return false;
        if (rocketMask == 0) return false;

        float halfX = CastSize.x * 0.5f;

        // 앞쪽 얇은 박스로 감지
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + rocketProbe * 0.5f), 0f);
        Vector2 size = new Vector2(rocketProbe, CastSize.y - castSkin * 2f);

        col = Physics2D.OverlapBox(center, size, 0f, rocketMask);
        if (!col) return false;

        float step = unitPerPixel;
        centerX = Mathf.Round(col.bounds.center.x / step) * step;

        float halfH = CastSize.y * 0.5f;

        if (rocketUseColliderTop)
        {
            // 로켓 콜라이더 top까지
            float topY = col.bounds.max.y + rocketTopExtra;
            targetCenterY = topY + halfH;
        }
        else
        {
            // 고정 상승량
            targetCenterY = transform.position.y + rocketRiseUnits;
        }

        targetCenterY = Mathf.Round(targetCenterY / step) * step;
        return true;
    }
    #endregion
    public bool DetectWallAhead(int dirSign)
    {
        if (reverseOnMask == 0) return false;

        float halfX = CastSize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + wallProbe * 0.5f), 0f);
        Vector2 size = new Vector2(wallProbe, CastSize.y - castSkin * 2f);

        return Physics2D.OverlapBox(center, size, 0f, reverseOnMask) != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isGameOverRequested)
            return;

        if (((1 << other.gameObject.layer) & monsterLayerMask) != 0)
        {
            RequestGameOver();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (reverseOnMask == 0) return;
        Gizmos.color = Color.cyan;

        float halfX = (bodyCol ? bodyCol.bounds.size.x : 0.5f) * 0.5f;
        float h = (bodyCol ? bodyCol.bounds.size.y : 0.9f) - castSkin * 2f;

        Vector2 center = (Vector2)transform.position + new Vector2(dir * (halfX + wallProbe * 0.5f), 0f);
        Vector2 size = new Vector2(wallProbe, h);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
