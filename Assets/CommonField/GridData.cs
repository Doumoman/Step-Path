using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Grid Data")]
public class GridData : ScriptableObject
{
    [System.NonSerialized]
    public Grid currentGrid;
    [System.NonSerialized]
    public Vector3Int positioncell;
}
