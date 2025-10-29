using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class PlayerAutoRunner : MonoBehaviour
{
    enum State { Run, Align, Climb }

    [Header("Pixel")]
    public int pixelsPerUnit = 16;
    public float runSpeedPixelsPerSec = 40f;
    public float climbSpeedPixelsPerSec = 24f;

    [Header("Gravity")]
    public float gravityPixelsPerSec2 = 480f;
    public float maxFallSpeedPixelsPerSec = 360f;

    [Header("Layers/Maps")]
    public LayerMask groundMask;          // 바닥(충돌)
    public Tilemap ladderMap;             // 사다리(콜라이더 없음)

    [Header("Wall Reverse (Inspector에서 선택)")]
    public LayerMask reverseOnMask;       // ★ 이 레이어(들)를 만나면 방향 반전
    public float wallProbe = 0.10f;       // 전방 감지 두께(월드단위)
    public bool reverseAlsoInAir = true;  // 공중에서도 벽 감지 시 반전할지
    public int reverseCooldownFrames = 6; // 연속 반전 방지 프레임

    [Header("Body/Collision")]
    public Vector2 bodySize = new(0.5f, 0.9f);
    public float skin = 0.01f;
    public float stepUpMax = 0.25f;       // 올라갈 수 있는 최대 턱
    public float groundProbe = 0.40f;     // 발바닥 아래로 지면 샘플 최대 거리

    [Header("Ladder")]
    public float ladderTopExtra = 0.05f;

    State state = State.Run;
    int dir = 1;
    float unitPerPixel;
    Vector2 pixelAccum;
    float vyPixels;
    float lockedY;            // 접지 잠금 Y
    bool onGround;           // 접지-잠금 중?
    int reverseCD;          // 반전 쿨다운

    Vector3Int curLadderCell;
    float targetLadderCenterX;

    void Awake()
    {
        unitPerPixel = 1f / Mathf.Max(1, pixelsPerUnit);
        if (!ladderMap)
        {
            foreach (var tm in GetComponentsInChildren<Tilemap>(true))
                if (tm.name == "Ladder") { ladderMap = tm; break; }
        }
    }

    void Update()
    {
        if (reverseCD > 0) reverseCD--;

        switch (state)
        {
            case State.Run: TickRun(); break;
            case State.Align: TickAlign(); break;
            case State.Climb: TickClimb(); break;
        }
    }

    // ───────────────────── States ─────────────────────

    void TickRun()
    {
        // 전방에 "반전 레이어"가 있으면 즉시 방향 전환 (공중/지면 옵션)
        if ((onGround || reverseAlsoInAir) && DetectWallAhead(dir) && reverseCD == 0)
        {
            dir *= -1;
            reverseCD = reverseCooldownFrames;
        }

        // 사다리 진입?
        if (IsOnLadderCell(out curLadderCell, out targetLadderCenterX))
        {
            onGround = false;
            state = State.Align;
            return;
        }

        // 지면 샘플
        if (SampleGroundY(transform.position, out float groundY))
        {
            if (!onGround) { onGround = true; lockedY = groundY; }
            else { lockedY = Mathf.Max(lockedY, groundY); } // 아래로 낮추지 않음

            vyPixels = 0f;

            // 수평 이동(+스텝업만 허용)
            float dx = runSpeedPixelsPerSec * unitPerPixel * dir * Time.deltaTime;
            MoveHorizontalWithCast(dx, ref lockedY);

            // Y 고정
            SnapYToLocked();

            // 수평 충돌로 막히면 방향 반전
            if (_lastHorizontalBlocked && reverseCD == 0)
            {
                dir *= -1;
                reverseCD = reverseCooldownFrames;
            }
        }
        else
        {
            // 낙하
            onGround = false;
            vyPixels = Mathf.Max(vyPixels - gravityPixelsPerSec2 * Time.deltaTime, -maxFallSpeedPixelsPerSec);

            float dx = runSpeedPixelsPerSec * unitPerPixel * dir * Time.deltaTime;
            float dy = vyPixels * unitPerPixel * Time.deltaTime;

            MoveHorizontalWithCast(dx, ref lockedY);
            MoveVerticalWithCast(dy);
        }
    }

    void TickAlign()
    {
        if (!IsOnLadderCell(out curLadderCell, out targetLadderCenterX))
        {
            state = State.Run; return;
        }

        // X 즉시 스냅
        float step = unitPerPixel;
        var p = transform.position;
        p.x = Mathf.Round(targetLadderCenterX / step) * step;
        transform.position = p;

        vyPixels = 0;
        onGround = false;
        state = State.Climb;
    }

    void TickClimb()
    {
        if (!IsOnLadderCell(out curLadderCell, out targetLadderCenterX))
        {
            MovePixelSnapped(new Vector2(0, ladderTopExtra));
            dir = (Random.value < 0.5f) ? -1 : 1;
            state = State.Run;
            return;
        }

        // X 고정, Y만 상승
        float step = unitPerPixel;
        var p = transform.position;
        p.x = Mathf.Round(targetLadderCenterX / step) * step;
        transform.position = p;

        float vy = climbSpeedPixelsPerSec * unitPerPixel * Time.deltaTime;
        MovePixelSnapped(new Vector2(0, vy));
    }

    // ────────────────── Movement helpers ──────────────────

    bool _lastHorizontalBlocked;

    void MoveHorizontalWithCast(float dx, ref float lockedYRef)
    {
        _lastHorizontalBlocked = false;
        if (dx == 0) return;

        Vector2 size = bodySize - new Vector2(skin * 2f, skin * 2f);
        Vector2 origin = transform.position;
        Vector2 dir2 = new Vector2(Mathf.Sign(dx), 0f);
        float dist = Mathf.Abs(dx) + skin;

        var hit = Physics2D.BoxCast(origin, size, 0f, dir2, dist, groundMask);

        if (hit.collider == null || hit.distance > Mathf.Abs(dx))
        {
            MovePixelSnapped(new Vector2(dx, 0));

            // 계단 스텝업(위로만)
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
            _lastHorizontalBlocked = true;
            float allow = Mathf.Max(0f, hit.distance - skin);
            if (allow > 0) MovePixelSnapped(new Vector2(Mathf.Sign(dx) * allow, 0));
        }
    }

    void MoveVerticalWithCast(float dy)
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

    void SnapYToLocked()
    {
        float step = unitPerPixel;
        var p = transform.position;
        p.y = Mathf.Round(lockedY / step) * step;
        transform.position = p;
    }

    // 발바닥 기준 지면 샘플링 → groundY(센터Y) 반환
    bool SampleGroundY(Vector3 worldPos, out float groundY)
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

    // 픽셀 스냅 이동
    void MovePixelSnapped(Vector2 worldDelta)
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

    // ───────────── Ladder helpers ─────────────

    bool IsOnLadderCell(out Vector3Int cell, out float centerX)
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

    // ───────────── Wall detection ─────────────

    bool DetectWallAhead(int dirSign)
    {
        if (reverseOnMask == 0) return false;

        // 플레이어 앞쪽에 얇은 오버랩 박스
        float halfX = bodySize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dirSign * (halfX + wallProbe * 0.5f), 0f);
        Vector2 size = new Vector2(wallProbe, bodySize.y - skin * 2f);

        return Physics2D.OverlapBox(center, size, 0f, reverseOnMask) != null;
    }

#if UNITY_EDITOR
    // 디버그 시각화
    void OnDrawGizmosSelected()
    {
        if (reverseOnMask == 0) return;
        Gizmos.color = Color.cyan;
        int sign = Mathf.Sign(runSpeedPixelsPerSec) == 0 ? 1 : Mathf.Sign(runSpeedPixelsPerSec) > 0 ? 1 : -1;
        float halfX = bodySize.x * 0.5f;
        Vector2 center = (Vector2)transform.position + new Vector2(dir * (halfX + wallProbe * 0.5f), 0f);
        Vector2 size = new Vector2(wallProbe, bodySize.y - skin * 2f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
