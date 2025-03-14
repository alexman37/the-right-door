using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This file controls everything related to "Rules" of the game.
 * Rules are an ever increasing list of instructions the player must follow carefully.
 * Failure to follow the rules may result in a variety of adverse consequences.
 */
public class Rule
{
    public delegate void BasicFunction();

    string id;                             // Unique to every rule
    string displayName;                    // Title
    string displayDesc;                    // Explains the rule
    string displaySubDesc;                 // Additional explanation

    List<RoomGenerationCallback> roomGenerationApplication;     // What things to do during room generation
    GameEvent ruleBreak;                                        // If this condition is met, the rule is broken
    List<RuleBrokenCallback> whenRuleBroken;                    // What to do when the specified Event is hit (rule is broken)


    public Rule(string i, string dispName, string dispDesc, string dispSub, 
        List<RoomGenerationCallback> roomGenApps, GameEvent conditionBroken, List<RuleBrokenCallback> whenBroken)
    {
        id = i;
        displayName = dispName;
        displayDesc = dispDesc;
        displaySubDesc = dispSub;
        roomGenerationApplication = roomGenApps;
        ruleBreak = conditionBroken;
        whenRuleBroken = whenBroken;
    }

    // Add new RoomGenCallback
    public void defineNewGenerationRule(RoomGenerationCallback call)
    {
        roomGenerationApplication.Add(call);
    }

    // Add new RuleBrokenCallback
    public void defineNewRuleBreakAction(RuleBrokenCallback call)
    {
        whenRuleBroken.Add(call);
    }
}

// This dictates something you should do during generation when this rule is active.
public class RoomGenerationCallback
{
    public GenerationPhase phase; // What phase of room gen does it affect?
    public int priority;          // When? (lower = earlier)
    Rule.BasicFunction function;  // What do you actually do.

    public RoomGenerationCallback(GenerationPhase p, int pr, Rule.BasicFunction func)
    {
        phase = p;
        priority = pr;
        function = func;
    }
}

// This dictates something you should do during generation when this rule is active.
public class RuleBrokenCallback
{
    public int priority;          // When is it evaluated relative to other rules pertaining to this event? (lower = earlier)
    Rule.BasicFunction function;  // What do you actually do.
}

// A list of rules.
public class RuleSet
{
    private HashSet<Rule> ruleSet;

    public RuleSet()
    {
        ruleSet = new HashSet<Rule>();
    }

    public RuleSet(List<Rule> rules)
    {
        ruleSet = new HashSet<Rule>();
        rules.ForEach(rule => ruleSet.Add(rule));
    }

    public void addRule(Rule r)
    {
        ruleSet.Add(r);
    }

    
}

public enum GenerationPhase
{
    LAYOUT_GEN,
    OBJECT_GEN,
    DOOR_GEN
}