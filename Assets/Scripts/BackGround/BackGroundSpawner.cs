using System.Collections.Generic;
using UnityEngine;

public class BackGroundSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private SpriteRenderer backgroundPrefab;

    [Header("Spawn Settings")]
    [Tooltip("첫 배경이 스폰될 월드 Y 위치")]
    [SerializeField] private float firstSpawnY = 4.2f;

    [Tooltip("다음 배경이 스폰될 Y 간격(누적)")]
    [SerializeField] private float stepY = 11.2f;

    [Tooltip("플레이어 기준으로 유지할 배경 개수")]
    [SerializeField] private int keepCount = 2;

    [Header("Rendering")]
    [SerializeField] private int sortingOrder = -99;
    [SerializeField] private string sortingLayerName = ""; // 비우면 변경 안 함

    // 내부 상태
    private readonly List<SpriteRenderer> _spawned = new();
    private float _nextSpawnY;

    private void Awake()
    {
        if (!player) Debug.LogError("[BackGroundSpawner] player가 비었습니다.");
        if (!backgroundPrefab) Debug.LogError("[BackGroundSpawner] backgroundPrefab이 비었습니다.");
    }

    private void Start()
    {
        if (!player || !backgroundPrefab) return;

        _nextSpawnY = firstSpawnY;
        EnsureTwoBackgroundsAhead();
    }

    private void Update()
    {
        if (!player || !backgroundPrefab) return;

        EnsureTwoBackgroundsAhead();
        CullFarBackgrounds();
    }

    private void EnsureTwoBackgroundsAhead()
    {
        float playerY = player.position.y;

        float topY = float.NegativeInfinity;
        for (int i = 0; i < _spawned.Count; i++)
            topY = Mathf.Max(topY, _spawned[i].transform.position.y);

        if (_spawned.Count == 0)
            topY = float.NegativeInfinity;

        while (_spawned.Count < keepCount || topY < playerY)
        {
            var bg = SpawnAtY(_nextSpawnY);
            _spawned.Add(bg);

            topY = bg.transform.position.y;
            _nextSpawnY += stepY;
        }
    }

    private SpriteRenderer SpawnAtY(float y)
    {
        // X는 항상 0으로 고정
        Vector3 pos = new Vector3(0f, y, 0f);
        var bg = Instantiate(backgroundPrefab, pos, Quaternion.identity, transform);

        bg.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName))
            bg.sortingLayerName = sortingLayerName;

        return bg;
    }

    private void CullFarBackgrounds()
    {
        if (_spawned.Count <= keepCount) return;

        float playerY = player.position.y;

        _spawned.Sort((a, b) =>
        {
            float da = Mathf.Abs(a.transform.position.y - playerY);
            float db = Mathf.Abs(b.transform.position.y - playerY);
            return da.CompareTo(db);
        });

        for (int i = _spawned.Count - 1; i >= keepCount; i--)
        {
            if (_spawned[i])
                Destroy(_spawned[i].gameObject);
            _spawned.RemoveAt(i);
        }
    }
}
