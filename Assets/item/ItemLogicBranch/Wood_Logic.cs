using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Wood Logic")]
public class Wood_L : ItemLogic
{
    public override string CraftingCheck(ItemDataHub ctx, ref bool craft) 
    {
        if (ctx == null) Debug.LogError("🚨 비상! ctx가 비어있습니다!");
        else if (ctx.map == null) Debug.LogError("🚨 비상! ctx.map(그리드)이 비어있습니다!");
        else if (ctx.data == null) Debug.LogError("🚨 비상! ctx.data(SO)가 비어있습니다!");
        string itemName = ctx.data.itemName;

        if (itemName == "wood") { Debug.Log("계단 생성 전달"); craft = true; return "stairs"; }
        else { craft = false; return null; }
    }


    public override void PlacedItemLogic(ItemDataHub ctx) 
    {
        if (ctx.data.isoriginal == false) return;
        int count = 0;
        Vector3Int pos = ctx.grid.positioncell;
        Vector3Int left = new Vector3Int(pos.x - 1, pos.y - 1, 0);
        Vector3Int right = new Vector3Int(pos.x + 1, pos.y - 1, 0);
        Tilemap gt = ctx.grid.ground;
        bool check = gt.HasTile(left);

        while (!check)
        {  
            if (count > 10) break;
            left.x -= 1;
            check = gt.HasTile(left);
            count++;
        }
        if(count < 10)
        {
            gt.SetTile(left, ctx.grid.gTile);
        }
        check = gt.HasTile(right);
        count = 0;
        while (!check)
        {
            if (count > 10) break;
            right.x += 1;
            check = gt.HasTile(right);
            count++;
        }
        if (count < 10)
        {
            gt.SetTile(right, ctx.grid.gTile);
        }


        ctx.grid.groundLposleft = left;
        ctx.grid.groundLposright = right;
        ctx.data.isoriginal = false;
        ctx.pd.CreateGroundL();

        return; 
    }

}

