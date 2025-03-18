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
    public static int wallHeight = 3;
    public static int wallWidth = 1;

    private static void regenRandomValues()
    {
        for (int i = 0; i < 10; i++) RANDOM_VALUES[i] = Random.value;
    }

    //TODO adjust these arguments to be more general
    // Especially need a better way to get tiles from the tilemap.
    public async static Task<Room> generateNewRoom(Coords criticalPoint, Tilemap standardTilemap, Tile demoTile1, Tile demoTile2)
    {
        //TODO: Find some semi-consistent way to do this
        regenRandomValues();
        Room newRoom = null;
        await Task.Run(() =>
        {
            LayoutShape shapeChosen = LayoutShape.RIGHT_TRIANGLE;

            //newRoom = LayoutGeneration.randomSquare(criticalPoint, (int)(RANDOM_VALUES[1] * 12 + 5), demoTile1); //TODO: Better tile choosing
            //newRoom = LayoutGeneration.randomRect(criticalPoint, (int)(RANDOM_VALUES[1] * 12 + 5), (int)(RANDOM_VALUES[2] * 12 + 5), demoTile1);
            newRoom = LayoutGeneration.randomRightTriangle(criticalPoint, (int)(RANDOM_VALUES[1] * 12 + 5), (int)(RANDOM_VALUES[1] * 12 + 5), demoTile1);
            ObjectGenerationInput inputs = WallGeneration.defineWalls(newRoom, shapeChosen, demoTile2);
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
        Debug.Log("Square size " + settledSize);
        int settledMinX = criticalPoint.x - settledSize / 2;
        int settledMaxX = criticalPoint.x + settledSize / 2;
        int settledMinY = criticalPoint.y - settledSize / 2;
        int settledMaxY = criticalPoint.y + settledSize / 2;

        List<RoomTile> workingTiles = new List<RoomTile>();

        for (int xx = settledMinX; xx < settledMaxX + 1; xx++)
        {
            for (int yy = settledMinY; yy < settledMaxY + 1; yy++)
            {
                // xx and yy are in REAL position.
                // But when we generate the tile array, they get converted over there into ROOM position.
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill);
                workingTiles.Add(tile);
            }
        }

        return generateTileArray(criticalPoint, workingTiles, settledMinX, settledMaxX, settledMinY, settledMaxY);
    }

    // Generate single rectangle centered around critical point
    public static Room randomRect(Coords criticalPoint, int settledWidth, int settledHeight, Tile fill)
    {
        int settledMinX = criticalPoint.x - settledWidth / 2;
        int settledMaxX = criticalPoint.x + settledWidth / 2;
        int settledMinY = criticalPoint.y - settledHeight / 2;
        int settledMaxY = criticalPoint.y + settledHeight / 2;

        List<RoomTile> workingTiles = new List<RoomTile>();

        for (int xx = settledMinX; xx < settledMaxX + 1; xx++)
        {
            for (int yy = settledMinY; yy < settledMaxY + 1; yy++)
            {
                // xx and yy are in REAL position.
                // But when we generate the tile array, they get converted over there into ROOM position.
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill);
                workingTiles.Add(tile);
            }
        }

        return generateTileArray(criticalPoint, workingTiles, settledMinX, settledMaxX, settledMinY, settledMaxY);
    }

    // Generate a single right triangle with the bottom left corner as the critical point
    //TODO how would we rotate this?
    public static Room randomRightTriangle(Coords criticalPoint, int settledBase, int settledHeight, Tile fill)
    {
        Debug.Log("Base " + settledBase + ", Height " + settledHeight);
        int settledMinX = criticalPoint.x - settledBase;
        int settledMaxX = criticalPoint.x + settledBase;
        int settledMinY = criticalPoint.y - settledHeight;
        int settledMaxY = criticalPoint.y + settledHeight;

        List<RoomTile> workingTiles = new List<RoomTile>();

        for (int xx = settledMinX; xx < settledMaxX + 1; xx++)
        {
            for (int yy = settledMinY; yy < (xx + 1 - settledMinX) + settledMinY; yy++)
            {
                // xx and yy are in REAL position.
                // But when we generate the tile array, they get converted over there into ROOM position.
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill);
                workingTiles.Add(tile);
            }
        }

        return generateTileArray(criticalPoint, workingTiles, settledMinX, settledMaxX, settledMinY, settledMaxY);
    }

    // Generate the actual 2D array which uses ROOM position
    // After this, the room is ready for the second stage of layout gen- wall and point mapping, or simply, "WallGeneration"
    private static Room generateTileArray(Coords criticalPoint, List<RoomTile> tiles, int minX, int maxX, int minY, int maxY)
    {
        // You must bake the walls into the RoomTile array- technically they will be tiles but also not really
        int trueWidth = maxX - minX + 1;
        int trueHeight = maxY - minY + 1;
        RoomTile[,] tileArray = new RoomTile[trueWidth + RoomGeneration.wallWidth * 2, trueHeight + RoomGeneration.wallHeight];

        //The tile array needs to be created with 0,0 as the absolute min- so we need to "convert" all prior positions
        foreach (RoomTile tile in tiles)
        {
            tile.tileCoords.offsetThis(-1 * minX + RoomGeneration.wallWidth, -1 * minY);
            tileArray[tile.tileCoords.x, tile.tileCoords.y] = tile;
        }

        Room createdRoom = new Room(criticalPoint, minX, minY, tileArray, trueWidth, trueHeight);
        return createdRoom;
    }
}

// WallGeneration makes the walls of this room, but also defines them and passes them on to ObjectGeneration
// We could have done this all in layout gen, and had it be unique to each shape, to improve efficiency...but...at HEAVY cost of readability
public static class WallGeneration
{
    public static ObjectGenerationInput defineWalls(Room room, LayoutShape shape, Tile demoTile)
    {
        ObjectGenerationInput inputs = new ObjectGenerationInput(room, shape);

        // First we get our bearings- where are the walls, the door points, the center, etc.
        int trueStartOfRoomX = (room.roomWidthWithWall - room.roomWidth) / 2;
        int wallWidth = trueStartOfRoomX;
        int wallHeight = room.roomHeightWithWall - room.roomHeight;
        for (int xx = trueStartOfRoomX; xx < room.roomWidth + wallWidth; xx++)
        {
            for(int yy = 0; yy < room.roomHeight; yy++)
            {
                //Only bother with the actual floor tiles that were generated in previous step
                if(room.tileArray[xx,yy] != null && room.tileArray[xx,yy].walkable)
                {
                    //If no tile above, this is FRONT WALL ADJACENT.
                    //Also, mark all non-existent tiles up to wallHeight above this as FRONT WALL tiles- you can't walk on them but we will draw them anyway
                    if (room.isWallEligible(new Coords(xx, yy + 1)))
                    {
                        Debug.Log("This is a front wall tile: " + new Coords(xx, yy + 1));
                        inputs.adjFrontWall.Add(room.tileArray[xx, yy]);
                        for (int h = 1; h <= wallHeight; h++)
                        {
                            room.tileArray[xx, yy + h] = new RoomTile(false, new Coords(xx, yy + h), demoTile);
                        }
                    }
                    //Both sides - mark this as SIDE WALL ADJACENT
                    // and the walls themselves are SIDE WALLS
                    if (room.isWallEligible(new Coords(xx - 1, yy)))
                    {
                        inputs.adjSideWall.Add(room.tileArray[xx, yy]);
                        for (int w = 1; w <= wallWidth; w++)
                        {
                            room.tileArray[xx - w, yy] = new RoomTile(false, new Coords(xx - w, yy), demoTile);
                        }
                    }
                    if (room.isWallEligible(new Coords(xx + 1, yy)))
                    {
                        inputs.adjSideWall.Add(room.tileArray[xx, yy]);
                        for (int w = 1; w <= wallWidth; w++)
                        {
                            room.tileArray[xx + w, yy] = new RoomTile(false, new Coords(xx + w, yy), demoTile);
                        }
                    }
                }
            }
        }

        return inputs;
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

        //TODO be smarter about this.
        room.entryPoint = new Coords(2, 2);

        return room;
    }
}









// This class is used by ObjectGeneration when determining where to place objects
public class ObjectGenerationInput {
    public Room room;
    public LayoutShape shape;
    public List<RoomTile> frontWall;
    public List<RoomTile> adjFrontWall;
    public List<RoomTile> sideWall;
    public List<RoomTile> adjSideWall;
    public List<RoomTile> backWall;
    public List<RoomTile> adjBackWall;
    public List<RoomTile> doorPoints;
    public List<RoomTile> generalCenter;

    public ObjectGenerationInput(Room room, LayoutShape shape)
    {
        this.room = room;
        frontWall = new List<RoomTile>();
        adjFrontWall = new List<RoomTile>();
        sideWall = new List<RoomTile>();
        adjSideWall = new List<RoomTile>();
        backWall = new List<RoomTile>();
        adjBackWall = new List<RoomTile>();
        doorPoints = new List<RoomTile>();
        generalCenter = new List<RoomTile>();
    }
}

public enum LayoutShape
{
    SQUARE,
    RECTANGLE,
    RIGHT_TRIANGLE,
}