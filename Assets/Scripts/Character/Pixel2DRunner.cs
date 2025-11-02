using UnityEngine;

/// �ȼ� ����Ʈ �̵� ����(Transform�� �ȼ��׸��忡 ����)
[DisallowMultipleComponent]
public class Pixel2DRunner: MonoBehaviour
{
    [Header("Pixel Settings")]
    [Tooltip("��������Ʈ PPU(Pixels Per Unit)")]
    public int pixelsPerUnit = 16;

    [Header("Pixel Settings")]
    public int pixelStep = 1;

    // ���� �̵�(���� ����)
    private Vector2 _accum;

    float PixelToUnits(int px) => (float)px / pixelsPerUnit;

    /// ���ϴ� ���� ��Ÿ�� �Է��ϸ�, �ȼ� �׸��忡 ���� ������ ���� �̵��� ����
    public void Move(Vector2 worldDelta)
    {
        _accum += worldDelta;

        // �� �ȼ��̳� �̵� �������� ���(�� �� ����)
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

            // ���� ��ġ�� �ȼ� �׸���� ����(�ε��Ҽ� ���� ���� ����)
            var p = transform.position;
            p.x = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
            p.y = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;
            transform.position = p;
        }
    }
}
