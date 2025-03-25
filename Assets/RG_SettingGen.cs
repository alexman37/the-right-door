using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// RG_SettingGen contains data used by RoomGeneration about each setting
/// For example, what the different tiles in the tilemap are and when they should be used
/// </summary>
public class RG_SettingGen : MonoBehaviour
{
    public delegate Room roomModification(Room room);
    // Tileset: What tileset does it use and how is it structured...
    public TilemapCodex tilemapCodex;

    // Floor patterns: Carpet, checkered tiling...
    public roomModification additionalLayoutGenSteps;

    // Wall patterns: Spaced wall lamps...
    public roomModification additionalWallGenSteps;

    // Objects: What objects are specific to the mansion, and where would they be placed...
    public List<RoomObject> settingRoomObjects;
    public roomModification additionalObjectGenSteps;

    // Doors: What do the doors look like here...
    public List<DoorObject> doorSprite;
}

[System.Serializable]
public class TilemapCodex
{
    public TileIndex baseFloor;
    public BorderedTileGroup frontWall;
    public TileIndex sideWall;

    public Dictionary<string, TileIndex> others;

    public TilemapCodex(TileIndex baseFloor, BorderedTileGroup frontWall, TileIndex sideWall, Dictionary<string, TileIndex> others)
    {
        this.baseFloor = baseFloor;
        this.frontWall = frontWall;
        this.sideWall = sideWall;
        this.others = others;
    }
}

[System.Serializable]
public class TileIndex
{
    public Tile tile;
}

[System.Serializable]
public class SingleTileIndex : TileIndex
{
    
} 

[System.Serializable]
public class BorderedTileGroup : TileIndex
{
    //There are 16 total variations
    public Tile none;
    public Tile right;
    public Tile top;
    public Tile left;
    public Tile bottom;
    public Tile topRight;
    public Tile topLeft;
    public Tile bottomLeft;
    public Tile bottomRight;
    public Tile verticalBars;
    public Tile horizontalBars;
    public Tile allButRight;
    public Tile allButTop;
    public Tile allButLeft;
    public Tile allButBottom;
    public Tile all4;

    private Tile[] ordered;

    public Tile getBorderedTile(int index)
    {
        if(ordered == null)
        {
            ordered = new[] { none, right, top, left, bottom, topRight, topLeft, bottomLeft, bottomRight, verticalBars, horizontalBars, allButRight, allButTop, allButLeft, allButBottom, all4 };
        }
        return ordered[index];
    }
}