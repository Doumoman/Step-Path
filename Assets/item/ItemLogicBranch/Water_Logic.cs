using Unity.VisualScripting;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
#endif

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Water Logic")]
public class Water_L : ItemLogic
{
    public override string CraftingCheck(ItemDataHub ctx, ref bool craft)
    {
        if (ctx == null) Debug.LogError("🚨 비상! ctx가 비어있습니다!");
        else if (ctx.map == null) Debug.LogError("🚨 비상! ctx.map(그리드)이 비어있습니다!");
        else if (ctx.data == null) Debug.LogError("🚨 비상! ctx.data(SO)가 비어있습니다!");
        string itemName = ctx.data.itemName;

        if (itemName == "mushroom") 
        {
            craft = true;
            ctx.isound.PlayMushroomG();
            return "bigmushroom"; 
        }
        else if (itemName == "sprout") 
        {
            craft = true;
            ctx.isound.PlayvineG();
            return "vine"; 
        }
        else { craft = false; return null; }
    }

    public override void PlacedItemLogic(ItemDataHub ctx, bool outofcamera)
    {
        return;
    }
}
