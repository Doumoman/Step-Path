using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using static UnityEditor.Progress;
#endif





//Background, Dragging, Crafting, Destroyed, Placed
public sealed class BackgroundState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    
    public BackgroundState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p;}
    public void Enter()
    {
        ctx.rect.anchoredPosition = ctx.spawnL;
        if(ctx.data.itemName == "wood") ctx.rect.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        else ctx.rect.localScale = new Vector3(2.45f, 2.45f, 2.45f);

    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if(ctx.Onbutton == false)
            {
                machine.ChangeState(new DraggingState(ctx, machine, prefabCreate));
                return;
            }
            else return;
            
        }
    }

    public void Exit()
    {

    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }
}

public sealed class DraggingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    readonly Transform item;
    Tilemap gt;
    bool CraftCheck;
    bool IsPlaceable;
    bool groundcheck;
    ItemDataHub placed_ctx;
    Vector2 originalscale;
    Vector3Int currentcellpos;
    int x, y;


    public DraggingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p;}
    public void Enter()
    {
        groundcheck = ctx.image.gameObject.layer >= 26;
        originalscale = ctx.rect.sizeDelta;
        ctx.data.isoriginal = true;
        gt = ctx.grid.ground;
    }

    public void Update()
    {
        if (groundcheck) x = y = 1;
        else x = y = 2;
        TrackingMouse(ctx, x, y);
        IsitPlaceable(ctx);
        OnPoint(ctx);


        if (Input.GetMouseButtonUp(0)) 
        {
   
            OffPoint(ctx);
            Movectx(ctx);

            if (ItemManager.instance.buttonhandler.isHovering)
            {
                ItemManager.instance.Reroll();
                return;
            }

            if (IsPlaceable && CraftCheck)
            {
                ctx.isound.PlaytileP();
                machine.ChangeState(new CraftingState(ctx, machine, prefabCreate, placed_ctx));
            }
            else if (IsPlaceable && !CraftCheck)
            {
                ctx.isound.PlaytileP();
                machine.ChangeState(new PlacedState(ctx, machine, prefabCreate));
            }
            else
            {
                
                if (ctx.image.Data.itemName == "water") 
                { 
                    machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
                    prefabCreate.DeletitemStack();
                    prefabCreate.Createitemimage(); 
                }
                
                ctx.isound.PlayTileP_fail();
                machine.ChangeState(new BackgroundState(ctx, machine, prefabCreate));
            }    
            return;
        }
        
    }

    public void Exit()
    {
        
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }

    void TrackingMouse(ItemDataHub ctx, int width, int height) 
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3Int cellPos = ctx.map.WorldToCell(mouseWorldPos);
        // 3. 짝수/홀수 여부 확인
        bool isEvenWidth = (width % 2 == 0);   
        bool isEvenHeight = (height % 2 == 0); 

        // 4. 최종 월드 좌표 계산 변수
        Vector3 finalWorldPos;

        // [X축 계산] 짝수면 '선(CellToWorld)', 홀수면 '중앙(GetCellCenterWorld)'
        if (isEvenWidth)
            finalWorldPos.x = ctx.map.CellToWorld(cellPos).x;
        else
            finalWorldPos.x = ctx.map.GetCellCenterWorld(cellPos).x;

        // [Y축 계산] 짝수면 '선(CellToWorld)', 홀수면 '중앙(GetCellCenterWorld)'
        if (isEvenHeight)
            finalWorldPos.y = ctx.map.CellToWorld(cellPos).y;
        else
            finalWorldPos.y = ctx.map.GetCellCenterWorld(cellPos).y;

        
        // Z축은 0으로 고정
        finalWorldPos.z = 0;

        if (groundcheck) finalWorldPos.y -= 0.125f;

        // 5. 월드 -> UI 스크린 좌표 변환 및 적용
        Vector3 snappedScreenPos = Camera.main.WorldToScreenPoint(finalWorldPos);


        
        ctx.rect.position = snappedScreenPos;

        if (groundcheck)
            ResizeImageToGrid(ctx, 1, 1);
        else
            ResizeImageToGrid(ctx, 2, 2);
        return;
    }

    void ResizeImageToGrid(ItemDataHub ctx, int sizeX, int sizeY)
    {
        ctx.rect.localScale = new Vector3(1f, 1f, 1f);
        Canvas canvas = ctx.rect.GetComponentInParent<Canvas>();
        Vector3 cellSize = ctx.map.cellSize;
        float targetWorldWidth = cellSize.x * sizeX;
        float targetWorldHeight = cellSize.y * sizeY;

        Vector3 worldBasePos = ctx.rect.position;

        // 가로/세로 끝점 (월드 좌표)
        Vector3 worldRightPos = worldBasePos + new Vector3(targetWorldWidth, 0, 0);
        Vector3 worldUpPos = worldBasePos + new Vector3(0, targetWorldHeight, 0);

        // 화면 좌표로 변환
        Vector3 screenBasePos = Camera.main.WorldToScreenPoint(worldBasePos);
        Vector3 screenRightPos = Camera.main.WorldToScreenPoint(worldRightPos);
        Vector3 screenUpPos = Camera.main.WorldToScreenPoint(worldUpPos);

        // 4. 픽셀 거리(크기) 계산
        float pixelWidth = Vector3.Distance(screenBasePos, screenRightPos);
        float pixelHeight = Vector3.Distance(screenBasePos, screenUpPos);

        float finalWidth = pixelWidth / canvas.scaleFactor;
        float finalHeight = pixelHeight / canvas.scaleFactor;
        // 5. UI 이미지(RectTransform)에 크기 적용

        ctx.rect.sizeDelta = new Vector2(finalWidth, finalHeight);
    }

    //가능 여부에 따른 스프라이트 투명도, 색상 전환.
    void OnPoint(ItemDataHub ctx)
    {
        if(ctx.im == null) return;
        Color CurrentColor = ctx.im.color;
        CurrentColor.a = 0.5f;
        if (IsPlaceable) 
        {
            CurrentColor = Color.Lerp(CurrentColor, Color.green, 0.5f);
            ctx.im.color = CurrentColor;
            return;
        }
        else
        {
            CurrentColor = Color.Lerp(CurrentColor, Color.red, 0.5f);
            ctx.im.color = CurrentColor;
            return;
        }
    }

    void OffPoint(ItemDataHub ctx)
    {
        ctx.im.color = ctx.originalColor;
        ctx.rect.sizeDelta = originalscale;
    }
    //배치 가능 여부 판단
    public void IsitPlaceable(ItemDataHub ctx)
    {
        int targetLayerIndex = LayerMask.NameToLayer("item");
        int higherLayerMask = ~0 << (targetLayerIndex + 1);
        int lowerLayerMask = (1 << targetLayerIndex) - 1;
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -Camera.main.transform.position.z; // 카메라와의 거리 (보통 10)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3Int cellPos = ctx.map.WorldToCell(mouseWorldPos); 
        Vector3 cellCenterPos;
        Vector3 cellCenterPosGround;
        Vector3 cellCenterPositem;
        currentcellpos = cellPos;

        if(groundcheck)
        {

            cellCenterPositem = ctx.map.GetCellCenterWorld(cellPos);
            cellCenterPosGround = new Vector3(cellCenterPositem.x, cellCenterPositem.y - ctx.map.cellSize.y, cellCenterPositem.z);
            
        }
        else
        {
            cellPos.y++;
            ctx.image.Grid.positioncell = cellPos;

            cellCenterPos = ctx.map.GetCellCenterWorld(cellPos);

            cellCenterPosGround = new Vector3(cellCenterPos.x - ctx.map.cellSize.x / 2, cellCenterPos.y - ctx.map.cellSize.y * 2, cellCenterPos.z);
            cellCenterPositem = ctx.map.CellToWorld(cellPos);
        }


        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.4f;

        Collider2D hitGround = Physics2D.OverlapBox(cellCenterPosGround, boxSize, 0f, LayerMask.GetMask("Ground"));
        Collider2D hitGroundCenter = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, lowerLayerMask);
        Collider2D hititem = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, higherLayerMask);

        

        ItemController con;
        ItemController con1;
        string name;
        string hititemName;

        if (hitGroundCenter != null)
        {
            con = hitGroundCenter.GetComponent<ItemController>();
            if (con != null) name = con.Data.itemName;
            else name = null;
        }
        else name = null;

        if (hititem != null)
        {
            con1 = hititem.GetComponent<ItemController>();
            if (con1 != null) hititemName = con1.Data.itemName;
            else hititemName = null;
        }
        else hititemName = null;

        if (groundcheck)
        {
            if (name == "wood" && ctx.image.Data.itemName == "wood") // 기본 땅의 경우
            {
                if (hititem == null && hitGroundCenter == null && cellPos.y % 4 == 0)
                {
                    IsPlaceable = true;
                    CraftCheck = false;
                    return;
                }
                else if (hititem == null && hitGroundCenter != null && cellPos.y % 4 == 0)
                {
                    
                    StairsCheck();
                    return;
                }
                else
                {
                    IsPlaceable = false;
                    CraftCheck = false;
                    return;
                }
            }
            else
            {
                if (hititem == null && hitGroundCenter == null && cellPos.y % 4 == 0)
                {
                    IsPlaceable = true;
                    CraftCheck = false;
                    return;
                }
                else
                {

                    IsPlaceable = false;
                    CraftCheck = false;
                    return;
                }
            }
        }
        else if (ctx.image.Data.itemName == "water") // 물
        {
            if (hititem == null && hitGroundCenter == null)
            {
                IsPlaceable = false;
                CraftCheck = false;
                return;
            }
            else if (hititem != null && hitGroundCenter == null)
            {
                if(hititemName == "mushroom" || hititemName == "sprout")
                {
                    IsPlaceable = true;
                    CraftCheck = true;
                }
                else
                {
                    IsPlaceable = false;
                    CraftCheck = false;
                }
                    return;
            }
            else
            {
                IsPlaceable = false;
                CraftCheck = false;
                return;
            }
        }
        else
        {
            if (hitGround != null && hititem == null && hitGroundCenter == null)
            {
                ItemController c = hitGround.GetComponent<ItemController>();

                if (c == null)
                {
                    IsPlaceable = true;
                    CraftCheck = false;
                    return;
                }


                if (c.ctx.data.itemName == "cloud")
                {
                    IsPlaceable = false;
                    CraftCheck = false;
                    return;
                }

                IsPlaceable = true;
                CraftCheck = false;
                return;
            }
            else if (hitGround != null && hititem != null && hitGroundCenter == null) // 2개에 동시에 겹칠 경우에 생각해봐야할듯
            {
                IsPlaceable = false;
                CraftCheck = true;
                return;
            }
            else
            {
                IsPlaceable = false;
                CraftCheck = false;
                return;
            }
        }

        

    }

    void Movectx(ItemDataHub ctx)
    {
        int targetLayerIndex = LayerMask.NameToLayer("item");
        int higherLayerMask = ~0 << (targetLayerIndex + 1);
        int lowerLayerMask = (1 << targetLayerIndex) - 1;
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -Camera.main.transform.position.z; // 카메라와의 거리 (보통 10)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3Int cellPos = ctx.map.WorldToCell(mouseWorldPos);
        Vector3 cellCenterPos;
        Vector3 cellCenterPositem;

        if (groundcheck)
        {
            cellPos.y++;
            ctx.image.Grid.positioncell = cellPos;
            cellPos.y--;
            cellCenterPositem = ctx.map.GetCellCenterWorld(cellPos);
        }
        else
        {
            cellPos.y++;
            ctx.image.Grid.positioncell = cellPos;

            cellCenterPos = ctx.map.GetCellCenterWorld(cellPos);
            cellCenterPositem = ctx.map.CellToWorld(cellPos);
        }


        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.4f;

        Collider2D hitGroundCenter = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, lowerLayerMask);
        Collider2D hititem = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, higherLayerMask);

        if (hititem != null)
        {
            ItemController c = hititem.GetComponent<ItemController>();
            placed_ctx = c.ctx;
        }
        else if(hitGroundCenter != null)
        {
            ItemController c = hitGroundCenter.GetComponent<ItemController>();
            if(c != null)
            {
                placed_ctx = c.ctx;
                Debug.Log("우드 ctx 전달 완");
                return;
            }
            else placed_ctx = null;
        }
        else placed_ctx = null;
    }

    void StairsCheck()
    {
        int layerMask = LayerMask.GetMask("ground");
        Vector3Int pos = currentcellpos;
        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.4f;
        Vector3Int checkleft = new Vector3Int(pos.x - 3, pos.y + 4, 0);
        Vector3Int checkleft1 = new Vector3Int(pos.x - 4, pos.y + 4, 0);

        Vector3Int checkright = new Vector3Int(pos.x + 3, pos.y + 4, 0);
        Vector3Int checkright1 = new Vector3Int(pos.x + 4, pos.y + 4, 0);

        Vector3 cellleftpos  = ctx.map.GetCellCenterWorld(checkleft); //cellleftpos.x += 2.225f; cellleftpos.y += 3.035f;
        Vector3 cellrightpos = ctx.map.GetCellCenterWorld(checkright); //cellrightpos.x += 2.225f; cellrightpos.y += 3.035f;
        Collider2D hititemleft = Physics2D.OverlapBox(cellleftpos, boxSize, 0f, layerMask);
        Collider2D hititemright = Physics2D.OverlapBox(cellrightpos, boxSize, 0f, layerMask);

        string leftname = null;
        string rightname = null;
        if (hititemleft != null)
        {
           ItemController con = hititemleft.GetComponent<ItemController>();
            leftname = con.ctx.data.name;
        }
        if (hititemright != null)
        {
            ItemController con = hititemright.GetComponent<ItemController>();
            rightname = con.ctx.data.name;
        }


        //Debug.Log($"검사 위치 left = ({cellleftpos.x},{cellleftpos.y}), right = ({cellrightpos.x},{cellrightpos.y})");
        CraftCheck = false;
        IsPlaceable = false;
        if (gt.HasTile(checkleft) || gt.HasTile(checkleft1) || leftname == "wood")
        {
            ctx.grid.stairsLeftcheck = true;
            CraftCheck = true;
            IsPlaceable = true;
        }
        if (gt.HasTile(checkright) || gt.HasTile(checkright1) || rightname == "wood")
        {
            ctx.grid.stairsRightcheck = true;
            CraftCheck = true;
            IsPlaceable = true;
        }

        return;
    }
    
}

public sealed class PlacedState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    bool ok = false;


    public PlacedState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p; }
    public void Enter()
    {
        if (ctx.image != null)
        {
            ctx.im.enabled = false;
            prefabCreate.CreateSimpleitem();
            machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
        }
    }

    public void Update()
    {
        if (!ok)
        {
            if (ctx == null || ctx.data == null || ctx.data.eachLogic == null) return;
            else 
            { 
                ctx.data.eachLogic.PlacedItemLogic(ctx);
                ok = true;
                if (ctx.data.itemName == "cloud") ctx.mono.StartCoroutine(DestroyCloud(ctx));
            }
        }
        
        if (ctx.data.itemName != "cloud" && ctx.data.itemName != "wood")
        {
            ctx.data.eachLogic.PlacedItemLogic(ctx);
        }

    }

    public void Exit()
    {
        if (ctx.image != null)
        {
            prefabCreate.Createitemimage();
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        
    }

    public IEnumerator DestroyCloud(ItemDataHub ctx)
    {
        yield return new WaitForSeconds(5f);
        if (ctx.data.forcloudsoundcheck)
        {
            ctx.isound.PlaycloudF();
            ctx.data.forcloudsoundcheck = false;
        }
        
        ctx.sm.ChangeState(new DestroyedState(ctx, ctx.sm, ctx.pd));
        
    }

}
public sealed class CraftingState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    ItemDataHub placed_ctx;
    bool IsCraftable;
    string itemName;
    string craftitemName;

    public CraftingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, ItemDataHub item) { ctx = c; machine = m; prefabCreate = p; placed_ctx = item; }
    public void Enter()
    {

        itemName = ctx.data.itemName;
        if(itemName != "water" && itemName != "wood") { machine.ChangeState(new BackgroundState(ctx, machine, prefabCreate)); return; }
        craftitemName = ctx.data.eachLogic.CraftingCheck(placed_ctx, ref IsCraftable);
        

        if (IsCraftable) // 조합되는 경우. 
        {
            placed_ctx.mono.Grid.craftedPos = placed_ctx.transform.position;
            placed_ctx.mono.Grid.crafteditemName = craftitemName;
            // 원래 placed되어 있던 프리팹 destroy로 전달. 
            if (ctx.data.itemName == "wood")
            {
                machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
                prefabCreate.CreateCrafteditem();// 해당 위치에 규격에 맞춰서 조합된 아이템 프리팹 생성
                prefabCreate.Createitemimage();
            }
            else
            {
                placed_ctx.sm.ChangeState(new DestroyedState(placed_ctx, placed_ctx.sm, placed_ctx.pd));
                machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
                prefabCreate.CreateCrafteditem();// 해당 위치에 규격에 맞춰서 조합된 아이템 프리팹 생성
                prefabCreate.Createitemimage();
            }


            
            return;
        }
        else // 조합 불가인 경우 
        {
            machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
            prefabCreate.DeletitemStack();
            prefabCreate.Createitemimage();
            return;
        }

    }

    public void Update()
    {
        
    }

    public void Exit()
    {

    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        
        
    }

    
}

public sealed class DestroyedState : IItemState 
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;

    public DestroyedState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p; }
    public void Enter()
    {
        machine.PopState();
        return;
    }

    public void Update()
    {

    }

    public void Exit()
    {
        if(ctx.image != null) Object.Destroy(ctx.image.gameObject);
        if(ctx.mono != null) Object.Destroy(ctx.mono.gameObject);
        //잘못 조합된 경우 / 조합했을 때 가장 최근의 프리팹의 destroy에만 프리팹 생성 전달
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }
}


