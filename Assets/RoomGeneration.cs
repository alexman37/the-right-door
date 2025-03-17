using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/* Room Generation handles, well, creation of rooms.
 * You call one of various methods to handle making the floors. For example: Square, jigsaw, etc.
 * Then you should figure out where the doors are
 * And then you should draw walls
 * And then you should add other items, puzzles and such into the room...
 * BASED ON a series of rules.
 * This is going to be a big and ugly one, so we'll split it up as much as we can.
 */
public static class RoomGeneration
{
    //TODO adjust these arguments to be more general
    public static void generateNewRoom(Coords criticalPoint, Tilemap standardTilemap, Tile demoTile)
    {
        Room newRoom = LayoutGeneration.randomSquare(criticalPoint, 12, demoTile); //TODO: Better tile choosing
        RoomBuilder.build(newRoom, standardTilemap);
    }
}

// This creates the tile map and adds in all the room's actual physical objects into the game
public static class RoomBuilder 
{
    public static void build(Room room, Tilemap tilemap)
    {
        foreach(RoomTile tile in room.tileArray)
        {
            if(tile != null)
            {
                tilemap.SetTile(tile.getRealPos(room.roomPosToRealPosXOffset, room.roomPosToRealPosYOffset), tile.tileType);
            }
        }
    }
}

// LayoutGeneration determines the floor, and the overall "shape" of the room
// All generation methods should return a 1D array of the REAL positions of all tiles.
// But in the end, generateTileArray() is called to build the 2D array of ROOM positions.
public static class LayoutGeneration
{
    // Generate a single square in the room centered around 0,0.
    public static Room randomSquare(Coords criticalPoint, int maxSize, Tile fill)
    {
        int minSize = 4;
        int settledSize = Random.Range(minSize, maxSize);
        int settledMin = -settledSize / 2;
        int settledMax = settledSize / 2;

        List<RoomTile> workingTiles = new List<RoomTile>();

        for(int xx = settledMin; xx < settledMax; xx++)
        {
            for (int yy = settledMin; yy < settledMax; yy++)
            {
                // xx and yy are in REAL position.
                // But when we generate the tile array, they get converted over there into ROOM position.
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill);
                workingTiles.Add(tile);
            }
        }

        return generateTileArray(criticalPoint, workingTiles, settledMin, settledMax, settledMin, settledMax);
    }

    public static void randomRect(int maxWidth, int maxHeight)
    {
        //int minSize = 10;
    }

    // Generate the actual 2D array which uses ROOM position
    private static Room generateTileArray(Coords criticalPoint, List<RoomTile> tiles, int minX, int maxX, int minY, int maxY)
    {
        Debug.Log("The tiles go from " + minX + " to " + maxX);
        RoomTile[,] tileArray = new RoomTile[maxX - minX + 1, maxY - minY + 1];

        //The tile array needs to be created with 0,0 as the absolute min- so we need to "convert" all prior positions
        foreach(RoomTile tile in tiles)
        {
            tile.tileCoords.offsetThis(-1 * minX, -1 * minY);
            tileArray[tile.tileCoords.x, tile.tileCoords.y] = tile;
        }

        return new Room(criticalPoint, minX, minY, tileArray);
    }
}


// ObjectGeneration fills up the room with objects.
// --> TODO: Objects based on rules
public static class ObjectGeneration
{
    public static Room fillWithObjects(Room room)
    {
        //TODO Eventually we generate with respect to whatever rules are active
        // For now we just rely on "generic" generation, adding in random things


        return room;
    }
}