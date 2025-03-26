using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.UI;

/*RoomManager essentially manages the "map" of the world
    What's tricky about it is that, as an optimization, we cycle through multiple different far-away locations for generating rooms
    This is so that we rarely have to actually switch the scene we're in, especially when going between different rooms in the same setting.
    So we have to keep track of all active rooms, as well as handle generation / cleanup of other ones asynchronously.
    This class DOES NOT handle the actual building/tiling of Rooms! That's done by RoomGeneration.
 */
public class RoomManager : MonoBehaviour
{
    const int MAX_ROOMS_LOADED = 9;
    public static Room activeRoom;
    public static Room[] loadedRooms;

    public static event Action<Room> currentRoomChanged;

    private static RoomManager instance;
    private static PlayerManager playerManager;
    private static Canvas fadeCanvas;
    private static Image fadeBlock;

    //TODO: REMOVE ALL OF THIS: It's just for testing.
    // Start is called before the first frame update
    public Tilemap standardTilemap;
    public RG_SettingGen theSetting;
    public RoomObject test1;
    public RoomObject test2;
    public DoorObject test3;
    public DoorObject test4;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        loadedRooms = new Room[MAX_ROOMS_LOADED];
        playerManager = FindObjectsOfType<PlayerManager>()[0];
        fadeCanvas = gameObject.transform.GetChild(0).GetComponent<Canvas>();
        fadeBlock = fadeCanvas.gameObject.transform.GetChild(0).GetComponent<Image>();

        // Get actions started
        currentRoomChanged += (Room r) => { };

        // Set up the STARTER ROOM
        // --> TODO This will probably get changed drastically
            RoomTile[,] startTileArray = new RoomTile[10, 6]; //THIS IS NOT ACCURATE TO THE REAL, INC. WALLS THING
            for (int xx = -6; xx <= 3; xx++)
            {
                for (int yy = -3; yy <= 2; yy++)
                {
                    startTileArray[xx + 6, yy + 3] = new RoomTile(true, new Coords(xx + 6, yy + 3), null, RoomTileType.FLOOR);
                }
            }
            Room starterRoom = new Room(new Coords(0,0), -6, -3, startTileArray, 10, 6);
            starterRoom.roomNumber = 0;

            for(int i = 1; i < MAX_ROOMS_LOADED; i++)
            {
                generateNewRoom(i, new Coords(200 * (i % Mathf.CeilToInt(Mathf.Sqrt(MAX_ROOMS_LOADED))), 
                    200 * (i / Mathf.CeilToInt(Mathf.Sqrt(MAX_ROOMS_LOADED)))), theSetting, standardTilemap);
            }
            //This is sooooo illegal


            starterRoom.addRoomObject(test1.properties);
            starterRoom.addRoomObject(test2.properties);
            starterRoom.addRoomObject(test3.properties);
            starterRoom.addRoomObject(test4.properties);
            test3.nextRoom = 1;

            Debug.Log("Setting active room");
            activeRoom = starterRoom;
    }

    // Generate a new room in one of our load spots
    async void generateNewRoom(int index, Coords pos, RG_SettingGen theSetting, Tilemap tl)
    {
        //TODO clear out the old room!!! (in changeCurrentRoom)

        // Now generate the new room.
        loadedRooms[index] = await RoomGeneration.generateNewRoom(index, pos, theSetting, tl);
    }



    // Generate a new room at a designated location.
    // We should make this an async process, and we have to be smart about when we do it.

    public static void changeCurrentRoom(int nextRoomIndex)
    {
        playerManager.stopMovement();
        Room changeToThis = loadedRooms[nextRoomIndex];

        //TODO unload the previous room here and generate a new one in its place
        activeRoom = changeToThis;

        //Fade transition
        instance.StartCoroutine(fadeTransition(1.5f, changeToThis));
    }

    private static IEnumerator fadeTransition(float timeToFade, Room changeToThis)
    {
        int steps = 20;
        fadeCanvas.gameObject.SetActive(true);
        for(int i = 0; i <= steps; i++)
        {
            fadeBlock.color = new Color(fadeBlock.color.r, fadeBlock.color.g, fadeBlock.color.b, (float) i / (float)steps);
            yield return new WaitForSeconds(timeToFade / steps);
        }

        //TODO: WAIT FOR NEXT ROOM TO BE LOADED
        // Call action when next room is ready- player position will change, among other things
        currentRoomChanged.Invoke(changeToThis);

        for (int i = 0; i <= steps; i++)
        {
            fadeBlock.color = new Color(fadeBlock.color.r, fadeBlock.color.g, fadeBlock.color.b, 1 - (float)i / (float)steps);
            yield return new WaitForSeconds(timeToFade / steps);
        }
        fadeCanvas.gameObject.SetActive(false);

        playerManager.startMovement();
    }
}





///
/// ROOMS, ROOM TILES, AND COORDINATE SYSTEM
/// 




// A Room is, well, a room. Its "tileArray" is a 2D array representing all "relevant" tiles, AKA walkable ones
// Some objects in this room can be outside of the tileArray bounds
public class Room
{
    public int roomNumber;
    public Coords criticalPoint; // Where room generation "begins." Could represent center, any corner, etc.
    public Coords entryPoint; // Where your characters spawn when entering this room - Vector2 since it's in real position, not room pos

    public int roomPosToRealPosXOffset;
    public int roomPosToRealPosYOffset;

    public RoomTile[,] tileArray;

    public int roomWidth;
    public int roomWidthWithWall;
    public int roomHeight;
    public int roomHeightWithWall;

    //Turns out Unity Dictionaries' ContainsKey() method only looks for Physical equality. Go figure...
    public Dictionary<Vector2Int, RoomObjectProperties> roomObjectsMap = new Dictionary<Vector2Int, RoomObjectProperties>();

    public Room(Coords crit, int xo, int yo, RoomTile[,] roomTiles, int trueWidth, int trueHeight)
    {
        criticalPoint = crit;
        roomPosToRealPosXOffset = xo;
        roomPosToRealPosYOffset = yo;
        this.tileArray = roomTiles; //TODO: Deep copy? is it a problem?
        roomWidth = trueWidth;
        roomWidthWithWall = roomTiles.GetLength(0);
        roomHeight = trueHeight;
        roomHeightWithWall = roomTiles.GetLength(1);
    }

    //Convert from Room position to real position
    public Vector2 convertRoomPosToRealPos(Coords roomPos)
    {
        return roomPos.offset(roomPosToRealPosXOffset, roomPosToRealPosYOffset).asVector2();
    }

    // Add this room object to the roomTile (if eligible). That means it can no longer be walked on
    public void addRoomObject(RoomObjectProperties ro)
    {
        Debug.Log("Adding object " + ro.objectName + " to " + ro.absoluteCoords.x + "," + ro.absoluteCoords.y + " in room " + roomNumber + " which is currently " + tileArray[ro.absoluteCoords.x, ro.absoluteCoords.y]);
        roomObjectsMap.Add(ro.absoluteCoords.asVector2Int(), ro);
        if (inBoundsIncludingWalls(ro.absoluteCoords))
        {
            tileArray[ro.absoluteCoords.x, ro.absoluteCoords.y].roomObjectProps = ro;
            tileArray[ro.absoluteCoords.x, ro.absoluteCoords.y].walkable = false;
        }

        foreach (Coords c in ro.relativePositions)
        {
            roomObjectsMap.Add(ro.absoluteCoords.offset(c.x, c.y).asVector2Int(), ro);

            Coords other = ro.absoluteCoords.offset(c.x, c.y);
            if (inBounds(other))
            {
                tileArray[other.x, other.y].roomObjectProps = ro;
                tileArray[other.x, other.y].walkable = false;
            }
        }
    }

    /// <summary>
    ///  Returns either the tile at coordinates (which may be null), or "null" if out of bounds entirely.
    /// </summary>
    public RoomTile getAtTileArray(Coords co)
    {
        if (!inBoundsIncludingWalls(co)) return null;
        else return tileArray[co.x, co.y];
    }

    // Is it "in bounds"?
    //TODO: I dont think we need this anymore...
    public bool inBounds(Coords wouldBeHere)
    {
        int trueStartOfRoomX = (roomWidthWithWall - roomWidth) / 2;
        return wouldBeHere.x >= trueStartOfRoomX && wouldBeHere.y >= 0 && wouldBeHere.x < trueStartOfRoomX + roomWidth && wouldBeHere.y < roomHeight;
    }

    // Note "bounds" is just a square that includes all floor tiles, all wall tiles, and potentially many nulls if the shape is irregular
    // This method is just for making sure we don't access outside of tileArray's indices.
    public bool inBoundsIncludingWalls(Coords wouldBeHere)
    {
        return wouldBeHere.x >= 0 && wouldBeHere.y >= 0 && wouldBeHere.x < roomWidthWithWall && wouldBeHere.y < roomHeightWithWall;
    }

    // Return room object at this position, if there is one. Check the tile first, and if nothing's there, check the roomObjects dictionary as well
    // Both checks should go pretty fast
    public RoomObject getRoomObjectAt(Coords position)
    {
        if (inBoundsIncludingWalls(position) && tileArray[position.x, position.y].roomObjectProps != null)
            return tileArray[position.x, position.y].roomObjectProps.roomObjectRef;
        //Potentially, there is a room object lying out of bounds- such as a door.
        else
        {
            if (roomObjectsMap.ContainsKey(position.asVector2Int())) return roomObjectsMap[position.asVector2Int()].roomObjectRef;
            else return null;
        }
    }

    // Returns the 4 adjacent "tiles" which could be null if they are out of bounds or simply not set
    public RoomTile[] getNeighbors(Coords coords)
    {
        return new RoomTile[] { 
            inBoundsIncludingWalls(coords.offset(1,0))  ? tileArray[coords.x + 1, coords.y] : null,
            inBoundsIncludingWalls(coords.offset(-1,0)) ? tileArray[coords.x - 1, coords.y] : null,
            inBoundsIncludingWalls(coords.offset(0,1))  ? tileArray[coords.x, coords.y + 1] : null,
            inBoundsIncludingWalls(coords.offset(0,-1)) ? tileArray[coords.x, coords.y - 1] : null
        };
    }

    // We must pass in a "true or false comparator" of some sort
    public delegate bool tileMatchesCheck(RoomTile tile);

    ///A very ugly method for getting the border status of a tile. Hide!
    public int getBorderStatus(Coords position, tileMatchesCheck comparator)
    {
        switch(getNumLikewiseNeighbors(position, comparator))
        {
            // 0: No likewise neighbors. make borders on all sides.
            // 4: All likewise neighbors. do nothing.
            case 0: return 15;
            case 4: return 0;
            case 1:
                if (isLikewiseNeighbor(position.offset(1, 0), comparator)) return 11;
                if (isLikewiseNeighbor(position.offset(0, 1), comparator)) return 12;
                if (isLikewiseNeighbor(position.offset(-1, 0), comparator)) return 13;
                else return 14;
            case 3:
                if (!isLikewiseNeighbor(position.offset(1, 0), comparator)) return 1;
                if (!isLikewiseNeighbor(position.offset(0, 1), comparator)) return 2;
                if (!isLikewiseNeighbor(position.offset(-1, 0), comparator)) return 3;
                else return 4;
            case 2:
                if (isLikewiseNeighbor(position.offset(-1, 0), comparator) && isLikewiseNeighbor(position.offset(0, -1), comparator)) return 5;
                if (isLikewiseNeighbor(position.offset(0, -1), comparator) && isLikewiseNeighbor(position.offset(1, 0), comparator)) return 6;
                if (isLikewiseNeighbor(position.offset(1, 0), comparator) && isLikewiseNeighbor(position.offset(0, 1), comparator)) return 7;
                if (isLikewiseNeighbor(position.offset(0, 1), comparator) && isLikewiseNeighbor(position.offset(-1, 0), comparator)) return 8;
                if (isLikewiseNeighbor(position.offset(0, 1), comparator) && isLikewiseNeighbor(position.offset(0, -1), comparator)) return 9;
                else return 10;
            default: return 0;
        }
    }

    /// How many tiles of a particular type does this tile border?
    public int getNumLikewiseNeighbors(Coords position, tileMatchesCheck comparator)
    {
        int count = 0;
        if (isLikewiseNeighbor(position.offset(1, 0), comparator)) count += 1;
        if (isLikewiseNeighbor(position.offset(-1, 0), comparator)) count += 1;
        if (isLikewiseNeighbor(position.offset(0, 1), comparator)) count += 1;
        if (isLikewiseNeighbor(position.offset(0, -1), comparator)) count += 1;
        return count;
    }

    /// Does the tile at position's type or fill match the provided criteria?
    private bool isLikewiseNeighbor(Coords position, tileMatchesCheck comparator)
    {
        return inBoundsIncludingWalls(position) && tileArray[position.x, position.y] != null && comparator(tileArray[position.x, position.y]);
    }
}

// RoomTile tileCoords keeps track of "ROOM POSITION" not real position
// But there is an easy way to convert to real position, assuming you know which room this is actually a part of.
public class RoomTile
{
    public bool walkable;
    public Coords tileCoords;
    public Tile tileFill;
    public RoomTileType type;
    public string genFlag; //a flag solely used in generation. could be anything

    public RoomObjectProperties roomObjectProps;
    public bool objectBase = false;

    public RoomTile(bool w, Coords c, Tile t, RoomTileType type)
    {
        walkable = w;
        tileCoords = c;
        tileFill = t;
        this.type = type;
    }

    public override string ToString()
    {
        return "(" + tileCoords.x + "," + tileCoords.y + ")";
    }

    public Vector3Int getRealPos(int roomPosToRealPosXOffset, int roomPosToRealPosYOffset)
    {
        return tileCoords.offset(roomPosToRealPosXOffset, roomPosToRealPosYOffset).asVector3Int();
    }

    public Coords[] getNeighborsCoords()
    {
        return new Coords[] { tileCoords.offset(1,0), tileCoords.offset(-1,0), tileCoords.offset(0,-1), tileCoords.offset(0,1) };
    }
}

public enum RoomTileType
{
    FLOOR,
    FRONT_WALL,
    SIDE_WALL
}

/* Our coordinate system is based in "room coords" and "real coords", where:
 * Room coords are oriented around 0,0 to cooperate with 2D arrays.
 * Real coords are the actual physical position in the game world.
 * It often will happen that you need to convert from one to the other, which is what the "offset" method can help with.
 * You'd store the constant x/y offset in the roomPosToRealPosOffset field of this Room.
*/
[System.Serializable]
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

    public Vector2 asVector2()
    {
        return new Vector2(x, y);
    }

    public override string ToString()
    {
        return "(" + x + "," + y + ")";
    }

    public override bool Equals(object obj)
    {
        Coords other = obj as Coords;
        return other.x == this.x && this.y == other.y;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}