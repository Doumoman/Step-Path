using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

public class ItemManager : MonoBehaviour
{
    [SerializeField] ItemPrepabDelegate prefab;
    [SerializeField] Grid grid;
    [SerializeField] GridData gridData;
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
        

        if (itemDatas[id].itemName == "wood" || itemDatas[id].itemName == "cloud") { spawnpos = grid.CellToWorld(gridData.positioncell); spawnpos.y -= 0.043f; }
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
                GameObject itemspawn = Instantiate(Crafteditems[i], gridData.craftedPos, Quaternion.identity, itemContainer);
                return;
            }
        }
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
