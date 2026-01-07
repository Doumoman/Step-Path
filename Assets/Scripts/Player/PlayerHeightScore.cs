using UnityEngine;

public class PlayerHeightScore : MonoBehaviour
{
    private float maxY;        // 지금까지 도달한 최고 높이
    private int lastScore;     // 마지막으로 반영된 점수

    void Start()
    {
        maxY = transform.position.y;
        lastScore = 0;
    }

    void Update()
    {
        float currentY = transform.position.y;

        // 위로 올라간 경우에만
        if (currentY > maxY)
        {
            maxY = currentY;

            // 1m 단위 점수
            int scoreByHeight = Mathf.FloorToInt(maxY);

            // 이미 반영된 점수보다 클 때만 추가
            if (scoreByHeight > lastScore)
            {
                int diff = scoreByHeight - lastScore;
                GameManager.Instance.AddScore(diff);
                lastScore = scoreByHeight;
            }
        }
    }
}

