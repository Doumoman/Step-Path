using UnityEngine;
using UnityEngine.UI;
public class PauseButton : MonoBehaviour
{
    [SerializeField] private Button OpenPauseButton;    // 일시정지 열기 버튼
    [SerializeField] private UIsPause pausePopup;  // 스크립트 연결

    void Start()
    {
        OpenPauseButton.onClick.AddListener(OpenPause);
    }

    void OpenPause()
    {
        pausePopup.OpenPause(); //  일시정지창 열기!
    }
}

