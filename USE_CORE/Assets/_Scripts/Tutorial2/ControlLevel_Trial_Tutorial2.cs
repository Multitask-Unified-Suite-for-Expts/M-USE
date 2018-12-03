using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using State_Namespace;

public class ControlLevel_Trial_Tutorial2 : ControlLevel
{
    //scene elements
    //#########CHANGE IN EXTENDED SCRIPT - 2 STIMS########
    public GameObject stim1;
    public GameObject stim2;
    public GameObject goCue;
    public GameObject fb;

    //trial variables
    [System.NonSerialized]
    public int trialCount, response;

    //#########CHANGE IN EXTENDED SCRIPT - parameters now controlled by variables instead of hardcoding########
    [System.NonSerialized]
    public float stimOnDur = 1f, responseMaxDur = 5f, fbDur = 0.5f, itiDur = 0.5f, posRange = 3f, minDistance = 1.5f, rewardProb = 0.85f;
    [System.NonSerialized]
    public int numTrials, numCorrect;

    public override void DefineControlLevel()
    {

        //define States within this Control Level
        State stimOn = new State("StimPres");
        State collectResponse = new State("Response");
        State feedback = new State("Feedback");
        State iti = new State("ITI");
        AddActiveStates(new List<State> { stimOn, collectResponse, feedback, iti });

        //Define stimOn State
        stimOn.AddStateInitializationMethod(() =>
        {
            //#########CHANGE IN EXTENDED SCRIPT - CHANGE STIM LOCATION########
            //choose x/y position of first stim randomly, move second stim until it is far enough away that it doesn't overlap
            Vector3 stim1pos = AssignRandomPos();
            Vector3 stim2pos = AssignRandomPos();
            while (Vector3.Distance(stim1pos,stim2pos) < minDistance){
                stim2pos = AssignRandomPos();
            }
            stim1.transform.position = stim1pos;
            stim2.transform.position = stim2pos;
            stim1.SetActive(true);
            stim2.SetActive(true);

            response = -1;
        });
        stimOn.AddTimer(itiDur, collectResponse);

        //Define collectResponse State
        collectResponse.AddStateInitializationMethod(() => goCue.SetActive(true));
        collectResponse.AddStateUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    //#########CHANGE IN EXTENDED SCRIPT - tag-based target detection########
                    if (hit.collider.gameObject.tag == "Target")
                    {
                        response = 1;
                        numCorrect++;
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
        collectResponse.AddTimer(responseMaxDur, feedback);
        collectResponse.SpecifyStateTermination(() => response > -1, feedback);
        collectResponse.AddStateDefaultTerminationMethod(() => goCue.SetActive(false));

        //Define feedback State
        feedback.AddStateInitializationMethod(() =>
        {
            fb.SetActive(true);
            Color col = Color.white;
            switch (response)
            {
                case -1:
                    col = Color.grey;
                    break;
                case 0:
                    if (Random.Range(0f, 1f) > rewardProb)
                    {
                        col = Color.green;
                    }else
                    {
                        col = Color.red;
                    }
                    break;
                case 1:
                    if (Random.Range(0f, 1f) <= rewardProb)
                    {
                        col = Color.green;
                    }
                    else
                    {
                        col = Color.red;
                    }
                    break;
                case 2:
                    col = Color.black;
                    break;
            }
            fb.GetComponent<RawImage>().color = col;
        });
        feedback.AddTimer(fbDur, iti, () => fb.SetActive(false));

        //Define iti state
        iti.AddStateInitializationMethod(() =>
        {
            stim1.SetActive(false);
            stim2.SetActive(false);
        });
        iti.AddTimer(itiDur, stimOn, () => trialCount++);

        AddControlLevelTerminationSpecification(() => trialCount > numTrials);
    }

    //#########CHANGE IN EXTENDED SCRIPT - CHOOSE RANDOM STIM LOCATION########
    Vector3 AssignRandomPos()
    {
        return new Vector3(Random.Range(-posRange, posRange), Random.Range(-posRange, posRange), 0);
    }
}
