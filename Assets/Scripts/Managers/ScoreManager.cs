using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private const string PREF_BEST_M = "BEST_SCORE_M";

    [Header("UI")]
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private string heightPrefix = "";
    [SerializeField] private string heightSuffix = "m";
    [SerializeField] private string bestPrefix = "";
    [SerializeField] private string bestSuffix = "m";
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

    private float _bestMeters;

    public float CurrentMeters { get; private set; }
    public float BestMeters => _bestMeters;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (unitsPerMeter <= 0f) unitsPerMeter = 1f;

        _bestMeters = PlayerPrefs.GetFloat(PREF_BEST_M, 0f);
    }

    private void Start()
    {
        CachePlayer();
        UpdateAllTexts();
    }

    private void Update()
    {
        UpdateMeters();
        TryUpdateBest();
        UpdateAllTexts();
    }
    private void CachePlayer()
    {
        if (player != null) return;

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
    }

    private void UpdateMeters()
    {
        if (player == null)
        {
            CachePlayer();
            if (player == null)
            {
                CurrentMeters = 0f;
                return;
            }
        }

        float y = player.position.y;
        if (useBaseOffset) y -= baseY;

        float meters = y / unitsPerMeter;
        if (!allowNegative) meters = Mathf.Max(0f, meters);

        CurrentMeters = meters;
    }

    private void TryUpdateBest()
    {
        if (CurrentMeters > _bestMeters)
        {
            _bestMeters = CurrentMeters;
            PlayerPrefs.SetFloat(PREF_BEST_M, _bestMeters);
            PlayerPrefs.Save();
        }
    }

    private void UpdateAllTexts()
    {
        string fmt = "F" + Mathf.Clamp(decimals, 0, 6);

        if (heightText != null)
        {
            // 인게임 현재 높이 표시
            heightText.text = heightPrefix + CurrentMeters.ToString(fmt) + heightSuffix;
        }

        if (bestText != null)
        {
            // 인게임 최고기록 항상 표시
            bestText.text = bestPrefix + _bestMeters.ToString(fmt) + bestSuffix;
        }
    }

    // 팝업이 값 갱신 요청할 때 호출용
    public string GetCurrentText()
    {
        string fmt = "F" + Mathf.Clamp(decimals, 0, 6);
        return CurrentMeters.ToString(fmt);
    }

    public string GetBestText()
    {
        string fmt = "F" + Mathf.Clamp(decimals, 0, 6);
        return _bestMeters.ToString(fmt);
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