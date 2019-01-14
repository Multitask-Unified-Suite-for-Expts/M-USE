using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;

public class ControlLevel_Main_Tutorial5_complete : ControlLevel {

    public GameObject textObj;
    public GameObject panelObj;

    public int numBlocks = 3;
    public int trialsPerBlock = 10;

    public string dataPath = "Macintosh HD/Users/marcus/Desktop/Data";
    public bool storeData = true;

    public override void DefineControlLevel(){
        State intro = new State("Intro");
        State mainTask = new State("MainTask");
        State goodbye = new State("Goodbye");
        AddActiveStates(new List<State> { intro, mainTask, goodbye });

        ControlLevel_TextSlides slideLevel = transform.GetComponent<ControlLevel_TextSlides>();
        ControlLevel_Block_Tutorial5_complete blockLevel = transform.GetComponent<ControlLevel_Block_Tutorial5_complete>();
        ControlLevel_Trial_Tutorial5_complete trialLevel = transform.GetComponent<ControlLevel_Trial_Tutorial5_complete>();

        DataController_Block_Tutorial5_complete blockData = GameObject.Find("DataControllers").GetComponent<DataController_Block_Tutorial5_complete>();
        DataController_Trial_Tutorial5_complete trialData = GameObject.Find("DataControllers").GetComponent<DataController_Trial_Tutorial5_complete>();
        blockData.storeData = storeData;
        trialData.storeData = storeData;
        blockData.folderPath = dataPath;
        trialData.folderPath = dataPath;
        blockData.fileName = "BlockData.txt";
        trialData.fileName = "TrialData.txt";

        blockLevel.blockData = blockData;
        trialLevel.trialData = trialData;

        slideLevel.slideText = new string[] {"Welcome to our study!\nThank you very much for participating.",
            "In this task you will be shown two objects on each trial. You will have to choose one of them by clicking on it with the mouse.",
            "Wait for the \"Go!\" signal before clicking.", "After clicking, you will get feedback. A green square means your choice was rewarded. A red square means it was not.",
            "Try to learn which object gives the most reward.", "Ask the experimenter if you have any questions, otherwise we will begin the experiment."};

        intro.AddChildLevel(slideLevel);
        intro.SpecifyTermination(() => slideLevel.Terminated, mainTask);

        blockLevel.numBlocks = numBlocks;
        blockLevel.numTrials = trialsPerBlock;
        mainTask.AddChildLevel(blockLevel);
        mainTask.SpecifyTermination(() => blockLevel.Terminated, goodbye);

        goodbye.AddInitializationMethod(() =>
        {
            textObj.SetActive(true);
            panelObj.SetActive(true);
            textObj.GetComponent<Text>().text = "Thank you very much for your time!";
        });
        goodbye.AddTimer(2f, null);
    }
}
