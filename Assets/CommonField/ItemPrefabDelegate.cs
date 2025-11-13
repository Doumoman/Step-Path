using UnityEngine;
using System;

[CreateAssetMenu(fileName = "ItemPrefabEvent", menuName = "ItemCreateEvent/Creating Event")]

public class ItemPrepabDelegate : ScriptableObject
{
    public Action OnSimpleItem;
    public Action OnCraftedItem;
    

    public void CreateSimpleItem()
    {
        OnSimpleItem?.Invoke();
    }

    public void CreateCraftedItem()
    {
        OnCraftedItem?.Invoke();
    }


}
