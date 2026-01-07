using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GameOverTrigger : MonoBehaviour
{
    [Header("Fall GameTrigger Follow Target")]
    [SerializeField] private Transform cameraTransform;

    [Header("Fall GameTrigger Offset (Camera 기준)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, -3f, 0f);

    [Header("GameOver - Height Stall")]
    public float heightStallLimitSec = 5f;      // 외부에서 수정 가능
    public float heightEpsilon = 0.01f;         // "상승" 판정 최소값(노이즈 방지)

    [Header("UI - Stall Gauge")]
    [SerializeField] private Slider stallGauge;          // 0~1
    [SerializeField] private float gaugeSmoothSpeed = 3f; // 값 변화 속도(초당)

    [Header("GameOver Popup")]
    [SerializeField] private UIsGameOver gameOverPopup;
    private float _lastBestY;
    private float _stallTimer;
    private bool _isGameOver;

    private float _gaugeValue = 1f;
    private float _gaugeTarget = 1f;

    private Rigidbody2D rb;
    private PlayerAutoRunner _player;
    private Transform _playerTr;

    private void Awake()
    {
        // Collider는 Trigger여야 함
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Trigger 이벤트 안정화를 위해 Kinematic Rigidbody2D 보장
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (stallGauge != null)
        {
            stallGauge.minValue = 0f;
            stallGauge.maxValue = 1f;
            stallGauge.value = 1f;
        }
    }
    private void Start()
    {
        // (요청대로) Update에서 계속 찾는 대신, 시작 시 1회 캐싱 + 필요 시 재시도
        CachePlayer();
        if (_playerTr != null)
        {
            _lastBestY = _playerTr.position.y;
            _stallTimer = 0f;
            SetGaugeInstant(1f);
        }
    }
    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        var pos = cameraTransform.position + offset;
        pos.z = transform.position.z; // 트리거의 z는 유지(2D면 보통 0)
        transform.position = pos;
    }
    private void Update()
    {
        if (_isGameOver) return;

        // 플레이어가 아직 캐싱 안 되었으면 주기적으로 캐싱 시도
        if (_playerTr == null)
        {
            CachePlayer();
            if (_playerTr == null) return;

            // 방금 잡혔으면 기준값 초기화
            _lastBestY = _playerTr.position.y;
            _stallTimer = 0f;
            SetGaugeInstant(1f);
        }

        float y = _playerTr.position.y;

        // "최고 높이" 갱신이 없으면 타이머 누적
        if (y > _lastBestY + heightEpsilon)
        {
            _lastBestY = y;
            _stallTimer = 0f;
            _gaugeTarget = 1f;
        }
        else
        {
            _stallTimer += Time.deltaTime;

            float t = Mathf.Clamp01(_stallTimer / Mathf.Max(0.0001f, heightStallLimitSec));
            _gaugeTarget = 1f - t;

            if (_stallTimer >= heightStallLimitSec)
            {
                TriggerGameOver();
                return;
            }
        }
        SmoothGauge();
    }
    private void SmoothGauge()
    {
        if (stallGauge == null) return;

        _gaugeValue = Mathf.MoveTowards(
            _gaugeValue,
            _gaugeTarget,
            gaugeSmoothSpeed * Time.deltaTime
        );

        stallGauge.value = _gaugeValue;
    }
    private void SetGaugeInstant(float v)
    {
        _gaugeValue = v;
        _gaugeTarget = v;
        if (stallGauge != null) stallGauge.value = v;
    }
    private void TriggerGameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        // 점수 저장/최고기록/이벤트/사운드 정리 => GameManager에서 일원화
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();

        // 플레이어 정지(로컬 처리)
        if (_player != null) _player.SetGameOver();

        // UI 표시(표시만 담당)
        if (gameOverPopup != null)
        {
            Debug.Log("게임오버");
            gameOverPopup.Show();
        }
    }

    private void CachePlayer()
    {
        // 태그 기반으로 찾고 PlayerAutoRunner 캐싱
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;

        _player = go.GetComponent<PlayerAutoRunner>();
        _playerTr = go.transform;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isGameOver) return;
        if (!other.CompareTag("Player")) return;

        // 낙하 트리거로 인한 게임오버
        _player = other.GetComponent<PlayerAutoRunner>();
        _playerTr = other.transform;

        TriggerGameOver();
    }
}