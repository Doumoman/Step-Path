using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class PlayerAutoRunner : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private int animLayer = 0;

    public const string ANIM_RUN = "Run";
    public const string ANIM_IDLE = "Idle";
    public const string ANIM_CLIMB = "Climb";
    public const string ANIM_JUMP = "Jump";
    public const string ANIM_FALL = "Fall";

    private int _runHash, _idleHash, _climbHash, _jumpHash, _fallHash;

    [Header("Pixel")]
    public int pixelsPerUnit = 16;
    public float runSpeedPixelsPerSec = 40f;
    public float climbSpeedPixelsPerSec = 24f;

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
    public Tilemap ladderMap;

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

    [Header("Ladder")]
    public float ladderTopExtra = 0.05f;

    [Header("Mushroom Jump")]
    public LayerMask mushroomMask;
    public float mushroomProbe = 0.12f;
    public int mushroomCooldownFrames = 8;
    public float jumpPeakHeightPixels = 64f;
    [Range(0.5f, 3f)]
    public float jumpHorizSpeedScale = 1.25f;
    public float mushroomJumpDelaySec = 0.25f;
    public bool requireGroundedAtLaunch = true;

    // ───────── FSM에서 접근해야 하는 런타임 필드들 ─────────
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

    [HideInInspector] public Vector3Int curLadderCell;
    [HideInInspector] public float targetLadderCenterX;

    [HideInInspector] public bool lastHorizontalBlocked;

    // 상태머신
    private PlayerStateMachine stateMachine;

    // ───────────────── 캐스트 기준 ─────────────────
    private Vector2 CastSize
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

        if (!ladderMap)
        {
            foreach (var tm in GetComponentsInChildren<Tilemap>(true))
                if (tm.name == "Ladder") { ladderMap = tm; break; }
        }

        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (!anim) anim = GetComponent<Animator>();

        _runHash = Animator.StringToHash(ANIM_RUN);
        _idleHash = Animator.StringToHash(ANIM_IDLE);
        _climbHash = Animator.StringToHash(ANIM_CLIMB);
        _jumpHash = Animator.StringToHash(ANIM_JUMP);
        _fallHash = Animator.StringToHash(ANIM_FALL);

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

        stateMachine.Update();
    }
    void LateUpdate()
    {
        if (sr) sr.flipX = (dir > 0);
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
    public void SetGameOver()
    {
        ChangeState(new PlayerGameOverState(this, stateMachine));
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
    public int RunHash => _runHash;
    public int IdleHash => _idleHash;
    public int ClimbHash => _climbHash;
    public int JumpHash => _jumpHash;
    public int FallHash => _fallHash;
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

    public bool IsOnLadderCell(out Vector3Int cell, out float centerX)
    {
        cell = default; centerX = 0f;
        if (!ladderMap) return false;

        var grid = ladderMap.layoutGrid;
        var c0 = grid.WorldToCell(transform.position);

        if (ladderMap.HasTile(c0)) { cell = c0; centerX = ladderMap.GetCellCenterWorld(c0).x; return true; }
        var cu = c0 + Vector3Int.up;
        if (ladderMap.HasTile(cu)) { cell = cu; centerX = ladderMap.GetCellCenterWorld(cu).x; return true; }
        var cd = c0 + Vector3Int.down;
        if (ladderMap.HasTile(cd)) { cell = cd; centerX = ladderMap.GetCellCenterWorld(cd).x; return true; }
        return false;
    }

    public bool DetectMushroomAhead(int dirSign)
    {
        if (mushroomMask == 0) return false;

        float halfX = CastSize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + mushroomProbe * 0.5f), 0f);
        Vector2 size = new Vector2(mushroomProbe, CastSize.y - castSkin * 2f);

        return Physics2D.OverlapBox(center, size, 0f, mushroomMask) != null;
    }

    public void StartMushroomJump(int jumpDir)
    {
        float vy0 = Mathf.Sqrt(Mathf.Max(0.0001f, 2f * gravityPixelsPerSec2 * jumpPeakHeightPixels));

        vyPixels = vy0;
        onGround = false;

        dir = jumpDir;
        jumpHorizSpeedPixelsPerSec = runSpeedPixelsPerSec * jumpHorizSpeedScale;

        mushroomCD = mushroomCooldownFrames;
    }


    public bool DetectWallAhead(int dirSign)
    {
        if (reverseOnMask == 0) return false;

        float halfX = CastSize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + wallProbe * 0.5f), 0f);
        Vector2 size = new Vector2(wallProbe, CastSize.y - castSkin * 2f);

        return Physics2D.OverlapBox(center, size, 0f, reverseOnMask) != null;
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
