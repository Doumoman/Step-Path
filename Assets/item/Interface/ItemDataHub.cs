using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Analytics;
using UnityEditor.Media;
using UnityEditor.Rendering;
#endif
public sealed class ItemDataHub
{
    public readonly ItemController mono;
    public readonly ImageController image;
    public readonly ItemStateMachine sm;
    public readonly ItemPrepabDelegate pd;
    public readonly itemSound isound;
    public readonly ItemData data;
    public readonly Transform transform;
    public readonly RectTransform rect;
    public readonly SpriteRenderer sr;
    public readonly Grid map;
    public readonly GridData grid;
    public readonly Image im;
    public readonly Color originalColor; //원래 스프라이트 컬러 백업본
    public Vector3 spawnL;
    public bool IsPlaceable; // 설치가능여부
    public bool IsObjecthere; // 설치할 위치의 오브젝트 위치 여부
    public bool IsCraftable; // 조합 가능 여부
    public bool Onbutton = false;

    

    public ItemDataHub(ItemController owner)
    {
        mono = owner;
        image = null;
        sm = owner.StateMachine;
        data = owner.Data;
        transform = owner.transform;
        pd = owner.Prefab;
        isound = owner.ItemSound;
        sr = owner.gameObject.GetComponentInChildren<SpriteRenderer>();
        spawnL = owner.SpawnL;
        map = owner.Grid.currentGrid;
        grid = owner.Grid;
        originalColor = sr.color;
    }

    public ItemDataHub(ImageController owner)
    {
        image = owner;
        sm = owner.StateMachine;
        data = owner.Data;
        transform = owner.transform;
        pd = owner.Prefab;
        isound = owner.ItemSound;
        spawnL = owner.SpawnL;
        rect = owner.GetComponent<RectTransform>();
        map = owner.Grid.currentGrid;
        grid = owner.Grid;
        im = owner.gameObject.GetComponent<Image>();
        originalColor = im.color;

    }

}
