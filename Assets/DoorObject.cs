using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// A door object is like a RoomObject, but a door.
/// It is either the lone "correct" door of the room, or an incorrect door.
/// Opening the correct door carries you over to the next room.
/// Opening the wrong door means the character who did must suffer consequences.
public class DoorObject : RoomObject
{
    public bool correct;
    private Animator animator;
    public int nextRoom;
    public WrongDoorConsequences consequences;

    // Start is called before the first frame update
    void Start()
    {
        animator = this.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // If correct, go to next room
    // If incorrect, suffer the consequences
    public void openDoor()
    {
        if (correct)
        {
            animator.SetTrigger("Opened");

            // RoomManager handles the fade transition
            RoomManager.changeCurrentRoom(nextRoom);
        } else
        {
            //TODO suffer consequences
            Debug.Log("You done goofed");
        }
    }
}

/// What happens when you open the wrong door.
/// Examples: Death, mood change, brought into trial room, injury (status effect), etc...
public class WrongDoorConsequences
{

}