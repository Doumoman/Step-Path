using UnityEngine;

public class Placeable : MonoBehaviour
{
    [Tooltip("셀 단위 크기 (1x1 기본). 가로x세로 셀 수")]
    public Vector2Int size = new Vector2Int(1, 1);

    [Tooltip("설치 시 Grid 셀 중앙에 정렬")]
    public bool alignToCellCenter = true;

    [Tooltip("설치된 후 차지하는 레이어(선택). 예: Placeables")]
    public string placedLayerName = "Default";
}

