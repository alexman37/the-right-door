using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;

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
    // "Noooo you cant just generate random numbers in an async block!!!" -beta unity.
    // So we generate a bunch of random floats and can use them at our discretion. Be sure to regen often.
    private static float[] RANDOM_VALUES = new float[50];

    private static void regenRandomValues()
    {
        for (int i = 0; i < 10; i++) RANDOM_VALUES[i] = Random.value;
    }

    //TODO adjust these arguments to be more general
    public async static Task<Room> generateNewRoom(Coords criticalPoint, Tilemap standardTilemap, Tile demoTile)
    {
        regenRandomValues();
        Room newRoom = null;
        await Task.Run(() =>
        {
            newRoom = LayoutGeneration.randomSquare(criticalPoint, (int)(RANDOM_VALUES[0] * 12 + 4), demoTile); //TODO: Better tile choosing
            ObjectGeneration.fillWithObjects(newRoom);
        }); 
        RoomBuilder.build(newRoom, standardTilemap);
        return newRoom;
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
    // Generate a single square in the room centered around the critical point.
    public static Room randomSquare(Coords criticalPoint, int settledSize, Tile fill)
    {
        int settledMinX = criticalPoint.x - settledSize / 2;
        int settledMaxX = criticalPoint.x + settledSize / 2;
        int settledMinY = criticalPoint.y - settledSize / 2;
        int settledMaxY = criticalPoint.y + settledSize / 2;

        List<RoomTile> workingTiles = new List<RoomTile>();

        for (int xx = settledMinX; xx < settledMaxX; xx++)
        {
            for (int yy = settledMinY; yy < settledMaxY; yy++)
            {
                // xx and yy are in REAL position.
                // But when we generate the tile array, they get converted over there into ROOM position.
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill);
                workingTiles.Add(tile);
            }
        }

        return generateTileArray(criticalPoint, workingTiles, settledMinX, settledMaxX, settledMinY, settledMaxY);
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

        Room createdRoom = new Room(criticalPoint, minX, minY, tileArray);
        return createdRoom;
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
        //TODO it's also gonna need data on wall tiles, adjto wall tiles, floor tiles eligible for items, etc.

        //TODO be smarter about this.
        room.entryPoint = new Coords(2, 2);

        return room;
    }
}