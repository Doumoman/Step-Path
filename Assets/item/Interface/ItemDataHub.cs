using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor.Analytics;
using UnityEditor.Media;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public sealed class ItemDataHub
{
    public readonly ItemController mono;
    public readonly ImageController image;
    public readonly ItemStateMachine sm;
    public readonly ItemPrepabDelegate pd;    
    public readonly ItemData data;
    public readonly Transform transform;
    public readonly RectTransform rect;
    public readonly SpriteRenderer sr;
    public readonly Grid map;
    public readonly Image im;
    public readonly Color originalColor; //원래 스프라이트 컬러 백업본
    public Vector3 spawnL;
    public bool IsPlaceable; // 설치가능여부
    public bool IsObjecthere; // 설치할 위치의 오브젝트 위치 여부
    public bool IsCraftable; // 조합 가능 여부
    

    public ItemDataHub(ItemController owner)
    {
        mono = owner;
        sm = owner.StateMachine;
        data = owner.Data;
        transform = owner.transform;
        pd = owner.Prefab;
        sr = owner.gameObject.GetComponent<SpriteRenderer>();
        spawnL = owner.SpawnL;
        map = owner.Grid.currentGrid;
        originalColor = sr.color;
    }

    public ItemDataHub(ImageController owner)
    {
        image = owner;
        sm = owner.StateMachine;
        data = owner.Data;
        transform = owner.transform;
        pd = owner.Prefab;
        spawnL = owner.SpawnL;
        rect = owner.GetComponent<RectTransform>();
        map = owner.Grid.currentGrid;
        im = owner.gameObject.GetComponent<Image>();
        originalColor = im.color;

    }

}
