using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using State_Namespace;

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

        AddControlLevelInitializationMethod(() => { 
            panelObj.SetActive(true);
        });

        textSlide.AddStateInitializationMethod(() =>
        {
            textObj.SetActive(true);
            textObj.GetComponent<Text>().text = slideText[slideCount] + defaultEndText;
            slideCount++;
        });
        textSlide.SpecifyStateTermination(() => InputBroker.GetKeyDown(defaultKey), blankSlide, ()=> textObj.SetActive(false));

        blankSlide.AddTimer(blankDur, textSlide);
        blankSlide.SpecifyStateTermination(() => slideCount == slideText.Length, null, () => panelObj.SetActive(false));

    }
}
