using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.EventSystems;

public class DialogueChoiceButton : MonoBehaviour, IPointerDownHandler
{
    public static event Action<string> dialogueChoiceMade;
    public string dialogueChoiceBlock;

    // Start is called before the first frame update
    void Start()
    {
        dialogueChoiceMade += (s) => { };
    }

    public void OnPointerDown(PointerEventData data)
    {
        Debug.Log("button pressed");
        dialogueChoiceMade.Invoke(dialogueChoiceBlock);
    }

    public void setButton(string opt, string disp)
    {
        dialogueChoiceBlock = opt;
        this.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = disp;
    }
}
