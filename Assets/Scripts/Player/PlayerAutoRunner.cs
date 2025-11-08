using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class PlayerAutoRunner : MonoBehaviour
{
    [Header("Pixel")]
    public int pixelsPerUnit = 16;
    public float runSpeedPixelsPerSec = 40f;
    public float climbSpeedPixelsPerSec = 24f;

    [Header("Gravity")]
    public float gravityPixelsPerSec2 = 480f;
    public float maxFallSpeedPixelsPerSec = 360f;

    [Header("Layers/Maps")]
    public LayerMask groundMask;
    public Tilemap ladderMap;

    [Header("Wall Reverse (Inspector에서 선택)")]
    public LayerMask reverseOnMask;
    public float wallProbe = 0.10f;
    public bool reverseAlsoInAir = true;
    public int reverseCooldownFrames = 6;

    [Header("Body/Collision")]
    public Vector2 bodySize = new(0.5f, 0.9f);
    public float skin = 0.01f;
    public float stepUpMax = 0.25f;
    public float groundProbe = 0.40f;

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

    void Awake()
    {
        unitPerPixel = 1f / Mathf.Max(1, pixelsPerUnit);

        if (!ladderMap)
        {
            foreach (var tm in GetComponentsInChildren<Tilemap>(true))
                if (tm.name == "Ladder") { ladderMap = tm; break; }
        }

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

    public void ChangeState(IPlayerState newState)
    {
        stateMachine.ChangeState(newState);

        // 카메라에 현재 상태 알려주기
        if (CameraMover.Instance != null)
        {
            CameraMover.Instance.OnPlayerStateChanged(newState);
        }
    }

    // ────────────────── Movement helpers (그대로 사용) ──────────────────

    public void MoveHorizontalWithCast(float dx, ref float lockedYRef)
    {
        lastHorizontalBlocked = false;
        if (dx == 0) return;

        Vector2 size = bodySize - new Vector2(skin * 2f, skin * 2f);
        Vector2 origin = transform.position;
        Vector2 dir2 = new Vector2(Mathf.Sign(dx), 0f);
        float dist = Mathf.Abs(dx) + skin;

        var hit = Physics2D.BoxCast(origin, size, 0f, dir2, dist, groundMask);

        if (hit.collider == null || hit.distance > Mathf.Abs(dx))
        {
            MovePixelSnapped(new Vector2(dx, 0));

            if (onGround && stepUpMax > 0f && SampleGroundY(transform.position, out float newGY))
            {
                if (newGY > lockedYRef && newGY - lockedYRef <= stepUpMax)
                {
                    lockedYRef = newGY;
                    SnapYToLocked();
                }
            }
        }
        else
        {
            lastHorizontalBlocked = true;
            float allow = Mathf.Max(0f, hit.distance - skin);
            if (allow > 0) MovePixelSnapped(new Vector2(Mathf.Sign(dx) * allow, 0));
        }
    }

    public void MoveVerticalWithCast(float dy)
    {
        if (dy == 0) return;

        Vector2 size = bodySize - new Vector2(skin * 2f, skin * 2f);
        Vector2 origin = transform.position;
        Vector2 dir2 = new Vector2(0f, Mathf.Sign(dy));
        float dist = Mathf.Abs(dy) + skin;

        var hit = Physics2D.BoxCast(origin, size, 0f, dir2, dist, groundMask);
        if (hit.collider == null || hit.distance > Mathf.Abs(dy))
        {
            MovePixelSnapped(new Vector2(0, dy));
        }
        else
        {
            float allow = Mathf.Max(0f, hit.distance - skin);
            if (allow > 0) MovePixelSnapped(new Vector2(0, Mathf.Sign(dy) * allow));
            if (dy < 0) vyPixels = 0;
        }
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
        float halfH = bodySize.y * 0.5f;
        Vector2 feet = (Vector2)worldPos + Vector2.down * (halfH - skin);
        var hit = Physics2D.Raycast(feet, Vector2.down, groundProbe, groundMask);
        if (hit.collider)
        {
            groundY = hit.point.y + halfH;
            return true;
        }
        groundY = 0;
        return false;
    }

    public void MovePixelSnapped(Vector2 worldDelta)
    {
        pixelAccum += worldDelta;
        float step = unitPerPixel;

        int px = 0, py = 0;
        if (Mathf.Abs(pixelAccum.x) >= step)
        {
            px = Mathf.FloorToInt(Mathf.Abs(pixelAccum.x) / step) * (int)Mathf.Sign(pixelAccum.x);
            pixelAccum.x -= px * step;
        }
        if (Mathf.Abs(pixelAccum.y) >= step)
        {
            py = Mathf.FloorToInt(Mathf.Abs(pixelAccum.y) / step) * (int)Mathf.Sign(pixelAccum.y);
            pixelAccum.y -= py * step;
        }

        if (px != 0 || py != 0)
        {
            transform.position += new Vector3(px * step, py * step, 0);
            var p = transform.position;
            p.x = Mathf.Round(p.x / step) * step;
            p.y = Mathf.Round(p.y / step) * step;
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

        float halfX = bodySize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + mushroomProbe * 0.5f), 0f);
        Vector2 size = new Vector2(mushroomProbe, bodySize.y - skin * 2f);

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

        float halfX = bodySize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + wallProbe * 0.5f), 0f);
        Vector2 size = new Vector2(wallProbe, bodySize.y - skin * 2f);

        return Physics2D.OverlapBox(center, size, 0f, reverseOnMask) != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (reverseOnMask == 0) return;
        Gizmos.color = Color.cyan;
        float halfX = bodySize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dir * (halfX + wallProbe * 0.5f), 0f);
        Vector2 size = new Vector2(wallProbe, bodySize.y - skin * 2f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
