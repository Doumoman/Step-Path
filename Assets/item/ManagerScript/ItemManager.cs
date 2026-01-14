using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
#endif
public class ItemManager : MonoBehaviour
{
    [SerializeField] ItemPrepabDelegate prefab;
    [SerializeField] Grid grid;
    [SerializeField] GridData gridData;
    [SerializeField] Tilemap groundtilemap;
    [SerializeField] public Transform itemContainer;
    [SerializeField] public Transform imageContainer;
    [SerializeField] public Transform Ground_item;
    [SerializeField] public TileBase groundTile;
    [SerializeField] public GameObject player;
    [SerializeField] public Image targetimage;
    public List<GameObject> itemPrefabs;
    public List<GameObject> itemimages;
    public List<GameObject> Crafteditems;
    public List<ItemData> itemDatas;
    public List<Sprite> secondSlotsprite;
    public Queue<int> itemnumstack = new Queue<int>();
    int sum = 0;
    int secondslot = -1;
    

    void Awake()
    {
        gridData.currentGrid = grid;
        gridData.ground = groundtilemap;
        gridData.gTile = groundTile;
        
        
    }

    private void Start()
    {
        SumOfFreq(itemDatas);
        //TestProbability(itemDatas);
        SpawnRandomItemImage();
        secondslot = RandomDraw(itemDatas);
        itemnumstack.Enqueue(secondslot);
        DisplaySecondSlot();
        PrintQueueState();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        prefab.Onitemimage += SpawnRandomItemImage;
        prefab.OnSimpleitem += Spawnitem;
        prefab.OnCrafteditem += SpawnCrafteditem;
        prefab.Dequeueitem += Deletitemstack;
        prefab.Grounditem += SpawnGrounditem;
        prefab.Clouditem += SpawnClouditem;
    }


    void SpawnRandomItemImage()
    {
        int num = RandomDraw(itemDatas);
        itemnumstack.Enqueue(num);
        if (secondslot != -1) secondslot = num;

        num = itemnumstack.Peek();
        
        if (itemimages == null || itemimages.Count == 0)
        {
            Debug.LogWarning("itemPrefabs 목록이 비어있습니다!");
            return;
        }
        GameObject imageToSpawn = Instantiate(itemimages[num], imageContainer, false);

        if (secondslot != -1) DisplaySecondSlot();
        PrintQueueState();

    }

    void DisplaySecondSlot()
    {
        if(secondslot >= 3 && secondslot <= 4) targetimage.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
        else targetimage.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        targetimage.sprite = secondSlotsprite[secondslot];
    }

    void Spawnitem()
    {
        int id = itemnumstack.Dequeue();
        if(itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogWarning("itemPrefabs 목록이 비어있습니다!");
            return;
        }
        gridData.playerpos = player.transform.position;

        Vector3 spawnpos;
        

        if (itemDatas[id].itemName == "wood" || itemDatas[id].itemName == "cloud") { spawnpos = grid.CellToWorld(gridData.positioncell); spawnpos.x -= 0.125f; spawnpos.y -= 0.062f; }
        else if(itemDatas[id].itemName == "rock" || itemDatas[id].itemName == "sprout") { spawnpos = grid.CellToWorld(gridData.positioncell); spawnpos.y -= 0.02f; }
        else { spawnpos = grid.CellToWorld(gridData.positioncell); }


            GameObject itemspawn;
        if (itemDatas[id].itemName == "wood" || itemDatas[id].itemName == "cloud") itemspawn = Instantiate(itemPrefabs[id], spawnpos, Quaternion.identity, Ground_item);
        else itemspawn = Instantiate(itemPrefabs[id], spawnpos, Quaternion.identity, itemContainer);
        PrintQueueState();
    }

    void SpawnCrafteditem()
    {
        itemnumstack.Dequeue();
        if (Crafteditems == null || Crafteditems.Count == 0)
        {
            Debug.LogWarning("Cragteditems 목록이 비어있습니다!");
            return;
        }

        if (Crafteditems[0] == null)  Debug.Log("아이템 ㅌ");

        GameObject itemspawn;
        for (int i = 0; i < Crafteditems.Count; i++)
        {
            ItemController c = Crafteditems[i].gameObject.GetComponent<ItemController>();
            if (gridData.crafteditemName == c.Data.itemName)
            {
                itemspawn = Instantiate(Crafteditems[i], gridData.craftedPos, Quaternion.identity, itemContainer);
                if (i == 2)
                {
                    if (itemspawn.transform.position.x < gridData.playerpos.x)
                    {
                        Vector3 saveScale = itemspawn.transform.localScale;
                        saveScale.x *= -1;
                        itemspawn.transform.localScale = saveScale;
                    }
                }

                return;
            }
        }
        PrintQueueState();
    }

    void SpawnGrounditem()
    {
        GameObject itemspawn;
        Vector3 spawnpos;
        Vector3Int realpos;
        Vector3Int createpos = gridData.groundLposleft; createpos.x++;
        while(createpos.x < gridData.positioncell.x - 1)
        {
            realpos = new Vector3Int(createpos.x + 1, createpos.y + 1, 0);
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.062f;
            itemspawn = Instantiate(itemPrefabs[3], spawnpos, Quaternion.identity, Ground_item);
            createpos.x++;
        }
        createpos = gridData.groundLposright; createpos.x--;
        while (createpos.x > gridData.positioncell.x - 1)
        {
            realpos = new Vector3Int(createpos.x + 1, createpos.y + 1, 0);
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.062f;
            itemspawn = Instantiate(itemPrefabs[3], spawnpos, Quaternion.identity, Ground_item);
            createpos.x--;
        }
        return;
    }

    void SpawnClouditem()
    {
        GameObject itemspawn;
        Vector3 spawnpos;
        Vector3Int realpos;
        Vector3Int createpos = gridData.groundLposleft; createpos.x++;
        while (createpos.x < gridData.positioncell.x - 1)
        {
            realpos = new Vector3Int(createpos.x + 1, createpos.y + 1, 0);
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.062f;
            itemspawn = Instantiate(itemPrefabs[4], spawnpos, Quaternion.identity, Ground_item);
            createpos.x++;
        }
        createpos = gridData.groundLposright; createpos.x--;
        while (createpos.x > gridData.positioncell.x - 1)
        {
            realpos = new Vector3Int(createpos.x + 1, createpos.y + 1, 0);
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.062f;
            itemspawn = Instantiate(itemPrefabs[4], spawnpos, Quaternion.identity, Ground_item);
            createpos.x--;
        }
        return;
    }

    void Deletitemstack()
    {
        itemnumstack.Dequeue();
        //PrintQueueState();
    }

    void SumOfFreq(List<ItemData> itemlist)
    {
        for(int i = 0; i < itemlist.Count; i++)
        {
            sum += itemlist[i].itemFrequency;
        } 
    }

    int RandomDraw(List<ItemData> itemlist)
    {
        
        int n = Random.Range(0,sum);
        for (int i = 0; i < itemlist.Count; i++) 
        {
            if(n >= itemlist[i].itemFrequency) n -= itemlist[i].itemFrequency;
            else { return i; }
        }

        return 0;
    }

    //확률 테스트
    /*
    void TestProbability(List<ItemData> itemlist)
    {
        int trialCount = 10000; // 1만 번 시도
        int[] results = new int[itemlist.Count]; // 결과 카운트용 배열

        // 1만 번 뽑기 실행
        for (int i = 0; i < trialCount; i++)
        {
            int pickedIndex = RandomDraw(itemlist);
            results[pickedIndex]++;
        }

        // 결과 출력
        Debug.Log($"=== 총 {trialCount}회 시뮬레이션 결과 ===");
        for (int i = 0; i < itemlist.Count; i++)
        {
            float ratio = (float)results[i] / trialCount * 100f; // 실제 나온 확률
            float expectedRatio = (float)itemlist[i].itemFrequency / sum * 100f; // 기획 의도 확률

            Debug.Log($"아이템[{i}]: {results[i]}회 당첨 ({ratio:F2}%) / 목표 확률: {expectedRatio:F2}%");
        }
    }
    */

    void PrintQueueState()
    {
        if (itemnumstack.Count == 0)
        {
            Debug.Log("큐가 비어있습니다. (Empty)");
            return;
        }

        // 예시 출력: [Front] 1, 2, 3, 4 [Back]
        Debug.Log($"[큐 상태] Front(앞) -> {string.Join(", ", itemnumstack)} <- Back(뒤)");
    }
}
