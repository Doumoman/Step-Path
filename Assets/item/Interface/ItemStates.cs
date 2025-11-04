using Unity.VisualScripting;
using UnityEngine;

public sealed class ItemIdleState : IItemState
{
    readonly ItemContext ctx;
    readonly ItemStateMachine machine;
    public ItemIdleState(ItemContext c, ItemStateMachine m) { ctx = c; machine = m; }
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

//Background, Dragging, Crafting, Destroyed
public sealed class BackgroundState 
{

}

public sealed class DraggingState 
{

}

public sealed class CraftingState
{ 

}

public sealed class DestroyedState 
{

}