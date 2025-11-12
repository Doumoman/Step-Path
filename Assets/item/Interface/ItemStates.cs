using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;


//Background, Dragging, Crafting, Destroyed, Placed
public sealed class BackgroundState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    public BackgroundState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
    public void Enter()
    {
        // 프리팹 위치 조정.
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
            //오브젝트는 터치시 터치 위치로 올라옴 + 설치 가능 위치, 불가능 위치에 따라 스프라이트로 구분.
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
         // Placed되지 않는 자리라 Background?? 정확히 알아햐함
        
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
        if (1 < 2) // Place 한 곳이 오브젝트 위인 경우 - Collider 기반? 애초에 포인터로 설치 가능, 불가능이니 
                    // Sprite의 상태에 따라 구분해도 될듯
        {
            //원래 Placed된 아이템을 CraftingState로 이동시킴
            machine.ChangeState(new CraftingState(ctx, machine));
        }
        else if (3 < 4)  // 설치되지 않는 구역일 경우
        {
            machine.ChangeState(new BackgroundState(ctx, machine));//타일맵 규격에 맞춰서 설치되는 로직
        }
        else if (5 < 6) // 설치되는 구역일 경우 - 이것도 스프라이트로 판정 가능? || 조합 성공 후 Place
        {
            //규격에 맞춰서 오브젝트 설치
            // 프리팹 생성 전달
            
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

    public CraftingState(ItemDataHub c, ItemStateMachine m) { ctx = c; machine = m; }
    public void Enter()
    {

    }

    public void Update()
    {
        if (1 < 2) // 조합되는 경우. 
        {
            // 원래 placed되어 있던 프리팹 destroy로 전달. 
            machine.ChangeState(new DestroyedState(ctx, machine));
            // 해당 위치에 규격에 맞춰서 조합된 아이템 프리팹 생성
        }
        else if (3 < 4) // 조합 불가인 경우
        {
            machine.ChangeState(new DestroyedState(ctx, machine));
        }
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
        machine.PopState();
    }

    public void Update()
    {

    }

    public void Exit()
    {
        //원래 Placed되어있던 아이템에서 Destroy된 거면 새로운 프리팹 생성X
        //잘못 조합된 경우 / 조합했을 때 가장 최근의 프리팹의 destroy에만 프리팹 생성 전달
    }
}

