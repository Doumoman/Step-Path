using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSound", menuName = "itemSound/itemSoundSO")]
public class itemSound : ScriptableObject
{
    [Header("구름 소멸")]
    public AudioClip cloudfading;
    [Header("아이템 배치")]
    public AudioClip tileplacing;
    [Header("덩굴 생성")]
    public AudioClip vinegrowing;

    [System.NonSerialized]
    public AudioSource audio;

    public Action cloudF;
    public Action tileP;
    public Action vineG;

    public void PlaycloudF()
    {
        cloudF?.Invoke();
    }

    public void PlaytileP()
    {
        tileP?.Invoke();
    }

    public void PlayvineG()
    {
        vineG?.Invoke();
    }
}
