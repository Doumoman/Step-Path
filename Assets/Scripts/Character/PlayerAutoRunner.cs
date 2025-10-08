using UnityEngine;

[RequireComponent(typeof(Pixel2DRunner), typeof(Collider2D))]
public class PlayerAutoRunner : MonoBehaviour
{
    enum State { Run, Climb }

    [Header("Auto Move")]
    public float runSpeedPixelsPerSec = 32f;   // 초당 픽셀
    public float climbSpeedPixelsPerSec = 24f;
    public int pixelsPerUnit = 16;

    [Header("Collision & Sense")]
    public LayerMask groundMask;   // Ground 레이어
    public LayerMask ladderMask;   // Ladder 레이어
    public Vector2 bodySize = new(0.5f, 0.9f);
    public float groundCheckDepth = 0.05f;
    public float frontCheckDistance = 0.35f;  // 전방 사다리 탐지/낭떠러지 감지
    public float ladderWidthAllowance = 0.3f; // 사다리 폭 여유
    public float ladderTopExtra = 0.2f;       // 사다리 상단 탈출 보정

    Pixel2DRunner mover;
    Collider2D col;

    State state = State.Run;
    int dir = 1;                 // +1: 오른쪽, -1: 왼쪽
    bool gravity = true;         // CLIMB 시 false
    float unitPerPixel;

    void Awake()
    {
        mover = GetComponent<Pixel2DRunner>();
        col = GetComponent<Collider2D>();
        mover.pixelsPerUnit = pixelsPerUnit;
        unitPerPixel = 1f / pixelsPerUnit;
    }

    void Update()
    {
        switch (state)
        {
            case State.Run: TickRun(); break;
            case State.Climb: TickClimb(); break;
        }
    }

    void TickRun()
    {
        // 발밑 지면 체크(없으면 낭떠러지 → 방향 반전)
        if (!IsGrounded())
        {
            // 한 칸 전방의 바닥이 없으면 뒤집기
            if (!HasGroundAhead())
                dir *= -1;
        }

        // 전방 사다리 있으면 → Climb 전환
        if (HasLadderAhead(out Vector2 ladderPos))
        {
            // 사다리 중앙으로 X 정렬
            float dx = ladderPos.x - transform.position.x;
            float stepX = Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), runSpeedPixelsPerSec * Time.deltaTime / pixelsPerUnit);
            mover.Move(new Vector2(stepX, 0));

            // 충분히 정렬되면 오르기 시작
            if (Mathf.Abs(dx) <= ladderWidthAllowance * 0.5f)
            {
                gravity = false;
                state = State.Climb;
                return;
            }
            return; // 정렬 중일 땐 수평만
        }

        // 평상시 수평 이동 (픽셀 이동)
        float vxUnitPerSec = runSpeedPixelsPerSec / pixelsPerUnit;
        mover.Move(new Vector2(dir * vxUnitPerSec * Time.deltaTime, 0));

        // 벽/막힘 감지 시 방향 반전 (추후에 삭제할 예정?)
        if (IsBlockedHorizontally(dir))
            dir *= -1;
    }

    void TickClimb()
    {
        // 위로 픽셀 이동
        float vyUnitPerSec = climbSpeedPixelsPerSec / pixelsPerUnit;
        mover.Move(new Vector2(0, vyUnitPerSec * Time.deltaTime));

        // 사다리 영역을 여전히 밟고 있는지 체크
        bool inLadder = Physics2D.OverlapBox(transform.position, new Vector2(bodySize.x, bodySize.y), 0f, ladderMask);
        if (!inLadder)
        {
            // 사다리 꼭대기를 벗어났다고 판단 → 살짝 더 올라가 주고 RUN 전환
            mover.Move(new Vector2(0, ladderTopExtra));
            gravity = true;
            // 꼭대기 올라오면 방향 반전(와리가리)
            dir *= -1;
            state = State.Run;
        }
    }

    bool IsGrounded()
    {
        var p = (Vector2)transform.position + Vector2.down * (bodySize.y * 0.5f);
        var hit = Physics2D.BoxCast(p, new Vector2(bodySize.x * 0.9f, groundCheckDepth), 0f, Vector2.down, 0f, groundMask);
        return hit.collider != null;
    }

    bool HasGroundAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * frontCheckDistance, 0);
        var hit = Physics2D.Raycast(origin, Vector2.down, bodySize.y + 0.2f, groundMask);
        return hit.collider != null;
    }

    bool HasLadderAhead(out Vector2 ladderCenter)
    {
        ladderCenter = Vector2.zero;
        // 전방에 사다리 있는지 박스 오버랩으로 조사
        Vector2 boxSize = new Vector2(ladderWidthAllowance, bodySize.y * 1.2f);
        Vector2 center = (Vector2)transform.position + new Vector2(dir * frontCheckDistance, 0);
        var hit = Physics2D.OverlapBox(center, boxSize, 0f, ladderMask);
        if (hit != null)
        {
            ladderCenter = hit.bounds.center;
            return true;
        }
        return false;
    }

    bool IsBlockedHorizontally(int direction)
    {
        Vector2 origin = (Vector2)transform.position;
        var hit = Physics2D.BoxCast(origin, bodySize, 0f, new Vector2(direction, 0), unitPerPixel, groundMask);
        return hit.collider != null;
    }
}