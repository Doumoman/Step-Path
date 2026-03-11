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
    [Header("물 배치")]
    public AudioClip waterpour;
    [Header("큰 버섯 생성")]
    public AudioClip mushroomgrow;
    [Header("계단 생성")]
    public AudioClip staircomplete;
    [Header("아이템 배치 실패")]
    public AudioClip tileplacing_fail;


    [System.NonSerialized]
    public AudioSource audio;

    public Action cloudF;
    public Action tileP;
    public Action vineG;
    public Action waterP;
    public Action mushroomG;
    public Action stairC;
    public Action tileP_fail;

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

    public void PlayWaterP()
    {
        waterP?.Invoke();
    }

    public void PlayMushroomG()
    {
        mushroomG?.Invoke();
    }

    public void PlayStairC()
    {
        stairC?.Invoke();
    }

    public void PlayTileP_fail()
    {
        tileP_fail.Invoke();
    }

}
