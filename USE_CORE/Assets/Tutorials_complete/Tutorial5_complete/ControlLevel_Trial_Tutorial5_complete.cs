using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using ConfigDynamicUI;

public class ControlLevel_Trial_Tutorial5_complete : ControlLevel
{
    //scene elements
    //#########CHANGE IN EXTENDED SCRIPT - 2 STIMS########
    public GameObject stim1;
    public GameObject stim2;
    public GameObject goCue;
    public GameObject fb;

    //trial variables
    [System.NonSerialized]
    public int trialCount = 0, response, trialInExpt = 0, rewardedTrials = 0;


    //#########CHANGE IN EXTENDED SCRIPT - parameters now controlled by variables instead of hardcoding########
    [System.NonSerialized]
    public float stimOnDur = 1f, responseMaxDur = 5f, fbDur = 0.5f, itiDur = 0.5f;
    
    [HideInInspector]
    [System.NonSerialized]
    public ConfigNumber rewardProb, minDistance;
    [HideInInspector]
    [System.NonSerialized]
    public ConfigNumberRangedInt posRange;

    [System.NonSerialized]
    public int numTrials, numCorrect;

    public ConfigUI configUI;

    public event System.Action OnTrialFinished;
    private void initConfigVariables(){
        posRange = configUI.CreateNumberRangedInt("Position Range", 2, 4);
        minDistance = configUI.CreateNumber("Min Distance", 1.5f).SetMin(1).SetMax(3);
        rewardProb = configUI.CreateNumber("Reward Probability", .85f).SetMin(.6f).SetMax(1);
        configUI.GenerateUI();
    }

    public override void DefineControlLevel()
    {

        initConfigVariables();

        //define States within this Control Level
        State stimOn = new State("StimPres");
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

            while (Vector3.Distance(stim1pos,stim2pos) < minDistance.value){
                stim2pos = AssignRandomPos();
            }
            stim1.transform.position = stim1pos;
            stim2.transform.position = stim2pos;
            stim1.SetActive(true);
            stim2.SetActive(true);

            response = -1;
        });
        stimOn.AddTimer(stimOnDur, collectResponse);

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
                    col = Color.grey;
                    break;
                case 0:
                    if (Random.Range(0f, 1f) > rewardProb.value)
                    {
                        col = Color.green;
                        rewardedTrials++;
                    }else
                    {
                        col = Color.red;
                    }
                    break;
                case 1:
                    if (Random.Range(0f, 1f) <= rewardProb.value)
                    {
                        col = Color.green;
                        rewardedTrials++;
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
        iti.AddInitializationMethod(() =>
        {
            stim1.SetActive(false);
            stim2.SetActive(false);
        });
        iti.AddTimer(itiDur, stimOn, () => {
                trialCount++; 
                trialInExpt++;            
                OnTrialFinished.Invoke();
            });

        this.AddTerminationSpecification(() => trialCount >= numTrials);
    }

    //#########CHANGE IN EXTENDED SCRIPT - CHOOSE RANDOM STIM LOCATION########
    Vector3 AssignRandomPos()
    {
        posRange.SetRandomValue();
        return new Vector3(Random.Range(-posRange.value, posRange.value), Random.Range(-posRange.value, posRange.value), 0);
        // return new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0);
    }
}
