using UnityEngine;

public class UIGlobalSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject inGameUIPrefab;

    void Awake()
    {
        // 이미 UI가 있으면 또 만들지 않음
        if (FindObjectOfType<UIsSceneInGame>() != null)
            return;

        // UI Prefab 생성
        Instantiate(inGameUIPrefab);
    }
}