using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* A RoomObject is any object that characters can interact with or at least potentially acknowledge.
 * At a technical level, it's a "thing" in the room that is not the walls or floor.
*/
public class RoomObject : MonoBehaviour
{
    public TextAsset dialogueFile;
    public string mouseOverLabel;
    public Coords[] gridPositions;

    private DialogueManager dialogueManager;

    private void Start()
    {
        dialogueManager = FindObjectsOfType<DialogueManager>()[0];
    }

    // If you are looking at an object and press E, you see its dialogue if there is any
    private void deployDialogue()
    {
        if (dialogueFile != null) dialogueManager.processConversation(dialogueFile);
    }

    private void OnMouseOver()
    {
        Debug.Log(mouseOverLabel);
    }
}
