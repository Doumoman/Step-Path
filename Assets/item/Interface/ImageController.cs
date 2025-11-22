using UnityEngine;
using UnityEngine.UI;

public class ImageController : MonoBehaviour
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

    
    public RectTransform Rect {  get; private set; }
    public Image imm { get; private set; }


    void Awake()
    {
        item = this.transform;
        Rect = this.GetComponent<RectTransform>();
        ctx = new ItemDataHub(this);
        imm = this.GetComponent<Image>();
        root.PushState(new BackgroundState(ctx, root, Prefab, item));
        _initialized = true;

        
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
