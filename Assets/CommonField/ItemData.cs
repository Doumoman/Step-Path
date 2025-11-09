using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    // 아이템 이름 / 리롤 비용 / 아이템 빈도
    [Header("아이템 이름")]
    public string itemName = "basic";

    [Header("필요 없는 아이템 리롤 비용")]
    public float rerollCost = 3f;

    [Header("아이템 빈도")]
    public float itemFrequency = 3f;


}
