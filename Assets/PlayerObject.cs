using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


// Represents the physical object for this character
public class PlayerObject : MonoBehaviour
{
    public GameObject characterObj;
    public Coords currPosition;
    public Direction direction;

    public ActiveCharacter character;
    public int order;

    //Spawn the character in a new room. Reset some of their stats
    public void quoteOnQuoteInitialize(Coords startPos, int order)
    {
        currPosition = startPos;
        this.order = order;
    }

    public void changeSprite(Direction direction, int frame)
    {
        this.direction = direction;
        string directionCat = convertDirectionToSpriteCategory(direction);
        characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(directionCat, character.ToString());
        changeAnimationFrame(frame);
    }

    public void changeAnimationFrame(int frame)
    {
        string directionCat = convertDirectionToSpriteCategory(direction);
        switch (frame)
        {
            case 0:
            case 2:
                characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(directionCat, character.ToString());
                break;
            case 1:
                characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(directionCat, character.ToString() + "_1");
                break;
            case 3:
                characterObj.GetComponent<SpriteRenderer>().sprite = getFromAssetBundle(directionCat, character.ToString() + "_2");
                break;
        }
    }

    // Use this to change position of a character instantly- for example, bringing them to another room
    // Need to pass in both since we don't have the new room, so we can't convert.
    public void moveToPosition(Coords newRoomPosition, Room room)
    {
        currPosition = newRoomPosition;

        // Offset real position by (0.5,0.5) so it's in the middle of that tile
        characterObj.transform.position = room.convertRoomPosToRealPos(newRoomPosition) + new Vector2(0.5f, 0.5f);
    }

    // Asset bundles needed for sprite movement
    private Sprite getFromAssetBundle(string bundleName, string assetName)
    {
        AssetBundle localAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));

        if (localAssetBundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            return null;
        }

        Sprite asset = localAssetBundle.LoadAsset<Sprite>(assetName);
        localAssetBundle.Unload(false);
        return asset;
    }

    private string convertDirectionToSpriteCategory(Direction dir)
    {
        switch(dir)
        {
            case Direction.NORTH: return "walk_outward";
            case Direction.SOUTH: return "walk_into";
            case Direction.WEST: return "walk_left";
            case Direction.EAST: return "walk_right";
            default: return "walk_into";
        }
    }
}

public enum Direction
{
    NORTH, EAST, SOUTH, WEST
} 