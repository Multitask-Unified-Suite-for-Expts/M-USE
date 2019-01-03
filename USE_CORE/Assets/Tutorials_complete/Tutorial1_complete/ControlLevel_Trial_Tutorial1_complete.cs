using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;

public class ControlLevel_Trial_Tutorial1_complete : ControlLevel
{
    //scene elements
    public GameObject trialStim;
    public GameObject goCue;
    public GameObject fb;

    //trial variables
    [System.NonSerialized]
    public int trialCount = 1;
    [System.NonSerialized]
    public int response = -1;

    public override void DefineControlLevel()
    {

        //define States within this Control Level
        State stimOn = new State("StimPres");
        State collectResponse = new State("Response");
        State feedback = new State("Feedback");
        State iti = new State("ITI");
        AddActiveStates(new List<State> { stimOn, collectResponse, feedback, iti });

        //Define stimOn State
        stimOn.AddInitializationMethod(() =>
        {
            trialStim.SetActive(true);
            response = -1;
            Debug.Log("Starting trial " + trialCount);
        });
        stimOn.AddTimer(1f, collectResponse);

        //Define collectResponse State
        collectResponse.AddInitializationMethod(() =>
        {
            goCue.GetComponent<Text>().text = "Rargh!";
            goCue.SetActive(true);
        });
        collectResponse.AddUpdateMethod(() =>
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
                    else
                    {
                        response = 0;
                    }
                }
                else
                {
                    response = 2;
                }
            }
        });
        collectResponse.AddTimer(5f, feedback);
        collectResponse.SpecifyTermination(() => response > -1, feedback);
        collectResponse.AddDefaultTerminationMethod(() => goCue.SetActive(false));

        //Define feedback State
        feedback.AddInitializationMethod(() =>
        {
            fb.SetActive(true);
            Color col = Color.white;
            switch (response)
            {
                case -1:
                    col = Color.grey;
                    break;
                case 0:
                    col = Color.red;
                    break;
                case 1:
                    col = Color.green;
                    break;
                case 2:
                    col = Color.black;
                    break;
            }
            fb.GetComponent<RawImage>().color = col;
        });
        feedback.AddTimer(1f, iti, () => fb.SetActive(false));

        //Define iti state
        iti.AddInitializationMethod(() => trialStim.SetActive(false));
        iti.AddTimer(2f, stimOn, () => trialCount++);

        this.AddTerminationSpecification(() => trialCount >= 5);
    }
}
