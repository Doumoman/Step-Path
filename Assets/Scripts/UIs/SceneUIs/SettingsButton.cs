using UnityEngine;
using UnityEngine.UI;

public class SettingsButton : MonoBehaviour
{
    [SerializeField] private Button openButton;          // 설정 열기 버튼
    [SerializeField] private UIsSettings settingsPanel;  // 설정창 스크립트 연결

    void Start()
    {
        openButton.onClick.AddListener(OpenSettings);
    }

    void OpenSettings()
    {
        settingsPanel.OpenSettings(); //  설정창 열기!
    }
}

