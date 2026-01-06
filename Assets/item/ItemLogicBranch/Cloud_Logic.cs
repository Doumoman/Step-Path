using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Logic/New Cloud Logic")]
 
public class Cloud_L : ItemLogic
{
    public override string CraftingCheck(ItemDataHub ctx, ref bool Craft) { return null; }
    public override void PlacedItemLogic(ItemDataHub ctx)
    {
        if (ctx.data.isoriginal == false) return;
        ctx.data.forcloudsoundcheck = true;
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
        check = gt.HasTile(right);
        count = 0;
        while (!check)
        {
            if (count > 10) break;
            right.x += 1;
            check = gt.HasTile(right);
            count++;
        }
        ctx.grid.groundLposleft = left;
        ctx.grid.groundLposright = right;
        ctx.data.isoriginal = false;
        ctx.pd.CreateCloudL();

        return;
    }
}
