using UnityEngine;

// 개발자 전용 아이템 사운드 음량 보정 테이블.
// 게임 내 슬라이더가 아니라, 에디터 인스펙터에서만 조정하는 설정 파일이다.
// 플레이어 마스터 볼륨(currentSFXVolume)과 곱해져 최종 음량이 결정된다.
[CreateAssetMenu(fileName = "ItemSoundConfig", menuName = "Sound/Item Sound Config")]
public class ItemSoundConfig : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        [Tooltip("Sound/item 폴더의 클립 파일명과 동일하게 입력 (예: Tile_Place)")]
        public string clipName;

        [Range(0f, 1f)]
        [Tooltip("개발자 보정 음량. 1 = 원본, 0.6 = 60%")]
        public float volume;
    }

    [Tooltip("조정할 사운드만 등록하면 된다. 목록에 없는 클립은 1.0(원본)으로 재생된다.")]
    public Entry[] entries;

    // clipName에 해당하는 보정 음량을 반환. 등록되지 않은 클립은 1f(원본).
    public float GetVolume(string clipName)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].clipName == clipName)
                    return entries[i].volume;
            }
        }
        return 1f;
    }
}
