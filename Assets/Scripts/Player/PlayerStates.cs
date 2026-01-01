using UnityEngine;

public class PlayerRunState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerRunState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        p.PauseAnim(false);
        p.PlayAnim(p.RunHash);
    }

    public void Tick()
    {
        // 1) 벽 감지 → 방향 반전
        if ((p.onGround || p.reverseAlsoInAir) &&
            p.DetectWallAhead(p.dir) &&
            p.reverseCD == 0)
        {
            p.dir *= -1;
            p.reverseCD = p.reverseCooldownFrames;
        }

        // 2) 사다리 진입
        if (p.IsOnLadderCell(out p.curLadderCell, out p.targetLadderCenterX))
        {
            p.onGround = false;
            p.ChangeState(new PlayerLadderClimbState(p, fsm));
            return;
        }

        // 3) 버섯 감지 → 점프 예약
        if (p.onGround && p.mushroomCD == 0 &&
            p.DetectMushroomAhead(p.dir) &&
            p.pendingJumpTimer < 0f)
        {
            p.pendingJumpTimer = Mathf.Max(0.01f, p.mushroomJumpDelaySec);
            p.pendingJumpDir = p.dir;
        }

        // 버섯 점프 예약 카운트다운
        if (p.pendingJumpTimer >= 0f)
        {
            p.pendingJumpTimer -= Time.deltaTime;

            if (p.pendingJumpTimer <= 0f &&
                (!p.requireGroundedAtLaunch || p.onGround))
            {
                p.StartMushroomJump(p.pendingJumpDir);
                p.pendingJumpTimer = -1f;
                p.ChangeState(new PlayerJumpState(p, fsm));
                return;
            }
        }

        // 4) 지면 샘플 & 이동
        if (p.SampleGroundY(p.transform.position, out float groundY))
        {
            if (!p.onGround)
            {
                p.onGround = true;
                p.lockedY = groundY;
            }
            else
            {
                p.lockedY = Mathf.Max(p.lockedY, groundY);
            }

            p.vyPixels = 0f;

            float dx = p.runSpeedPixelsPerSec * p.unitPerPixel * p.dir * Time.deltaTime;
            p.MoveHorizontalWithCast(dx, ref p.lockedY);
            p.SnapYToLocked();

            if (p.lastHorizontalBlocked && p.reverseCD == 0)
            {
                p.dir *= -1;
                p.reverseCD = p.reverseCooldownFrames;
            }
        }
        else
        {
            // 땅을 잃음 → Fall
            p.onGround = false;
            p.ChangeState(new PlayerFallState(p, fsm));
        }
    }

    public void Exit()
    {
        p.pendingJumpTimer = -1f;
    }
}

public class PlayerFallState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerFallState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        p.onGround = false;
        p.PauseAnim(true);
    }

    public void Tick()
    {
        // 사다리 발견 → 탑승
        if (p.IsOnLadderCell(out p.curLadderCell, out p.targetLadderCenterX))
        {
            p.ChangeState(new PlayerLadderClimbState(p, fsm));
            return;
        }

        // 공중에서 벽 반전
        if (p.reverseAlsoInAir &&
            p.DetectWallAhead(p.dir) &&
            p.reverseCD == 0)
        {
            p.dir *= -1;
            p.reverseCD = p.reverseCooldownFrames;
        }

        float dt = Time.deltaTime;
        p.vyPixels = Mathf.Max(p.vyPixels - p.gravityPixelsPerSec2 * dt,
                               -p.maxFallSpeedPixelsPerSec);

        float dx = p.runSpeedPixelsPerSec * p.unitPerPixel * p.dir * dt;
        float dy = p.vyPixels * p.unitPerPixel * dt;

        p.MoveHorizontalWithCast(dx, ref p.lockedY);
        p.MoveVerticalWithCast(dy);

        // 착지 → Run
        if (p.SampleGroundY(p.transform.position, out float groundY))
        {
            // "닿기 직전"에만 스냅: 현재 y(중심)와 groundY(스냅될 중심 y)의 차이가 매우 작을 때만
            float dyToSnap = p.transform.position.y - groundY;

            // 최소 1~2픽셀은 허용 (픽셀 스냅 기반이라 너무 작으면 착지가 늦을 수 있음)
            float tol = Mathf.Max(p.unitPerPixel * 2f, p.groundSnapToleranceUnits);

            if (dyToSnap <= tol)
            {
                p.lockedY = groundY;
                p.onGround = true;
                p.vyPixels = 0f;
                p.SnapYToLocked();
                p.ChangeState(new PlayerRunState(p, fsm));
            }
        }
    }

    public void Exit() 
    {
        p.PauseAnim(false);
    }
}
public class PlayerLadderClimbState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerLadderClimbState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        p.vyPixels = 0;
        p.onGround = false;
    }

    public void Tick()
    {
        if (!p.IsOnLadderCell(out p.curLadderCell, out p.targetLadderCenterX))
        {
            p.MovePixelSnapped(new Vector2(0, p.ladderTopExtra));
            p.dir = (Random.value < 0.5f) ? -1 : 1;
            p.ChangeState(new PlayerRunState(p, fsm));
            return;
        }

        float step = p.unitPerPixel;
        var pos = p.transform.position;
        pos.x = Mathf.Round(p.targetLadderCenterX / step) * step;
        p.transform.position = pos;

        float vy = p.climbSpeedPixelsPerSec * p.unitPerPixel * Time.deltaTime;
        p.MovePixelSnapped(new Vector2(0, vy));
    }

    public void Exit() { }
}
public class PlayerJumpState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerJumpState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        // StartMushroomJump 에서 vyPixels, jumpHorizSpeedPixelsPerSec 세팅됨
    }

    public void Tick()
    {
        float dt = Time.deltaTime;

        float dx = p.jumpHorizSpeedPixelsPerSec * p.unitPerPixel * p.dir * dt;
        float dy = p.vyPixels * p.unitPerPixel * dt;

        p.MoveHorizontalWithCast(dx, ref p.lockedY);
        p.MoveVerticalWithCast(dy);

        p.vyPixels = Mathf.Max(p.vyPixels - p.gravityPixelsPerSec2 * dt,
                               -p.maxFallSpeedPixelsPerSec);

        if (p.vyPixels <= 0f &&
            p.SampleGroundY(p.transform.position, out float gy))
        {
            float step = p.unitPerPixel;
            if (Mathf.Abs(p.transform.position.y - gy) <= 2f * step)
            {
                p.lockedY = gy;
                p.onGround = true;
                p.SnapYToLocked();

                p.reverseCD = p.reverseCooldownFrames; // 착지 직후 반전 방지
                p.ChangeState(new PlayerRunState(p, fsm));
            }
        }
    }

    public void Exit() { }
}
public class PlayerStairClimbState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerStairClimbState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        // TODO: 계단 시작 세팅
    }

    public void Tick()
    {
        // TODO: 계단 이동 로직
    }

    public void Exit() { }
}
public class PlayerLiftingState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerLiftingState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        // TODO: 로켓/승강기 시작 세팅
    }

    public void Tick()
    {
        // TODO: 위로 올라가는 로직
    }

    public void Exit() { }
}

public class PlayerGameOverState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerGameOverState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        // 이동/누적값/속도 전부 정지
        p.vyPixels = 0f;
        p.pixelAccum = Vector2.zero;

        // 벽반전/버섯 같은 예약/쿨다운도 정지시키고 싶으면 초기화
        p.pendingJumpTimer = -1f;
        p.reverseCD = 0;
        p.mushroomCD = 0;

        // 애니메이션 멈춤(원하면 Idle 재생 후 멈추도록 바꿔도 됨)
        p.PauseAnim(true);
    }

    public void Tick()
    {
        // 아무것도 하지 않음 = 완전 정지
    }

    public void Exit()
    {
        // 보통 게임오버는 Exit 안 함
    }
}