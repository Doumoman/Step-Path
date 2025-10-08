using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerAutoRunner : MonoBehaviour
{
    enum State { Run, Align, Climb }

    [Header("Pixel Move")]
    public int pixelsPerUnit = 16;
    public float runSpeedPixelsPerSec = 32f;
    public float climbSpeedPixelsPerSec = 24f;

    [Header("Layers")]
    public LayerMask groundMask;   // 바닥만
    public int ladderLayer;        // Ladder 레이어 번호 (LayerMask 말고 index)

    [Header("Sizes")]
    public Vector2 bodySize = new(0.5f, 0.9f);
    public float groundCheckDepth = 0.05f;

    [Header("Ladder")]
    [Tooltip("사다리 중앙 X와 정렬 허용 오차(셀 단위)")]
    public float ladderAlignEpsCells = 0.2f;
    [Tooltip("꼭대기에서 살짝 더 올려주기(월드)")]
    public float ladderTopExtra = 0.2f;

    // 내부
    State state = State.Run;
    int dir = 1;
    float unitPerPixel;
    Vector2 accum;

    Collider2D curLadder;          // 현재 겹친 사다리
    int ladderContactCount = 0;    // 트리거 중첩 안정화
    float targetLadderCenterX;     // 정렬 목표 X
    int climbGraceFrames = 0;
    const int CLIMB_GRACE_N = 3;

    void Awake()
    {
        unitPerPixel = 1f / Mathf.Max(1, pixelsPerUnit);
    }

    void Update()
    {
        switch (state)
        {
            case State.Run: TickRun(); break;
            case State.Align: TickAlign(); break;
            case State.Climb: TickClimb(); break;
        }
    }

    // -------- States --------
    void TickRun()
    {
        // 사다리와 트리거로 겹치면 정렬 상태 진입
        if (ladderContactCount > 0 && curLadder)
        {
            targetLadderCenterX = curLadder.bounds.center.x;
            // 사다리 방향 바라보도록
            dir = (targetLadderCenterX - transform.position.x) >= 0 ? 1 : -1;
            state = State.Align;
            return;
        }

        // 바닥 없으면 반전 (간단한 왕복)
        if (!IsGrounded()) dir *= -1;

        float vx = runSpeedPixelsPerSec * unitPerPixel * Time.deltaTime;
        MovePixelSnapped(new Vector2(dir * vx, 0f));
    }

    void TickAlign()
    {
        if (curLadder == null || ladderContactCount <= 0)
        {
            state = State.Run;
            return;
        }

        float dx = targetLadderCenterX - transform.position.x;
        float step = Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), runSpeedPixelsPerSec * unitPerPixel * Time.deltaTime);
        MovePixelSnapped(new Vector2(step, 0));

        float eps = ladderAlignEpsCells * unitPerPixel;
        if (Mathf.Abs(dx) <= eps)
        {
            climbGraceFrames = CLIMB_GRACE_N;
            state = State.Climb;
        }
    }

    void TickClimb()
    {
        // 사다리 위로만 이동
        float vy = climbSpeedPixelsPerSec * unitPerPixel * Time.deltaTime;
        MovePixelSnapped(new Vector2(0, vy));

        // 사다리에서 벗어났다면(꼭대기 도달)
        if (ladderContactCount <= 0 || curLadder == null)
        {
            if (climbGraceFrames-- > 0) return; // 잠깐은 허용
            MovePixelSnapped(new Vector2(0, ladderTopExtra));
            dir *= -1; // 와리가리 전환
            state = State.Run;
        }
    }

    // -------- Pixel-snap move --------
    void MovePixelSnapped(Vector2 worldDelta)
    {
        accum += worldDelta;
        float step = unitPerPixel;

        int px = 0, py = 0;
        if (Mathf.Abs(accum.x) >= step)
        {
            px = Mathf.FloorToInt(Mathf.Abs(accum.x) / step) * (int)Mathf.Sign(accum.x);
            accum.x -= px * step;
        }
        if (Mathf.Abs(accum.y) >= step)
        {
            py = Mathf.FloorToInt(Mathf.Abs(accum.y) / step) * (int)Mathf.Sign(accum.y);
            accum.y -= py * step;
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

    // -------- Helpers --------
    bool IsGrounded()
    {
        var p = (Vector2)transform.position + Vector2.down * (bodySize.y * 0.5f);
        var hit = Physics2D.BoxCast(p, new Vector2(bodySize.x * 0.9f, groundCheckDepth), 0f, Vector2.down, 0f, groundMask);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    // -------- Trigger ladder only --------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == ladderLayer)
        {
            curLadder = other;
            ladderContactCount++;
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (other == curLadder)
        {
            ladderContactCount = Mathf.Max(0, ladderContactCount - 1);
            if (ladderContactCount == 0) curLadder = null;
        }
        else if (other.gameObject.layer == ladderLayer)
        {
            ladderContactCount = Mathf.Max(0, ladderContactCount - 1);
        }
    }
}
