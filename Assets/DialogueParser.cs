using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/* Dialogue Parser is a static class that parses dialogue files, and returns a "DialogueSet"- an object that can be used by the DialogueManager.
 * 
 * These files are divided into one or more "blocks", which are specified like so:
 *   /BLOCK=(name)
 *      ...
 *   /ENDBLOCK
 * Blocks may or may not be used depending on choices you make in a conversation.
 * There should always be a block named "start" and it will be the first to play.
 * 
 *  In blocks, lines of dialogue are formatted in the following way:
        CHAR(portrait)[wait]:line
     Where
        - "CHAR" is the character speaking - e.g. HAZEL, WINTER.
        - "portrait" is the name of one of their portraits: e.g. happy, armscrossed.
          - If a portrait of this name was not found, use a designated default and log an error.
        - "wait" is optional. If it's specified, the dialogue box will temporarily go away and then reappear after this many seconds.
          - Used to indicate a shift in conversation / topic.
        - "line" is the actual line of dialogue the character will say.
          - TODO: Additional formatting for this.


      CONDITIONALS
        Conditionals check for 'global' variables, such as a character's mood, or whether or not they are even alive.
        TODO

      CHOICES
        Choices are when the player is given the chance to say the next line.
        They are specified like so:
            /CHOICE
                (name):(display)
                ...
            /ENDCHOICE
        - Where name is the name of this choice (AND the name of the block it represents),
            - And display is how it appears to the player.
        - When the choice is hit in parsing it will immediately appear on screen.
            - Some other text may follow afterwards before going to the next block.
 */


public class DialogueParser
{
    //TODO: Parse conditionals.

    public static DialogueSet parseDialogue(TextAsset txtFile)
    {
        string[] raw = txtFile.text.Split('\n');
        string currBlock = null;
        List<DialogueBlock> dialogueBlocks = new List<DialogueBlock>();
        List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

        bool inChoice = false;
        List<(string opt, string disp)> choiceOptions = new List<(string opt, string disp)>();
        DialogueChoice choice = null;

        for(int i = 0; i < raw.Length; i++)
        {
            string currLine = raw[i];
            currLine = currLine.Trim();

            // Ignore comments.
            if(currLine.Length > 0 && currLine[0] != '#')
            {
                if (currLine.Contains("/BLOCK="))
                {
                    currBlock = currLine.Split('=')[1];
                }
                //Ignore everything before the first block.
                else if (currBlock != null)
                {
                    //Check for directives.
                    if (currLine.Contains("/ENDBLOCK"))
                    {
                        DialogueBlock dBlock = new DialogueBlock(currBlock, dialogueEntries);
                        dialogueBlocks.Add(dBlock);
                        dialogueEntries.Clear();
                        currBlock = null;
                    }

                    else if (currLine.Contains("/CHOICE"))
                    {
                        inChoice = true;
                    }

                    else if (currLine.Contains("/ENDCHOICE"))
                    {
                        inChoice = false;
                        choice = new DialogueChoice(new List<(string opt, string disp)>(choiceOptions));
                        dialogueEntries.Add(choice);
                        choiceOptions.Clear();
                    }

                    else if (currLine.Contains("/IF"))
                    {
                        //TODO
                    }

                    //Otherwise, this is an actual spoken line of dialogue.
                    else if(!inChoice)
                    {
                        //TODO Debug.Log("I see the line is " + currLine);
                        int idx1 = currLine.IndexOf('(');
                        int idx2 = currLine.IndexOf(')');
                        int idx3 = currLine.IndexOf('[');
                        int idx4 = currLine.IndexOf(']');
                        int idx5 = currLine.IndexOf(':');
                        string charSpeaking = currLine.Substring(0, idx1);
                        string charPortraitName = currLine.Substring(idx1 + 1, idx2 - idx1 - 1);
                        int charWaitTime = idx3 == -1 ? 0 : System.Int32.Parse(currLine.Substring(idx3 + 1, (idx4 - idx3) - 1));
                        string spoken = currLine.Substring(idx5 + 1);

                        DialogueLine newLine = new DialogueLine(charSpeaking, charPortraitName, charWaitTime, spoken);
                        dialogueEntries.Add(newLine);
                    }


                    // in choice check has to come after all the directives checking...
                    else
                    {
                        string[] choiceParse = currLine.Split(':');
                        if(choiceParse.Length > 1)
                        {
                            choiceOptions.Add((choiceParse[0], choiceParse[1]));
                        }
                    }
                }
            }

        }

        return new DialogueSet(dialogueBlocks, txtFile.name);
    }

}
