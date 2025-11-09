using Unity.VisualScripting;
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

public sealed class DraggingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
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

public sealed class CraftingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
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