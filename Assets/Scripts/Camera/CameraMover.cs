using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraMover : MonoBehaviour
{
    public static CameraMover Instance { get; private set; }

    [Header("Follow Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
    [SerializeField] private float followLerp = 8f;

    [Header("Zoom")]
    [SerializeField] private float targetOrthoSize = 5f;
    [SerializeField] private float zoomLerp = 3f;

    [Header("Vertical Align On Run")]
    [SerializeField] private float heightAlignDuration = 0.5f;

    [Header("Horizontal Limit")]
    [SerializeField] private float minX = -999f;
    [SerializeField] private float maxX = 999f;

    [Header("Vertical Limit (Min Only)")]
    [SerializeField] private float minY = -999f;

    private Camera cam;

    // 낙하/등반 상태에서 Y 잠금
    private bool lockHeight = false;

    // Run 정렬 코루틴 중 (LateUpdate가 Y를 건드리지 않도록)
    private bool aligningHeight = false;
    private Coroutine heightAlignRoutine;

    // 평소 상태에서 “카메라가 내려가지 않도록” 고정/상한으로 쓰는 Y
    private float followY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (player == null)
        {
            var runner = FindObjectOfType<PlayerAutoRunner>();
            if (runner != null) player = runner.transform;
        }

        if (player != null)
        {
            Vector3 pos = player.position + offset;
            pos.z = transform.position.z;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Max(pos.y, minY);
            transform.position = pos;

            // 시작 시 followY를 현재 카메라 Y로 초기화
            followY = transform.position.y;

            if (cam != null && cam.orthographic)
                cam.orthographicSize = targetOrthoSize;
        }
        else
        {
            followY = transform.position.y;
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 current = transform.position;

        // 목표 계산
        Vector3 desired = player.position + offset;
        desired.z = current.z;
        desired.x = Mathf.Clamp(desired.x, minX, maxX);

        // Y 계산
        float desiredY = Mathf.Max(desired.y, minY);

        float targetY;

        // 1) 낙하/등반 등 잠금 상태: Y 유지
        if (lockHeight)
        {
            targetY = current.y;
        }
        // 2) Run 정렬 코루틴 중: Y는 코루틴이 담당 (LateUpdate는 유지)
        else if (aligningHeight)
        {
            targetY = current.y;
        }
        // 3) 평소: 절대 내려가지 않음 (올라갈 때만 followY 갱신)
        else
        {
            followY = Mathf.Max(followY, desiredY); // 내려갈 때는 갱신 안 됨
            targetY = followY;
        }

        Vector3 target = new Vector3(desired.x, targetY, desired.z);

        // 부드럽게 이동
        transform.position = Vector3.Lerp(current, target, followLerp * Time.deltaTime);

        // 최종 Clamp (X / minY만)
        Vector3 clamped = transform.position;
        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Max(clamped.y, minY);
        transform.position = clamped;

        // 줌 보간
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize =
                Mathf.Lerp(cam.orthographicSize, targetOrthoSize, zoomLerp * Time.deltaTime);
        }
    }

    public void OnPlayerStateChanged(IPlayerState newState)
    {
        bool isLockHeightState =
            newState is PlayerFallState ||
            newState is PlayerLadderClimbState ||
            newState is PlayerStairClimbState;

        if (isLockHeightState)
        {
            lockHeight = true;
            StopAlignRoutine();
            return;
        }

        if (newState is PlayerLiftingState)
        {
            lockHeight = false;
            StopAlignRoutine();

            // lifting 끝나고 평소로 돌아갈 때도 현재 카메라 Y를 기준으로 재고정
            followY = transform.position.y;
            return;
        }

        if (newState is PlayerRunState)
        {
            lockHeight = false;
            StopAlignRoutine();
            // 플레이어 기준 목표 Y가 "현재 카메라 Y보다 높을 때만" 정렬
            float desiredY = Mathf.Max((player.position + offset).y, minY);
            if (desiredY > transform.position.y + 0.0001f)
                heightAlignRoutine = StartCoroutine(AlignHeightToPlayerSmooth());
            else
                followY = transform.position.y; // 아래로는 안 가게 현재값으로 고정

            return;
        }

        lockHeight = false;
        StopAlignRoutine();

        // 기타 상태로 돌아왔을 때도 현재 카메라 Y를 기준으로 재고정
        followY = transform.position.y;
    }

    private void StopAlignRoutine()
    {
        aligningHeight = false;

        if (heightAlignRoutine != null)
        {
            StopCoroutine(heightAlignRoutine);
            heightAlignRoutine = null;
        }
    }

    private IEnumerator AlignHeightToPlayerSmooth()
    {
        if (player == null) yield break;

        aligningHeight = true;

        float duration = Mathf.Max(0.01f, heightAlignDuration);
        float elapsed = 0f;

        float startY = transform.position.y;
        float targetY = Mathf.Max((player.position + offset).y, minY);

        targetY = Mathf.Max(targetY, startY);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(startY, targetY, t);
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Max(pos.y, minY);

            transform.position = pos;
            yield return null;
        }

        // 종료 보정
        Vector3 finalPos = transform.position;
        finalPos.y = Mathf.Max((player.position + offset).y, minY);
        finalPos.x = Mathf.Clamp(finalPos.x, minX, maxX);
        transform.position = finalPos;

        aligningHeight = false;
        heightAlignRoutine = null;

        // ★ 중요: 코루틴 종료 후 “현재 카메라 Y”를 followY로 동기화
        // 이후 평소 상태에서 이 값보다 내려가지 않음
        followY = transform.position.y;
    }

    // 필요하면 외부에서 강제로 “현재 플레이어 높이 기준으로” followY를 리셋하는 함수
    public void ResetFollowYToPlayer()
    {
        if (player == null) return;
        followY = Mathf.Max((player.position + offset).y, minY);
    }
}