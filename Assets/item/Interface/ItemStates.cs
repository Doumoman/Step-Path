using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public sealed class ItemIdleState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    public ItemIdleState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
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

//Background, Dragging, Crafting, Destroyed, Placed
public sealed class BackgroundState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    public BackgroundState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
    public void Enter()
    {

    }

    public void Update()
    {
        if (Input.GetMouseButton(0))
        {
            machine.ChangeState(new DraggingState(ctx, machine));
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

    public DraggingState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
    public void Enter()
    {
        // 마우스 위치에 포인터 따라오는 것 on
    }

    public void Update()
    {
        while (Input.GetMouseButton(0))
        {
            //오브젝트는 터치시 위로 올라옴 + 설치 가능 위치, 불가능 위치에 따라 스프라이트로 구분.
            if (Input.GetMouseButtonUp(0))
            {
                machine.ChangeState(DetectPlaced());
                // + 포인터 끄기
                return;
            }
            return;
        }
    }

    public void Exit()
    {

    }

    public IItemState DetectPlaced()
    {
        if (1 < 2) return new PlacedState(ctx, machine); // Placed될 때. 그 이후에 Crafting 판단
        else if(3<4) return new BackgroundState(ctx, machine); // Placed되지 않는 자리라 Background?? 정확히 알아햐함
        
    }
}

public sealed class CraftingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;

    public CraftingState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
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

public sealed class DestroyedState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;

    public DestroyedState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
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

public sealed class PlacedState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;

    public PlacedState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
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