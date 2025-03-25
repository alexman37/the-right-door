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
    private static float[] RANDOM_VALUES = new float[100];
    private static int RANDOM_VALUE_INDEX = 0;
    public static int wallHeight = 3;
    public static int wallWidth = 1;

    ///  Regenerate the random values list.
    private static void regenRandomValues()
    {
        RANDOM_VALUE_INDEX = 0;
        for (int i = 0; i < 100; i++) RANDOM_VALUES[i] = Random.value;
    }

    /// Get the next random value
    public static float getRandomValue()
    {
        float randomValue = RANDOM_VALUES[RANDOM_VALUE_INDEX % 100];
        RANDOM_VALUE_INDEX++;
        return randomValue;
    }

    /// Get the next random value in an interger range- inclusive on both ends
    public static int getRandomRangeInt(int min, int max)
    {
        float randomValue = getRandomValue();
        return Mathf.FloorToInt(min + (int)((float)(max - min + 1) * randomValue));
    }

    //TODO adjust these arguments to be more general
    // Especially need a better way to get tiles from the tilemap.
    public async static Task<Room> generateNewRoom(int index, Coords criticalPoint, RG_SettingGen theSetting, Tilemap standardTilemap)
    {
        //TODO: Find some semi-consistent way to do this
        regenRandomValues();
        Room newRoom = null;
        await Task.Run(() =>
        {
            LayoutShape shapeChosen = LayoutShape.RECTANGLE;

            //newRoom = LayoutGeneration.randomSquare(criticalPoint, getRandomRangeInt(5,10), theSetting.tilemapCodex.baseFloor.tile);
            newRoom = LayoutGeneration.randomRect(criticalPoint, getRandomRangeInt(5, 10), getRandomRangeInt(5, 10), theSetting.tilemapCodex.baseFloor.tile);
            newRoom.roomNumber = index;
            //newRoom = LayoutGeneration.randomRightTriangle(criticalPoint, getRandomRangeInt(5,11), getRandomRangeInt(5,11), theSetting.tilemapCodex.baseFloor.tile);
            newRoom = theSetting.additionalLayoutGenSteps(newRoom);
            ObjectGenerationInput inputs = WallGeneration.defineWalls(newRoom, shapeChosen, theSetting);
            ObjectGeneration.fillWithObjects(newRoom, inputs, theSetting);
            DoorGeneration.addDoorsToRoom(newRoom, inputs, theSetting);
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
                tilemap.SetTile(tile.getRealPos(room.roomPosToRealPosXOffset, room.roomPosToRealPosYOffset), tile.tileFill);
                if(tile.roomObject != null && tile.objectBase)
                {
                    GameObject physical = GameObject.Instantiate(tile.roomObject.physicalObjectRef);
                    physical.transform.position = tile.getRealPos(room.roomPosToRealPosXOffset, room.roomPosToRealPosYOffset) + new Vector3(0.5f,0.5f);
                }
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
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill, RoomTileType.FLOOR);
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
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill, RoomTileType.FLOOR);
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
            //Triangle: y = slope * x + base_value, or (rise/run) * x + base_value
            for (int yy = settledMinY; yy < (int)(((float)settledHeight / (float)settledBase) * (float)(xx + 1 - settledMinX) + settledMinY); yy++)
            {
                // xx and yy are in REAL position.
                // But when we generate the tile array, they get converted over there into ROOM position.
                RoomTile tile = new RoomTile(true, new Coords(xx, yy), fill, RoomTileType.FLOOR);
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
    public static ObjectGenerationInput defineWalls(Room room, LayoutShape shape, RG_SettingGen theSetting)
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
                    if (room.tileArray[xx, yy + 1] == null)
                    {
                        inputs.adjFrontWall.Add(room.tileArray[xx, yy]);
                        for (int h = 1; h <= wallHeight; h++)
                        {
                            room.tileArray[xx, yy + h] = new RoomTile(false, new Coords(xx, yy + h), theSetting.tilemapCodex.frontWall.none, RoomTileType.FRONT_WALL);
                            // The first of its kind (lowest to ground) can potentially become a door tile.
                            if(h == 1) inputs.doorPoints.Add(room.tileArray[xx, yy + 1]);
                        }
                    }
                    //Both sides - mark this as SIDE WALL ADJACENT
                    // and the walls themselves are SIDE WALLS
                    if (room.tileArray[xx - 1, yy] == null)
                    {
                        inputs.adjSideWall.Add(room.tileArray[xx, yy]);
                        for (int w = 1; w <= wallWidth; w++)
                        {
                            room.tileArray[xx - w, yy] = new RoomTile(false, new Coords(xx - w, yy), theSetting.tilemapCodex.sideWall.tile, RoomTileType.SIDE_WALL);
                            if (w == 1) inputs.doorPoints.Add(room.tileArray[xx - 1, yy]);
                        }
                    }
                    if (room.tileArray[xx + 1, yy] == null)
                    {
                        inputs.adjSideWall.Add(room.tileArray[xx, yy]);
                        for (int w = 1; w <= wallWidth; w++)
                        {
                            room.tileArray[xx + w, yy] = new RoomTile(false, new Coords(xx + w, yy), theSetting.tilemapCodex.sideWall.tile, RoomTileType.SIDE_WALL);
                            if (w == 1) inputs.doorPoints.Add(room.tileArray[xx + 1, yy]);
                        }
                    }
                }
            }
        }

        // "Add borders" to the walls by simply changing their sprite.
        for (int xx = 0; xx < room.roomWidthWithWall; xx++)
        {
            for (int yy = 0; yy < room.roomHeightWithWall; yy++)
            {
                RoomTile tile = room.tileArray[xx, yy];
                if (tile != null && tile.type == RoomTileType.FRONT_WALL)
                {
                    int borderStatus = room.getBorderStatus(new Coords(xx, yy), (tile) => { return tile.type == RoomTileType.FRONT_WALL; });
                    tile.tileFill = theSetting.tilemapCodex.frontWall.getBorderedTile(borderStatus);
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
    public static Room fillWithObjects(Room room, ObjectGenerationInput inputs, RG_SettingGen setting)
    {
        // Generic generation: Add in the room objects specific to this setting.
        foreach(RoomObject roomObj in setting.settingRoomObjects)
        {
            List<RoomTile> allPossiblePlaces = aggregatePossiblePlaces(roomObj, inputs);
            // For each object, add it up to maxPossible times. Each potential occurance has same prob as before.
            for(int occ = 0; occ < roomObj.properties.maxPossible; occ++)
            {
                int attemptsRemaining = 5;
                TrySpawningObject:
                    if(attemptsRemaining == 0)
                    {
                        Debug.LogWarning("Failed to spawn this object- ran out of attempts!: " + roomObj.properties.objectName);
                        continue;
                    }
                    attemptsRemaining--;
                    if (RoomGeneration.getRandomValue() < roomObj.properties.probability)
                    {
                        //Spawn if conditions are met: first you have to find a location
                        if(allPossiblePlaces.Count == 0)
                        {
                            Debug.LogWarning("Ran out of places to put objects in this room! Could not create " + roomObj.properties.objectName);
                            continue;
                        }

                        //We clone the properties of the template here and replace them with our own
                        RoomObjectProperties newProperties = new RoomObjectProperties(roomObj.properties);
                        newProperties.physicalObjectRef = roomObj.physicalObjectRef;

                        RoomTile tile = allPossiblePlaces[RoomGeneration.getRandomRangeInt(0, allPossiblePlaces.Count - 1)];

                        //If this location looks good, then spawn the object there.
                        //Otherwise, try and spawn the object again
                        if(verifyLocation(tile, newProperties, room))
                        {
                            spawnRoomObject(newProperties, room, tile);
                            tile.objectBase = true;

                            allPossiblePlaces.Remove(tile);
                            foreach (Coords coords in newProperties.relativePositions)
                            {
                                Coords actualRelative = tile.tileCoords.offset(coords.x, coords.y);
                                allPossiblePlaces.Remove(room.tileArray[actualRelative.x, actualRelative.y]);
                            }
                        } else
                        {
                            goto TrySpawningObject;
                        }
                    }
                    //else break; ???
            }
        }

        //TODO be smarter about this.
        room.entryPoint = new Coords(2, 2);

        return room;
    }

    /// Make sure there is nothing at where this room object is being placed - or any of its relative positions
    private static bool verifyLocation(RoomTile tile, RoomObjectProperties props, Room room)
    {
        // Check for anything in the absolute or relative positions of this tile
        if (room.getRoomObjectAt(tile.tileCoords) == null)
        {
            foreach(Coords co in props.relativePositions)
            {
                Coords actualRelative = tile.tileCoords.offset(co.x, co.y);
                if (room.getRoomObjectAt(actualRelative) != null)
                {
                    return false;
                }
            }
            return true;
        }
        else return false;
    }

    // Takes steps to add the object to the room
    // Do not physically create it- that still happens at the end of everything, in build()
    private static void spawnRoomObject(RoomObjectProperties props, Room room, RoomTile tile)
    {
        props.absoluteCoords = tile.tileCoords;
        room.addRoomObject(props);
    }

    //TODO incorporate a set of roomObj places that are already taken
    private static List<RoomTile> aggregatePossiblePlaces(RoomObject obj, ObjectGenerationInput inputs)
    {
        List<RoomTile> theFullList = new List<RoomTile>();
        foreach(RoomObjectGenLocation loc in obj.properties.genLocation)
        {
            switch (loc)
            {
                case RoomObjectGenLocation.FRONT_WALL: theFullList.AddRange(inputs.frontWall); break;
                case RoomObjectGenLocation.ADJ_FRONT_WALL: theFullList.AddRange(inputs.adjFrontWall); break;
                case RoomObjectGenLocation.SIDE_WALL: theFullList.AddRange(inputs.sideWall); break;
                case RoomObjectGenLocation.ADJ_SIDE_WALL: theFullList.AddRange(inputs.adjSideWall); break;
                case RoomObjectGenLocation.BACK_WALL: theFullList.AddRange(inputs.backWall); break;
                case RoomObjectGenLocation.ADJ_BACK_WALL: theFullList.AddRange(inputs.adjBackWall); break;
                case RoomObjectGenLocation.DOOR_POINTS: theFullList.AddRange(inputs.doorPoints); break;
                case RoomObjectGenLocation.GENERAL_CENTER: theFullList.AddRange(inputs.generalCenter); break;
            }
        }
        theFullList.RemoveAll(tile => tile.roomObject != null);
        return theFullList;
    }
}


// Add doors to the room.
// TODO: This HEAVILY relies on the rules!!!
public static class DoorGeneration
{
    public static Room addDoorsToRoom(Room room, ObjectGenerationInput inputs, RG_SettingGen setting)
    {
        // Generic generation: Add in the room objects specific to this setting.
        int numDoors = RoomGeneration.getRandomRangeInt(3, 6);

        DoorObject doorInstance = setting.doorSprite[0]; //TODO: Whatd we need the list for again...?

        for (int d = 0; d < numDoors; d++)
        {
            int numAttempts = 0;

            TryAddingDoor:
            RoomTile tile = inputs.doorPoints[RoomGeneration.getRandomRangeInt(0, inputs.doorPoints.Count - 1)];
            if (numAttempts < 5 && !isClearPathway(tile, room))
            {
                numAttempts++;
                goto TryAddingDoor;
            } else if(numAttempts >= 5)
            {
                Debug.LogWarning("Could not add in door #" + d + ": ran out of attempts");
                continue;
            }

            spawnRoomObject(doorInstance, room, tile);
            tile.objectBase = true;

            inputs.doorPoints.Remove(tile);

            //Some doors may take up multiple spaces in the future...
            /*foreach (Coords coords in roomObj.relativePositions)
            {
                Coords actualRelative = tile.tileCoords.offset(coords.x, coords.y);
                allPossiblePlaces.Remove(room.tileArray[actualRelative.x, actualRelative.y]);
            }*/
        }

        return room;
    }

    /// Can players get to the door?
    /// NOTE: This is a lazy check right now, because we can reasonably assume room objects' locations will be somewhat predictable"
    private static bool isClearPathway(RoomTile tile, Room room)
    {
        switch(tile.type)
        {
            case RoomTileType.FLOOR:
                Coords co = tile.tileCoords;
                return tile.roomObject == null && (getPossibleObject(co.offset(1,0), room) == null ||
                    getPossibleObject(co.offset(-1, 0), room) == null ||
                    getPossibleObject(co.offset(0, -1), room) == null ||
                    getPossibleObject(co.offset(0, 1), room) == null);
            case RoomTileType.SIDE_WALL:
                Coords coA = tile.tileCoords;
                return tile.roomObject == null && getPossibleObject(coA.offset(1, 0), room) == null && getPossibleObject(coA.offset(-1, 0), room) == null;
            case RoomTileType.FRONT_WALL:
                Coords coB = tile.tileCoords;
                return tile.roomObject == null && getPossibleObject(coB.offset(0, -1), room) == null;
            // TODO Back walls - just the opposite of front walls.
            default:  return false;
        }
    }

    private static RoomObjectProperties getPossibleObject(Coords co, Room room)
    {
        if (room.getAtTileArray(co) == null) return null;
        else return room.tileArray[co.x, co.y].roomObject;
    }

    // Takes steps to add the object to the room
    // Do not physically create it- that still happens at the end of everything, in build()
    private static void spawnRoomObject(RoomObject obj, Room room, RoomTile tile)
    {
        RoomObjectProperties newProperties = new RoomObjectProperties(obj.properties);
        newProperties.physicalObjectRef = obj.physicalObjectRef;
        newProperties.absoluteCoords = tile.tileCoords;
        room.addRoomObject(newProperties);
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