using NUnit.Framework.Constraints;
using System.Collections;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

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
        ctx.rect.localScale = new Vector3(2.45f, 2.45f, 2.45f);

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
        gt = ctx.grid.ground;
        
    }

    public void Update()
    {
        if (ctx.grid.itemInputLocked)
            return;

        if (groundcheck)
        {
            x = 2; y = 1;
        }
        else x = y = 2;

        ctx.image.transform.SetParent(ctx.folder.transform, false);
        TrackingMouse(ctx, x, y);
        IsitPlaceable(ctx);
        OnPoint(ctx);
        


        if (Input.GetMouseButtonUp(0)) 
        {
   
            OffPoint(ctx);

            if (TryHandleDropButton(ctx))
                return;

            Movectx(ctx);


            if (IsPlaceable && CraftCheck)
            {
                SoundManager.Instance.PlayItemSound("Tile_Place");
                machine.ChangeState(new CraftingState(ctx, machine, prefabCreate, placed_ctx));
            }
            else if (IsPlaceable && !CraftCheck)
            {
                SoundManager.Instance.PlayItemSound("Tile_Place");
                if (groundcheck) ctx.data.isoriginal = true;
                machine.ChangeState(new PlacedState(ctx, machine, prefabCreate));
            }
            else
            {
                
                if (ctx.image.Data.itemName == "water") 
                { 
                    machine.ChangeState(new DestroyedState(ctx, machine, prefabCreate));
                    prefabCreate.DeletitemStack();
                    prefabCreate.Createitemimage();
                    SoundManager.Instance.PlayItemSound("Water_Pour");
                }
                else
                {
                    SoundManager.Instance.PlayItemSound("Tile_Place_Fail");
                    machine.ChangeState(new BackgroundState(ctx, machine, prefabCreate));
                    ctx.image.transform.SetParent(ctx.itemContainerfolder.transform, false);
                }
                    
            }    
            return;
        }
        bool TryHandleDropButton(ItemDataHub ctx)
        {
            if (ctx.grid.dropButtonHandlers == null) return false;

            foreach (ButtonHandler handler in ctx.grid.dropButtonHandlers)
            {
                if (handler == null) continue;
                if (!handler.IsMouseOver()) continue;

                switch (handler.buttonType)
                {
                    case DragDropButtonType.Reroll:
                        prefabCreate.Rerolltheitem();
                        return true;

                    case DragDropButtonType.Pause:
                        // 아이템은 소비하지 않고 원래 슬롯으로 복귀
                        machine.ChangeState(new BackgroundState(ctx, machine, prefabCreate));
                        ctx.image.transform.SetParent(ctx.itemContainerfolder.transform, false);

                        handler.InvokeDropAction();
                        return true;
                }
            }

            return false;
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
        {
            ResizeImageToGrid(ctx, 2, 1);
        }
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
        int n = 28;
        int targetLayerIndex = LayerMask.NameToLayer("item");
        int higherLayerMask = ~0 << (targetLayerIndex + 1);
        int lowerLayerMask = (1 << targetLayerIndex) - 1;

        int lowerThanN = (1 << n) - 1;
        int forGLayerMask = higherLayerMask & lowerThanN;

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
        Vector3 cellCenterPositemG = cellCenterPositem;
        cellCenterPositemG.x -= 0.125f;
        Collider2D hitGround = Physics2D.OverlapBox(cellCenterPosGround, boxSize, 0f, LayerMask.GetMask("Ground"));
        Collider2D hitGroundCenter = Physics2D.OverlapBox(cellCenterPositemG, boxSize, 0f, lowerLayerMask);
        Collider2D forGgroundCenter = Physics2D.OverlapBox(cellCenterPositem, boxSize, 0f, forGLayerMask);
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

        if (forGgroundCenter != null)
        {
            con1 = forGgroundCenter.GetComponent<ItemController>();
            if (con1 != null) name = con1.Data.itemName;
            else hititemName = null;
        }
        else hititemName = null;

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
                if (forGgroundCenter == null && hitGroundCenter == null && cellPos.y % 4 == 0)
                {
                    IsPlaceable = true;
                    CraftCheck = false;
                    return;
                }
                else if (forGgroundCenter == null && hitGroundCenter != null && cellPos.y % 4 == 0)
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
            else // 구름
            {
                if (forGgroundCenter == null && hitGroundCenter == null && cellPos.y % 4 == 0)
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
                if(hititemName == "mushroom")
                {
                    IsPlaceable = true;
                    CraftCheck = true;
                }
                else if(hititemName == "sprout")
                {
                    CraftCheck = true;
                    IsPlaceable = true;
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
            if (cellPos.x % 2 == 0) 
                cellPos.x++;
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
        int layerMask = LayerMask.GetMask("Ground");
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
            if(con != null) leftname = con.ctx.data.name;
        }
        if (hititemright != null)
        {
            ItemController con = hititemright.GetComponent<ItemController>();
            if (con != null) rightname = con.ctx.data.name;
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
    
    /* Vine 위쪽 땅 존재여부에 따른 생성 조건 설정 함수
    void VineCheck(Collider2D hititme)
    {
        Vector3Int pos = ctx.map.WorldToCell(hititme.transform.position);
        Vector3Int checkup1 = new Vector3Int(pos.x, pos.y + 3, 0);
        Vector3Int checkup2 = new Vector3Int(pos.x - 1, pos.y + 3, 0);

        int layerMask = LayerMask.GetMask("Ground");
        Vector2 boxSize = (Vector2)ctx.map.cellSize * 0.4f;
        Vector3 overlapPos1 = ctx.map.GetCellCenterWorld(checkup1); 
        Vector3 overlapPos2 = ctx.map.GetCellCenterWorld(checkup2); 
        Collider2D hitground1 = Physics2D.OverlapBox(overlapPos1, boxSize, 0f, layerMask);
        Collider2D hitground2 = Physics2D.OverlapBox(overlapPos2, boxSize, 0f, layerMask);

        CraftCheck = false;
        IsPlaceable = false;
        if (gt.HasTile(checkup1) || gt.HasTile(checkup2) || hitground1 != null || hitground2 != null)
        {
            CraftCheck = true;
            IsPlaceable = true;
        }
        return;
    }
    */
}

public sealed class PlacedState : IItemState
{
    readonly ItemDataHub ctx;
    readonly ItemStateMachine machine;
    readonly ItemPrepabDelegate prefabCreate;
    bool ok = false;
    bool outofcamera = false;


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
                ctx.data.eachLogic.PlacedItemLogic(ctx, outofcamera);
                ok = true;
                if (ctx.data.itemName == "cloud") ctx.mono.StartCoroutine(DestroyCloud(ctx));
            }
        }
        
        if (ctx.data.itemName != "cloud" )
        {
            outofcamera = OutOfCamera(ctx);
            ctx.data.eachLogic.PlacedItemLogic(ctx, outofcamera);
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
        float lifeTime = 15f;
        float fadeTime = 5f;
        float normalTime = lifeTime - fadeTime;

        // 10초 동안은 그대로 유지
        yield return new WaitForSeconds(normalTime);

        SpriteRenderer sr = ctx.sr;

        // SpriteRenderer가 없으면 기존처럼 바로 남은 시간 기다렸다가 삭제
        if (sr == null)
        {
            yield return new WaitForSeconds(fadeTime);

            if (ctx.data.forcloudsoundcheck)
            {
                SoundManager.Instance.PlayItemSound("Cloud_Fade");
                ctx.data.forcloudsoundcheck = false;
            }

            ctx.sm.ChangeState(new DestroyedState(ctx, ctx.sm, ctx.pd));
            yield break;
        }

        Color baseColor = sr.color;
        float timer = 0f;

        // 점멸 속도. 값이 클수록 빠르게 깜빡임
        float blinkSpeed = 12f;

        bool soundPlayed = false;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeTime);

            // 전체적으로 서서히 투명해지는 알파
            float fadeAlpha = Mathf.Lerp(baseColor.a, 0f, t);

            // 깜빡임 강도
            // 0.35 ~ 1 사이로 흔들리게 해서 완전히 사라졌다 나타나는 느낌은 줄임
            float blink = Mathf.Lerp(0.35f, 1f, Mathf.PingPong(timer * blinkSpeed, 1f));

            Color c = baseColor;
            c.a = fadeAlpha * blink;
            sr.color = c;

            yield return null;
        }

        // 마지막엔 완전히 투명하게
        Color finalColor = baseColor;
        finalColor.a = 0f;
        sr.color = finalColor;
        SoundManager.Instance.PlayItemSound("Cloud_Fade");
        ctx.sm.ChangeState(new DestroyedState(ctx, ctx.sm, ctx.pd));
    }

    bool OutOfCamera(ItemDataHub ctx)
    {
        // 내 실제 월드 좌표를 0~1 사이의 화면 비율 좌표로 변환
        Vector3 viewPos = Camera.main.WorldToViewportPoint(ctx.transform.position);

        if (viewPos.y < -0.01f || viewPos.y > 1.01f)
        {
            return true;
        }

        return false;
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
            if (placed_ctx.mono.Grid.crafteditemName == "bigmushroom") placed_ctx.mono.Grid.craftedPos.y -= 0.07f;
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


