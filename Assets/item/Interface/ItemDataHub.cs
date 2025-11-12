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
    public Vector3 spawnL;
    

    public ItemDataHub(ItemController owner)
    {
        mono = owner;
        sm = owner.StateMachine;
        data = owner.Data;
        transform = owner.transform;
        pd = owner.Prefab;
        spawnL = mono.SpawnL;
    }
}
