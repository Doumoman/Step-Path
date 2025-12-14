using UnityEngine;

public abstract class ItemLogic : ScriptableObject
{
    public abstract string CraftingCheck(ItemDataHub ctx, ref bool craft);

    public abstract void PlacedItemLogic(ItemDataHub ctx);
}
