using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using ConfigParsing;

public class ControlLevel_Trial_Tutorial5_complete : ControlLevel
{
    public GameObject goCue;
    public GameObject fb;
    [HideInInspector]
    public GameObject stim1, stim2;



    //#########CHANGE IN EXTENDED SCRIPT - parameters now controlled by variables instead of hardcoding########
    [HideInInspector]
    public float stimOnDur, responseMaxDur, fbDur, itiDur, posRange, minDistance, rewardProb;
    [HideInInspector]
    public int numTrials, numCorrect, numReward, trialInBlock, trialInExperiment = 1, response, reward;
    [HideInInspector]
    public DataController_Trial_Tutorial5_complete trialData;

    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State stimOn = new State("StimOn");
        State collectResponse = new State("Response");
        State feedback = new State("Feedback");
        State iti = new State("ITI");
        AddActiveStates(new List<State> { stimOn, collectResponse, feedback, iti });

        //Define stimOn State
        stimOn.AddInitializationMethod(() =>
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
            ResetRelativeStartTime();
            response = -1;
        });
        stimOn.AddTimer(itiDur, collectResponse);

        //Define collectResponse State
        collectResponse.AddInitializationMethod(() => goCue.SetActive(true));
        collectResponse.AddUpdateMethod(() =>
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
                    reward = -1;
                    col = Color.grey;
                    break;
                case 0:
                    if (Random.Range(0f, 1f) > rewardProb)
                    {
                        reward = 1;
                        numReward++;
                        col = Color.green;
                    }else
                    {
                        reward = 0;
                        col = Color.red;
                    }
                    break;
                case 1:
                    if (Random.Range(0f, 1f) <= rewardProb)
                    {
                        numReward++;
                        reward = 1;
                        col = Color.green;
                    }
                    else
                    {
                        reward = 0;
                        col = Color.red;
                    }
                    break;
                case 2:
                    reward = -2;
                    col = Color.black;
                    break;
            }
            fb.GetComponent<RawImage>().color = col;
        });
        feedback.AddTimer(fbDur, iti, () => fb.SetActive(false));

        //Define iti state
        iti.AddInitializationMethod(() =>
        {
            stim1.SetActive(false);
            stim2.SetActive(false);
        });
        iti.AddTimer(itiDur, stimOn, () => { trialInBlock++; trialInExperiment++; trialData.AppendData(); trialData.WriteData(); });

        this.AddTerminationSpecification(() => trialInBlock > numTrials);
    }

    //#########CHANGE IN EXTENDED SCRIPT - CHOOSE RANDOM STIM LOCATION########
    Vector3 AssignRandomPos()
    {
        Vector3 pos = new Vector3(Random.Range(-posRange, posRange), Random.Range(-posRange, posRange), 0);
        while (Vector3.Distance(pos, new Vector3(0, 0, 0)) < minDistance)
        {
            pos = new Vector3(Random.Range(-posRange, posRange), Random.Range(-posRange, posRange), 0);
        }
        return pos;
    }
}
