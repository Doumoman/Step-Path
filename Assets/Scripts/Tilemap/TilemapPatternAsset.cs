using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tilemap/Pattern Asset")]
public class TilemapPatternAsset : ScriptableObject
{
    [Serializable]
    public struct Cell
    {
        public string layer;        // Tilemap 게임오브젝트 이름 (예: "Ground", "Ladder")
        public Vector3Int pos;      // 원본 셀 좌표(그리드 기준, origin 보정 전)
        public TileBase tile;       // 레퍼런스
        public Matrix4x4 transform; // 회전/뒤집기 등
        public Color color;         // 타일 색상
    }

    public Vector3Int origin;       // 최소 셀 좌표(좌상단 기준) - 복원 시 보정에 사용
    public Vector3Int size;         // 전체 크기 (정보용)
    public List<Cell> cells = new();
}