using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using State_Namespace;

public class CtrlLvl_Trial_Tutorial1_complete : ControlLevel
{
    //scene elements
    public GameObject trialStim;
    public GameObject goCue;
    public GameObject fb;

    private Camera cam;

    public int trialCount;

    private int response;


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


        stimOn.AddStateInitializationMethod(() =>
        {
            trialStim.SetActive(true);
            response = -1;
            trialCount++;
            Debug.Log("starting trial " + trialCount);
        });
        stimOn.AddTimer(1f, collectResponse);

        collectResponse.AddStateInitializationMethod(() => goCue.SetActive(true));
        collectResponse.AddStateUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject == trialStim)
                    {
                        response = 1;
                    }
                }
            }
        });
        collectResponse.AddTimer(5f, feedback);
        collectResponse.SpecifyStateTermination(() => response > -1, feedback);
        collectResponse.AddStateDefaultTerminationMethod(() => goCue.SetActive(false));

        feedback.AddStateInitializationMethod(() =>
        {
            fb.SetActive(true);
            Color col = Color.white;
            switch (response)
            {
                case -1:
                    col = Color.grey;
                    break;
                case 1:
                    col = Color.green;
                    break;
            }
            fb.GetComponent<RawImage>().color = col;
        });
        feedback.AddTimer(1f, iti, ()=>fb.SetActive(false));

        iti.AddStateInitializationMethod(() => trialStim.SetActive(false));
        iti.AddTimer(2f, stimOn);

        AddControlLevelTerminationSpecification(() => trialCount >= 5);
    }

}