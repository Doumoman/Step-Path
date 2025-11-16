using UnityEngine;
using System;

[CreateAssetMenu(fileName = "ItemPrefabEvent", menuName = "ItemCreateEvent/Creating Event")]

public class ItemPrepabDelegate : ScriptableObject
{
    public Action OnSimpleitem;
    public Action OnCrafteditem;
    

    public void CreateSimpleitem()
    {
        OnSimpleitem?.Invoke();
    }

    public void CreateCrafteditem()
    {
        OnCrafteditem?.Invoke();
    }


}
