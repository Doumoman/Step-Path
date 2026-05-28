using System.Collections.Generic;
using UnityEngine;

public class BackGroundSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Prefabs (Stages)")]
    [SerializeField] private SpriteRenderer backgroundPrefab1;
    [SerializeField] private SpriteRenderer betweenPrefab12;
    [SerializeField] private float betweenSize12 = 4.08f;

    [SerializeField] private SpriteRenderer backgroundPrefab2;
    [SerializeField] private SpriteRenderer betweenPrefab23;
    [SerializeField] private float betweenSize23 = 4.08f;

    [SerializeField] private SpriteRenderer backgroundPrefab3;

    [Header("Stage Switch (Player Height)")]
    [SerializeField] private float stage2StartPlayerY = 20f;
    [SerializeField] private float stage3StartPlayerY = 40f;

    [Header("Stage Switch BGM (Player Height)")]
    [SerializeField] private float stage2BGMStartPlayerY = 40f;
    [SerializeField] private float stage3BGMStartPlayerY = 75f;
    public float Stage2StartPlayerY => stage2StartPlayerY;
    public float Stage3StartPlayerY => stage3StartPlayerY;

    public float Stage2BGMStartPlayerY => stage2BGMStartPlayerY;
    public float Stage3BGMStartPlayerY => stage3BGMStartPlayerY;

    [Header("Spawn Settings")]
    [Tooltip("첫 배경의 하단(bounds.min.y)이 위치할 월드 Y")]
    [SerializeField] private float firstSpawnBottomY = 6.2f;

    [Tooltip("배경(1/2/3)끼리 쌓이는 하단~하단 간격")]
    [SerializeField] private float stepY = 11.2f;

    [Tooltip("기존 방식 유지: keepCount or topY < playerY면 스폰")]
    [SerializeField] private int keepCount = 2;

    [Header("Spawn Ahead")]
    [Tooltip("플레이어가 topY에 닿기 전에 미리 스폰할 여유 거리(월드 Y)")]
    [SerializeField] private float spawnAheadMarginY = 8f;

    [Header("Cull Settings (New)")]
    [Tooltip("플레이어 기준으로 이 값 * stepY 보다 아래면 비활성화(풀로 회수)")]
    [SerializeField] private int cullBelowSteps = 3;

    [Header("Rendering")]
    [SerializeField] private int sortingOrder = -99;
    [SerializeField] private string sortingLayerName = "";

    // =========================
    // 내부 상태
    // =========================
    public enum Stage { S1, S2, S3 }
    public Stage CurrentStage => _stage;
    private Stage _stage = Stage.S1;

    private bool _stage2Triggered;
    private bool _stage3Triggered;

    private bool _stage2bgmTriggered;
    private bool _stage3bgmTriggered;

    private float _nextSpawnBottomY;

    // 활성 객체들
    private readonly List<SpriteRenderer> _active = new();

    // Between은 보호
    private readonly HashSet<SpriteRenderer> _betweenSet = new();

    // 풀(프리팹별로 재사용)
    private readonly Dictionary<SpriteRenderer, Stack<SpriteRenderer>> _pool = new();

    private void Start()
    {
        if (!player) { Debug.LogError("[BackGroundSpawner] player가 비었습니다."); return; }

        _nextSpawnBottomY = firstSpawnBottomY;

        EnsureKeepCountAhead();
        CullByBelowSteps();
    }

    private void Update()
    {
        if (!player) return;

        // 단 한번만 트리거
        if (!_stage2Triggered && player.position.y >= stage2StartPlayerY)
        {
            _stage2Triggered = true;
            SpawnBetweenAtNext(betweenPrefab12, betweenSize12);
            _stage = Stage.S2;
        }

        if (!_stage2bgmTriggered && player.position.y >= stage2BGMStartPlayerY)
        {
            _stage2bgmTriggered = true;
            SoundManager.Instance.PlayBgm("Play2");
        }

        if (!_stage3Triggered && player.position.y >= stage3StartPlayerY)
        {
            _stage3Triggered = true;
            SpawnBetweenAtNext(betweenPrefab23, betweenSize23);
            _stage = Stage.S3;
        }

        if (!_stage3bgmTriggered && player.position.y >= stage3BGMStartPlayerY)
        {
            _stage3bgmTriggered = true;
            SoundManager.Instance.PlayBgm("Play3");
        }

        EnsureKeepCountAhead();
        CullByBelowSteps();
    }

    private SpriteRenderer CurrentStagePrefab()
    {
        return _stage switch
        {
            Stage.S1 => backgroundPrefab1,
            Stage.S2 => backgroundPrefab2,
            Stage.S3 => backgroundPrefab3,
            _ => backgroundPrefab1
        };
    }

    // =========================
    // 기존 스폰 방식 유지
    // =========================
    private void EnsureKeepCountAhead()
    {
        float playerY = player.position.y;

        float topY = float.NegativeInfinity;
        for (int i = 0; i < _active.Count; i++)
        {
            var sr = _active[i];
            if (!sr || !sr.gameObject.activeSelf) continue;
            topY = Mathf.Max(topY, sr.bounds.max.y);
        }

        while (CountActive() < keepCount || topY < playerY + spawnAheadMarginY)
        {
            var prefab = CurrentStagePrefab();
            var bg = SpawnAlignedBottom(prefab, _nextSpawnBottomY);
            _active.Add(bg);

            topY = Mathf.Max(topY, bg.bounds.max.y);
            _nextSpawnBottomY += stepY;
        }
    }


    private int CountActive()
    {
        int c = 0;
        for (int i = 0; i < _active.Count; i++)
            if (_active[i] && _active[i].gameObject.activeSelf) c++;
        return c;
    }

    // =========================
    // Between은 다음 위치에 1번만 끼움
    // =========================
    private void SpawnBetweenAtNext(SpriteRenderer betweenPrefab, float betweenSize)
    {
        if (!betweenPrefab) return;

        var sr = SpawnAlignedBottom(betweenPrefab, _nextSpawnBottomY);
        _active.Add(sr);
        _betweenSet.Add(sr);

        _nextSpawnBottomY += Mathf.Max(0f, betweenSize);
    }

    // =========================
    // 새 Cull: 플레이어 기준 아래로 N*stepY보다 더 내려가면 회수(풀링)
    // =========================
    private void CullByBelowSteps()
    {
        float playerY = player.position.y;
        float cutoffY = playerY - (Mathf.Max(1, cullBelowSteps) * stepY);

        // 활성 리스트를 순회하며 cutoff 아래면 비활성화
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var sr = _active[i];
            if (!sr) { _active.RemoveAt(i); continue; }
            if (!sr.gameObject.activeSelf) continue;

            // Between은 절대 회수하지 않음
            if (_betweenSet.Contains(sr)) continue;

            // "하단" 기준으로 컷: 아래로 충분히 내려갔으면 회수
            if (sr.bounds.max.y < cutoffY) // 아예 화면 아래 한참 밑이면
            {
                ReleaseToPool(sr);
                _active.RemoveAt(i);
            }
        }
    }

    // =========================
    // 풀링
    // =========================
    private SpriteRenderer SpawnAlignedBottom(SpriteRenderer prefab, float targetBottomY)
    {
        var sr = GetFromPool(prefab);
        sr.transform.SetParent(transform, false);

        // 일단 대충 위치
        sr.transform.position = new Vector3(0f, targetBottomY, 0f);
        sr.transform.rotation = Quaternion.identity;

        sr.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName))
            sr.sortingLayerName = sortingLayerName;

        // 하단 정렬
        float curBottom = sr.bounds.min.y;
        float delta = targetBottomY - curBottom;
        sr.transform.position += new Vector3(0f, delta, 0f);

        sr.gameObject.SetActive(true);
        return sr;
    }

    private SpriteRenderer GetFromPool(SpriteRenderer prefab)
    {
        if (!_pool.TryGetValue(prefab, out var stack))
        {
            stack = new Stack<SpriteRenderer>();
            _pool[prefab] = stack;
        }

        if (stack.Count > 0)
            return stack.Pop();

        // 새로 생성
        var sr = Instantiate(prefab);
        return sr;
    }

    private void ReleaseToPool(SpriteRenderer sr)
    {
        // 어떤 프리팹에서 왔는지 알아야 같은 풀에 넣을 수 있는데,
        // 간단히 "현재 스프라이트 이름 기반" 같은 걸로 추적하면 불안정함.
        // 그래서 가장 확실한 방식: 생성 시 prefabKey를 컴포넌트로 붙여서 기록.

        var key = sr.GetComponent<PoolKey>();
        if (key == null)
        {
            // 키가 없으면 안전하게 Destroy(여기 오면 설정 누락)
            Destroy(sr.gameObject);
            return;
        }

        if (!_pool.TryGetValue(key.prefab, out var stack))
        {
            stack = new Stack<SpriteRenderer>();
            _pool[key.prefab] = stack;
        }

        sr.gameObject.SetActive(false);
        stack.Push(sr);
    }

    // =========================
    // PoolKey: 어떤 프리팹 풀에 돌아갈지 기록
    // =========================
    private class PoolKey : MonoBehaviour
    {
        public SpriteRenderer prefab;
    }

    // Instantiate된 객체에 PoolKey를 심어둠(한 번만)
    private void EnsurePoolKey(SpriteRenderer instance, SpriteRenderer prefab)
    {
        var key = instance.GetComponent<PoolKey>();
        if (key == null) key = instance.gameObject.AddComponent<PoolKey>();
        key.prefab = prefab;
    }

    // GetFromPool에서 새로 만든 경우 key 심기
    private SpriteRenderer Instantiate(SpriteRenderer prefab)
    {
        var sr = UnityEngine.Object.Instantiate(prefab);
        EnsurePoolKey(sr, prefab);
        return sr;
    }
}
