using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// 패턴 SO를 읽어 타일을 깔고, 필요시 레이어/콜라이더를 자동 세팅.
/// ※ Rigidbody2D 절대 강제 생성하지 않음(옵션으로만).
public class TilemapPatternSpawner : MonoBehaviour
{
    [Header("기존 패턴 스폰 (정적)")]
    public TilemapPatternAsset[] patterns;      // 여러 패턴을 넣어둔다.
    public Vector3Int[] cellOffsets;            // patterns와 길이 맞추면 각자 오프셋 적용
    public bool clearBeforeSpawn = false;       // 스폰 전에 싹 비우기
    public bool createMissingLayers = true;     // 패턴에 필요한 레이어 Tilemap 자동 생성

    [Header("타겟 Grid (없으면 자동 생성)")]
    public Transform targetGridRoot;            // 여기에 레이어별 Tilemap 존재/생성

    [Header("레이어 이름 규약")]
    public string[] groundLayerNames = { "Ground" };   // 바닥(충돌)
    public string[] ladderLayerNames = { "Ladder" };   // 사다리(Trigger)

    [Header("Ground 콜라이더 옵션")]
    public bool useCompositeForGround = false;         // true면 TilemapCollider→Composite로 합치기
    public bool addStaticRigidbodyIfComposite = false; // ↑일 때만 상위/자신에 Static RB 추가

    // ===================== 3×3 스트리밍 옵션 =====================
    [Header("=== 청크 스트리밍 옵션 ===")]
    public bool useChunkStreaming = false;             // true면 아래 스트리밍 모드 사용

    [Tooltip("플레이어 Transform (위치 기준으로 청크 이동 판정).")]
    public Transform player;

    [Tooltip("시작 청크 좌표 (대부분 0,0이면 충분).")]
    public Vector2Int startChunk = Vector2Int.zero;

    [Tooltip("언로드 시 지울 오브젝트 레이어(아이템 등).")]
    public LayerMask dynamicObjectMask;

    [Tooltip("모든 청크에 공통으로 더해지는 셀 오프셋 (예: 0,-1,0 넣으면 전체 맵이 그리드 한 칸 아래로).")]
    public Vector3Int globalCellOffset = Vector3Int.zero;

    Dictionary<string, Tilemap> _layerMap;
    Grid _grid;

    // 스트리밍용 상태
    Vector2Int _currentChunk;
    readonly HashSet<Vector2Int> _loadedChunks = new();

    // 스트리밍에서 청크 크기를 참조할 패턴 (모든 패턴은 같은 size를 쓴다고 가정)
    TilemapPatternAsset ChunkBasePattern
    {
        get
        {
            if (patterns != null && patterns.Length > 0) return patterns[0];
            return null;
        }
    }

    void Awake()
    {
        if (patterns == null) patterns = new TilemapPatternAsset[0];

        EnsureGridAndLayers(); // Grid/Tilemap 준비(+콜라이더 정책 적용)

        if (useChunkStreaming)
        {
            InitChunkStreaming();
        }
        else
        {
            // 기존: 한 번만 굽고 끝나는 방식
            if (clearBeforeSpawn) ClearAllTiles();

            for (int i = 0; i < patterns.Length; i++)
            {
                var asset = patterns[i];
                if (!asset) continue;

                var offset = (cellOffsets != null && i < cellOffsets.Length)
                    ? cellOffsets[i]
                    : Vector3Int.zero;

                Spawn(asset, offset);
            }

            RebuildAllColliders();
        }
    }

    void Update()
    {
        if (!useChunkStreaming || !_grid || !player) return;

        var basePattern = ChunkBasePattern;
        if (!basePattern) return;

        // 플레이어의 현재 청크 좌표 계산
        Vector3Int cell = _grid.WorldToCell(player.position);
        Vector2Int newChunk = new Vector2Int(
            Mathf.FloorToInt(cell.x / (float)basePattern.size.x),
            Mathf.FloorToInt(cell.y / (float)basePattern.size.y)
        );

        if (newChunk != _currentChunk)
        {
            _currentChunk = newChunk;
            UpdateChunksAround(_currentChunk);
        }
    }

    // ===================== 내부 구현: 공통 =====================

    void EnsureGridAndLayers()
    {
        // 1) GridRoot 확보(없으면 생성). Rigidbody는 옵션으로만 추가.
        if (!targetGridRoot)
        {
            var gridGO = new GameObject("GridRoot");
            gridGO.transform.SetParent(transform, false);
            _grid = gridGO.AddComponent<Grid>();
            targetGridRoot = gridGO.transform;
        }
        else
        {
            _grid = targetGridRoot.GetComponent<Grid>();
            if (!_grid) _grid = targetGridRoot.gameObject.AddComponent<Grid>();
        }

        // 2) 패턴에 필요한 레이어 이름 수집
        var neededLayers = new HashSet<string>();
        foreach (var p in patterns)
        {
            if (!p) continue;
            foreach (var c in p.cells) neededLayers.Add(c.layer);
        }

        // 3) 존재하는 타일맵 매핑
        _layerMap = new Dictionary<string, Tilemap>();
        foreach (var tm in targetGridRoot.GetComponentsInChildren<Tilemap>(true))
            _layerMap[tm.gameObject.name] = tm;

        // 4) 누락 레이어 자동 생성
        if (createMissingLayers)
        {
            foreach (var layer in neededLayers)
            {
                if (_layerMap.ContainsKey(layer)) continue;
                var go = new GameObject(layer);
                go.transform.SetParent(targetGridRoot, false);
                var tm = go.AddComponent<Tilemap>();
                go.AddComponent<TilemapRenderer>();
                _layerMap[layer] = tm;
            }
        }

        // 5) 각 레이어 콜라이더 정책 적용(절대 RB 강제 안 함)
        foreach (var pair in _layerMap)
        {
            var tm = pair.Value;
            var go = tm.gameObject;

            // BoxCollider2D는 타일 단위 충돌과 맞지 않으므로 제거
            var box = go.GetComponent<BoxCollider2D>();
            if (box) DestroyImmediate(box);

            var tmc = go.GetComponent<TilemapCollider2D>();
            if (!tmc) tmc = go.AddComponent<TilemapCollider2D>();
            tmc.enabled = true;

            if (groundLayerNames.Contains(go.name))
            {
                tmc.isTrigger = false;
                tmc.usedByComposite = useCompositeForGround;

                // Composite 사용 여부에 따라 부착/제거
                var comp = go.GetComponent<CompositeCollider2D>();
                if (useCompositeForGround)
                {
                    if (!comp) comp = go.AddComponent<CompositeCollider2D>();
                    comp.geometryType = CompositeCollider2D.GeometryType.Polygons;

                    // 옵션: Composite가 필요할 때만 Static Rigidbody 보장
                    if (addStaticRigidbodyIfComposite)
                    {
                        var rb = go.GetComponent<Rigidbody2D>();
                        if (!rb) rb = go.AddComponent<Rigidbody2D>();
                        rb.bodyType = RigidbodyType2D.Static;
                    }
                }
                else
                {
                    if (comp) DestroyImmediate(comp);
                }
            }
            else if (ladderLayerNames.Contains(go.name))
            {
                tmc.isTrigger = true;
            }
            else
            {
                // 기타 레이어는 팔레트에서 ColliderType=None 등으로 제어
            }
        }
    }

    public void ClearAllTiles()
    {
        foreach (var tm in _layerMap.Values) tm.ClearAllTiles();
    }

    public void Spawn(TilemapPatternAsset asset, Vector3Int offset)
    {
        foreach (var c in asset.cells)
        {
            if (!_layerMap.TryGetValue(c.layer, out var tm)) continue;

            var p = (c.pos - asset.origin) + offset; // 원점 보정 후 오프셋
            tm.SetTile(p, c.tile);
            tm.SetTransformMatrix(p, c.transform);
            tm.SetColor(p, c.color);
        }
    }

    void RebuildAllColliders()
    {
        foreach (var tm in _layerMap.Values)
        {
            var tmc = tm.GetComponent<TilemapCollider2D>();
            if (tmc) tmc.ProcessTilemapChanges();
        }
        Physics2D.SyncTransforms(); // 물리 쿼리 최신화
    }

    // ===================== 스트리밍 전용 구현 =====================

    void InitChunkStreaming()
    {
        var basePattern = ChunkBasePattern;
        if (!basePattern)
        {
            Debug.LogError("[TilemapPatternSpawner] useChunkStreaming=true인데 patterns 배열에 패턴이 없습니다.", this);
            return;
        }
        if (!player)
        {
            Debug.LogError("[TilemapPatternSpawner] useChunkStreaming=true인데 player가 비어 있습니다.", this);
            return;
        }

        if (clearBeforeSpawn) ClearAllTiles();
        _loadedChunks.Clear();

        // 플레이어 위치 기준으로 초기 청크 잡기 (startChunk를 쓰고 싶으면 이 부분 대신 사용)
        if (_grid)
        {
            Vector3Int cell = _grid.WorldToCell(player.position);
            _currentChunk = new Vector2Int(
                Mathf.FloorToInt(cell.x / (float)basePattern.size.x),
                Mathf.FloorToInt(cell.y / (float)basePattern.size.y)
            );
        }
        else
        {
            _currentChunk = startChunk;
        }

        UpdateChunksAround(_currentChunk);
    }

    void UpdateChunksAround(Vector2Int center)
    {
        // center 기준 3×3 필요한 청크 집합
        HashSet<Vector2Int> needed = new();
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                needed.Add(new Vector2Int(center.x + dx, center.y + dy));
            }
        }

        // 언로드할 청크
        List<Vector2Int> toRemove = new();
        foreach (var c in _loadedChunks)
        {
            if (!needed.Contains(c))
                toRemove.Add(c);
        }

        foreach (var c in toRemove)
            UnloadChunk(c);

        // 새로 로드할 청크
        foreach (var c in needed)
        {
            if (_loadedChunks.Contains(c)) continue;
            SpawnChunk(c);
        }

        RebuildAllColliders();
    }

    void SpawnChunk(Vector2Int chunk)
    {
        // 이 청크에 쓸 패턴을 랜덤(좌표 기반 결정적)으로 고른다.
        var pattern = PickPatternForChunk(chunk);
        if (!pattern) return;

        Vector3Int offset = ChunkToCellOffset(chunk, pattern);
        Spawn(pattern, offset);
        _loadedChunks.Add(chunk);
    }

    void UnloadChunk(Vector2Int chunk)
    {
        ClearChunkTiles(chunk);
        ClearChunkDynamicObjects(chunk);
        _loadedChunks.Remove(chunk);
    }

    // 청크 좌표에 따른 패턴 선택 (좌표 기반 결정적 랜덤)
    TilemapPatternAsset PickPatternForChunk(Vector2Int chunk)
    {
        if (patterns == null || patterns.Length == 0) return null;
        if (patterns.Length == 1) return patterns[0];

        int hash = chunk.x * 73856093 ^ chunk.y * 19349663;
        int idx = Mathf.Abs(hash) % patterns.Length;
        return patterns[idx];
    }

    Vector3Int ChunkToCellOffset(Vector2Int chunk, TilemapPatternAsset pattern)
    {
        int stepX = Mathf.Max(1, pattern.size.x - 1);
        int stepY = Mathf.Max(1, pattern.size.y - 1);

        return new Vector3Int(
            chunk.x * stepX,
            chunk.y * stepY,
            0
        ) + globalCellOffset;
    }
    void ClearChunkTiles(Vector2Int chunk)
    {
        var basePattern = ChunkBasePattern;
        if (!basePattern) return;

        Vector3Int origin = ChunkToCellOffset(chunk, basePattern);
        Vector3Int size = basePattern.size;

        foreach (var tm in _layerMap.Values)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var p = new Vector3Int(origin.x + x, origin.y + y, 0);
                    tm.SetTile(p, null);
                    tm.SetTransformMatrix(p, Matrix4x4.identity);
                    tm.SetColor(p, Color.white);
                }
            }
        }
    }

    void ClearChunkDynamicObjects(Vector2Int chunk)
    {
        var basePattern = ChunkBasePattern;
        if (!basePattern || dynamicObjectMask == 0 || !_grid) return;

        // 청크의 셀 영역 → 월드 박스로 변환
        Vector3Int cellMin = ChunkToCellOffset(chunk, basePattern);
        Vector3Int cellMax = cellMin + new Vector3Int(basePattern.size.x, basePattern.size.y, 0);

        Vector3 worldMin = _grid.CellToWorld(cellMin);
        Vector3 worldMax = _grid.CellToWorld(cellMax);

        Vector3 center = (worldMin + worldMax) * 0.5f;
        Vector3 size3 = worldMax - worldMin;
        Vector2 size = new Vector2(Mathf.Abs(size3.x), Mathf.Abs(size3.y));

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, dynamicObjectMask);
        foreach (var h in hits)
        {
            if (h && h.gameObject)
                Destroy(h.gameObject);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!useChunkStreaming || !_grid) return;

        var basePattern = ChunkBasePattern;
        if (!basePattern) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                Vector2Int c = new Vector2Int(_currentChunk.x + dx, _currentChunk.y + dy);
                Vector3Int cellMin = ChunkToCellOffset(c, basePattern);
                Vector3Int cellMax = cellMin + new Vector3Int(basePattern.size.x, basePattern.size.y, 0);

                Vector3 worldMin = _grid.CellToWorld(cellMin);
                Vector3 worldMax = _grid.CellToWorld(cellMax);

                Vector3 center = (worldMin + worldMax) * 0.5f;
                Vector3 size3 = worldMax - worldMin;
                Vector3 size = new Vector3(Mathf.Abs(size3.x), Mathf.Abs(size3.y), 0f);

                Gizmos.DrawCube(center, size);
            }
        }
    }
#endif
}
