using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;
using TMPro;

/* DialogueManager is the class that actually goes about displaying dialogue on the screen.
 * 
 * It adjusts the dialogue setup/box as necessary, and draws/manages the character portrait.
 * It also handles visual effects such as Text type, the symbols, and emphasized letters.
 * 
 * For it to work, it needs a "DialogueSet" object that specifies all of this.
 * DialogueSets are obtained by parsing a dialogue text file, which is done by the DialogueParser.
*/

//Why not static?: DialogueManager has to communicate with UI objects.
public class DialogueManager : MonoBehaviour
{
    public GameObject dialogueContainer;
    Image dc_symbol;
    Image dc_portrait;
    TextMeshProUGUI dc_text;
    Button dc_choice;
    List<Button> new_choice_buttons = new List<Button>();

    DialogueSet currConversation = null;
    DialogueBlock block;
    string nextBlock = null;
    int entryInBlock = 0;

    bool finishedDialogueLine = false;

    // Needed to know whether or not we should do the quickslide
    string lastCharToSpeak = "";

    public static event Action anyClick;

    // PlayerManager needed to know when to stop movement / other interactions
    PlayerManager playerManager;

    private void Start()
    {
        // Get actions started
        anyClick += () => { };
        DialogueChoiceButton.dialogueChoiceMade += makeChoice;

        // Symbol, Portrait and Text fields are in dialogue container
        dc_symbol = dialogueContainer.transform.GetChild(0).GetComponent<Image>();
        dc_portrait = dialogueContainer.transform.GetChild(1).GetComponent<Image>();
        dc_text = dialogueContainer.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        dc_choice = dialogueContainer.transform.GetChild(3).GetComponent<Button>();

        playerManager = FindObjectsOfType<PlayerManager>()[0];
    }

    private void Update()
    {
        if(finishedDialogueLine && Input.GetMouseButtonDown(0))
        {
            finishedDialogueLine = false;
            anyClick.Invoke();
        }
    }

    // Start with the first line of dialogue in the "start" block
    // And set up the subscription for 
    public void processConversation(TextAsset txt)
    {
        if(currConversation == null)
        {
            DialogueSet dialogue = DialogueParser.parseDialogue(txt);
            currConversation = dialogue;
            block = currConversation.dialogueBlocks.Find(block => block.blockName == "start");

            // Stop movement
            playerManager.stopMovement();

            anyClick += advanceDialogue;
            startConversation();
        } else
        {
            Debug.LogError("Could not process conversation " + txt.name + " - a conversation is already in progress");
        }
    }

    // Either display the next line of dialogue in this block or go to the next block (or end the conversation)
    private void advanceDialogue()
    {
        dc_text.text = "";

        if(entryInBlock == block.entries.Count - 1)
        {
            if(nextBlock != null)
            {
                block = currConversation.dialogueBlocks.Find(block => block.blockName == nextBlock);
                if(block != null)
                {
                    entryInBlock = 0;
                    nextBlock = null;
                    StartCoroutine(display(block.entries[0]));
                    //TODO Immediately display choices
                    //if(block.entries[1] is DialogueChoice)
                    //{
                    //    advanceDialogue();
                    //}
                } else
                {
                    Debug.LogError("Abandoning conversation, no block named " + nextBlock + " was found");
                    endConversation();
                }
            } else
            {
                endConversation();
            }
        } else
        {
            StartCoroutine(display(block.entries[++entryInBlock]));
            // TODO Immediately display choices
            //if (block.entries[entryInBlock+1] is DialogueChoice)
            //{
            //    advanceDialogue();
            //}
        }
    }

    // Takes necessary visual actions to display a line of dialogue- such as set character portrait
    IEnumerator display(DialogueEntry entry)
    {
        if(entry is DialogueLine)
        {
            DialogueLine dl = entry as DialogueLine;
            // If there is a wait time associated with this line, then wait before deploying it
            if (dl.waitTime > 0)
            {
                dialogueContainer.SetActive(false);
                yield return new WaitForSeconds(dl.waitTime);
                dialogueContainer.SetActive(true);
            }

            // Set symbol, sprite, and text
            dc_symbol.sprite = getSymbolFromLine(dl);
            dc_portrait.sprite = getPortraitFromLine(dl);

            if(lastCharToSpeak != dl.character)
            {
                StartCoroutine(quickslide());
            }
            lastCharToSpeak = dl.character;

            yield return textType(dl.line);

            //You must wait a small amount of time before advancing to the next line of dialogue on clicks
            yield return new WaitForSeconds(0.5f);
            finishedDialogueLine = true;

        } else
        {
            DialogueChoice dc = entry as DialogueChoice;
            deployChoice(dc);
        }
        yield return null;
    }

    // Quickslide animation - characters quickly slide onto the screen when beginning a line IF not already there
    IEnumerator quickslide()
    {
        // Configure these to adjust how it looks
        float startingLeft = 200;
        float timeToTake = 0.05f;
        float steps = 5;

        float ratio = startingLeft / steps;
        dc_symbol.transform.position += new Vector3(-startingLeft, 0, 0);
        dc_portrait.transform.position += new Vector3(-startingLeft, 0, 0);
        for(int i = 0; i < steps; i++)
        {
            dc_symbol.transform.position += new Vector3(ratio, 0, 0);
            dc_portrait.transform.position += new Vector3(ratio, 0, 0);
            yield return new WaitForSeconds(timeToTake / steps);
        }
        yield return null;
    }

    // Text type animation
    IEnumerator textType(string line)
    {
        float timeBetweenTypes = 0.005f;
        string activeDirective = "";
        Dictionary<string, string> charHexCodes = new Dictionary<string, string>()
        {
            { "0", "#aaaaaa" }, //inner monologue
            { "1", "#418c49" }, //hazel
            { "2", "#007f7f" }, //winter
            { "3", "#ef5c00" }, //cassidy
            { "4", "#642f7c" }, //allison
            { "5", "#7f6b06" }, //damon
            { "6", "#992a2a" }, //angelo
            { "7", "#ffffff" }, //white
            { "8", "#000000" }, //black
            { "9", "#aa0000" }, //red
        };

        //TODO: Can only handle one directive at a time, per the moment
        char[] chars = line.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char curr = chars[i];
            if (curr == '\\')
            {
                char code = chars[i + 1];
                // Newline (Not a directive)
                if(code == 'n')
                {
                    dc_text.text = dc_text.text + '\n';
                }
                // Change color (refer to map)
                if (code > 47 && code < 58)
                {
                    dc_text.text = dc_text.text + "<color=" + charHexCodes[code.ToString()] + ">";
                    activeDirective = "color";
                }
                // Bold
                if (code == 'b')
                {
                    dc_text.text = dc_text.text + "<b>";
                    activeDirective = "b";
                }
                // Italic
                if (code == 'i')
                {
                    dc_text.text = dc_text.text + "<i>";
                    activeDirective = "i";
                }
                // Small
                if (code == 's')
                {
                    dc_text.text = dc_text.text + "<sub>";
                    activeDirective = "sub";
                }
                // End directive
                if (code == '!')
                {
                    dc_text.text = dc_text.text + "</" + activeDirective + ">";
                }
                i++;
            }
            else
            {
                dc_text.text = dc_text.text + curr;
            }
            yield return new WaitForSeconds(timeBetweenTypes);
            
        }

        yield return null;
    }

    // Put a choice onto the screen. Wait and see which choice the player picks. Then set it as "nextBlock"
    private void deployChoice(DialogueChoice dc)
    {
        anyClick -= advanceDialogue;

        int numChoices = dc.blocks.Count;
        float offset = 100;

        // First we have to put the choices onto the screen- this requires a bit of math
        float startPosY = dc_choice.image.rectTransform.localPosition.y;
        float adjustedStartPos = startPosY + ((offset / 2) * (numChoices - 1));

        for(int i = 0; i < numChoices; i++)
        {
            Button copy = GameObject.Instantiate(dc_choice, dialogueContainer.transform);

            // Set the button's position and values
            copy.image.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, adjustedStartPos - i * offset);
            copy.GetComponent<DialogueChoiceButton>().setButton(dc.blocks[i].opt, dc.blocks[i].disp);

            new_choice_buttons.Add(copy);
            copy.gameObject.SetActive(true);
        }
    }

    // When a choice is made, clear it from the screen and advance the conversation
    private void makeChoice(string choice)
    {
        Debug.Log("choice made");
        nextBlock = choice;
        foreach(Button b in new_choice_buttons)
        {
            Destroy(b.gameObject);
        }
        new_choice_buttons.Clear();

        finishedDialogueLine = true;
        anyClick += advanceDialogue;
    }




    // Begin a normal click-through conversation
    private void startConversation()
    {
        dialogueContainer.SetActive(true);
        StartCoroutine(display(block.entries[0]));
    }

    // End this conversation, return to gameplay
    private void endConversation()
    {
        anyClick -= advanceDialogue;
        currConversation = null;
        block = null;
        nextBlock = null;
        entryInBlock = 0;
        dialogueContainer.SetActive(false);

        // Restart movement again
        playerManager.startMovement();
    }








    // Get the proper symbol, based on this line of dialogue
    private Sprite getSymbolFromLine(DialogueLine line)
    {
        string symbolCode = "";
        switch (line.character.ToLower())
        {
            case "hazel": symbolCode = "bowtie"; break;
            case "winter": symbolCode = "snowflake"; break;
            case "cassidy": symbolCode = "heart"; break;
            case "allison": symbolCode = "spiral"; break;
            case "damon": symbolCode = "crown"; break;
            case "angelo": symbolCode = "star"; break;
            default: symbolCode = "empty"; break;
        }
        return getFromAssetBundle("symbols", symbolCode);
    }

    // Get the proper portrait, based on this line of dialogue's speaker AND their mood
    // TODO: If you can't find their mood for whatever reason just use their "default" portrait
    private Sprite getPortraitFromLine(DialogueLine line)
    {
        Sprite next = getFromAssetBundle(line.character.ToLower() + "_portraits", line.portrait.ToLower());
        return next;
    }






    // We make use of asset bundles to load the necessary stuff by name pretty quickly
    // Apparently you can do this async too. Maybe we can improve this later?
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
}








// DIVISIONS

/* DialogueSet objects are obtained through the DialogueParser.
 * A DialogueSet represents a single "conversation"
 * Conditionals have already been parsed out by this point- the conversation is well defined, except for any potential choices...
 * DialogueChoices determine the course of a conversation. Depending on what you answer, it will shift to the corresponding DialogueBlock.
*/
public class DialogueSet
{
    public List<DialogueBlock> dialogueBlocks;
    public string setName;

    public DialogueSet(List<DialogueBlock> blocks, string setName)
    {
        dialogueBlocks = blocks;
        this.setName = setName;
    }
}

/* A DialogueBlock is a subsection of a conversation, which may or may not be reached depending on the choices you make.
 * There is always a block named "start" and it's the first part of the conversation to play.
 * A DialogueBlock can have at most one choice in it- although you can have other lines of dialogue play after the choice.
 */
public class DialogueBlock
{
    public string blockName;
    public List<DialogueEntry> entries;

    public DialogueBlock(string bname, List<DialogueEntry> ls)
    {
        blockName = bname;
        entries = new List<DialogueEntry>(ls);
    }
}

// A DialogueEntry is either a spoken line, or a set of choices for the player.
public interface DialogueEntry { }

/* DialogueLines is a single thing said by a character in the text box.
  * They can be made up of multiple newlines.
  * MAX_DIALOGUE_LINES signifies how many newlines there can be at most.
  * 
  * A line will have a character speaking, a portrait for them, and a waittime.
  * 
  * The lines, themselves, are specified in something like Markdown:
     * Use double asterisks ** surrounding bold things
     * Use underscore _ surrounding italics
     * Use parenthesis ( ) surrounding inner thoughts
*/
public class DialogueLine : DialogueEntry
{
    public const int MAX_DIALOGUE_LINES = 3;

    public string character;
    public string portrait;
    public int waitTime;
    public string line = "";

    public DialogueLine(string c, string p, int w, string ls)
    {
        character = c;
        portrait = p;
        waitTime = w;
        if(0 <= MAX_DIALOGUE_LINES) // TODO: better length checking
        {
            line = ls;
        }
    }
}

/* A DialogueChoice represents when you, the player, are given the option to choose what someone says
 * It may impact the rest of the conversation from there on out.
 * So text files are structured in a way that specifies "if you choose this, go here and finish out the conversation."
 */
public class DialogueChoice : DialogueEntry
{
    public List<(string opt, string disp)> blocks; //names of blocks to go to, and how they appear in text.
    //code smell? IDGAF. nO more objects.

    public DialogueChoice(List<(string opt, string dist)> bs)
    {
        blocks = bs;
    }
}
