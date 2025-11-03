using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementSystem : MonoBehaviour
{
    [Header("필수")]
    public Grid grid;
    public Camera worldCamera;
    public BuildBlocker blocker;

    [Header("선택")]
    public Transform placedRoot; // 설치된 오브젝트를 정리해서 넣을 부모

    // UI가 드래그 끝에서 호출
    public bool TryPlace(GameObject placeablePrefab, Vector3 screenPosition, out GameObject instance)
    {
        instance = null;

        // UI 위에서 드랍되면 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        // 화면좌표 → 월드 → 셀
        var worldPos = worldCamera.ScreenToWorldPoint(screenPosition);
        worldPos.z = 0f;
        var cell = grid.WorldToCell(worldPos);

        // 프리팹의 셀 크기
        var placeable = placeablePrefab.GetComponent<Placeable>();
        var size = placeable ? placeable.size : new Vector2Int(1, 1);

        // 금지 체크
        if (blocker != null && blocker.IsBlocked(cell, size))
            return false;

        // 스폰
        var spawnPos = grid.GetCellCenterWorld(cell);
        instance = Instantiate(placeablePrefab, spawnPos, Quaternion.identity, placedRoot ? placedRoot : null);

        if (placeable && placeable.alignToCellCenter)
            instance.transform.position = spawnPos;

        if (placeable && !string.IsNullOrEmpty(placeable.placedLayerName))
            instance.layer = LayerMask.NameToLayer(placeable.placedLayerName);

        // 점유 표시
        blocker?.MarkOccupied(cell, size);
        return true;
    }
}