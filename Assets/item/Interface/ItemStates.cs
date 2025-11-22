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
    public BackgroundState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        if (ctx.image != null)
        {
            ctx.rect.anchoredPosition = ctx.spawnL;
        }

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
    bool CraftCheck;

    public DraggingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        

    }

    public void Update()
    { 
        OnPoint(ctx);
        TrackingMouse(ctx);
        if (Input.GetMouseButtonUp(0)) 
        {
            OffPoint(ctx);
            IsitPlaceable(ctx);
            if (ctx.IsPlaceable && CraftCheck)
            {
                machine.ChangeState(new CraftingState(ctx, machine, prefabCreate));
            }
            else if (ctx.IsPlaceable && !CraftCheck)
            {
                machine.ChangeState(new PlacedState(ctx, machine, prefabCreate, item));
            }
            else machine.ChangeState(new BackgroundState(ctx, machine, prefabCreate, item));
                return;
        }
        
    }

    public void Exit()
    {
        
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }

    void TrackingMouse(ItemDataHub ctx) 
    {
        ctx.rect.position = Input.mousePosition;
        return;
    }

    //가능 여부에 따른 스프라이트 투명도, 색상 전환.
    void OnPoint(ItemDataHub ctx)
    {
        if(ctx.im == null) return;
        Color CurrentColor = ctx.im.color;
        CurrentColor.a = 0.5f;
        if (ctx.IsPlaceable) //오브젝트 놓을 수 있음 - 조건 추가해야함. 땅에 겹쳐지지 않도록 해야함.
        {
            CurrentColor = Color.Lerp(CurrentColor, Color.green, 0.5f);
            ctx.im.color = CurrentColor;
            return;
        }
        else
        {
            CurrentColor = Color.Lerp(CurrentColor, Color.red, 0.5f);
            ctx.im.color = CurrentColor;
            return;
        }
    }

    void OffPoint(ItemDataHub ctx)
    {
        ctx.im.color = ctx.originalColor;
    }
    //배치 가능 여부 판단
    public void IsitPlaceable(ItemDataHub ctx)
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -Camera.main.transform.position.z; // 카메라와의 거리 (보통 10)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3Int cellPos = ctx.map.WorldToCell(mouseWorldPos);

        Vector3 cellCenterPos = ctx.map.GetCellCenterWorld(cellPos);


        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.9f;

        Collider2D hitGround = Physics2D.OverlapBox(cellCenterPos, boxSize, 0f, LayerMask.GetMask("Ground"));
        Collider2D hititem = Physics2D.OverlapBox(cellCenterPos, boxSize, 0f, ~0);

        if (hitGround == null && hititem == null)
        {
            ctx.IsPlaceable = true;
            CraftCheck = false;
            return; 
        }
        else if(hitGround == null && hititem != null) 
        {
            ctx.IsPlaceable = true;
            CraftCheck = true;
            return; 
        }
        else
        {
            ctx.IsPlaceable = false;
            CraftCheck = false;
            return;
        }
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


