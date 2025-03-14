using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* A GameEvent is a broad term for anything that should be watched out for while the player is playing the game.
 * Some events include:
 *   - Standing still (or not)
 *   - Mouse clicking
 *   - Going to the menu
 *   - Chatting
 *   - Etc...
 * When specified, a GameEvent will be listened for constantly in an Update function running somewhere...probably the GameManager or PlayerManager.
 * You should be careful about when a GameEvent should be listened for, and when not.
 * 
 * The Condition is a Coroutine that is constantly checked for in some way.
 */
public class GameEvent
{
    public Coroutine conditionWatch;
}
