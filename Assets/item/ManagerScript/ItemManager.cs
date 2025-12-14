using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEditor;

public class ItemManager : MonoBehaviour
{
    [SerializeField] ItemPrepabDelegate prefab;
    [SerializeField] Grid grid;
    [SerializeField] GridData gridData;
    [SerializeField] public Transform itemContainer;
    [SerializeField] public Transform imageContainer;
    public List<GameObject> itemPrefabs;
    public List<GameObject> itemimages;
    public List<GameObject> Crafteditems;
    private Queue<int> itemnumstack = new Queue<int>();
    

    void Awake()
    {
        gridData.currentGrid = grid;
        
    }

    private void Start()
    {
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
        int num = Random.Range(0, itemimages.Count);
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

        if (id >= itemimages.Count - 2) { spawnpos = grid.CellToWorld(gridData.positioncell); spawnpos.y -= 0.043f; }
        else { spawnpos = grid.CellToWorld(gridData.positioncell); }

        GameObject itemspawn = Instantiate(itemPrefabs[id], spawnpos, Quaternion.identity, itemContainer);
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
}
