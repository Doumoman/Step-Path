using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Sprout Logic")]
public class Sprout_L : ItemLogic
{
    public override string CraftingCheck(ItemDataHub ctx, ref bool Craft) { return null; }

    public override void PlacedItemLogic(ItemDataHub ctx) { return; }

}
