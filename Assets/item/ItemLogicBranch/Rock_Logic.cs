using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Rock Logic")]
public class Rock_L : ItemLogic
{
    public override string CraftingCheck(ItemDataHub ctx, ref bool Craft) { return null; }

    public override void PlacedItemLogic(ItemDataHub ctx) { return; }

}
