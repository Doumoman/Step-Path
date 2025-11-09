using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class BackgroundChanger : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;   // UI 배경 이미지
    [SerializeField] private Sprite[] backgroundSprites; // 높이 구간별 배경 이미지들

    private int currentIndex = -1; // 현재 적용된 배경의 인덱스 저장(초기 -1로 해서 시작 시 강제 교체)

    void Update()
    {
        // GameManager에서 현재 점수(높이) 가져오기
        float height = GameManager.Instance.GetScore();

        // 100m마다 한 단계 전환 → 예: 0~99=0, 100~199=1 ...
        int newIndex = Mathf.FloorToInt(height / 100f);

        // 인덱스가 바뀌면 배경 교체
        if (newIndex != currentIndex && newIndex < backgroundSprites.Length)
            //새로 계산한 newIndex가 현재 적용된 currentIndex와 다르고(변화가 있을 때)
            //& newIndex가 backgroundSprites 배열범위 안일때
        {
            currentIndex = newIndex; 
            StartCoroutine(FadeToNextBackground(backgroundSprites[currentIndex]));
            //페이드 아웃 스프라이트 교체 페이드 인
        }
    }
    private IEnumerator FadeToNextBackground(Sprite nextSprite)
    {
        // 페이드 아웃
        for (float t = 0; t < 1f; t += Time.deltaTime) //t는 경과시간비율
        {
            backgroundImage.color = new Color(1, 1, 1, 1 - t);
            yield return null; // 코루틴을 한 프레임 멈춤, 루프는 계속
        }

        backgroundImage.sprite = nextSprite;

        // 페이드 인
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            backgroundImage.color = new Color(1, 1, 1, t);
            yield return null; 
        }
    }
}

