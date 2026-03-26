using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ImageController : MonoBehaviour
{
    [SerializeField] ItemData data;
    [SerializeField] Vector3 spawnL;
    [SerializeField] ItemPrepabDelegate prefab;
    [SerializeField] GridData grid;
    [SerializeField] itemSound itemS;


    public ItemStateMachine StateMachine => root;
    public ItemData Data => data;
    public GridData Grid => grid;
    public ItemPrepabDelegate Prefab => prefab;
    public itemSound ItemSound => itemS;
    ItemStateMachine root = new();
    public Transform item { get; private set; }

    public Vector3 SpawnL => spawnL;

    bool _initialized = false;

    public ItemDataHub ctx;


    


    void Awake()
    {
        item = this.transform;
        ctx = new ItemDataHub(this);
        root.PushState(new BackgroundState(ctx, root, Prefab));
        _initialized = true;
    }

    private void Start()
    {
        ctx.folder = GameObject.Find("itemDraggingState");
        ctx.itemContainerfolder = GameObject.Find("Firstitem");
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
