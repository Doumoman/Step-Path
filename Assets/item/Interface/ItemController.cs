using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Tilemaps;

public class ItemController : MonoBehaviour
{
    [SerializeField] ItemData data;
    [SerializeField] Vector3 spawnL;
    [SerializeField] ItemPrepabDelegate prefab;
    [SerializeField] GridData grid;
    public ItemStateMachine StateMachine => root;
    public ItemData Data => data;
    public GridData Grid => grid;
    public ItemPrepabDelegate Prefab => prefab;
    ItemStateMachine root = new();
    public Transform item { get; private set; }

    public Vector3 SpawnL => spawnL;

    bool _initialized = false;

    public ItemDataHub ctx;

    private void Awake()
    {
        item = this.transform;
        ctx = new ItemDataHub(this);
        _initialized = true;
    }

    void Start()
    {
        
        root.PushState(new PlacedState(ctx, root, Prefab));
       
    }

    private void Update()
    {
        if (!_initialized) return;
        root.Update();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        root.OnTriggerEnter2D(collision);
    }
}
