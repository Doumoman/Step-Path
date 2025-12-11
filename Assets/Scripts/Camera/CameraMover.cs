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
    [Tooltip("카메라가 이동할 수 있는 X 최소값")]
    [SerializeField] private float minX = -999f;

    [Tooltip("카메라가 이동할 수 있는 X 최대값")]
    [SerializeField] private float maxX = 999f;

    private Camera cam;

    private bool lockHeight = false;
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
            transform.position = pos;

            if (cam != null && cam.orthographic)
                cam.orthographicSize = targetOrthoSize;
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 current = transform.position;
        Vector3 target = player.position + offset;

        // 높이 잠금 상태에서는 Y 유지
        if (lockHeight)
            target.y = current.y;

        // Z는 고정
        target.z = current.z;

        // X 제한 적용
        target.x = Mathf.Clamp(target.x, minX, maxX);

        // 부드럽게 따라감
        transform.position = Vector3.Lerp(current, target, followLerp * Time.deltaTime);

        // 줌도 보간
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize =
                Mathf.Lerp(cam.orthographicSize, targetOrthoSize, zoomLerp * Time.deltaTime);
        }
    }

    public void OnPlayerStateChanged(IPlayerState newState)
    {
        bool isVerticalChanging =
            newState is PlayerFallState ||
            newState is PlayerLadderClimbState ||
            newState is PlayerStairClimbState ||
            newState is PlayerLiftingState;

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

        if (newState is PlayerRunState)
        {
            if (heightAlignRoutine != null)
                StopCoroutine(heightAlignRoutine);

            heightAlignRoutine = StartCoroutine(AlignHeightToPlayerSmooth());
            return;
        }

        lockHeight = false;
    }

    private IEnumerator AlignHeightToPlayerSmooth()
    {
        if (player == null)
        {
            lockHeight = false;
            yield break;
        }

        lockHeight = true;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, heightAlignDuration);

        float startY = transform.position.y;
        float targetY = (player.position + offset).y;

        float startX = transform.position.x; // X는 유지하되...
        float clampedX = Mathf.Clamp(startX, minX, maxX); // 혹시 초과했으면 다시 Clamp 적용
        transform.position = new Vector3(clampedX, startY, transform.position.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(startY, targetY, t);
            pos.x = Mathf.Clamp(pos.x, minX, maxX); // 코루틴 중에도 보정 유지
            transform.position = pos;

            yield return null;
        }

        Vector3 finalPos = transform.position;
        finalPos.y = (player.position + offset).y;
        finalPos.x = Mathf.Clamp(finalPos.x, minX, maxX);
        transform.position = finalPos;

        lockHeight = false;
        heightAlignRoutine = null;
    }
}
