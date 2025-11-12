using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemController : MonoBehaviour
{
    [SerializeField] ItemData data;
    [SerializeField] Vector3 spawnL;
    [SerializeField] ItemPrepabDelegate prefab;
    public ItemStateMachine StateMachine => root;
    public ItemData Data => data;
    public ItemPrepabDelegate Prefab => prefab;
    ItemStateMachine root = new();
    public Transform item { get; private set; }

    public Vector3 SpawnL => spawnL;

    bool _initialized = false;

    public ItemDataHub ctx;
    void Awake()
    {
        root.PushState(new BackgroundState(ctx, root, Prefab));
    }

    private void Update()
    {
        if (!_initialized) return;
        root.Update();
    }
}
