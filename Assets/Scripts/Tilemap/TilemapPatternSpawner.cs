using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// 패턴 SO를 읽어 타일을 깔고, 필요시 레이어/콜라이더를 자동 세팅.
/// ※ Rigidbody2D 절대 강제 생성하지 않음(옵션으로만).
public class TilemapPatternSpawner : MonoBehaviour
{
    [Header("패턴들")]
    public TilemapPatternAsset[] patterns;
    public Vector3Int[] cellOffsets;            // patterns와 길이 맞추면 각자 오프셋 적용

    [Header("타겟 Grid (없으면 자동 생성)")]
    public Transform targetGridRoot;            // 여기에 레이어별 Tilemap 존재/생성
    public bool clearBeforeSpawn = false;       // 스폰 전에 싹 비우기
    public bool createMissingLayers = true;     // 패턴에 필요한 레이어 Tilemap 자동 생성

    [Header("레이어 이름 규약")]
    public string[] groundLayerNames = { "Ground" };   // 바닥(충돌)
    public string[] ladderLayerNames = { "Ladder" };   // 사다리(Trigger)

    [Header("Ground 콜라이더 옵션")]
    public bool useCompositeForGround = false;         // true면 TilemapCollider→Composite로 합치기
    public bool addStaticRigidbodyIfComposite = false; // ↑일 때만 상위/자신에 Static RB 추가

    Dictionary<string, Tilemap> _layerMap;

    void Awake()
    {
        if (patterns == null) patterns = new TilemapPatternAsset[0];

        EnsureGridAndLayers();          // Grid/Tilemap 준비(+콜라이더 정책 적용)
        if (clearBeforeSpawn) ClearAllTiles();

        for (int i = 0; i < patterns.Length; i++)
        {
            var asset = patterns[i];
            if (!asset) continue;

            var offset = (cellOffsets != null && i < cellOffsets.Length) ? cellOffsets[i] : Vector3Int.zero;
            Spawn(asset, offset);
        }

        RebuildAllColliders();          // 마지막에 충돌 갱신
    }

    // ========== 내부 구현 ==========

    void EnsureGridAndLayers()
    {
        // 1) GridRoot 확보(없으면 생성). Rigidbody는 옵션으로만 추가.
        if (!targetGridRoot)
        {
            var gridGO = new GameObject("GridRoot");
            gridGO.transform.SetParent(transform, false);
            gridGO.AddComponent<Grid>();
            targetGridRoot = gridGO.transform;
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
            }
            else
            {
                // 기타 레이어: 기본값(충돌 필요 없으면 타일 팔레트에서 ColliderType=None을 쓰세요)
                // 여기선 특별히 건드리지 않음
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
}
