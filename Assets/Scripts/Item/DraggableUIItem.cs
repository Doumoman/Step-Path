using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("연결")]
    public PlacementSystem placementSystem;
    public GameObject placeablePrefab;        // 끌어다 놓을 대상 프리팹
    public Canvas parentCanvas;               // 이 UI가 속한 캔버스
    public Image dragGhostTemplate;           // 드래그 중 마우스 따라다니는 유령 이미지(선택)

    private Image ghost;
    private RectTransform ghostRect;
    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cg) cg.blocksRaycasts = false;

        if (dragGhostTemplate)
        {
            ghost = Instantiate(dragGhostTemplate, parentCanvas.transform);
            ghostRect = ghost.rectTransform;
            ghost.gameObject.SetActive(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position, parentCanvas.worldCamera, out var localPos);
        ghostRect.localPosition = localPos;
        // 고스트와 모든 자식 Graphic의 레이캐스트 비활성화
        ghost.raycastTarget = false;
        foreach (var g in ghost.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        // 아예 레이어를 Ignore Raycast로
        ghost.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (cg) cg.blocksRaycasts = true;

        if (placementSystem != null && placeablePrefab != null)
        {
            if (!placementSystem.TryPlace(placeablePrefab, eventData.position, out var placed))
            {
                // 실패 이펙트/사운드가 필요하면 여기서
                // 예: 흔들림/빨간 테두리 등
            }
        }

        if (ghost) Destroy(ghost.gameObject);
    }
}
