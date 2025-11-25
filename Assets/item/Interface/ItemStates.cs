using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;





//Background, Dragging, Crafting, Destroyed, Placed
public sealed class BackgroundState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    readonly Transform item;
    public BackgroundState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        if (ctx.image != null)
        {
            ctx.rect.anchoredPosition = ctx.spawnL;
        }

    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            machine.ChangeState(new DraggingState(ctx, machine, prefabCreate, item));
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

    public DraggingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        

    }

    public void Update()
    {
        TrackingMouse(ctx, 2, 2);
        OnPoint(ctx);
        IsitPlaceable(ctx);
        if (Input.GetMouseButtonUp(0)) 
        {
   
            OffPoint(ctx);
            
            if (IsPlaceable && CraftCheck)
            {
                machine.ChangeState(new CraftingState(ctx, machine, prefabCreate));
            }
            else if (IsPlaceable && !CraftCheck)
            {
                machine.ChangeState(new PlacedState(ctx, machine, prefabCreate, item));
            }
            else machine.ChangeState(new BackgroundState(ctx, machine, prefabCreate, item));
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

        // 5. 월드 -> UI 스크린 좌표 변환 및 적용
        Vector3 snappedScreenPos = Camera.main.WorldToScreenPoint(finalWorldPos);
        ctx.rect.position = snappedScreenPos;

        ResizeImageToGrid(ctx, 2, 2);
        return;
    }

    void ResizeImageToGrid(ItemDataHub ctx, int sizeX, int sizeY)
    {
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

        // 5. UI 이미지(RectTransform)에 크기 적용
        ctx.rect.sizeDelta = new Vector2(pixelWidth, pixelHeight);
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

    void OffPoint( ItemDataHub ctx)
    {
        ctx.im.color = ctx.originalColor;
    }
    //배치 가능 여부 판단
    public void IsitPlaceable(ItemDataHub ctx)
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -Camera.main.transform.position.z; // 카메라와의 거리 (보통 10)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3Int cellPos = ctx.map.WorldToCell(mouseWorldPos);

        Vector3 cellCenterPos = ctx.map.GetCellCenterWorld(cellPos);

        Vector3 cellCenterPosGround = new Vector3(cellCenterPos.x - ctx.map.cellSize.x / 2, cellCenterPos.y - ctx.map.cellSize.y*2, cellCenterPos.z);
        Vector3 cellCenterPositem = ctx.map.CellToWorld(cellPos);



        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.6f;

        Collider2D hitGround = Physics2D.OverlapBox(cellCenterPosGround, boxSize, 0f, LayerMask.GetMask("Ground"));
        Collider2D hitGroundCenter = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, LayerMask.GetMask("item"));
        Collider2D hititem = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, LayerMask.GetMask("item"));

        if (hitGround != null && hititem == null && hitGroundCenter == null)
        {
            IsPlaceable = true;
            CraftCheck = false;
            return; 
        }
        else if(hitGround != null && hititem != null && hitGroundCenter == null) 
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
    
}

public sealed class PlacedState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    readonly Transform item;

    public PlacedState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p, Transform i) { ctx = c; machine = m; prefabCreate = p; item = i; }
    public void Enter()
    {
        prefabCreate.CreateSimpleitem();
    }

    public void Update()
    {
        if (ctx.IsObjecthere) //오브젝트가 존재함 - crafting 판정 필요 - 상태 전환
        {
            //원래 Placed된 아이템을 CraftingState로 이동시킴
            machine.ChangeState(new CraftingState(ctx, machine, prefabCreate));
            return;
        }
        
        //item 로직 실행 - 단일 오브젝트 설치. 로직 계속 실행하도록. 이 로직 안에 
    }

    public void Exit()
    {
        
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

    public CraftingState(ItemDataHub c, ItemStateMachine m, ItemPrepabDelegate p) { ctx = c; machine = m; prefabCreate = p; }
    public void Enter()
    {

    }

    public void Update()
    {
        if (ctx.IsCraftable) // 조합되는 경우. 
        {
            // 원래 placed되어 있던 프리팹 destroy로 전달. 
            machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
            prefabCreate.OnCrafteditem();// 해당 위치에 규격에 맞춰서 조합된 아이템 프리팹 생성
            return;
        }
        else // 조합 불가인 경우
        {
            machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
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

public sealed class DestroyedState : IItemState // 잘못 조합된 경우
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
        
        //잘못 조합된 경우 / 조합했을 때 가장 최근의 프리팹의 destroy에만 프리팹 생성 전달
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {

    }
}


