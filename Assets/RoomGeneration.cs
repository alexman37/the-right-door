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
public class RoomGeneration : MonoBehaviour
{
    public static Room activeRoom;

    // Start is called before the first frame update
    public Tilemap standardTilemap;
    public Tile demoTile;

    void Start()
    {
        // Set up the STARTER ROOM
        // --> TODO This will probably get changed drastically
        RoomTile[,] startTileArray = new RoomTile[10, 6];
        for(int xx = -6; xx <= 3; xx++)
        {
            for(int yy = -3; yy <= 2; yy++)
            {
                startTileArray[xx + 6, yy + 3] = new RoomTile(true, new Coords(xx + 6, yy + 3), demoTile);
            }
        }
        Room starterRoom = new Room(-6, -3, startTileArray);

        Debug.Log("Setting active room");
        activeRoom = starterRoom;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void generateNewRoom()
    {
        Room newRoom = LayoutGeneration.randomSquare(12, demoTile); //TODO: Better tile choosing
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
    public static Room randomSquare(int maxSize, Tile fill)
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

        return generateTileArray(workingTiles, settledMin, settledMax, settledMin, settledMax);
    }

    public static void randomRect(int maxWidth, int maxHeight)
    {
        //int minSize = 10;
    }

    // Generate the actual 2D array which uses ROOM position
    private static Room generateTileArray(List<RoomTile> tiles, int minX, int maxX, int minY, int maxY)
    {
        Debug.Log("The tiles go from " + minX + " to " + maxX);
        RoomTile[,] tileArray = new RoomTile[maxX - minX + 1, maxY - minY + 1];

        //The tile array needs to be created with 0,0 as the absolute min- so we need to "convert" all prior positions
        foreach(RoomTile tile in tiles)
        {
            tile.tileCoords.offsetThis(-1 * minX, -1 * minY);
            tileArray[tile.tileCoords.x, tile.tileCoords.y] = tile;
        }

        return new Room(minX, minY, tileArray);
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

// A Room is made up of a 2D array representing all relevant tiles, and, a...
public class Room
{
    public int roomPosToRealPosXOffset;
    public int roomPosToRealPosYOffset;

    public RoomTile[,] tileArray;

    public int roomWidth;
    public int roomHeight;

    public Room(int xo, int yo, RoomTile[,] roomTiles)
    {
        roomPosToRealPosXOffset = xo;
        roomPosToRealPosYOffset = yo;
        this.tileArray = roomTiles; //TODO: Deep copy? is it a problem?
        roomWidth = roomTiles.GetLength(0);
        roomHeight = roomTiles.GetLength(1);
    }
}

// RoomTile tileCoords keeps track of "ROOM POSITION" not real position
// But there is an easy way to convert to real position, assuming you know which room this is actually a part of.
public class RoomTile
{
    public bool walkable;
    public Coords tileCoords;
    public Tile tileType;
    public RoomTile(bool w, Coords c, Tile t)
    {
        walkable = w;
        tileCoords = c;
        tileType = t;
    }

    public override string ToString()
    {
        return "(" + tileCoords.x + "," + tileCoords.y + ")";
    }

    public Vector3Int getRealPos(int roomPosToRealPosXOffset, int roomPosToRealPosYOffset)
    {
        return tileCoords.offset(roomPosToRealPosXOffset, roomPosToRealPosYOffset).asVector3Int();
    }
}

/* Our coordinate system is based in "room coords" and "real coords", where:
 * Room coords are oriented around 0,0 to cooperate with 2D arrays.
 * Real coords are the actual physical position in the game world.
 * It often will happen that you need to convert from one to the other, which is what the "offset" method can help with.
 * You'd store the constant x/y offset in the roomPosToRealPosOffset field of this Room.
*/
public class Coords
{
    public int x;
    public int y;
    public Coords(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    // When the tilemap is ready to be built you need to reset the grid to a (0,0) scale
    public Coords offset(int amountX, int amountY)
    {
        return new Coords(this.x + amountX, this.y + amountY);
    }

    public void offsetThis(int amountX, int amountY)
    {
        this.x += amountX;
        this.y += amountY;
    }

    public Vector3Int asVector3Int()
    {
        return new Vector3Int(x, y, 0);
    }

    public Vector2Int asVector2Int()
    {
        return new Vector2Int(x, y);
    }

    public override string ToString()
    {
        return "(" + x + "," + y + ")";
    }
}