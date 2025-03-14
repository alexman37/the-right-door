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

    private List<(float time, float xChange, float yChange)> moveInputs;
    private Coroutine movingCoroutine;

    KeyCode holdingKey = KeyCode.None;

    // Start is called before the first frame update
    void Start()
    {
        moveInputs = new List<(float time, float xChange, float yChange)>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            movingCoroutine = StartCoroutine(controlMovement());
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            StopCoroutine(movingCoroutine);
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
                    case KeyCode.D: yield return congaLineMovement(timeToMove1Tile, 1, 0, "walk_right"); break;
                    case KeyCode.A:  yield return congaLineMovement(timeToMove1Tile, -1, 0, "walk_left"); break;
                    case KeyCode.W:    yield return congaLineMovement(timeToMove1Tile, 0, 1, "walk_outward"); break;
                    case KeyCode.S:  yield return congaLineMovement(timeToMove1Tile, 0, -1, "walk_into"); break;
                }
            }
            yield return new WaitForSeconds(0.02f);
        }
    }

    // Change the direction this sprite is looking
    private void changeSpriteDirection(PlayerObject character, string direction)
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
        if (inBounds(wouldBeHere) && RoomGeneration.activeRoom.tileArray[wouldBeHere.x, wouldBeHere.y].walkable)
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
        else Debug.Log("I was stopped at " + wouldBeHere);

        
    }

    // Move the characters. Follow the leader.
    IEnumerator congaLineMovement(float time, float xC, float yC, string direction)
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
            if (q == playerObjects.Length) moveInputs.RemoveAt(playerObjects.Length); //test
            else
            {
                (_, float xChange, float yChange) = moveInputs[q];

                changeSpriteDirection(playerObjects[q], direction);
                StartCoroutine(moveOneOver(playerObjects[q], time, xChange, yChange));
            }
        }
        yield return new WaitForSeconds(time);
    }



    private bool inBounds(Coords wouldBeHere)
    {
        return wouldBeHere.x >= 0 && wouldBeHere.y >= 0 && wouldBeHere.x < RoomGeneration.activeRoom.roomWidth && wouldBeHere.y < RoomGeneration.activeRoom.roomHeight;
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