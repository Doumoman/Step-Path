using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemState
{
    void Enter();
    void Update();
    void Exit();

}


public sealed class ItemStateMachine
{
    readonly Stack<IItemState> stack = new();

    public IItemState Current => stack.Count > 0 ? stack.Peek() : null;

    public void ChangeState(IItemState next)
    {
        while (stack.Count > 0)
        {
            var s = stack.Pop();
            s.Exit();
        }
        if (next != null)
        {
            stack.Push(next);
            next.Exit();
        }
    }

    public void PushState(IItemState overlay)
    {
        if (Current != null) Current.Exit();
        stack.Push(overlay);
        overlay.Enter();
    }

    public void PopState()
    {
        if (stack.Count == 0) return;
        var top = stack.Pop();
        top.Exit();
        if (Current != null) Current.Enter();
    }

    public void Update() => Current?.Update();
}
