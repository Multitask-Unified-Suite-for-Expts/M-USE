/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using ConfigParsing;

[System.Serializable]
public class Stim{
    [System.NonSerialized]
    public GameObject gameObject;

    // public const int num_of_colors = 2;
    // public const int num_of_shapes = 2;
    // public const int num_of_locations = 2;

    // public int color_value = 0;
    // public int shape_value = 0;
    // public int location_value = 0;

    public int[] featureValues;

    public bool isTarget = false;

    public Stim(int[] featureValues){
        this.featureValues = featureValues;
    }

    public void SetFeatureValue(int dim, int value){
        this.featureValues[dim] = value;
    }

    // public Stim(int shape, int color, int location, GameObject g){
    //     this.SetShape(shape);
    //     this.SetColor(color);
    //     this.SetLocation(location);
    //     this.gameObject = g;
    // }

    // public void SetShape(int value){
    //     this.shape_value = value;
    // }
    // public void SetColor(int value){
    //     this.color_value = value;
    // }
    // public void SetLocation(int value){
    //     this.location_value = value;
    // }

}

public class ControlLevel_Trial_Tutorial6_complete : ControlLevel
{
    public GameObject goCue;
    public GameObject fb;
    public GameObject sphere, cube;


    // Task Config
    public int numDimensions = 3;
    public int numFeatureValuesPerDimension = 2;


    /////////////
    public Stim[] stims;

    public Color[] ColorFeatureValues = {Color.red, Color.green};
    public Transform[] LocationFeatureValues = new Transform[2]; 

    public int targetDimension = 0;
    public int targetFeature = 0;

    public int targetAction = 0;
    public int targetStim = 0;


    //#########CHANGE IN EXTENDED SCRIPT - parameters now controlled by variables instead of hardcoding########
    [HideInInspector]
    public float responseMaxDur, fbDur, itiDur, rewardProb;
    [HideInInspector]
    public int numTrials, numCorrect, numReward, trialInBlock, trialInExperiment = 1, response, reward;
    [HideInInspector]
    public DataController_Trial_Tutorial6_complete trialData;

    public System.Action OnStartTrial, OnGoPeriod;
    public System.Action<int> OnReward, OnAbortTrial;
    public System.Action<bool> OnTrialEnd;

    public bool startTrial;

    public override void DefineControlLevel()
    {

        // this.stims[0] = new Stim(0,0,0, this.sphere);
        // this.stims[1] = new Stim(1,1,1, this.cube);



        //define States within this Control Level
        State stimOn = new State("StimOn");
        State waitToStartTrial = new State("StartTrial");
        State collectResponse = new State("Response");
        State feedback = new State("Feedback");
        State iti = new State("ITI");
        AddActiveStates(new List<State> { stimOn, waitToStartTrial, collectResponse, feedback, iti });

        //Define stimOn State
        stimOn.AddInitializationMethod(() =>
        {
            Debug.Log("New trial started");
            //#########CHANGE IN EXTENDED SCRIPT - CHANGE STIM LOCATION########
            //choose x/y position of first stim randomly, move second stim until it is far enough away that it doesn't overlap
            // Vector3 stim1pos = AssignRandomPos();
            // Vector3 stim2pos = AssignRandomPos();
            // while (Vector3.Distance(stim1pos,stim2pos) < minDistance){
            //     stim2pos = AssignRandomPos();
            // }
            // stim1.transform.position = stim1pos;
            // stim2.transform.position = stim2pos;

            AssignFeaturesToStimuli();
            sphere.SetActive(true);
            cube.SetActive(true);
            ResetRelativeStartTime();
            response = -1;
            if(OnStartTrial != null)
                OnStartTrial.Invoke();
        });
        stimOn.AddTimer(itiDur, waitToStartTrial);

        waitToStartTrial.SpecifyTermination(() => startTrial, collectResponse);

        //Define collectResponse State
        collectResponse.AddInitializationMethod(() => {
                goCue.SetActive(true);
                if(OnGoPeriod != null)
                    OnGoPeriod.Invoke();
            });
        collectResponse.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                Debug.Log("mouse down");
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
                    reward = 0;
                    col = Color.black;
                    break;
            }
            fb.GetComponent<RawImage>().color = col;
            Debug.Log("reward:" + reward);
            if(reward == -1){
                Debug.Log("calling OnAbortTrial:" + OnAbortTrial);
                if(OnAbortTrial != null)
                    OnAbortTrial.Invoke(-1);
            }
            else{
                Debug.Log("calling OnReward:" + OnReward);
                if(OnReward != null)
                    OnReward.Invoke(reward);
            }
        });
        feedback.AddTimer(fbDur, iti, () => fb.SetActive(false));

        //Define iti state
        iti.AddInitializationMethod(() =>
        {
            sphere.SetActive(false);
            cube.SetActive(false);
        });
        iti.AddTimer(itiDur, stimOn, () => { 
            trialInBlock++; 
            trialInExperiment++; 
            trialData.AppendData(); 
            trialData.WriteData(); 
            if(OnTrialEnd != null)
                OnTrialEnd.Invoke(trialInBlock > numTrials);
        });

        this.AddTerminationSpecification(() => trialInBlock > numTrials);
    }

    public void SetTargetFeature(){
        // Debug.Log("setting target feature");
        // dimensions: 0 - shape, 1 - color, 2 - location
        this.targetDimension = Random.Range(0, 3);
        this.targetFeature = Random.Range(0, 2);
    }

    void AssignFeaturesToStimuli()
    {
        // Debug.Log("AssignFeaturesToStimuli");
        // assign the features randomly
        stims = new Stim[2];
        stims[0] = new Stim(new int[this.numDimensions]);
        stims[1] = new Stim(new int[this.numDimensions]);
        var first_stim = Random.Range(0,2);
        var second_stim = 1 - first_stim;
        for(int dim = 0; dim < this.numDimensions; dim++){
            int random_feature = Random.Range(0, this.numFeatureValuesPerDimension);
            stims[first_stim].SetFeatureValue(dim, random_feature);
            if (this.targetDimension == dim){
                if(random_feature != this.targetFeature){
                    stims[second_stim].SetFeatureValue(dim, this.targetFeature);
                    stims[second_stim].isTarget = true;
                    targetStim = 1;
                }else{
                    while(true){
                        random_feature = Random.Range(0, this.numFeatureValuesPerDimension);
                        if (random_feature != this.targetFeature)
                            break;
                    }
                    stims[second_stim].SetFeatureValue(dim, random_feature);
                    stims[first_stim].isTarget = true;   
                    targetStim = 0;
                }
            }
            else{
                while(true){
                    var r = Random.Range(0, this.numFeatureValuesPerDimension);
                    if (random_feature != r){
                        random_feature = r;
                        break;
                    }
                }
                stims[second_stim].SetFeatureValue(dim, random_feature);
            }
        }

        // Debug.Log("setting physical properties of stims");
        // set the stimuli gameobjects' properties according to the features assigned
        foreach(var stim in stims){
            // shape: 0 - sphere, 1 - cube
            if(stim.featureValues[0] == 0)
                stim.gameObject = sphere;
            else
                stim.gameObject = cube;
            
            // color
            // Debug.Log("color:" + stim.featureValues[1]);
            stim.gameObject.GetComponent<Renderer>().material.color = this.ColorFeatureValues[stim.featureValues[1]];

            // location
            // Debug.Log("location:" + stim.featureValues[2]);
            stim.gameObject.transform.position = this.LocationFeatureValues[stim.featureValues[2]].position;
        }
        
        if(targetStim == 0){
            stims[0].gameObject.tag = "Target";
            stims[1].gameObject.tag = "NotTarget";
        }else{
            stims[1].gameObject.tag = "Target";
            stims[0].gameObject.tag = "NotTarget";
        }

        this.targetAction = this.targetStim;

    }
}
