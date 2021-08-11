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

public class ControlLevel_Trial_Tutorial3_complete : ControlLevel
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
        iti.AddInitializationMethod(() =>
        {
            stim1.SetActive(false);
            stim2.SetActive(false);
        });
        iti.AddTimer(itiDur, stimOn, () => trialCount++);

        this.AddTerminationSpecification(() => trialCount > numTrials, ()=> Debug.Log(trialCount + " " + numTrials));
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
