using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
#endif
[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Mushroom Logic")]
public class Mushroom_L : ItemLogic
{
    public override string CraftingCheck(ItemDataHub ctx, ref bool Craft) { return null; }

    public override void PlacedItemLogic(ItemDataHub ctx, bool outofcamera)
    {
        if (outofcamera)
            ctx.sm.ChangeState(new DestroyedState(ctx, ctx.sm, ctx.pd));
        else
            return;
    }
}