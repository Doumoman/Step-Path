using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor.Analytics;
using UnityEditor.Media;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;

public sealed class ItemDataHub
{
    public readonly ItemController mono;
    public readonly ItemStateMachine sm;
    public readonly ItemPrepabDelegate pd;    
    public readonly ItemData data;
    public readonly Transform transform;
    public readonly SpriteRenderer sr;
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
        spawnL = mono.SpawnL;
        originalColor = sr.color;
    }

    
}
