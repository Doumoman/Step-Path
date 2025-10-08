#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapPatternBaker
{
    [MenuItem("Tools/Tilemap/Save Selected Grid as Pattern Asset")]
    public static void SaveSelectedGrid()
    {
        var go = Selection.activeGameObject;
        if (!go)
        {
            EditorUtility.DisplayDialog("Pattern Baker", "Grid를 선택하세요.", "OK");
            return;
        }

        var grid = go.GetComponentInChildren<Grid>();
        if (!grid)
        {
            EditorUtility.DisplayDialog("Pattern Baker", "선택한 객체에 Grid가 없습니다.", "OK");
            return;
        }

        var path = EditorUtility.SaveFilePanelInProject(
            "Save Pattern Asset", "NewTilemapPattern", "asset", "패턴 에셋 저장 경로를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;

        var asset = ScriptableObject.CreateInstance<TilemapPatternAsset>();
        var tilemaps = grid.GetComponentsInChildren<Tilemap>(true);

        bool hasAnyCell = false;
        Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

        foreach (var tm in tilemaps)
        {
            var bounds = tm.cellBounds; // 타일맵 자체의 작업범위
            foreach (var p in bounds.allPositionsWithin)
            {
                var tile = tm.GetTile(p);
                if (!tile) continue;

                // 셀 기록
                var cell = new TilemapPatternAsset.Cell
                {
                    layer = tm.gameObject.name,
                    pos = p,
                    tile = tile,
                    transform = tm.GetTransformMatrix(p),
                    color = tm.GetColor(p)
                };
                asset.cells.Add(cell);

                // min/max 갱신
                if (!hasAnyCell) { min = max = p; hasAnyCell = true; }
                else
                {
                    if (p.x < min.x) min.x = p.x;
                    if (p.y < min.y) min.y = p.y;
                    if (p.z < min.z) min.z = p.z;
                    if (p.x > max.x) max.x = p.x;
                    if (p.y > max.y) max.y = p.y;
                    if (p.z > max.z) max.z = p.z;
                }
            }
        }

        if (!hasAnyCell)
        {
            EditorUtility.DisplayDialog("Pattern Baker", "그리드 내에 타일이 없습니다.", "OK");
            return;
        }

        // origin/size 저장
        asset.origin = min;
        // size = (max - min + 1)
        asset.size = new Vector3Int(max.x - min.x + 1, max.y - min.y + 1, max.z - min.z + 1);

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;

        EditorUtility.DisplayDialog(
            "Pattern Baker",
            $"저장 완료\nCells: {asset.cells.Count}\nOrigin: {asset.origin}\nSize: {asset.size}",
            "OK"
        );
    }
}
#endif
