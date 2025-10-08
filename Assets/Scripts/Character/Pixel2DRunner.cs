using UnityEngine;

/// 픽셀 퍼펙트 이동 전용(Transform를 픽셀그리드에 스냅)
[DisallowMultipleComponent]
public class Pixel2DRunner: MonoBehaviour
{
    [Header("Pixel Settings")]
    [Tooltip("스프라이트 PPU(Pixels Per Unit)")]
    public int pixelsPerUnit = 16;

    [Header("Pixel Settings")]
    public int pixelStep = 1;

    // 누적 이동(월드 단위)
    private Vector2 _accum;

    float PixelToUnits(int px) => (float)px / pixelsPerUnit;

    /// 원하는 월드 델타를 입력하면, 픽셀 그리드에 맞춰 스냅된 실제 이동을 수행
    public void Move(Vector2 worldDelta)
    {
        _accum += worldDelta;

        // 몇 픽셀이나 이동 가능한지 계산(각 축 독립)
        float unitPerPixel = PixelToUnits(pixelStep);

        int moveXPixels = 0;
        int moveYPixels = 0;

        if (Mathf.Abs(_accum.x) >= unitPerPixel)
        {
            moveXPixels = Mathf.FloorToInt(Mathf.Abs(_accum.x) / unitPerPixel) * (int)Mathf.Sign(_accum.x);
            _accum.x -= moveXPixels * unitPerPixel;
        }

        if (Mathf.Abs(_accum.y) >= unitPerPixel)
        {
            moveYPixels = Mathf.FloorToInt(Mathf.Abs(_accum.y) / unitPerPixel) * (int)Mathf.Sign(_accum.y);
            _accum.y -= moveYPixels * unitPerPixel;
        }

        if (moveXPixels != 0 || moveYPixels != 0)
        {
            Vector3 move =
                new Vector3(moveXPixels * unitPerPixel, moveYPixels * unitPerPixel, 0f);

            transform.position += move;

            // 최종 위치도 픽셀 그리드로 스냅(부동소수 누적 오차 방지)
            var p = transform.position;
            p.x = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
            p.y = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;
            transform.position = p;
        }
    }
}
