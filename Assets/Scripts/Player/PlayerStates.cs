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
        p.pendingMushroom = false;
        p.pendingClimb = false;
        p.pendingStairs = false;
        p.pendingRocket = false;
        p.PauseAnim(false);
        p.PlayAnim(p.WalkHash);
    }

    public void Tick()
    {

        // 덩쿨/사다리 감지 → 목표X 저장(한 번만)
        if (!p.pendingClimb && p.DetectClimbableAhead(p.dir, out var col, out var cx, out var targetCY))
        {
            p.pendingClimb = true;
            p.pendingClimbTargetX = cx;
            p.pendingClimbTargetCenterY = targetCY;
        }
        // 계단 감지
        if (!p.pendingStairs && p.onGround && p.stairsCD == 0 && p.DetectStairsAhead(p.dir, out var stairCol, out float sx))
        {
            p.pendingStairs = true;
            p.pendingStairsTargetX = sx;
        }
        // 로켓 감지 → 목표X 저장(한 번만)
        if (!p.pendingRocket && p.onGround && p.rocketCD == 0 &&
            p.DetectRocketAhead(p.dir, out var rocketCol, out var rx, out var targetcenterY))
        {
            p.pendingRocket = true;
            p.pendingRocketTargetX = rx;

            p.pendingRocketCol = rocketCol;

            // LiftingState로 넘길 값 저장(진입 시점에 세팅해도 되지만 여기서 저장해둠)
            p.rocketCenterX = rx;
            p.rocketStartCenterY = p.transform.position.y;
            p.rocketTargetCenterY = targetcenterY;
        }
        // 벽 감지 → 방향 반전
        if ((p.onGround || p.reverseAlsoInAir) &&
            p.DetectWallAhead(p.dir) &&
            p.reverseCD == 0)
        {
            p.dir *= -1;
            p.reverseCD = p.reverseCooldownFrames;
            p.pendingClimb = false;
            p.pendingMushroom = false;
            p.pendingStairs = false;
            p.pendingRocket = false;
            p.pendingRocketCol = null;
        }

        // 버섯 감지
        if (!p.pendingMushroom && p.onGround && p.mushroomCD == 0 &&p.DetectMushroomAheadX(p.dir, out float mx, out var kind))
        {
            p.pendingMushroom = true;
            p.pendingMushroomTargetX = mx;
            p.pendingMushroomKind = kind;
        }

        // 지면 샘플 & 이동
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
        float x = p.transform.position.x;
        float tol = p.EnterXTolUnits;

        // 클라임 실행 조건: 목표X에 도달/통과
        if (p.pendingClimb)
        {
            bool reached = (p.dir > 0) ? (x >= p.pendingClimbTargetX - tol)
                                      : (x <= p.pendingClimbTargetX + tol);

            if (reached)
            {
                p.pendingClimb = false;

                p.climbCenterX = p.pendingClimbTargetX;
                p.climbTargetCenterY = p.pendingClimbTargetCenterY;

                p.onGround = false;
                p.ChangeState(new PlayerLadderClimbState(p, fsm));
                return;
            }
        }

        // 버섯 점프 실행 조건: 목표X에 도달/통과
        if (p.pendingMushroom)
        {
            bool reached = Mathf.Abs(x - p.pendingMushroomTargetX) <= tol;

            if (reached && p.onGround && p.mushroomCD == 0)
            {
                p.pendingMushroom = false;

                p.StartMushroomJump(p.dir, p.pendingMushroomKind);
                p.ChangeState(new PlayerJumpState(p, fsm));
                return;
            }
        }

        if (p.pendingStairs)
        {
            bool reached = Mathf.Abs(x - p.pendingStairsTargetX) <= tol;
            if (reached && p.onGround && p.stairsCD == 0)
            {
                p.pendingStairs = false;
                p.onGround = false; // 계단은 자체적으로 y를 올릴 거라 일단 공중 처리
                p.ChangeState(new PlayerStairClimbState(p, fsm));
                return;
            }
        }
        if (p.pendingRocket)
        {
            bool reached = Mathf.Abs(x - p.pendingRocketTargetX) <= tol;

            if (reached && p.onGround && p.rocketCD == 0)
            {
                p.pendingRocket = false;

                if (p.pendingRocketCol != null)
                {
                    // Destroy 방식
                    GameObject.Destroy(p.pendingRocketCol.gameObject);

                    p.pendingRocketCol = null;
                }

                // X 정렬 후 진입(상태에서 X 고정)
                p.SnapXTo(p.rocketCenterX);

                p.onGround = false;
                p.vyPixels = 0f;
                p.pixelAccum = Vector2.zero;

                p.ChangeState(new PlayerLiftingState(p, fsm));
                return;
            }
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


        // 2) 공중에서 벽 반전
        if (p.reverseAlsoInAir &&
            p.DetectWallAhead(p.dir) &&
            p.reverseCD == 0)
        {
            p.dir *= -1;
            p.reverseCD = p.reverseCooldownFrames;
        }

        float dt = Time.deltaTime;
        p.vyPixels = Mathf.Max(p.vyPixels - p.JumpGravityDownPixelsPerSec2 * dt,
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

        p.PauseAnim(false);
        p.PlayAnim(p.ClimbHash);
    }

    public void Tick()
    {
        // 중심 정렬
        p.SnapXTo(p.climbCenterX);

        float dt = Time.deltaTime;
        float dy = p.climbSpeedPixelsPerSec * p.unitPerPixel * dt;

        // 위로 이동(천장/타일 충돌은 groundMask로 처리됨)
        p.MoveVerticalWithCast(dy);

        // 목표 높이에 도달하면 종료
        float eps = p.unitPerPixel; // 1픽셀 정도 여유
        if (p.transform.position.y >= p.climbTargetCenterY - eps)
        {
            // 목표 높이로 스냅
            var pos = p.transform.position;
            pos.y = p.climbTargetCenterY;
            p.transform.position = pos;

            p.StartClimbCooldown();
            // 착지 가능한지 확인
            if (p.SampleGroundY(p.transform.position, out float groundY))
            {
                p.lockedY = groundY;
                p.onGround = true;
                p.vyPixels = 0f;
                p.SnapYToLocked();

                p.reverseCD = p.reverseCooldownFrames;
                p.ChangeState(new PlayerRunState(p, fsm));
            }
            else
            {
                // 바닥이 없으면 그냥 낙하 상태로
                p.ChangeState(new PlayerFallState(p, fsm));
            }
        }
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
        p.PauseAnim(false);
        p.PlayAnim(p.JumpHash);
    }

    public void Tick()
    {
        float dt = Time.deltaTime;

        float dx = p.jumpHorizSpeedPixelsPerSec * p.unitPerPixel * p.dir * dt;
        float dy = p.vyPixels * p.unitPerPixel * dt;

        p.MoveHorizontalWithCast(dx, ref p.lockedY);
        p.MoveVerticalWithCast(dy);

        float g = (p.vyPixels > 0f) ? p.JumpGravityUpPixelsPerSec2 : p.JumpGravityDownPixelsPerSec2;
        p.vyPixels = Mathf.Max(p.vyPixels - g * dt, -p.maxFallSpeedPixelsPerSec);

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
    private bool prevFlipX;
    public PlayerStairClimbState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        p.vyPixels = 0f;
        p.PauseAnim(false);
        p.PlayAnim(p.StairClimbHash); // 계단 전용 애니 없으면 걷기로
        var sr = p.Sprite;
        if (sr)
        {
            prevFlipX = sr.flipX;
            p.overrideFlip = true;
            sr.flipX = !sr.flipX;
        }
    }

    public void Tick()
    {
        float dt = Time.deltaTime;

        // 계단 진행: x를 앞으로 움직이고, y는 선형식으로 맞춘다
        float dx = p.runSpeedPixelsPerSec * p.unitPerPixel * p.stairMoveDir * dt;

        // 수평 이동은 기존 캐스트로 막힘 처리(벽/천장 등)
        float dummyLockedY = p.lockedY;
        p.MoveHorizontalWithCast(dx, ref dummyLockedY);

        float x = p.transform.position.x;
        float minX = Mathf.Min(p.stairStart.x, p.stairEnd.x);
        float maxX = Mathf.Max(p.stairStart.x, p.stairEnd.x);
        float cx = Mathf.Clamp(x, minX, maxX);

        float denom = (p.stairEnd.x - p.stairStart.x);
        float t = (Mathf.Abs(denom) < 0.0001f) ? 0f : (cx - p.stairStart.x) / denom;
        t = Mathf.Clamp01(t);

        float yLine = Mathf.Lerp(p.stairStart.y, p.stairEnd.y, t);

        //“원본 선분의 총 상승량”
        float rawRise = (p.stairEnd.y - p.stairStart.y);

        //“원하는 총 상승량” (항상 +stairsRiseUnits 만큼 올라가게)
        //    rawRise가 0이면(수평) 방어.
        float scale = 1f;
        if (Mathf.Abs(rawRise) > 0.0001f)
        {
            // end에서 정확히 start + stairsRiseUnits 되도록 스케일
            scale = p.stairsRiseUnits / rawRise;
        }
        // 진행감만 조절하고 싶으면(선택) t를 가공 (형태만 바뀌고 최종상승량은 고정)
        float te = (p.stairsShapeEase == 1f) ? t : Mathf.Pow(t, 1f / p.stairsShapeEase);
        // y를 “정규화 스케일”로 변환 (끝에서는 정확히 +stairsRiseUnits)
        float yScaled = p.stairStart.y + (Mathf.Lerp(p.stairStart.y, p.stairEnd.y, te) - p.stairStart.y) * scale;

        float halfH = p.CastSize.y * 0.5f;
        float targetCenterY = yScaled + halfH;

        // y 스냅(픽셀 스냅 적용)
        var pos = p.transform.position;
        pos.y = Mathf.Round(targetCenterY / p.unitPerPixel) * p.unitPerPixel;
        p.transform.position = pos;

        // 끝 도달 체크
        float tol = p.EnterXTolUnits;
        bool finished =
            (p.stairMoveDir > 0) ? (p.transform.position.x >= p.stairEnd.x - tol)
                                 : (p.transform.position.x <= p.stairEnd.x + tol);

        if (finished)
        {
            // 계단 끝에서 바닥 스냅 후 Run 복귀
            p.StartStairsCooldown();

            if (p.SampleGroundY(p.transform.position, out float gy))
            {
                p.lockedY = gy;
                p.onGround = true;
                p.vyPixels = 0f;
                p.SnapYToLocked();

                p.reverseCD = p.reverseCooldownFrames; // 끝에서 즉시 반전 방지(원하면 제거)
                p.ChangeState(new PlayerRunState(p, fsm));
            }
            else
            {
                p.ChangeState(new PlayerFallState(p, fsm));
            }
        }
    }

    public void Exit() 
    {
        var sr = p.Sprite;
        if (sr)
        {
            sr.flipX = prevFlipX;
            p.overrideFlip = false;
        }
    }
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
        p.vyPixels = 0f;
        p.pixelAccum = Vector2.zero;
        p.onGround = false;
        p.SnapXTo(p.rocketCenterX);

        p.PauseAnim(false);
        p.PlayAnim(p.RocketHash);
    }

    public void Tick()
    {
        // X는 계속 고정 (드리프트 방지)
        p.SnapXTo(p.rocketCenterX);

        float dt = Time.deltaTime;

        // Y만 상승
        float dy = p.rocketLiftSpeedPixelsPerSec * p.unitPerPixel * dt;
        p.MoveVerticalWithCast(dy);

        // 목표 높이 도달 체크
        float eps = p.unitPerPixel; // 1px 여유
        if (p.transform.position.y >= p.rocketTargetCenterY - eps)
        {
            var pos = p.transform.position;
            pos.y = p.rocketTargetCenterY;
            p.transform.position = pos;

            p.StartRocketCooldown();

            // 상승 종료 후: 바닥이 있으면 Run, 없으면 Fall
            if (p.SampleGroundY(p.transform.position, out float groundY))
            {
                p.lockedY = groundY;
                p.onGround = true;
                p.vyPixels = 0f;
                p.SnapYToLocked();

                p.reverseCD = p.reverseCooldownFrames;
                p.ChangeState(new PlayerRunState(p, fsm));
            }
            else
            {
                p.PlayAnim(p.WalkHash);
                p.ChangeState(new PlayerFallState(p, fsm));
            }
        }
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