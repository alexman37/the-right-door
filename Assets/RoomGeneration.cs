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
    // Start is called before the first frame update
    public Tilemap standardTilemap;
    public Tile demoTile;

    void Start()
    {
        generateNewRoom();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void generateNewRoom()
    {
        RoomTile[,] tileArray = LayoutGeneration.randomSquare(12, demoTile);
        RoomBuilder.build(standardTilemap, tileArray);
    }
}

// This creates the tile map and adds in all the room's actual physical objects into the game
public static class RoomBuilder 
{
    public static void build(Tilemap tilemap, RoomTile[,] tileArray)
    {
        foreach(RoomTile tile in tileArray)
        {
            if(tile != null)
            {
                tilemap.SetTile(tile.tileCoords.realPos.asVector3Int(), tile.tileType);
            }
        }
    }
}

// LayoutGeneration determines the floor, and the overall "shape" of the room
// All generation methods should return a 2D array of the 
public static class LayoutGeneration
{
    // Generate a single square in the room centered around 0,0.
    public static RoomTile[,] randomSquare(int maxSize, Tile fill)
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
                RoomTile tile = new RoomTile(true, new RoomTileCoords(
                    new Coords(xx, yy),
                    new Coords(xx, yy) //TODO this is def gonna change
                ), fill);
                workingTiles.Add(tile);
            }
        }

        return generateTileArray(workingTiles, settledMin, settledMax, settledMin, settledMax);
    }

    public static void randomRect(int maxWidth, int maxHeight)
    {
        int minSize = 10;
    }

    // Generate the actual 2D array
    private static RoomTile[,] generateTileArray(List<RoomTile> tiles, int minX, int maxX, int minY, int maxY)
    {
        Debug.Log("The tiles go from " + minX + " to " + maxX);
        RoomTile[,] tileArray = new RoomTile[maxX - minX + 1, maxY - minY + 1];

        //The tile array needs to be created with 0,0 as the absolute min- so we need to "convert" all prior positions
        foreach(RoomTile tile in tiles)
        {
            tile.offsetRoomGrid(-1 * minX, -1 * minY);
            Coords thisTilesRoomGridPos = tile.tileCoords.roomGridPos;
            Debug.Log(tile);
            tileArray[thisTilesRoomGridPos.x, thisTilesRoomGridPos.y] = tile;
        }

        return tileArray;
        
    }
}

public class RoomTile
{
    public bool walkable;
    public RoomTileCoords tileCoords;
    public Tile tileType;
    public RoomTile(bool w, RoomTileCoords r, Tile t)
    {
        walkable = w;
        tileCoords = r;
        tileType = t;
    }

    public void offsetRoomGrid(int amountX, int amountY)
    {
        tileCoords.roomGridPos.offset(amountX, amountY);
    }

    public override string ToString()
    {
        return "(" + tileCoords.roomGridPos.x + "," + tileCoords.roomGridPos.y + ")";
    }
}

// Handles location of a room tile.
// "RoomGridPos" is based on the room grid, from 0,0 to the max
// "Real pos" is the actual position in the game world of this tile
public class RoomTileCoords
{
    public Coords roomGridPos;
    public Coords realPos;
    public RoomTileCoords(Coords room, Coords real)
    {
        roomGridPos = room;
        realPos = real;
    }
}

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
    public void offset(int amountX, int amountY)
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
}