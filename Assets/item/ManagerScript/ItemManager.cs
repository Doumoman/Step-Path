using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ItemManager : MonoBehaviour
{
    [SerializeField] ItemPrepabDelegate prefab;
    [SerializeField] Grid grid;
    [SerializeField] GridData gridData;
    public List<GameObject> itemPrefabs;
    public Transform itemContainer;

    void Start()
    {
        SpawnRandomItem();
        gridData.currentGrid = grid;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        prefab.OnSimpleitem += SpawnRandomItem;
    }


    void SpawnRandomItem()
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogWarning("itemPrefabs 목록이 비어있습니다!");
            return;
        }
        GameObject prefabToSpawn = itemPrefabs[0];

        Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity, itemContainer);
    }
}
