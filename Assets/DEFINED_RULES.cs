using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* When defining rules, specify in order:
 *    - ID                   -   unique identifier
 *    - displayName          -   "title" for the rule
 *    - displayDesc          -   description for the rule
 *    - displaySubDesc       -   additional description
 *    - roomGenApplication   -   call these functions at a certain phase in room gen
 *    - ruleBreak            -   watch out for this condition, if it's met, the rule is broken
 *    - whenRuleBroken       -   if rule is broken, call these functions
 */
public static class DEFINED_RULES
{

    // The Right Door Rule: When in doubt, just go in the rightmost door.
    public static Rule RightDoorRule = new Rule(
        "right_door",
        "The Right Door",
        "Only one door is the right one",
        "When all else fails, return to this rule",
        new List<RoomGenerationCallback>
        {
            new RoomGenerationCallback(
                GenerationPhase.LAYOUT_GEN, //TODO door gen!!!
                100,
                () =>
                {
                    Debug.Log("If I were generating doors, Id do something here");
                }
            )
        },
        new GameEvent(), //TODO
        new List<RuleBrokenCallback>
        {

        }
    );
}
