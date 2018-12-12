using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;

public class ControlLevel_Main_Tutorial3_complete : ControlLevel {

    public GameObject textObj;
    public GameObject panelObj;

    public int numBlocks = 3;
    public int trialsPerBlock = 10;

    public override void DefineControlLevel(){
        State intro = new State("Intro");
        State mainTask = new State("MainTask");
        State goodbye = new State("Goodbye");
        AddActiveStates(new List<State> { intro, mainTask, goodbye });

        ControlLevel_TextSlides slideLevel = transform.GetComponent<ControlLevel_TextSlides>();
        ControlLevel_Block_Tutorial3_complete blockLevel = transform.GetComponent<ControlLevel_Block_Tutorial3_complete>();

        slideLevel.slideText = new string[] {"Welcome to our study!\nThank you very much for participating.",
            "In this task you will be shown two objects on each trial. You will have to choose one of them by clicking on it with the mouse.",
            "Wait for the \"Go!\" signal before clicking.", "After clicking, you will get feedback. A green square means your choice was rewarded. A red square means it was not.",
            "Try to learn which object gives the most reward.", "Ask the experimenter if you have any questions, otherwise we will begin the experiment."};

        intro.AddChildLevel(slideLevel);
        intro.SpecifyStateTermination(() => slideLevel.Terminated, mainTask);

        blockLevel.numBlocks = numBlocks;
        blockLevel.numTrials = trialsPerBlock;
        mainTask.AddChildLevel(blockLevel);
        mainTask.SpecifyStateTermination(() => blockLevel.Terminated, goodbye);

        goodbye.AddStateInitializationMethod(() =>
        {
            textObj.SetActive(true);
            panelObj.SetActive(true);
            textObj.GetComponent<Text>().text = "Thank you very much for your time!";
        });
        goodbye.AddTimer(2f, null);
    }
}
