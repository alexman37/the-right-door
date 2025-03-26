using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/* PlayerManager manages everything about the player of the game (who is controlling one of the 6 characters in the world at any given time.)
 * The script is generally the highest level of this, so it keeps track of what the player is doing and what they're allowed to do
 * Handles many things including:
 *   - Movement (disabled in dialogue, cutscenes, etc.)
 *   - Interaction with RoomObjects
 */
public class PlayerManager : MonoBehaviour
{
    //playerObject[0] is the character you are currently controlling
    public PlayerObject[] playerObjects;
    public ActiveCharacter activeChar;

    //Related to movement
    private List<(float time, float xChange, float yChange)> moveInputs;
    private Coroutine movingCoroutine; private bool isMoving;
    KeyCode holdingKey = KeyCode.None;

    // Start is called before the first frame update
    void Start()
    {
        RoomManager.currentRoomChanged += updateCurrentRoom;

        moveInputs = new List<(float time, float xChange, float yChange)>();
        startMovement();
    }

    // Update is called once per frame
    void Update()
    {
        //Interaction
        if(Input.GetKeyDown(KeyCode.E))
        {
            RoomObject maybeRoomObject = getInteractingWithEKeyObject();
            if(maybeRoomObject != null)
            {
                // If it's a door, open it!
                // Else, if this room object has a dialogue conversation, play it!
                if(maybeRoomObject is DoorObject)
                {
                    //TODO: Character consequences- here or DoorObject? Probably here.
                    (maybeRoomObject as DoorObject).openDoor();
                } else
                {
                    maybeRoomObject.deployDialogue();
                }
            }
        }

        if(Input.GetKey(KeyCode.D))
        {
            holdingKey = KeyCode.D;
        }
        if (Input.GetKey(KeyCode.A))
        {
            holdingKey = KeyCode.A;
        }
        if (Input.GetKey(KeyCode.W))
        {
            holdingKey = KeyCode.W;
        }
        if (Input.GetKey(KeyCode.S))
        {
            holdingKey = KeyCode.S;
        }
    }

    IEnumerator controlMovement()
    {
        float timeToMove1Tile = 0.2f;
        
        while (true)
        {
            if (holdingKey != KeyCode.None && !Input.GetKey(holdingKey))
            {
                holdingKey = KeyCode.None;
            }
            if (holdingKey != KeyCode.None)
            {
                switch (holdingKey)
                {
                    case KeyCode.D: yield return congaLineMovement(timeToMove1Tile, 1, 0, Direction.EAST); break;
                    case KeyCode.A:  yield return congaLineMovement(timeToMove1Tile, -1, 0, Direction.WEST); break;
                    case KeyCode.W:    yield return congaLineMovement(timeToMove1Tile, 0, 1, Direction.NORTH); break;
                    case KeyCode.S:  yield return congaLineMovement(timeToMove1Tile, 0, -1, Direction.SOUTH); break;
                }
            }
            yield return new WaitForSeconds(0.02f);
        }
    }

    // Change the direction this sprite is looking
    private void changeSpriteDirection(PlayerObject character, Direction direction)
    {
        character.changeSprite(direction, 0);
    }

    // Move a tile, and adjust animation frame as you go. Takes a certain amount of time
    IEnumerator moveOneOver(PlayerObject c, float timeToMove1Tile, float xChange, float yChange)
    {
        float steps = 30;
        float animationLoops = 1;
        int prevAnimation = 0;

        //If applicable...
        Coords wouldBeHere = c.currPosition.offset((int)xChange, (int)yChange);
        if (inBounds(wouldBeHere) && RoomManager.activeRoom.tileArray[wouldBeHere.x, wouldBeHere.y].walkable)
        {
            // Update their charPosition in the PlayerObject once and immediately
            c.currPosition.offsetThis((int)xChange, (int)yChange);
            for (int i = 0; i < steps; i++)
            {
                // Update their physical location in-game periodically
                c.characterObj.transform.position = c.characterObj.transform.position + new Vector3(1 / steps * xChange, 1 / steps * yChange, 0);

                //We end up using a total of 4 sprites in a loop: 0, 1, 0, 2...
                int nextIFrame = (int)(i / (steps / animationLoops / 4)) % 4;
                if (prevAnimation != nextIFrame)
                {
                    c.changeAnimationFrame(nextIFrame);
                }
                yield return new WaitForSeconds(timeToMove1Tile / steps);
            }
        }
        //else Debug.Log("I was stopped at " + wouldBeHere);

        
    }

    // Move the characters. Follow the leader.
    IEnumerator congaLineMovement(float time, float xC, float yC, Direction direction)
    {
        // Add to the front of moveInputs- shift everything else down!
        moveInputs.Add((time, xC, yC));
        for(int i = moveInputs.Count - 1; i > 0; i--)
        {
            (float, float, float) tmp = moveInputs[i - 1];
            moveInputs[i - 1] = moveInputs[i];
            moveInputs[i] = tmp;
        }
        for(int q = 0; q < moveInputs.Count; q++)
        {
            if (q == playerObjects.Length) moveInputs.RemoveAt(playerObjects.Length);
            else
            {
                (_, float xChange, float yChange) = moveInputs[q];

                changeSpriteDirection(playerObjects[q], direction);
                StartCoroutine(moveOneOver(playerObjects[q], time, xChange, yChange));
            }
        }
        yield return new WaitForSeconds(time);
    }

    /*Get the object you'd like to interact with by pressing the E key
     If you're standing on top of something, do that
     Otherwise, get the thing one grid space in front of you*/
    private RoomObject getInteractingWithEKeyObject()
    {
        RoomObject onTopOf;

        // Try getting on top of object first.
        onTopOf = RoomManager.activeRoom.getRoomObjectAt(playerObjects[0].currPosition);
        if (onTopOf != null) return onTopOf;

        // Else, look in front of you.
        // This may end up being on a "wall", or, one tile "out of bounds"
        switch(playerObjects[0].direction)
        {
            case Direction.NORTH:
                return RoomManager.activeRoom.getRoomObjectAt(playerObjects[0].currPosition.offset(0, 1));
            case Direction.SOUTH:
                return RoomManager.activeRoom.getRoomObjectAt(playerObjects[0].currPosition.offset(0, -1));
            case Direction.EAST:
                return RoomManager.activeRoom.getRoomObjectAt(playerObjects[0].currPosition.offset(1, 0));
            case Direction.WEST:
                return RoomManager.activeRoom.getRoomObjectAt(playerObjects[0].currPosition.offset(-1, 0));
            default: return null;
        }
    }


    private bool inBounds(Coords wouldBeHere)
    {
        return RoomManager.activeRoom.inBounds(wouldBeHere);
    }

    // Room transition - move all characters to next spot, and other things
    private void updateCurrentRoom(Room r)
    {
        Debug.Log("Changing room to..." + r);
        moveInputs.Clear();
        foreach (PlayerObject playerObj in playerObjects)
        {
            // Physical equality...screw unity, man
            playerObj.moveToPosition(new Coords(r.entryPoint.x, r.entryPoint.y), r);
        }
    }

    public void startMovement()
    {
        if(!isMoving)
        {
            isMoving = true;
            movingCoroutine = StartCoroutine(controlMovement());
        }
    }

    public void stopMovement()
    {
        if (isMoving)
        {
            isMoving = false;
            StopCoroutine(movingCoroutine);
        }
    }


}


public enum ActiveCharacter
{
    hazel,
    winter,
    cassidy,
    allison,
    damon,
    angelo
}