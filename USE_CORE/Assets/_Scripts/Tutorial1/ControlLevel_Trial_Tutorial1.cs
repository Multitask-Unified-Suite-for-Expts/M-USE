using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using State_Namespace;

public class ControlLevel_Trial_Tutorial1 : ControlLevel
{
    //scene elements
    public GameObject trialStim;
    public GameObject goCue;
    public GameObject fb;

    //trial variables
    [System.NonSerialized]
    public int trialCount;
    [System.NonSerialized]
    public int response;

    public override void DefineControlLevel()
    {
        //initalize this Control Level
        InitializeControlLevel("CtrlLvl_Trial");

        //define States within this Control Level
        State stimOn = new State("StimPres");
        State collectResponse = new State("Response");
        State feedback = new State("Feedback");
        State iti = new State("ITI");
        AddActiveStates(new List<State> { stimOn, collectResponse, feedback, iti });

        //Define stimOn Stat
        stimOn.AddStateInitializationMethod(() =>
        {
            trialStim.SetActive(true);
            response = -1;
            trialCount++;
            Debug.Log("starting trial " + trialCount);
        });
        stimOn.AddTimer(1f, collectResponse);


    }
}
