using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* A RoomObject is any object that characters can interact with or at least potentially acknowledge.
 * At a technical level, it's a "thing" in the room that is not the walls or floor.
 * The RoomObject itself represents the physical object in game...all its data is in the easily manipulable RoomObjectProperties
*/
public class RoomObject : MonoBehaviour
{
    [HideInInspector]
    public GameObject physicalObjectRef; // Needed bc of main thread BS

    public RoomObjectProperties properties;
    private static DialogueManager dialogueManager;

    private void Start()
    {
        dialogueManager = FindObjectsOfType<DialogueManager>()[0];
        physicalObjectRef = this.gameObject;
    }

    // If you are looking at an object and press E, you see its dialogue if there is any
    public void deployDialogue()
    {
        if (properties.dialogueFile != null) dialogueManager.processConversation(properties.dialogueFile);
    }

    /*private void OnMouseOver()
    {
        Debug.Log(mouseOverLabel);
    }*/

    /// Rotate this particular room object (by rotating all its relative positions).
    public void rotate(RotationOrder order)
    {
        /* Roation logic
         * LEFT:      Invert X, then flip X and Y.
         * RIGHT:     Invert Y, then flip X and Y.
         * HALFMOON:  Invert X and Y.
         */
        switch (order)
        {
            case RotationOrder.LEFT:
                foreach(Coords co in properties.relativePositions)
                {
                    co.x *= -1;
                    int temp = co.x;
                    co.x = co.y;
                    co.y = temp;
                }
                break;
            case RotationOrder.RIGHT:
                foreach (Coords co in properties.relativePositions)
                {
                    co.y *= -1;
                    int temp = co.x;
                    co.x = co.y;
                    co.y = temp;
                }
                break;
            case RotationOrder.HALFMOON:
                foreach (Coords co in properties.relativePositions)
                {
                    co.x *= -1;
                    co.y *= -1;
                }
                break;
        }
    }
}

/* RoomObjectProperties stores all the data of the RoomObject
 * When making a "new" of this RoomObject from a template, the first thing we wanna do is actually copy and modify its properties
 * This was necessary once we realized templates were using the old one's position if multiple were being created at once,
 * And they were all clobbering / fighting with each other...yadda yadda...
 */
[System.Serializable]
public class RoomObjectProperties
{
    //Properties:
    public string objectName;
    public TextAsset dialogueFile;
    public string mouseOverLabel;
    public Coords absoluteCoords; // Where in the world is this?
    public Coords[] relativePositions; // RELATIVE to absolute coords, the object also extends to these places

    //Physical Game Object Reference
    [HideInInspector]
    public GameObject physicalObjectRef;
    public RoomObject roomObjectRef;

    //Generation:
    public List<RoomObjectGenLocation> genLocation; // Where to spawn this object in random rooms
    public float probability; //0-1, with 1 being guaranteed to spawn
    public int maxPossible; // How many? (probability is calculated each time)

    // Default const.
    public RoomObjectProperties()
    {

    }

    // All room objects should already have their properties specified in the prefab, we'll just copy them over from there.
    public RoomObjectProperties(RoomObjectProperties template)
    {
        objectName = template.objectName;
        dialogueFile = template.dialogueFile;
        mouseOverLabel = template.mouseOverLabel;
        absoluteCoords = new Coords(template.absoluteCoords.x, template.absoluteCoords.y);
        relativePositions = template.relativePositions.Clone() as Coords[];
        genLocation = new List<RoomObjectGenLocation>(template.genLocation);
        probability = template.probability;
        maxPossible = template.maxPossible;
    }
}

public enum RoomObjectGenLocation
{
    FRONT_WALL,
    ADJ_FRONT_WALL,
    SIDE_WALL,
    ADJ_SIDE_WALL,
    BACK_WALL,
    ADJ_BACK_WALL,
    DOOR_POINTS,
    GENERAL_CENTER
}

public enum RotationOrder
{
    LEFT,
    RIGHT,
    HALFMOON
}