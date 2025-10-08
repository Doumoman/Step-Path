using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapPatternSpawner : MonoBehaviour
{
    public TilemapPatternAsset pattern;
    public Vector3Int cellOffset;       // 이 오프셋만큼 이동시켜 깔기(스테이지 상 위치)
    public Transform targetGridRoot;    // 이 아래에 레이어별 Tilemap들이 있다고 가정

    Dictionary<string, Tilemap> _layerMap;

    void Awake()
    {
        // 레이어 이름 → Tilemap 매핑
        _layerMap = new Dictionary<string, Tilemap>();
        foreach (var tm in targetGridRoot.GetComponentsInChildren<Tilemap>(true))
        {
            _layerMap[tm.gameObject.name] = tm;
        }

        Spawn(pattern, cellOffset);
    }

    public void Spawn(TilemapPatternAsset asset, Vector3Int offset)
    {
        foreach (var c in asset.cells)
        {
            if (!_layerMap.TryGetValue(c.layer, out var tm)) continue;

            var p = c.pos + offset;
            tm.SetTile(p, c.tile);
            tm.SetTransformMatrix(p, c.transform);
            tm.SetColor(p, c.color);
        }

        // 필요하면 Collider2D 리빌드
        foreach (var tm in _layerMap.Values)
        {
            var col = tm.GetComponent<TilemapCollider2D>();
            if (col) col.ProcessTilemapChanges();
        }
    }
}
