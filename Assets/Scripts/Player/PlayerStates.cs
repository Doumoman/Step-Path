using UnityEngine;

public class PlayerRunState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;
    private float walkSoundTimer;
    private const float walkSoundInterval = 0.1f;
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
        walkSoundTimer = 0f;
    }

    public void Tick()
    {
        // 걷는 사운드 재생
        if (p.onGround)
        {
            walkSoundTimer += Time.deltaTime;

            if (walkSoundTimer >= walkSoundInterval)
            {
                walkSoundTimer = 0f;
                SoundManager.Instance.PlayPlayerSound("Walk_Step");
            }
        }
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
            bool reached = (p.dir > 0)
                ? x >= p.pendingMushroomTargetX - tol
                : x <= p.pendingMushroomTargetX + tol;

            if (reached && p.onGround && p.mushroomCD == 0)
            {
                p.pendingMushroom = false;

                // 기본값은 처음 감지했던 버섯 종류
                var finalKind = p.pendingMushroomKind;

                // 점프 직전에 현재 targetX 근처 버섯 종류를 다시 확인
                if (p.DetectMushroomKindNearX(p.pendingMushroomTargetX, out var currentKind))
                {
                    finalKind = currentKind;
                }

                p.StartMushroomJump(p.dir, finalKind);
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
                    Debug.Log("Rocket interaction target: " + p.pendingRocketCol.gameObject.name);

                    // Destroy 대신 일단 비활성화 테스트
                    p.pendingRocketCol.enabled = false;

                    var sr = p.pendingRocketCol.GetComponentInParent<SpriteRenderer>();
                    if (sr != null) sr.enabled = false;

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
    private float climbSoundTimer;
    private const float climbSoundInterval = 0.3f;
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
        climbSoundTimer = 0f;
    }

    public void Tick()
    {
        // 중심 정렬
        p.SnapXTo(p.climbCenterX);

        float dt = Time.deltaTime;
        float dy = p.climbSpeedPixelsPerSec * p.unitPerPixel * dt;

        // 클라임 사운드 주기 재생
        climbSoundTimer += dt;
        if (climbSoundTimer >= climbSoundInterval)
        {
            climbSoundTimer = 0f;
            SoundManager.Instance.PlayPlayerSound("Climb_Vine");
        }


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
        SoundManager.Instance.PlayObjectSound("Mushroom_Bounce");
    }

    public void Tick()
    {
        float dt = Time.deltaTime;

        // 점프 중 벽 충돌 → 방향 반전
        if (p.DetectWallAhead(p.dir) && p.reverseCD == 0)
        {
            p.dir *= -1;
            p.reverseCD = p.reverseCooldownFrames;
        }

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
    private bool backingDown;

    public PlayerStairClimbState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player;
        fsm = machine;
    }

    public void Enter()
    {
        p.vyPixels = 0f;
        p.onGround = false;
        backingDown = false;

        p.PauseAnim(false);
        p.PlayAnim(p.StairClimbHash);

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

        // 올라가는 중에만 벽 감지
        if (!backingDown && p.DetectWallAhead(p.stairMoveDir))
        {
            backingDown = true;
            p.pixelAccum = Vector2.zero;
        }

        // 올라갈 땐 기존 방향, 벽 만나면 반대 방향으로 내려오기
        int moveDir = backingDown ? -p.stairMoveDir : p.stairMoveDir;

        float dx = p.runSpeedPixelsPerSec * p.unitPerPixel * moveDir * dt;

        float dummyLockedY = p.lockedY;
        p.MoveHorizontalWithCast(dx, ref dummyLockedY);

        float x = p.transform.position.x;
        float minX = Mathf.Min(p.stairStart.x, p.stairEnd.x);
        float maxX = Mathf.Max(p.stairStart.x, p.stairEnd.x);
        float cx = Mathf.Clamp(x, minX, maxX);

        float denom = p.stairEnd.x - p.stairStart.x;
        float t = Mathf.Abs(denom) < 0.0001f ? 0f : (cx - p.stairStart.x) / denom;
        t = Mathf.Clamp01(t);

        float rawRise = p.stairEnd.y - p.stairStart.y;

        float scale = 1f;
        if (Mathf.Abs(rawRise) > 0.0001f)
        {
            scale = p.stairsRiseUnits / rawRise;
        }

        float te = p.stairsShapeEase == 1f
            ? t
            : Mathf.Pow(t, 1f / p.stairsShapeEase);

        float yScaled =
            p.stairStart.y +
            (Mathf.Lerp(p.stairStart.y, p.stairEnd.y, te) - p.stairStart.y) * scale;

        float halfH = p.CastSize.y * 0.5f;
        float targetCenterY = yScaled + halfH;

        var pos = p.transform.position;
        pos.y = Mathf.Round(targetCenterY / p.unitPerPixel) * p.unitPerPixel;
        p.transform.position = pos;

        float tol = p.EnterXTolUnits;

        if (!backingDown)
        {
            bool finished =
                p.stairMoveDir > 0
                    ? p.transform.position.x >= p.stairEnd.x - tol
                    : p.transform.position.x <= p.stairEnd.x + tol;

            if (finished)
            {
                FinishStairs();
            }
        }
        else
        {
            bool returnedToStart =
                p.stairMoveDir > 0
                    ? p.transform.position.x <= p.stairStart.x + tol
                    : p.transform.position.x >= p.stairStart.x - tol;

            if (returnedToStart)
            {
                CancelStairsAndReturn();
            }
        }
    }

    private void FinishStairs()
    {
        p.StartStairsCooldown();

        if (p.SampleGroundY(p.transform.position, out float gy))
        {
            p.lockedY = gy;
            p.onGround = true;
            p.vyPixels = 0f;
            p.SnapYToLocked();

            p.reverseCD = p.reverseCooldownFrames;
            p.ChangeState(new PlayerRunState(p, fsm));
        }
        else
        {
            p.ChangeState(new PlayerFallState(p, fsm));
        }
    }

    private void CancelStairsAndReturn()
    {
        p.StartStairsCooldown();

        if (p.SampleGroundY(p.transform.position, out float gy))
        {
            p.lockedY = gy;
            p.onGround = true;
            p.vyPixels = 0f;
            p.SnapYToLocked();

            // 여기서 방향 반전하지 않음.
            // 방향을 바꾸면 다음 계단 감지가 꼬일 수 있음.
            p.reverseCD = p.reverseCooldownFrames;

            p.ChangeState(new PlayerRunState(p, fsm));
        }
        else
        {
            p.ChangeState(new PlayerFallState(p, fsm));
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
    private bool _finished;
    public PlayerGameOverState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player; fsm = machine;
    }

    public void Enter()
    {
        // 이동/누적값/속도 정지
        p.vyPixels = 0f;
        p.pixelAccum = Vector2.zero;
        p.onGround = false;

        // 각종 예약/쿨다운 정리
        p.pendingJumpTimer = -1f;
        p.reverseCD = 0;
        p.mushroomCD = 0;
        p.climbCD = 0;
        p.stairsCD = 0;
        p.rocketCD = 0;

        p.pendingClimb = false;
        p.pendingMushroom = false;
        p.pendingStairs = false;
        p.pendingRocket = false;
        p.pendingRocketCol = null;

        // 중요: 멈추지 말고 게임오버 애니 재생
        p.PauseAnim(false);
        p.PlayAnim(p.GameOverHash, true);

        _finished = false;
    }

    public void Tick()
    {
        if (_finished) return;
        if (p.Animator == null) return;

        AnimatorStateInfo stateInfo = p.Animator.GetCurrentAnimatorStateInfo(0);

        // GameOver 상태가 실제로 재생 중이고, 1회 재생이 끝났으면 콜백 실행
        if (!p.Animator.IsInTransition(0) &&
            stateInfo.shortNameHash == p.GameOverHash &&
            stateInfo.normalizedTime >= 1f)
        {
            _finished = true;
            p.InvokeGameOverAnimationFinished();
        }
    }

    public void Exit()
    {
        // 보통 게임오버는 Exit 안 함
    }
}
public class PlayerEndingState : IPlayerState
{
    private readonly PlayerAutoRunner p;
    private readonly PlayerStateMachine fsm;

    public PlayerEndingState(PlayerAutoRunner player, PlayerStateMachine machine)
    {
        p = player;
        fsm = machine;
    }

    public void Enter()
    {
        // 기존 이동 관련 값 초기화
        p.vyPixels = 0f;
        p.pixelAccum = Vector2.zero;
        p.onGround = false;
        p.lastHorizontalBlocked = false;

        // 예약된 상호작용 제거
        p.pendingClimb = false;
        p.pendingMushroom = false;
        p.pendingStairs = false;
        p.pendingRocket = false;
        p.pendingRocketCol = null;

        // 애니메이션은 멈추지 않음
        p.PauseAnim(false);

        // 기본은 Idle
        p.PlayAnim(p.IdleHash);
    }

    public void Tick()
    {
        // 의도적으로 아무것도 안 함.
        // 이동, 낙하, 충돌, 자동달리기, 로켓, 사다리 처리 전부 중단.
        // 엔딩 중 위치 이동은 EndingManager가 transform.position으로 직접 담당.
    }

    public void Exit()
    {
    }
}