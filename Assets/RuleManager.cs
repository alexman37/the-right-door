using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuleManager : MonoBehaviour
{
    public RuleSet activeRules;

    // Start is called before the first frame update
    void Start()
    {
        //TODO: Maybe we have to wait for the class creation or something
        activeRules = new RuleSet();

        activeRules.addRule(DEFINED_RULES.RightDoorRule);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
