using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;





//Background, Dragging, Crafting, Destroyed, Placed
public sealed class BackgroundState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    readonly Transform item;
    private Camera mainCamera;
    public BackgroundState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        mainCamera = Camera.main;
        Vector2 screenspawn = mainCamera.ScreenToWorldPoint(ctx.spawnL);
        item.position = screenspawn;

    }

    public void Update()
    {
        Vector2 screenspawn = mainCamera.ScreenToWorldPoint(ctx.spawnL);
        item.position = screenspawn;
        if (Input.GetMouseButtonDown(0))
        {
            machine.ChangeState(new DraggingState(ctx, machine, prefabCreate, item));
            return;
            
        }
    }

    public void Exit()
    {

    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }
}

public sealed class DraggingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    readonly Transform item;
    RaycastHit2D isitPlaceable;
    Vector2 raystart;

    public DraggingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        

    }

    public void Update()
    { 
        OnPoint(ctx);
        TrackingMouse(item);
        raystart = (Vector2)item.position + (Vector2.down * 0.255f);
        Debug.DrawRay(raystart, Vector2.down * 0.375f, new Color(1, 0, 0));
        isitPlaceable = Physics2D.Raycast(raystart, Vector2.down, 0.375f, LayerMask.GetMask("Ground"));
        if (Input.GetMouseButtonUp(0)) 
        {
            OffPoint(ctx);
            machine.ChangeState(DetectPlaced(ctx, machine, prefabCreate, item));
            return;
        }
        
    }

    public void Exit()
    {
        EndTrackingMouse(item);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "item") ctx.IsObjecthere = true;
        else if (!collision)
        {
            ctx.IsPlaceable = false;
        }
        else
        {
            ctx.IsObjecthere = false;
            ctx.IsPlaceable = true;
        }

        
    }

    void TrackingMouse(Transform item) 
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = new Vector2(mousePosition.x - mousePosition.x % 0.25f, mousePosition.y - mousePosition.y % 0.25f); // 위치 규격 맞추는 작업은 프리팹 종류에 따라 SO로 따로 받도록 해야함
        item.position = mousePosition;
        return;
    }

    void EndTrackingMouse(Transform item)
    {
        Vector2 currentPosition = item.position;
        currentPosition = new Vector2(currentPosition.x,currentPosition.y);
        item.position = currentPosition;
        return;
    }

    public IItemState DetectPlaced(ItemDataHub ctx, ItemStateMachine machine, ItemPrepabDelegate prefabCreate, Transform item)
    {
        if (isitPlaceable.collider) return new PlacedState(ctx, machine, prefabCreate, item); // Placed될 때. 그 이후에 Crafting 판단
        else return new BackgroundState(ctx, machine, prefabCreate, item);
        
    }

    //가능 여부에 따른 스프라이트 투명도, 색상 전환.
    void OnPoint(ItemDataHub ctx)
    {
        if(ctx.sr == null) return;
        Color CurrentColor = ctx.sr.color;
        CurrentColor.a = 0.5f;
        if (isitPlaceable.collider && ctx.IsPlaceable) //오브젝트 놓을 수 있음 - 조건 추가해야함. 땅에 겹쳐지지 않도록 해야함.
        {
            CurrentColor = Color.Lerp(CurrentColor, Color.green, 0.5f);
            ctx.sr.color = CurrentColor;
            return;
        }
        else
        {
            CurrentColor = Color.Lerp(CurrentColor, Color.red, 0.5f);
            ctx.sr.color = CurrentColor;
            return;
        }
    }

    void OffPoint(ItemDataHub ctx)
    {
        ctx.sr.color = ctx.originalColor;
    }
    //배치 가능 여부 판단
    
}

public sealed class PlacedState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    readonly Transform item;

    public PlacedState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        prefabCreate.CreateSimpleitem();
    }

    public void Update()
    {
        if (ctx.IsObjecthere) //오브젝트가 존재함 - crafting 판정 필요 - 상태 전환
        {
            //원래 Placed된 아이템을 CraftingState로 이동시킴
            machine.ChangeState(new CraftingState(ctx, machine, prefabCreate));
            return;
        }
        
        //item 로직 실행 - 단일 오브젝트 설치. 로직 계속 실행하도록. 이 로직 안에 
    }

    public void Exit()
    {
        
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        
    }

}
public sealed class CraftingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;

    public CraftingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p; }
    public void Enter()
    {

    }

    public void Update()
    {
        if (ctx.IsCraftable) // 조합되는 경우. 
        {
            // 원래 placed되어 있던 프리팹 destroy로 전달. 
            machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
            prefabCreate.OnCrafteditem();// 해당 위치에 규격에 맞춰서 조합된 아이템 프리팹 생성
            return;
        }
        else // 조합 불가인 경우
        {
            machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
            return;
        }
    }

    public void Exit()
    {

    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        
        
    }
}

public sealed class DestroyedState : IItemState // 잘못 조합된 경우
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;

    public DestroyedState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p; }
    public void Enter()
    {
        machine.PopState();
        return;
    }

    public void Update()
    {

    }

    public void Exit()
    {
        
        //잘못 조합된 경우 / 조합했을 때 가장 최근의 프리팹의 destroy에만 프리팹 생성 전달
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }
}


