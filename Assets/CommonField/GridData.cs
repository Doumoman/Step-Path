using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Grid/Grid Data")]
public class GridData : ScriptableObject
{
    [System.NonSerialized]
    public Grid currentGrid;
    [System.NonSerialized]
    public Vector3Int positioncell;
    [System.NonSerialized]
    public string crafteditemName;
    [System.NonSerialized]
    public Vector3 craftedPos;
    [System.NonSerialized]
    public Vector3Int groundLposleft;
    [System.NonSerialized]
    public Vector3Int groundLposright;
    [System.NonSerialized]
    public Tilemap ground;
    [System.NonSerialized]
    public TileBase gTile;
    [System.NonSerialized]
    public Vector3 playerpos;
    [System.NonSerialized]
    public ButtonHandler buttonHandler;
    [System.NonSerialized]
    public bool stairsRightcheck = false;
    [System.NonSerialized]
    public bool stairsLeftcheck = false;
}
