using UnityEngine;
using UnityEngine.EventSystems; // 필수

public class ButtonHandler : MonoBehaviour
{
    public bool isHovering = false;
    RectTransform rec;

    private void Awake()
    {
        rec = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            if(RectTransformUtility.RectangleContainsScreenPoint(rec, Input.mousePosition, null))
            {
                isHovering = true;
            }
            else
            {
                isHovering = false;
            }
        }
    }

}
