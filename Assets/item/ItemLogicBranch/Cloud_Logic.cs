using UnityEngine;
using System.Threading.Tasks;
[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Cloud Logic")]
 
public class Cloud_L : ItemLogic
{
    public override string CraftingCheck(ItemDataHub ctx, ref bool Craft) { return null; }
    public override async void PlacedItemLogic(ItemDataHub ctx)
    {
        await Task.Delay(5000);
        ctx.sm.ChangeState(new DestroyedState(ctx, ctx.sm, ctx.pd));
    }

    
}
