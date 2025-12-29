using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEditor.Progress;





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


    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            machine.ChangeState(new DraggingState(ctx, machine, prefabCreate));
            return;
            
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
    bool CraftCheck;
    bool IsPlaceable;
    bool groundcheck;
    ItemDataHub placed_ctx;
    Vector2 originalscale;

    public DraggingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p;}
    public void Enter()
    {
        groundcheck = ctx.image.gameObject.layer >= 26;
        originalscale = ctx.rect.sizeDelta;
    }

    public void Update()
    {
        TrackingMouse(ctx, 2, 2);
        IsitPlaceable(ctx);
        OnPoint(ctx);
        if (Input.GetMouseButtonUp(0)) 
        {
   
            OffPoint(ctx);
            Movectx(ctx);
            if (IsPlaceable && CraftCheck)
            {
                machine.ChangeState(new CraftingState(ctx, machine, prefabCreate, placed_ctx));
            }
            else if (IsPlaceable && !CraftCheck)
            {
                machine.ChangeState(new PlacedState(ctx, machine, prefabCreate));
            }
            else
            {
                if (ctx.image.gameObject.layer == 24) { machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate)); prefabCreate.DeletitemStack(); prefabCreate.Createitemimage(); }
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

        if (groundcheck)
            finalWorldPos.y += 0.15f;
        // Z축은 0으로 고정
        finalWorldPos.z = 0;
            
        // 5. 월드 -> UI 스크린 좌표 변환 및 적용
        Vector3 snappedScreenPos = Camera.main.WorldToScreenPoint(finalWorldPos);
        ctx.rect.position = snappedScreenPos;

        if (groundcheck)
            ResizeImageToGrid(ctx, 2, 1);
        else
            ResizeImageToGrid(ctx, 2, 2);
        return;
    }

    void ResizeImageToGrid(ItemDataHub ctx, int sizeX, int sizeY)
    {
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

        Vector3Int cellPos = ctx.map.WorldToCell(mouseWorldPos); cellPos.y++;
        ctx.image.Grid.positioncell = cellPos;

        Vector3 cellCenterPos = ctx.map.GetCellCenterWorld(cellPos);

        Vector3 cellCenterPosGround = new Vector3(cellCenterPos.x - ctx.map.cellSize.x / 2, cellCenterPos.y - ctx.map.cellSize.y*2, cellCenterPos.z);
        Vector3 cellCenterPositem = ctx.map.CellToWorld(cellPos);



        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.4f;

        Collider2D hitGround = Physics2D.OverlapBox(cellCenterPosGround, boxSize, 0f, LayerMask.GetMask("Ground"));
        Collider2D hitGroundCenter = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, lowerLayerMask);
        Collider2D hititem = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, higherLayerMask);

        ItemController con;
        string name;

        if (hitGroundCenter != null)
        {
            con = hitGroundCenter.GetComponent<ItemController>();
            if (con != null) name = con.Data.itemName;
            else name = null;
        }
        else name = null;

        if (groundcheck)
        {
            if (name == "wood" && ctx.image.gameObject.layer == 27)
            {
                if (hititem == null && hitGroundCenter == null)
                {
                    IsPlaceable = true;
                    CraftCheck = false;
                    return;
                }
                else if (hititem == null && hitGroundCenter != null)
                {
                    IsPlaceable = true;
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
            else
            {
                if (hititem == null && hitGroundCenter == null)
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
        else if (ctx.image.gameObject.layer == 24)
        {
            if (hititem == null && hitGroundCenter == null)
            {
                IsPlaceable = false;
                CraftCheck = false;
                return;
            }
            else if (hititem != null && hitGroundCenter == null)
            {
                IsPlaceable = true;
                CraftCheck = true;
                Debug.Log("조합 가능");
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
                Debug.Log("조합 가능");
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

        Vector3Int cellPos = ctx.map.WorldToCell(mouseWorldPos); cellPos.y++;
        ctx.image.Grid.positioncell = cellPos;

        Vector3 cellCenterPos = ctx.map.GetCellCenterWorld(cellPos);

        Vector3 cellCenterPosGround = new Vector3(cellCenterPos.x - ctx.map.cellSize.x / 2, cellCenterPos.y - ctx.map.cellSize.y * 2, cellCenterPos.z);
        Vector3 cellCenterPositem = ctx.map.CellToWorld(cellPos);



        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.4f;
        Collider2D hititem = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, higherLayerMask);
        Collider2D hitGroundCenter = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, lowerLayerMask);
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
        if (ctx == null || ctx.data == null ||  ctx.data.eachLogic == null) ctx.data.eachLogic.PlacedItemLogic(ctx);

    }

    public void Update()
    {
        if (!ok)
        {
            if (ctx == null || ctx.data == null || ctx.data.eachLogic == null) return;
            else { ctx.data.eachLogic.PlacedItemLogic(ctx); ok = true; }
        }
        
        if (ctx.data.itemName != "cloud")
        {
            ctx.data.eachLogic.PlacedItemLogic(ctx);
        }

    }

    public void Exit()
    {
        if(ctx.data.itemName != "cloud") prefabCreate.Createitemimage();
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        
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
        return;
        
    }

    public void Update()
    {
        if (IsCraftable) // 조합되는 경우. 
        {
            placed_ctx.mono.Grid.craftedPos = placed_ctx.transform.position;
            placed_ctx.mono.Grid.crafteditemName = craftitemName;
            // 원래 placed되어 있던 프리팹 destroy로 전달. 
            placed_ctx.sm.ChangeState(new DestroyedState(placed_ctx, placed_ctx.sm, placed_ctx.pd));
            machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
            
            prefabCreate.OnCrafteditem();// 해당 위치에 규격에 맞춰서 조합된 아이템 프리팹 생성
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


