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
        cg.blocksRaycasts = false;

        if (dragGhostTemplate)
        {
            ghost = Instantiate(dragGhostTemplate, parentCanvas.transform);
            ghostRect = ghost.rectTransform;
            ghost.gameObject.SetActive(true);

            ghost.raycastTarget = false;

            foreach (var g in ghost.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

            CanvasGroup ghostCg = ghost.GetComponent<CanvasGroup>();
            if (ghostCg == null)
                ghostCg = ghost.gameObject.AddComponent<CanvasGroup>();

            ghostCg.blocksRaycasts = false;
            ghostCg.interactable = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out var localPos
        );

        ghostRect.localPosition = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;

        if (placementSystem != null && placeablePrefab != null)
        {
            placementSystem.TryPlace(placeablePrefab, eventData, out var placed);
        }

        if (ghost)
            Destroy(ghost.gameObject);
    }
}
