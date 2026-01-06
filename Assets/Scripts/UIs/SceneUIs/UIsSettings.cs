using UnityEngine;
using UnityEngine.UI;

public class UIsSettings : MonoBehaviour
{
    [Header("슬라이더")]
    [SerializeField] private Slider BGMSlider;
    [SerializeField] private Slider SFXSlider;

    [Header("버튼")]
    [SerializeField] private Button SettingsCloseButton;

    private void Awake()
    {
    }

    private void Start()
    {
        // 슬라이더 초기값 설정 (기존 사운드매니저 값 반영)
        BGMSlider.value = GameManager.Sound.GetBGMVolume();
        SFXSlider.value = GameManager.Sound.GetSFXVolume();

        // 슬라이더 변경 시 바로 반영되게 리스너 등록
        BGMSlider.onValueChanged.AddListener(OnBGMValueChanged);
        SFXSlider.onValueChanged.AddListener(OnSFXValueChanged);

        // OK 버튼 눌렀을 때 창 닫기
        SettingsCloseButton.onClick.AddListener(OnClickOK);
    }

    // 배경음 볼륨 조절
    private void OnBGMValueChanged(float value)
    {
        GameManager.Sound.SetBGMVolume(value);
    }

    // 효과음 볼륨 조절
    private void OnSFXValueChanged(float value)
    {
        GameManager.Sound.SetSFXVolume(value);
    }

    // OK 버튼 → 설정 저장 후 닫기
    private void OnClickOK()
    {
        // 사운드 설정을 저장 (PlayerPrefs 사용)
        PlayerPrefs.SetFloat("BGMVolume", BGMSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", SFXSlider.value);
        PlayerPrefs.Save();

        // 설정창 닫기
        gameObject.SetActive(false);
    }

    // 외부에서 설정창을 열 때 호출할 함수
    public void OpenSettings()
    {
        // 저장된 값 불러오기 (최초 1회만 필요)
        BGMSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        SFXSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        gameObject.SetActive(true);
    }
}