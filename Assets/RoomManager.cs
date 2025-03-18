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
    public Room[] loadedRooms;

    public static event Action<Room> currentRoomChanged;

    private static RoomManager instance;
    private static PlayerManager playerManager;
    private static Canvas fadeCanvas;
    private static Image fadeBlock;

    //TODO: REMOVE ALL OF THIS: It's just for testing.
    // Start is called before the first frame update
    public Tilemap standardTilemap;
    public Tile demoTile;
    public Tile demoTile2;
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
        RoomTile[,] startTileArray = new RoomTile[10, 6];
        for (int xx = -6; xx <= 3; xx++)
        {
            for (int yy = -3; yy <= 2; yy++)
            {
                startTileArray[xx + 6, yy + 3] = new RoomTile(true, new Coords(xx + 6, yy + 3), demoTile);
            }
        }
        Room starterRoom = new Room(new Coords(0,0), -6, -3, startTileArray, 10, 6);

        generateNewRoom(1, new Coords(200, 0), standardTilemap, demoTile, demoTile2);
        //This is sooooo illegal
        

        starterRoom.addRoomObject(test1);
        starterRoom.addRoomObject(test2);
        starterRoom.addRoomObject(test3);
        starterRoom.addRoomObject(test4);

        Debug.Log("Setting active room");
        activeRoom = starterRoom;
    }

    // Generate a new room in one of our load spots
    async void generateNewRoom(int index, Coords pos, Tilemap tl, Tile demo, Tile demo2)
    {
        //TODO clear out the old room!!!

        // Now generate the new room.
        loadedRooms[index] = await RoomGeneration.generateNewRoom(pos, tl, demo, demo2);

        //TODO
        test3.nextRoom = loadedRooms[1];
    }



    // Generate a new room at a designated location.
    // We should make this an async process, and we have to be smart about when we do it.

    public static void changeCurrentRoom(Room changeToThis)
    {
        playerManager.stopMovement();
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
    public Dictionary<Vector2Int, RoomObject> roomObjectsMap = new Dictionary<Vector2Int, RoomObject>();

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

    // Add this room object
    public void addRoomObject(RoomObject ro)
    {
        roomObjectsMap.Add(ro.absoluteCoords.asVector2Int(), ro);
        if (inBounds(ro.absoluteCoords))
        {
            tileArray[ro.absoluteCoords.x, ro.absoluteCoords.y].roomObject = ro;
            tileArray[ro.absoluteCoords.x, ro.absoluteCoords.y].walkable = false;
        }

        foreach (Coords c in ro.relativePositions)
        {
            roomObjectsMap.Add(c.asVector2Int(), ro);

            Coords other = ro.absoluteCoords.offset(c.x, c.y);
            if (inBounds(other))
            {
                tileArray[other.x, other.y].roomObject = ro;
                tileArray[other.x, other.y].walkable = false;
            }
        }
    }

    public bool inBounds(Coords wouldBeHere)
    {
        int trueStartOfRoomX = (roomWidthWithWall - roomWidth) / 2;
        return wouldBeHere.x >= trueStartOfRoomX && wouldBeHere.y >= 0 && wouldBeHere.x < trueStartOfRoomX + roomWidth && wouldBeHere.y < roomHeight;
    }

    //Only use this in wall generation
    public bool isWallEligible(Coords neighboring)
    {
        return !inBounds(neighboring) || tileArray[neighboring.x, neighboring.y] == null;
    }

    public RoomObject getRoomObjectAt(Coords position)
    {
        if (inBounds(position))
            return tileArray[position.x, position.y].roomObject;
        //Potentially, there is a room object lying out of bounds- such as a door.
        else
        {

            if (roomObjectsMap.ContainsKey(position.asVector2Int())) return roomObjectsMap[position.asVector2Int()];
            else return null;
        }
    }
}

// RoomTile tileCoords keeps track of "ROOM POSITION" not real position
// But there is an easy way to convert to real position, assuming you know which room this is actually a part of.
public class RoomTile
{
    public bool walkable;
    public Coords tileCoords;
    public Tile tileType;
    public RoomObject roomObject;
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