using UnityEngine;
using UnityEngine.Events;

public enum DragDropButtonType
{
    None,
    Reroll,
    Pause
}

public class ButtonHandler : MonoBehaviour
{
    public bool isHovering = false;

    [Header("드롭 액션")]
    public DragDropButtonType buttonType = DragDropButtonType.None;

    [Header("Pause 같은 일반 UI 동작 연결")]
    public UnityEvent onDropAction;

    RectTransform rec;

    private void Awake()
    {
        rec = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            isHovering = RectTransformUtility.RectangleContainsScreenPoint(
                rec,
                Input.mousePosition,
                null
            );
        }
    }

    public bool IsMouseOver()
    {
        if (rec == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            rec,
            Input.mousePosition,
            null
        );
    }

    public void InvokeDropAction()
    {
        onDropAction?.Invoke();
    }
}