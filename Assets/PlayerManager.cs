using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/* PlayerManager manages everything about the player of the game (who is controlling one of the 6 characters in the world at any given time.)
 * The script is generally the highest level of this, so it keeps track of what the player is doing and what they're allowed to do
 * Handles many things including:
 *   - Movement (disabled in dialogue, cutscenes, etc.)
 *   - etc...
 */
public class PlayerManager : MonoBehaviour
{
    //playerObject[0] is the character you are currently controlling
    public PlayerObject[] playerObjects;
    public ActiveCharacter activeChar;

    private List<(float time, float xChange, float yChange)> moveInputs;

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
            StartCoroutine(controlMovement());
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            StopCoroutine(controlMovement());
        }
    }

    IEnumerator controlMovement()
    {
        float timeToMove1Tile = 0.3f;
        

        while (true)
        {
            if(Input.GetKey(KeyCode.D))
            {
                yield return congaLineMovement(timeToMove1Tile, 1, 0, "walk_right");
            }
            else if (Input.GetKey(KeyCode.A))
            {
                yield return congaLineMovement(timeToMove1Tile, -1, 0, "walk_left");
            }
            else if (Input.GetKey(KeyCode.W))
            {
                yield return congaLineMovement(timeToMove1Tile, 0, 1, "walk_outward");
            }
            else if (Input.GetKey(KeyCode.S))
            {
                yield return congaLineMovement(timeToMove1Tile, 0, -1, "walk_into");
            }
            yield return new WaitForSeconds(0.01f);
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
        float steps = 60;
        float animationLoops = 1;
        int prevAnimation = 0;
        for (int i = 0; i < steps; i++)
        {
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