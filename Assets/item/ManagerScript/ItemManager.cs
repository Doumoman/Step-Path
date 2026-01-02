using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Tilemaps;

public class ItemManager : MonoBehaviour
{
    [SerializeField] ItemPrepabDelegate prefab;
    [SerializeField] Grid grid;
    [SerializeField] GridData gridData;
    [SerializeField] Tilemap groundtilemap;
    [SerializeField] public Transform itemContainer;
    [SerializeField] public Transform imageContainer;
    [SerializeField] public Transform Ground_item;
    public List<GameObject> itemPrefabs;
    public List<GameObject> itemimages;
    public List<GameObject> Crafteditems;
    public List<ItemData> itemDatas;
    private Queue<int> itemnumstack = new Queue<int>();
    int sum = 0;
    

    void Awake()
    {
        gridData.currentGrid = grid;
        gridData.ground = groundtilemap;
        
        
    }

    private void Start()
    {
        SumOfFreq(itemDatas);
        SpawnRandomItemImage();
        
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
        if (itemimages == null || itemimages.Count == 0)
        {
            Debug.LogWarning("itemPrefabs 목록이 비어있습니다!");
            return;
        }
        GameObject imageToSpawn = Instantiate(itemimages[num], imageContainer, false);

    }

    void Spawnitem()
    {
        int id = itemnumstack.Dequeue();
        if(itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogWarning("itemPrefabs 목록이 비어있습니다!");
            return;
        }

        Vector3 spawnpos;
        

        if (itemDatas[id].itemName == "wood" || itemDatas[id].itemName == "cloud") { spawnpos = grid.CellToWorld(gridData.positioncell); spawnpos.x -= 0.125f; spawnpos.y -= 0.035f; }
        else { spawnpos = grid.CellToWorld(gridData.positioncell); }


        GameObject itemspawn;
        if (itemDatas[id].itemName == "wood" || itemDatas[id].itemName == "cloud") itemspawn = Instantiate(itemPrefabs[id], spawnpos, Quaternion.identity, Ground_item);
        else itemspawn = Instantiate(itemPrefabs[id], spawnpos, Quaternion.identity, itemContainer);
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
        
        for(int i = 0; i < Crafteditems.Count; i++)
        {
            ItemController c = Crafteditems[i].gameObject.GetComponent<ItemController>();
            if (gridData.crafteditemName == c.Data.itemName)
            {
                GameObject itemspawn = Instantiate(Crafteditems[i], gridData.craftedPos, Quaternion.identity, Ground_item);
                return;
            }
        }
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
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.035f;
            itemspawn = Instantiate(itemPrefabs[3], spawnpos, Quaternion.identity, Ground_item);
            createpos.x++;
        }
        createpos = gridData.groundLposright; createpos.x--;
        while (createpos.x > gridData.positioncell.x - 1)
        {
            realpos = new Vector3Int(createpos.x + 1, createpos.y + 1, 0);
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.035f;
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
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.035f;
            itemspawn = Instantiate(itemPrefabs[4], spawnpos, Quaternion.identity, Ground_item);
            createpos.x++;
        }
        createpos = gridData.groundLposright; createpos.x--;
        while (createpos.x > gridData.positioncell.x - 1)
        {
            realpos = new Vector3Int(createpos.x + 1, createpos.y + 1, 0);
            spawnpos = grid.CellToWorld(realpos); spawnpos.x -= 0.125f; spawnpos.y -= 0.035f;
            itemspawn = Instantiate(itemPrefabs[4], spawnpos, Quaternion.identity, Ground_item);
            createpos.x--;
        }
        return;
    }

    void Deletitemstack()
    {
        itemnumstack.Dequeue();
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

    
}
