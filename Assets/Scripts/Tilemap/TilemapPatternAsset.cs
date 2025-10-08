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
        public string layer;        // 타일맵 GameObject 이름(예: "Ground", "Ladder", "Deco")
        public Vector3Int pos;      // 로컬 셀 좌표
        public TileBase tile;       // 타일 레퍼런스
        public Matrix4x4 transform; // 회전/뒤집기 등(필요 없으면 생략 가능)
        public Color color;         // 타일 색상(필요 없으면 생략 가능)
    }

    public Vector3Int size;           // bounds.size (정보용)
    public List<Cell> cells = new();  // 모든 레이어/셀
}
