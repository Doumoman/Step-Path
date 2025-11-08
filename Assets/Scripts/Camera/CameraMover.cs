using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraMover : MonoBehaviour
{
    public static CameraMover Instance { get; private set; }

    [Header("Follow Target")]
    [SerializeField] private Transform player;      // PlayerAutoRunner 붙어 있는 오브젝트
    [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
    [SerializeField] private float followLerp = 8f; // 카메라가 플레이어를 따라가는 속도

    [Header("Zoom")]
    [Tooltip("카메라가 플레이어를 얼마나 확대해서 볼지(작을수록 더 확대)")]
    [SerializeField] private float targetOrthoSize = 5f;
    [SerializeField] private float zoomLerp = 3f;

    [Header("Vertical Align On Run")]
    [Tooltip("Run 상태로 돌아왔을 때 Y를 맞출 시간(초)")]
    [SerializeField] private float heightAlignDuration = 0.5f;

    private Camera cam;

    // 높이 잠금 여부 (Fall, LadderClimb, StairClimb, Lifting 동안 true)
    private bool lockHeight = false;

    // Run으로 돌아왔을 때 Y를 서서히 맞출 코루틴
    private Coroutine heightAlignRoutine;

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
        // Inspector에서 안 넣었으면 자동으로 찾기
        if (player == null)
        {
            var runner = FindObjectOfType<PlayerAutoRunner>();
            if (runner != null) player = runner.transform;
        }

        // 시작 시 카메라 위치를 플레이어에 맞춰 세팅
        if (player != null)
        {
            Vector3 pos = player.position + offset;
            pos.z = transform.position.z; // Z는 그대로(2D 카메라 깊이)
            transform.position = pos;

            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = targetOrthoSize;
            }
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 current = transform.position;
        Vector3 target = player.position + offset;

        // 높이 잠금 상태라면 Y는 유지
        if (lockHeight)
            target.y = current.y;

        // Z는 카메라 깊이 유지
        target.z = current.z;

        // 부드럽게 따라가기
        transform.position = Vector3.Lerp(current, target, followLerp * Time.deltaTime);

        // 줌도 서서히 보간
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize =
                Mathf.Lerp(cam.orthographicSize, targetOrthoSize, zoomLerp * Time.deltaTime);
        }
    }

    /// <summary>
    /// 플레이어 상태가 바뀔 때 PlayerAutoRunner에서 호출해주면 됨
    /// </summary>
    public void OnPlayerStateChanged(IPlayerState newState)
    {
        bool isVerticalChanging =
            newState is PlayerFallState ||
            newState is PlayerLadderClimbState ||
            newState is PlayerStairClimbState ||
            newState is PlayerLiftingState;

        // 높이 움직이는 상태 → Y 잠금 + 코루틴 중지
        if (isVerticalChanging)
        {
            lockHeight = true;
            if (heightAlignRoutine != null)
            {
                StopCoroutine(heightAlignRoutine);
                heightAlignRoutine = null;
            }
            return;
        }

        // Run 상태 → 코루틴으로 Y 정렬 시작
        if (newState is PlayerRunState)
        {
            if (heightAlignRoutine != null)
                StopCoroutine(heightAlignRoutine);

            heightAlignRoutine = StartCoroutine(AlignHeightToPlayerSmooth());
            return;
        }

        // 그 외 상태들(예: Jump 등)은 그냥 Y 따라가도록 잠금 해제
        lockHeight = false;
    }

    /// <summary>
    /// Run 상태에서 플레이어 높이에 맞춰 카메라 Y를 서서히 맞추는 코루틴
    /// </summary>
    private IEnumerator AlignHeightToPlayerSmooth()
    {
        if (player == null)
        {
            lockHeight = false;
            yield break;
        }

        // 코루틴 동안은 Y는 이 코루틴이 책임지고 조정하므로 잠금 유지
        lockHeight = true;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, heightAlignDuration);

        float startY = transform.position.y;
        float targetY = (player.position + offset).y;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(startY, targetY, t);
            transform.position = pos;

            yield return null;
        }

        // 마지막에 정확히 맞춰주고, 이후부터는 다시 자유롭게 따라가도록 잠금 해제
        Vector3 finalPos = transform.position;
        finalPos.y = (player.position + offset).y;
        transform.position = finalPos;

        lockHeight = false;
        heightAlignRoutine = null;
    }
}