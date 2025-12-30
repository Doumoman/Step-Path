using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// 패턴 SO를 읽어 타일을 깔고, 필요시 레이어/콜라이더를 자동 세팅.
/// ※ Rigidbody2D 절대 강제 생성하지 않음(옵션으로만).
/// === 세로(Vertical) 청크 스트리밍 전용 버전 ===
/// - 좌우(dx) 청크 생성 로직 제거
/// - 청크는 Y축(세로)으로만 로드/언로드
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

    // ===================== 세로 스트리밍 옵션 =====================
    [Header("=== 세로(Vertical) 청크 스트리밍 옵션 ===")]
    public bool useChunkStreaming = false;             // true면 아래 스트리밍 모드 사용

    [Header("스트리밍 범위(세로 청크 개수)")]
    [Min(1)] public int streamRows = 10;               // 세로 청크 개수 (10이면 총 10줄)

    [Tooltip("플레이어 Transform (위치 기준으로 청크 이동 판정).")]
    public Transform player;

    [Tooltip("시작 청크 Y좌표(그리드가 없거나 player가 없을 때 fallback).")]
    public int startChunkY = 0;

    [Tooltip("언로드 시 지울 오브젝트 레이어(아이템 등).")]
    public LayerMask dynamicObjectMask;

    [Tooltip("모든 청크에 공통으로 더해지는 셀 오프셋 (예: 0,-1,0 넣으면 전체 맵이 그리드 한 칸 아래로).")]
    public Vector3Int globalCellOffset = Vector3Int.zero;

    [Header("청크 간격/스텝(셀 단위)")]
    [Tooltip("0이면 패턴 size 기반 자동 스텝 사용. 1 이상이면 고정 스텝(셀) 사용.")]
    [Min(0)] public int chunkStepYCells = 0;

    [Tooltip("자동 스텝일 때(size.y-1 기반)에 추가로 띄울 셀 수.")]
    [Min(0)] public int extraGapYCells = 0;

    Dictionary<string, Tilemap> _layerMap;
    Grid _grid;

    // 스트리밍용 상태(세로만)
    int _currentChunkY;
    readonly HashSet<int> _loadedChunkYs = new();

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

        Vector3Int cell = _grid.WorldToCell(player.position);

        int stepY = GetStepY(basePattern);

        int newChunkY = Mathf.FloorToInt(cell.y / (float)stepY);

        if (newChunkY != _currentChunkY)
        {
            _currentChunkY = newChunkY;
            UpdateChunksAroundY(_currentChunkY);
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

        // 5) 각 레이어 콜라이더 정책 적용(RB 강제 안 함)
        foreach (var pair in _layerMap)
        {
            var tm = pair.Value;
            var go = tm.gameObject;

            var box = go.GetComponent<BoxCollider2D>();
            if (box) DestroyImmediate(box);

            var tmc = go.GetComponent<TilemapCollider2D>();
            if (!tmc) tmc = go.AddComponent<TilemapCollider2D>();
            tmc.enabled = true;

            if (groundLayerNames.Contains(go.name))
            {
                tmc.isTrigger = false;
                tmc.usedByComposite = useCompositeForGround;

                var comp = go.GetComponent<CompositeCollider2D>();
                if (useCompositeForGround)
                {
                    if (!comp) comp = go.AddComponent<CompositeCollider2D>();
                    comp.geometryType = CompositeCollider2D.GeometryType.Polygons;

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
        Physics2D.SyncTransforms();
    }

    // ===================== 세로 스트리밍 전용 구현 =====================

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
        _loadedChunkYs.Clear();

        int stepY = GetStepY(basePattern);

        if (_grid)
        {
            Vector3Int cell = _grid.WorldToCell(player.position);
            _currentChunkY = Mathf.FloorToInt(cell.y / (float)stepY);
        }
        else
        {
            _currentChunkY = startChunkY;
        }

        UpdateChunksAroundY(_currentChunkY);
    }

    void UpdateChunksAroundY(int centerY)
    {
        int rows = Mathf.Max(1, streamRows);

        // (down + 1 + up) = rows
        int down = rows / 2;
        int up = rows - down - 1;

        HashSet<int> needed = new();
        for (int dy = -down; dy <= up; dy++)
            needed.Add(centerY + dy);

        // 언로드
        List<int> toRemove = new();
        foreach (var y in _loadedChunkYs)
        {
            if (!needed.Contains(y))
                toRemove.Add(y);
        }

        foreach (var y in toRemove)
            UnloadChunkY(y);

        // 로드
        foreach (var y in needed)
        {
            if (_loadedChunkYs.Contains(y)) continue;
            SpawnChunkY(y);
        }

        RebuildAllColliders();
    }

    void SpawnChunkY(int chunkY)
    {
        var pattern = PickPatternForChunkY(chunkY);
        if (!pattern) return;

        Vector3Int offset = ChunkYToCellOffset(chunkY, pattern);
        Spawn(pattern, offset);
        _loadedChunkYs.Add(chunkY);
    }

    void UnloadChunkY(int chunkY)
    {
        ClearChunkTilesY(chunkY);
        ClearChunkDynamicObjectsY(chunkY);
        _loadedChunkYs.Remove(chunkY);
    }

    // 세로 청크 Y에 따른 패턴 선택(결정적)
    TilemapPatternAsset PickPatternForChunkY(int chunkY)
    {
        if (patterns == null || patterns.Length == 0) return null;
        if (patterns.Length == 1) return patterns[0];

        // y만 사용
        int hash = chunkY * 83492791;
        int idx = Mathf.Abs(hash) % patterns.Length;
        return patterns[idx];
    }

    int GetStepY(TilemapPatternAsset basePattern)
    {
        if (chunkStepYCells > 0) return Mathf.Max(1, chunkStepYCells);
        return Mathf.Max(1, (basePattern.size.y - 1) + extraGapYCells);
    }

    Vector3Int ChunkYToCellOffset(int chunkY, TilemapPatternAsset pattern)
    {
        var basePattern = ChunkBasePattern ? ChunkBasePattern : pattern;
        int stepY = GetStepY(basePattern);

        // X는 0 고정(좌우 청크 없음)
        return new Vector3Int(
            0,
            chunkY * stepY,
            0
        ) + globalCellOffset;
    }

    void ClearChunkTilesY(int chunkY)
    {
        var basePattern = ChunkBasePattern;
        if (!basePattern) return;

        Vector3Int origin = ChunkYToCellOffset(chunkY, basePattern);
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

    void ClearChunkDynamicObjectsY(int chunkY)
    {
        var basePattern = ChunkBasePattern;
        if (!basePattern || dynamicObjectMask == 0 || !_grid) return;

        Vector3Int cellMin = ChunkYToCellOffset(chunkY, basePattern);
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

        int rows = Mathf.Max(1, streamRows);
        int down = rows / 2;
        int up = rows - down - 1;

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);

        for (int dy = -down; dy <= up; dy++)
        {
            int y = _currentChunkY + dy;

            Vector3Int cellMin = ChunkYToCellOffset(y, basePattern);
            Vector3Int cellMax = cellMin + new Vector3Int(basePattern.size.x, basePattern.size.y, 0);

            Vector3 worldMin = _grid.CellToWorld(cellMin);
            Vector3 worldMax = _grid.CellToWorld(cellMax);

            Vector3 center = (worldMin + worldMax) * 0.5f;
            Vector3 size3 = worldMax - worldMin;
            Vector3 size = new Vector3(Mathf.Abs(size3.x), Mathf.Abs(size3.y), 0f);

            Gizmos.DrawCube(center, size);
        }
    }
#endif
}