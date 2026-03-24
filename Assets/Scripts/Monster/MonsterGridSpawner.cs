using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MonsterGridSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] monsterPrefabs;

    [Header("Optional Block Check")]
    [SerializeField] private Tilemap groundTilemap; // 발판/벽 체크용 (없으면 비워둬도 됨)

    [Header("Spawn Rule - Grid Based")]
    [SerializeField] private int yInterval = 4;              // y축 4칸마다 체크
    [SerializeField] private int heightStepForBonus = 50;     // y 50칸 상승마다
    [SerializeField] private float chanceBonusPerStep = 0.05f; // +5%
    [SerializeField] private float baseSpawnChance = 0.05f;  // 기본 5%
    [SerializeField] private float maxSpawnChance = 0.5f;    // 최대 50%

    [Header("Spawn Unlock Height")]
    [SerializeField] private float spawnUnlockY = 30f;   // 30m 전까지 스폰 금지
    [SerializeField] private float startSpawnChance = 0.05f; // 30m 도달 시 시작 확률 5%

    [Header("Spawn Area")]
    [SerializeField] private int centerSafeWidth = 2;        // 가운데 2칸 비움
    [SerializeField] private int sideWidth = 16;             // 좌우 16칸씩
    [SerializeField] private Vector2Int monsterSize = new Vector2Int(2, 2); // 2x2 점유
    [SerializeField] private int spawnAheadRows = 8;         // 플레이어 위로 몇 줄 미리 검사할지

    [Header("Spawn Offset (Grid 기준)")]
    [SerializeField] private Vector2Int spawnOffset = new Vector2Int(-12, 1);

    [Header("Grid Origin")]
    [SerializeField] private Vector3Int gridOriginCell = Vector3Int.zero;
    [SerializeField] private bool usePlayerAsCenterX = true; // true면 플레이어 x 기준, false면 origin 기준

    private float _bestPlayerY;
    private int _heightBonusLevel;

    // 이미 처리한 y row
    private HashSet<int> _processedRows = new HashSet<int>();

    private void Start()
    {
        if (grid == null)
            grid = FindObjectOfType<Grid>();

        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        if (player != null)
            _bestPlayerY = player.position.y;
    }

    private void Update()
    {
        if (grid == null || player == null || monsterPrefabs == null || monsterPrefabs.Length == 0)
            return;

        UpdateDifficultyByHeight();
        ProcessSpawnRows();
    }

    private void UpdateDifficultyByHeight()
    {
        if (player.position.y > _bestPlayerY)
            _bestPlayerY = player.position.y;

        Vector3Int bestCell = grid.WorldToCell(new Vector3(player.position.x, _bestPlayerY, 0f));
        Vector3Int originCell = gridOriginCell;

        int climbedCells = bestCell.y - originCell.y;
        _heightBonusLevel = Mathf.Max(0, climbedCells / Mathf.Max(1, heightStepForBonus));
    }

    private void ProcessSpawnRows()
    {
        Vector3Int playerCell = grid.WorldToCell(player.position);

        // 현재 플레이어 위쪽으로 몇 줄 미리 체크
        int startY = playerCell.y;
        int endY = playerCell.y + spawnAheadRows * yInterval;

        for (int y = startY; y <= endY; y++)
        {
            int relativeY = y - gridOriginCell.y;

            // 4칸 간격 줄만 체크
            if (relativeY < 0 || relativeY % yInterval != 0)
                continue;

            if (_processedRows.Contains(y))
                continue;

            _processedRows.Add(y);

            TrySpawnAtRow(y, playerCell);
        }
    }

    private void TrySpawnAtRow(int rowY, Vector3Int playerCell)
    {
        if (_bestPlayerY < spawnUnlockY)
            return;

        float currentChance = GetCurrentSpawnChance();

        if (Random.value > currentChance)
            return;

        bool spawnLeft = Random.value < 0.5f;
        TrySpawnOneSideGuaranteed(rowY, spawnLeft, playerCell);
    }

    private void TrySpawnOneSideGuaranteed(int rowY, bool isLeft, Vector3Int playerCell)
    {
        int centerX = usePlayerAsCenterX ? playerCell.x : gridOriginCell.x;
        int halfSafe = centerSafeWidth / 2;

        int minX;
        int maxX;

        if (isLeft)
        {
            maxX = centerX - halfSafe - 1;
            minX = maxX - sideWidth + 1;
        }
        else
        {
            minX = centerX + halfSafe;
            maxX = minX + sideWidth - 1;
        }

        maxX -= (monsterSize.x - 1);

        if (minX > maxX)
            return;

        int spawnX = Random.Range(minX, maxX + 1);
        Vector3Int spawnCell = new Vector3Int(spawnX, rowY, 0) + (Vector3Int)spawnOffset;

        if (!CanSpawnAt(spawnCell))
            return;

        SpawnMonster(spawnCell);
    }
    private float GetCurrentSpawnChance()
    {
        if (_bestPlayerY < spawnUnlockY)
            return 0f;

        float extraHeight = _bestPlayerY - spawnUnlockY;

        int bonusLevel = Mathf.FloorToInt(extraHeight / heightStepForBonus);

        float chance = startSpawnChance + (bonusLevel * chanceBonusPerStep);
        return Mathf.Clamp(chance, 0f, maxSpawnChance);
    }

    private bool CanSpawnAt(Vector3Int startCell)
    {
        // 2x2 점유 검사
        for (int dy = 0; dy < monsterSize.y; dy++)
        {
            for (int dx = 0; dx < monsterSize.x; dx++)
            {
                Vector3Int checkCell = new Vector3Int(startCell.x + dx, startCell.y + dy, 0);

                // groundTilemap이 있으면 타일이 차 있는 자리는 스폰 금지
                if (groundTilemap != null && groundTilemap.HasTile(checkCell))
                    return false;
            }
        }

        // 월드 충돌 검사도 추가 가능
        Vector3 worldMin = grid.GetCellCenterWorld(startCell);
        Vector3 worldMax = grid.GetCellCenterWorld(new Vector3Int(
            startCell.x + monsterSize.x - 1,
            startCell.y + monsterSize.y - 1,
            startCell.z));

        Vector2 center = (worldMin + worldMax) * 0.5f;
        Vector2 size = new Vector2(
            Mathf.Abs(worldMax.x - worldMin.x) + grid.cellSize.x,
            Mathf.Abs(worldMax.y - worldMin.y) + grid.cellSize.y
        );

        Collider2D hit = Physics2D.OverlapBox(center, size * 0.9f, 0f);
        if (hit != null)
            return false;

        return true;
    }

    private void SpawnMonster(Vector3Int cellPos)
    {
        int idx = Random.Range(0, monsterPrefabs.Length);
        GameObject prefab = monsterPrefabs[idx];

        Vector3 worldPos = grid.GetCellCenterWorld(cellPos);

        // 2x2 크기의 중심 보정
        Vector3 offset = new Vector3(
            (monsterSize.x - 1) * grid.cellSize.x * 0.5f,
            (monsterSize.y - 1) * grid.cellSize.y * 0.5f,
            0f
        );

        Instantiate(prefab, worldPos + offset, Quaternion.identity);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (grid == null) return;

        Vector3Int centerCell = gridOriginCell;

        if (player != null && usePlayerAsCenterX)
        {
            Vector3Int playerCell = grid.WorldToCell(player.position);
            centerCell.x = playerCell.x;
        }

        int halfSafe = centerSafeWidth / 2;

        // 왼쪽 16칸
        for (int x = centerCell.x - halfSafe - sideWidth; x < centerCell.x - halfSafe; x++)
        {
            DrawCellGizmo(new Vector3Int(x, centerCell.y, 0), Color.red);
        }

        // 오른쪽 16칸
        for (int x = centerCell.x + halfSafe; x < centerCell.x + halfSafe + sideWidth; x++)
        {
            DrawCellGizmo(new Vector3Int(x, centerCell.y, 0), Color.blue);
        }
    }

    private void DrawCellGizmo(Vector3Int cell, Color color)
    {
        Gizmos.color = color;
        Vector3 pos = grid.GetCellCenterWorld(cell);
        Gizmos.DrawWireCube(pos, grid.cellSize);
    }
#endif
}