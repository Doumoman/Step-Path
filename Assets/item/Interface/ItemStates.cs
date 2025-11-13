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
    public BackgroundState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        item.position = ctx.spawnL; // 프리팹 위치 조정.
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            machine.ChangeState(new DraggingState(ctx, machine, prefabCreate, item));
            return;
            
        }
    }

    public void Exit()
    {

    }
}

public sealed class DraggingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    readonly Transform item;

    public DraggingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        // 마우스 위치에 포인터 따라오는 것 on
        TrackingMouse(item);

    }

    public void Update()
    { 
        OnPoint(ctx);
        if (Input.GetMouseButtonUp(0)) //오브젝트는 터치시 터치 위치로 올라오는 것 구현 필요   
        {
            OffPoint(ctx);
            machine.ChangeState(DetectPlaced(ctx));
            return;
        }
    }

    public void Exit()
    {
        //마우스 포인터 따라다니기 off
        EndTrackingMouse(item);
    }

    void TrackingMouse(Transform item) // Mathf로 올림한 수치는 타일맵 규격에 따라 수정해야함.
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = new Vector2(Mathf.Round(mousePosition.x), Mathf.Round(mousePosition.y));
        item.position = mousePosition;
        return;
    }

    void EndTrackingMouse(Transform item)
    {
        Vector2 currentPosition = item.position;
        currentPosition = new Vector2(Mathf.Round(currentPosition.x), Mathf.Round(currentPosition.y));
        item.position = currentPosition;
        return;
    }

    public IItemState DetectPlaced(ItemDataHub ctx)
    {
        if (ctx.IsPlaceable) return new PlacedState(ctx, machine, prefabCreate, item); // Placed될 때. 그 이후에 Crafting 판단
        else return new BackgroundState(ctx, machine, prefabCreate, item);
        
    }

    //가능 여부에 따른 스프라이트 투명도, 색상 전환.
    void OnPoint(ItemDataHub ctx)
    {
        if(ctx.sr == null) return;
        Color CurrentColor = ctx.sr.color;
        CurrentColor.a = 0.5f;
        if (IsitPlaceable(ctx)) //오브젝트 놓을 수 있음
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
    bool IsitPlaceable(ItemDataHub ctx) // 타일맵 받아와서 검사해야함-구현 필요
    {
        return true;
    }
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

    }

    public void Update()
    {
        if (ctx.IsPlaceable && ctx.IsObjecthere) // Place 한 곳이 오브젝트 위인 경우 - Collider 기반? 애초에 포인터로 설치 가능, 불가능이니 
                    // Sprite의 상태에 따라 구분해도 될듯
        {
            //원래 Placed된 아이템을 CraftingState로 이동시킴
            machine.ChangeState(new CraftingState(ctx, machine, prefabCreate));
            return;
        }
        else if (ctx.IsPlaceable && !ctx.IsObjecthere)  // 설치되는 구역인데 오브젝트가 없을 경우
        {
            machine.ChangeState(new BackgroundState(ctx, machine, prefabCreate, item));
            //규격에 맞춰서 오브젝트 설치
            // 프리팹 생성 전달
            return;
        }
        else // 설치 자체가 안될 경우
        {
            machine.ChangeState(new MissPlacedtoDestroy(ctx, machine, prefabCreate));
        }
        

        
        
    }

    public void Exit()
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
            // 해당 위치에 규격에 맞춰서 조합된 아이템 프리팹 생성

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
}

public sealed class MissPlacedtoDestroy : IItemState //설치 미스로 인한 destroy
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    public MissPlacedtoDestroy(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p; }
    public void Enter()
    {

    }

    public void Update()
    {

    }

    public void Exit()
    {

    }
}

