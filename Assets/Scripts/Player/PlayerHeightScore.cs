using UnityEngine;

public class PlayerHeightScore : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private float unitsPerMeter = 1f;

    [Header("Base")]
    [SerializeField] private bool useBaseOffset = true;
    [SerializeField] private float baseY = 0f;

    private float maxY;     // base 적용된 최고 Y(유닛)
    private int lastScore;  // 마지막 반영된 '미터' 값

    void Start()
    {
        if (unitsPerMeter <= 0f) unitsPerMeter = 1f;

        if (useBaseOffset)
            baseY = transform.position.y; // 시작 위치를 0m로 보고 싶으면 이게 안전

        float y = transform.position.y - (useBaseOffset ? baseY : 0f);
        maxY = y;
        lastScore = Mathf.FloorToInt(maxY / unitsPerMeter);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        float y = transform.position.y - (useBaseOffset ? baseY : 0f);

        if (y > maxY)
        {
            maxY = y;

            int meters = Mathf.FloorToInt(maxY / unitsPerMeter);
            if (meters > lastScore)
            {
                int diff = meters - lastScore;
                GameManager.Instance.AddScore(diff);
                lastScore = meters;
            }
        }
    }

    // UI용으로 현재/최고(이번 런) 높이 제공도 가능
    public int CurrentMeters =>
        Mathf.FloorToInt((transform.position.y - (useBaseOffset ? baseY : 0f)) / unitsPerMeter);

    public int BestMeters => lastScore;
}