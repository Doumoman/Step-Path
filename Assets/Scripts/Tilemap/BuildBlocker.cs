using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildBlocker : MonoBehaviour
{
    [Header("필수")]
    public Grid grid;
    public Tilemap groundTilemap;  // Ground 타일이 들어있는 타일맵
    public Tilemap ladderTilemap;  // Ladder 타일이 들어있는 타일맵

    [Header("보조(선택) - 물리 충돌로도 막기")]
    public LayerMask blockedPhysicsMask; // Ground, Ladder, 기타 막을 레이어들

    // 이미 우리가 설치 완료한 셀들(중복 설치 방지)
    private readonly HashSet<Vector3Int> occupied = new();

    public void MarkOccupied(Vector3Int cell, Vector2Int size)
    {
        ForEachFootprint(cell, size, c => occupied.Add(c));
    }

    public void UnmarkOccupied(Vector3Int cell, Vector2Int size)
    {
        ForEachFootprint(cell, size, c => occupied.Remove(c));
    }

    public bool IsBlocked(Vector3Int cell, Vector2Int size)
    {
        bool blocked = false;

        ForEachFootprint(cell, size, c =>
        {
            if (blocked) return;

            // 1) 이미 설치된 우리 오브젝트가 있는 셀인가?
            if (occupied.Contains(c)) { blocked = true; return; }

            // 2) Ground 또는 Ladder 타일이 존재하는가?
            if ((groundTilemap && groundTilemap.GetTile(c) != null) ||
                (ladderTilemap && ladderTilemap.GetTile(c) != null))
            {
                blocked = true; return;
            }

            // 3) 보조: Physics2D로도 막기(해당 셀의 AABB 영역에 금지 레이어가 있으면)
            if (blockedPhysicsMask.value != 0)
            {
                var worldCenter = grid.GetCellCenterWorld(c);
                var cellSize = (Vector2)grid.cellSize;
                var hit = Physics2D.OverlapBox(worldCenter, cellSize * 0.98f, 0f, blockedPhysicsMask);
                if (hit) { blocked = true; return; }
            }
        });

        return blocked;
    }

    private void ForEachFootprint(Vector3Int root, Vector2Int size, System.Action<Vector3Int> action)
    {
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
                action(new Vector3Int(root.x + x, root.y + y, root.z));
    }
}
