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
        var totalBounds = new BoundsInt();
        bool first = true;

        foreach (var tm in tilemaps)
        {
            // 비어있는 타일 스킵
            var bounds = tm.cellBounds;
            var all = bounds.allPositionsWithin;
            foreach (var p in all)
            {
                var tile = tm.GetTile(p);
                if (!tile) continue;

                if (first) { totalBounds = bounds; first = false; }
                else totalBounds.Encapsulate(new BoundsInt(p, Vector3Int.one));

                var cell = new TilemapPatternAsset.Cell
                {
                    layer = tm.gameObject.name,
                    pos = p,
                    tile = tile,
                    transform = tm.GetTransformMatrix(p),
                    color = tm.GetColor(p)
                };
                asset.cells.Add(cell);
            }
        }

        asset.size = totalBounds.size;
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        EditorUtility.DisplayDialog("Pattern Baker", $"저장 완료: {asset.cells.Count} cells", "OK");
    }
}
#endif
