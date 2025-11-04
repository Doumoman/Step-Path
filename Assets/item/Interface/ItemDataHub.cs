using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;

public sealed class ItemContext
{
    public readonly ItemController mono;
    public readonly ItemStateMachine sm;
}
