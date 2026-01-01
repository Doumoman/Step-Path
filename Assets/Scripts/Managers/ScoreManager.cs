using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private string prefix = "Height: ";
    [SerializeField] private string suffix = " m";
    [SerializeField] private int decimals = 0;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Scale")]
    [Tooltip("Y좌표가 몇 단위 올라가면 1m로 표시할지. 예) 1이면 1유닛=1m, 2이면 2유닛=1m")]
    [SerializeField] private float unitsPerMeter = 1f;

    [Header("Options")]
    [Tooltip("시작 높이를 0으로 보정하려면 체크 후 기준 Y를 지정")]
    [SerializeField] private bool useBaseOffset = true;
    [SerializeField] private float baseY = 0f;

    [SerializeField] private bool allowNegative = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (unitsPerMeter <= 0f) unitsPerMeter = 1f;
    }

    private void Start()
    {
        CachePlayer();
        UpdateHeightText(); // 시작 시 1회 갱신
    }

    private void Update()
    {
        UpdateHeightText();
    }

    private void CachePlayer()
    {
        if (player != null) return;

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
    }

    private void UpdateHeightText()
    {
        if (heightText == null) return;

        if (player == null)
        {
            CachePlayer();
            if (player == null)
            {
                heightText.text = prefix + "--" + suffix;
                return;
            }
        }

        float y = player.position.y;
        if (useBaseOffset) y -= baseY;

        float meters = y / unitsPerMeter;
        if (!allowNegative) meters = Mathf.Max(0f, meters);

        // 소수점 자리수 반영
        string fmt = "F" + Mathf.Clamp(decimals, 0, 6);
        heightText.text = prefix + meters.ToString(fmt) + suffix;
    }

    // 런타임에 플레이어를 직접 지정하고 싶을 때 사용.
    public void SetPlayer(Transform playerTransform) => player = playerTransform;

    // 현재 Y를 기준 높이(0m)로 잡고 싶을 때 호출.
    public void SetBaseToCurrentPlayerY()
    {
        if (player == null) CachePlayer();
        if (player == null) return;

        baseY = player.position.y;
        useBaseOffset = true;
    }

    // 환산 비율 변경(몇 유닛당 1m)
    public void SetUnitsPerMeter(float value)
    {
        unitsPerMeter = Mathf.Max(0.0001f, value);
    }
}