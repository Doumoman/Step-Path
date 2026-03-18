using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Stairs Logic")]
public class Stairs_L : ItemLogic
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