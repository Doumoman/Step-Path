using System.Collections.Generic;
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

    [Header("UI 차단 판정")]
    [SerializeField] private string placementUiTag = "PlacementUI";

    public bool TryPlace(GameObject placeablePrefab, PointerEventData eventData, out GameObject instance)
    {
        instance = null;

        if (placeablePrefab == null) return false;
        if (grid == null) return false;
        if (worldCamera == null) return false;

        // 진짜 버튼/팝업 같은 UI 위에 드랍한 경우만 배치 금지
        if (IsPointerOverBlockingUI(eventData))
            return false;

        Vector3 screenPosition = eventData.position;

        var worldPos = worldCamera.ScreenToWorldPoint(screenPosition);
        worldPos.z = 0f;

        var cell = grid.WorldToCell(worldPos);

        var placeable = placeablePrefab.GetComponent<Placeable>();
        var size = placeable ? placeable.size : new Vector2Int(1, 1);

        if (blocker != null && blocker.IsBlocked(cell, size))
            return false;

        var spawnPos = grid.GetCellCenterWorld(cell);
        instance = Instantiate(placeablePrefab, spawnPos, Quaternion.identity, placedRoot ? placedRoot : null);

        if (placeable && placeable.alignToCellCenter)
            instance.transform.position = spawnPos;

        if (placeable && !string.IsNullOrEmpty(placeable.placedLayerName))
            instance.layer = LayerMask.NameToLayer(placeable.placedLayerName);

        blocker?.MarkOccupied(cell, size);
        return true;
    }

    private bool IsPointerOverBlockingUI(PointerEventData eventData)
    {
        if (EventSystem.current == null) return false;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            GameObject go = result.gameObject;
            if (go == null) continue;

            // 배치용 아이템 UI는 무시
            if (go.CompareTag(placementUiTag))
                continue;

            if (go.GetComponentInParent<DraggableUIItem>() != null)
                continue;

            // 그 외 Button, Popup, Panel, Slider 등은 배치 차단 UI로 봄
            return true;
        }

        return false;
    }
}