using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GameOverTriggerFollower : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform cameraTransform;

    [Header("Offset (Camera 기준)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, -3f, 0f);

    private Rigidbody2D rb;

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
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // ✅ 부드럽게 X, 바로 위치 고정
        var pos = cameraTransform.position + offset;
        pos.z = transform.position.z; // 트리거의 z는 유지(2D면 보통 0)
        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("게임오버");
        // 1) 게임오버 처리
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();

        // 2) 플레이어 상태를 GameOver로 전환
        var player = other.GetComponent<PlayerAutoRunner>();
        if (player != null)
            player.SetGameOver();
    }
}