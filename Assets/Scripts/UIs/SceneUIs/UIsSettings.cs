using UnityEngine;
using UnityEngine.UI;

public class UIsSettings : MonoBehaviour
{
    [Header("슬라이더")]
    [SerializeField] private Slider BGMSlider;
    [SerializeField] private Slider SFXSlider;

    [Header("버튼")]
    [SerializeField] private Button SettingsCloseButton;

    private void Start()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogError("SoundManager가 씬에 존재하지 않습니다!");
            return;
        }

        // 슬라이더 초기값 설정 (SoundManager에서 가져오기)
        BGMSlider.value = SoundManager.Instance.GetBGMVolume();
        SFXSlider.value = SoundManager.Instance.GetSFXVolume();

        // 슬라이더 변경 시 바로 볼륨 적용
        BGMSlider.onValueChanged.AddListener(OnBGMValueChanged);
        SFXSlider.onValueChanged.AddListener(OnSFXValueChanged);

        // 닫기 버튼 클릭 시
        SettingsCloseButton.onClick.AddListener(CloseSettings);
    }

    // ===== BGM 슬라이더 이벤트 =====
    private void OnBGMValueChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
    }

    // ===== SFX 슬라이더 이벤트 =====
    private void OnSFXValueChanged(float value)
    {
        SoundManager.Instance.SetSFXVolume(value);

        // 슬라이더 체감용 버튼 클릭 효과음
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click);
    }

    // ===== 설정창 닫기 =====
    private void CloseSettings()
    {
        // PlayerPrefs 저장
        PlayerPrefs.SetFloat("BGMVolume", BGMSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", SFXSlider.value);
        PlayerPrefs.Save();
        SoundManager.Instance.EffectSoundOn(SoundManager.SFXType.Button_Click); // 버튼 클릭 효과음
        

        gameObject.SetActive(false);
    }

    // 외부에서 열 때
    public void OpenSettings()
    {
        // 최신 값 불러오기
        BGMSlider.value = SoundManager.Instance.GetBGMVolume();
        SFXSlider.value = SoundManager.Instance.GetSFXVolume();
        gameObject.SetActive(true);
    }
}