using UnityEngine;
using System;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ItemPrefabEvent", menuName = "ItemCreateEvent/Creating Event")]

public class ItemPrepabDelegate : ScriptableObject
{
    public Action OnSimpleitem;
    public Action Onitemimage;
    public Action OnCrafteditem;
    public Action Dequeueitem;
    public Action Grounditem;
    public Action Clouditem;
    public Action Rerollitem;



    public void CreateSimpleitem()
    {
        OnSimpleitem?.Invoke();
    }

    public void Createitemimage()
    {
        Onitemimage?.Invoke();
    }

    public void CreateCrafteditem()
    {
        OnCrafteditem?.Invoke();
    }

    public void DeletitemStack()
    {
        Dequeueitem?.Invoke();
    }

    public void CreateGroundL(ItemDataHub ctx)
    {
        Grounditem?.Invoke();
        ctx.data.isoriginal = false;
    }

    public void CreateCloudL()
    {
        Clouditem?.Invoke();
    }

    public void Rerolltheitem()
    {
        Rerollitem?.Invoke();
    }
}
