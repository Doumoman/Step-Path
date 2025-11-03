using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemController : MonoBehaviour
{
    public ItemStateMachine StateMachine => root;
    ItemStateMachine root = new();
    public Transform item { get; private set; }

    bool _initialized = false;

    void Awake()
    {
        item = GameObject.FindWithTag("item_Something").transform;
    }

    private void Update()
    {
        if (!_initialized) return;
        root.Update();
    }
}
