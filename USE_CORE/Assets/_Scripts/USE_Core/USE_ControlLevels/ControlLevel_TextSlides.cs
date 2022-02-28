using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;

public class ControlLevel_TextSlides : ControlLevel {

    public float blankDur = 0.2f;
    public GameObject textObj;
    public GameObject panelObj;
    public string defaultEndText = "\n\nPress the space bar to continue.";
    public KeyCode defaultKey = KeyCode.Space;
    public string[] slideText;
    private int slideCount = 0;

    public override void DefineControlLevel()
    {
        State textSlide = new State("TextSlide");
        State blankSlide = new State("BlankSlide");
        AddActiveStates(new List<State> { textSlide, blankSlide });

        this.AddInitializationMethod(() => { 
            panelObj.SetActive(true);
        });

        //display text
        textSlide.AddInitializationMethod(() =>
        {
        textObj.SetActive(true);
        textObj.GetComponent<Text>().text = slideText[slideCount] + defaultEndText;
            slideCount++;
        });
        textSlide.SpecifyTermination(() => InputBroker.GetKeyDown(defaultKey), blankSlide, ()=> textObj.SetActive(false));

        //blank slide to make slide change more obvious
        blankSlide.AddTimer(blankDur, textSlide, () => panelObj.SetActive(true));
        blankSlide.SpecifyTermination(() => slideCount == slideText.Length, ()=> null, () => panelObj.SetActive(false));

    }
}
