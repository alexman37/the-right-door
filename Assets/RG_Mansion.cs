using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  Mansion-specific generation of floor, walls, etc.
/// </summary>
public class RG_Mansion : RG_SettingGen
{
    private void Start()
    {
        this.additionalLayoutGenSteps = allButEdgesBecomeCarpet;
    }

    // Tileset: What tileset does it use and how is it structured...
    public BorderedTileGroup carpet;

    // Floor patterns: Carpet, checkered tiling...

    // Wall patterns: Spaced wall lamps...

    // Objects: What objects are specific to the mansion, and where would they be placed...

    // Doors: What do the doors look like here...

    // Make all floor tiles except for those bordering a wall (or nothing) into carpet
    public Room allButEdgesBecomeCarpet(Room room)
    {
        List<RoomTile> accumulatedCarpetTiles = new List<RoomTile>();

        for (int xx = 0; xx < room.roomWidthWithWall; xx++)
        {
            for (int yy = 0; yy < room.roomHeightWithWall; yy++)
            {
                Coords currPos = new Coords(xx, yy);
                RoomTile tile = room.tileArray[xx, yy];
                // Only bother for tiles that are completely surrounded by floor
                if (room.getNumLikewiseNeighbors(currPos, (tile) => { return tile.type == RoomTileType.FLOOR; }) == 4) {
                    bool notADiagonal = true;
                    foreach (RoomTile tileNext in room.getNeighbors(currPos))
                    {
                        if(tileNext != null && room.getNumLikewiseNeighbors(tileNext.tileCoords, (tileNext) => { return tile.type == RoomTileType.FLOOR; }) < 4)
                            notADiagonal = false;
                    }
                    if (notADiagonal)
                    {
                        //TODO: Make another overload checking for tileFill? Something to do with carpet...
                        // But it will have to be in yet ANOTHER loop of some kind at the end...blegh
                        // Could at least mitigate it by building up a carpet tile list the whole time, then just doing that at the end
                        tile.tileFill = carpet.tile;
                        tile.genFlag = "carpet";
                        accumulatedCarpetTiles.Add(tile);
                    }
                }
            }
        }

        foreach(RoomTile tile in accumulatedCarpetTiles)
        {
            int borderStatus = room.getBorderStatus(tile.tileCoords, (tile) => { return tile.genFlag == "carpet"; });
            tile.tileFill = carpet.getBorderedTile(borderStatus);
        }
        return room;
    }
}
