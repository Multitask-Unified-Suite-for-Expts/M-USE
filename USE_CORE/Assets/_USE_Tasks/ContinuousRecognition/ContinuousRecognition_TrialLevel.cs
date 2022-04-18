using UnityEngine;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;
using System;

using ICSharpCode.SharpZipLib.Zip.Compression;
using Random = UnityEngine.Random;

public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef CurrentTrialDef => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();
    private StimGroup currentTrialStims, resultStims, display;
    
    // game object variables
    public GameObject StartButton;
    private GameObject trialStim;

    // effort reward variables
    private int context;

    // misc variables
    private Ray mouseRay;
    private bool variablesLoaded;
    private int trialCount;

    private Color[] colors = new[]
    {
        new Color(0.1f, 0.59f, 0.28f),
        new Color(0.54f, 0.18f, 0.18f),
        new Color(0.6275f, 0.3216f, 0.1765f),
        new Color(0.8275f, 0.3f, 0.5275f),
        new Color(0.46f, 0.139f, 0.5471f)
    };

    // trial variables

    public override void DefineControlLevel()
    {
        // define States 
        State initTrial = new State("InitTrial");
        State displayStims = new State("DisplayStims");
        State chooseStim = new State("ChooseStim");
        State touchFeedback = new State("TouchFeedback");
        State tokenFeedback = new State("TokenFeedback");
        State displayResult = new State("DisplayResult");
        State trialEnd = new State("TrialEnd");
        //SelectionHandler<ContinuousRecognition_StimDef> mouseHandler = new SelectionHandler<ContinuousRecognition_StimDef>();
        AddActiveStates(new List<State> {initTrial, displayStims, chooseStim, touchFeedback, tokenFeedback, displayResult, trialEnd});
        
        //TODO testing
        Text commandText = null;

        // --------------SetupTrial-----------------
        bool started = false;
        SetupTrial.AddUpdateMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
        });
        SetupTrial.SpecifyTermination(() => true, initTrial);

        initTrial.AddInitializationMethod(() =>
        {
            trialCount++;
            if (trialCount != 1)
            {
                changeContext(colors);
            }
            
            if (context != 0)
            {
                Debug.Log("Context is " + context);
                //Disable all game objects
            }

            context = CurrentTrialDef.Context;
            StartButton.SetActive(true);
        });

        // --------------update InitTrial -----------------
        initTrial.AddUpdateMethod(() =>
        {
            StartButton.SetActive(true);
            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(mouseRay, out hit))
                {
                    if (hit.transform.name == "StartButton")
                    {
                        Debug.Log("pressed start button");
                        started = true;
                    }
                }
            }
        });
        initTrial.SpecifyTermination(() => started, displayStims, () => StartButton.SetActive(false));

        // --------------Initialize displayStims State -----------------
        displayStims.AddTimer(() => CurrentTrialDef.DisplayStimsDuration, chooseStim);

        // --------------chooseStims State -----------------
        bool StimIsChosen = false;
        bool isNew = false;
        GameObject chosen = null;
        GameObject selected = null;
        bool terminate = false;
        chooseStim.AddUpdateMethod(() =>
        {
            StimIsChosen = false;
            isNew = false;
            //chosen = null;
            if (InputBroker.GetMouseButtonDown(0))
            {
                chosen = GetClickedObj();
                updateBlockDefs(chosen);

                // con't
                StimDefPointer sdPointer = chosen.GetComponent<StimDefPointer>();
                if (!sdPointer)
                {
                    return;
                }
                else
                {
                    StimIsChosen = true;
                }

                ContinuousRecognition_StimDef sd = sdPointer.GetStimDef<ContinuousRecognition_StimDef>();
                //bool correct = false;
                //correct = sd.PreviouslyChosen;
                if (sd.PreviouslyChosen == false)
                {
                    Debug.Log("NOT CHOSEN BEFORE");
                    sd.PreviouslyChosen = true;
                    Debug.Log(sd.PreviouslyChosen);
                    isNew = true;
                    //CurrentTrialDef.isNew = true;
                }
                else
                {
                    isNew = false;
                    //CurrentTrialDef.isNew = false;
                    Debug.Log("CHOSEN BEFORE");
                }
            }
        });
        chooseStim.SpecifyTermination(() => StimIsChosen, touchFeedback);
        chooseStim.AddTimer(() => CurrentTrialDef.ChooseStimDuration, FinishTrial);

        // --------------touchFeedback State -----------------
        bool touchFeedbackFinish = false;
        touchFeedback.AddInitializationMethod(() =>
        {
            if (!StimIsChosen) return;
            if (isNew)
            {
                HaloFBController.ShowPositive(chosen);
            }
            else
            {
                HaloFBController.ShowNegative(chosen);
            }
        });
        touchFeedback.AddTimer(() => CurrentTrialDef.TouchFeedbackDuration, tokenFeedback);
        //tokenFeedback.SpecifyTermination(() => !isNew, FinishTrial, ()=>Debug.Log("[tokenFeedback]: going to finishTrial"));
        
        bool end = false;
        tokenFeedback.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (isNew)
            {
                TokenFBController.AddTokens(chosen, 1);
            }
            else
            {
                AudioFBController.Play("Negative");
                end = true;
            }
            Debug.Log("TRIAL COUNT IS " + trialCount + "; MAX TRIAL COUNT IS " +
                      CurrentTrialDef.maxNumTrials);
        });
        //tokenFeedback.SpecifyTermination(() => !isNew, ()=>displayResult);
        //tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), trialEnd);

        tokenFeedback.SpecifyTermination(()=> (end || trialCount == CurrentTrialDef.maxNumTrials), displayResult);
        tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating() && (trialCount < CurrentTrialDef.maxNumTrials) && isNew, trialEnd);

        bool dd = false;
        bool displayed = false;
        /*
        displayResult.AddInitializationMethod(() =>
        {
            Debug.Log("[DisplayResult]: in display result");
            // just for testing
            commandText = GameObject.Find("CommandText").GetComponent<Text>();
            commandText.text = "displaying result";
        });*/
        displayResult.AddTimer(() => 5f, ()=>trialEnd, ()=>dd = true);
        trialEnd.AddTimer(() => CurrentTrialDef.TrialEndDuration, FinishTrial);
        
        /*
        FinishTrial.SpecifyTermination(
            () => (trialCount > (CurrentTrialDef.nObjectsMinMax[1] - CurrentTrialDef.nObjectsMinMax[0] + 1)) ||
                  terminate && touchFeedbackFinish, ()=>null, ()=>Debug.Log("end block"));*/
        this.AddTerminationSpecification(()=>dd);
        //FinishTrial.SpecifyTermination(() => !isNew, () => null, ()=>Debug.Log("[FinishTrial]: finishing trial"));
    }

    // Helper Functions
    protected override void DefineTrialStims()
    {
        // display stims if haven't reached max trial count
        if (CurrentTrialDef.trialCount <= (CurrentTrialDef.nObjectsMinMax[1] - CurrentTrialDef.nObjectsMinMax[0] + 1))
        {
            // in the first trial, just randomly choose two stims out of all stims 
            if (CurrentTrialDef.trialCount == 0)
            {
                for (int i = 0; i < CurrentTrialDef.BlockStimIndices.Length; i++)
                {
                    CurrentTrialDef.UnseenStims.Add(CurrentTrialDef.BlockStimIndices[i]);
                }

                int[] tmp = new int [CurrentTrialDef.nObjectsMinMax[0]];
                for (int i = 0; i < CurrentTrialDef.nObjectsMinMax[0]; i++)
                {
                    int num = Random.Range(0, CurrentTrialDef.BlockStimIndices.Length);
                    while (Array.IndexOf(tmp, num) != -1)
                    {
                        num = Random.Range(0, CurrentTrialDef.BlockStimIndices.Length);
                    }

                    tmp[i] = num;

                    CurrentTrialDef.TrialStimIndices.Add(num);
                    CurrentTrialDef.UnseenStims.Remove(num);
                }
            }
            else
            {
                Debug.Log("NumTrialStims: " + CurrentTrialDef.numTrialStims);

                float[] ratio = getRatio(CurrentTrialDef.Ratio);
                int[] ratio_array = getStimNum(ratio);
                int PC_num = ratio_array[0];
                int N_num = ratio_array[1];
                int PNC_num = ratio_array[2];

                Debug.Log("Chosen Count IS: " + PC_num + "   Count IS: " + ratio[0] * CurrentTrialDef.numTrialStims);
                Debug.Log("New Count IS: " + N_num + "     Count IS: " + ratio[1] * CurrentTrialDef.numTrialStims);
                Debug.Log("Previously Not Chosen Count IS: " + PNC_num + "     Count IS: " +
                          ratio[2] * CurrentTrialDef.numTrialStims);


                CurrentTrialDef.TrialStimIndices.Clear();
                int PC_length = CurrentTrialDef.PreviouslyChosenStimuli.Count;
                for (int i = 0; i < PC_num && PC_length > 0; i++)
                {
                    int id = CurrentTrialDef.PreviouslyChosenStimuli[
                        Random.Range(0, CurrentTrialDef.PreviouslyChosenStimuli.Count - 1)];
                    while (CurrentTrialDef.TrialStimIndices.Contains(id) && PC_length > 0)
                    {
                        id = CurrentTrialDef.PreviouslyChosenStimuli[
                            Random.Range(0, CurrentTrialDef.PreviouslyChosenStimuli.Count - 1)];
                        PC_length--;
                    }

                    if (!CurrentTrialDef.TrialStimIndices.Contains(id))
                    {
                        Debug.Log("added previously chosen: " + id);
                        CurrentTrialDef.TrialStimIndices.Add(id);
                        CurrentTrialDef.UnseenStims.Remove(id);
                        PC_length--;
                    }
                }

                int N_length = CurrentTrialDef.UnseenStims.Count;
                for (int i = 0; i < N_num && N_length > 0; i++)
                {
                    int id = CurrentTrialDef.UnseenStims[Random.Range(0, CurrentTrialDef.UnseenStims.Count - 1)];
                    while (CurrentTrialDef.TrialStimIndices.Contains(id) && N_length > 0)
                    {
                        id = CurrentTrialDef.UnseenStims[Random.Range(0, CurrentTrialDef.UnseenStims.Count - 1)];
                        N_length--;
                    }

                    if (!CurrentTrialDef.TrialStimIndices.Contains(id))
                    {
                        Debug.Log("added new: " + id);
                        CurrentTrialDef.TrialStimIndices.Add(id);
                        CurrentTrialDef.UnseenStims.Remove(id);
                        N_length--;
                    }
                }

                int PNC_length = CurrentTrialDef.PreviouslyNotChosenStimuli.Count;
                for (int i = 0; i < PNC_num && PNC_length > 0; i++)
                {
                    int id = CurrentTrialDef.PreviouslyNotChosenStimuli[
                        Random.Range(0, CurrentTrialDef.PreviouslyNotChosenStimuli.Count - 1)];
                    while (CurrentTrialDef.TrialStimIndices.Contains(id) && PNC_length > 0)
                    {
                        id = CurrentTrialDef.PreviouslyNotChosenStimuli[
                            Random.Range(0, CurrentTrialDef.PreviouslyNotChosenStimuli.Count - 1)];
                        PNC_length--;
                    }

                    if (!CurrentTrialDef.TrialStimIndices.Contains(id))
                    {
                        Debug.Log("added previously not chosen: " + id);
                        CurrentTrialDef.TrialStimIndices.Add(id);
                        PNC_length--;
                    }
                }

                while (CurrentTrialDef.TrialStimIndices.Count < CurrentTrialDef.numTrialStims && N_length > 0)
                {
                    int id = CurrentTrialDef.UnseenStims[Random.Range(0, CurrentTrialDef.UnseenStims.Count - 1)];
                    while (CurrentTrialDef.TrialStimIndices.Contains(id) && N_length > 0)
                    {
                        id = CurrentTrialDef.UnseenStims[Random.Range(0, CurrentTrialDef.UnseenStims.Count - 1)];
                        N_length--;
                    }

                    if (!CurrentTrialDef.TrialStimIndices.Contains(id))
                    {
                        Debug.Log("added new: " + id);
                        CurrentTrialDef.TrialStimIndices.Add(id);
                        CurrentTrialDef.UnseenStims.Remove(id);
                        N_length--;
                    }
                }
            }

            List<int> results = CurrentTrialDef.PreviouslyChosenStimuli;

            // Log for debugging
            getLog(CurrentTrialDef.UnseenStims, "UnseenStims");
            getLog(CurrentTrialDef.PreviouslyChosenStimuli, "PreviouslyChosenStimuli");
            getLog(CurrentTrialDef.PreviouslyNotChosenStimuli, "PreviouslyNotChosenStimuli");
            getLog(CurrentTrialDef.TrialStimIndices, "TrialStimIndices");

            // set trial stims for display results state    
            display = new StimGroup("ResultStims", ExternalStims, results);
            int len = results.Count;
            Vector3[] locs = new Vector3[len];
            for (int i = 0; i < len; i++)
            {
                locs[i] = CurrentTrialDef.Grid[i];
            }
            Debug.Log(locs);
            Debug.Log(
                "[DisplayResult]: length of location is " + locs.Length + "; length of chosen is " + results.Count);
            display.SetLocations(locs);
            display.SetVisibilityOnOffStates(GetStateFromName("DisplayResult"), GetStateFromName("TrialEnd"));
            TrialStims.Add(display);
            
            // set trial stims
            //currentTrialStims = new StimGroup("TrialStims", ExternalStims, CurrentTrialDef.TrialStimIndices);
            //currentTrialStims.SetLocations(CurrentTrialDef.TrialStimLocations);
            //currentTrialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"),
              //  GetStateFromName("TokenFeedback"));
            //TrialStims.Add(currentTrialStims);
            
        }
    }
    

    void loadVariables()
    {
        trialCount = 0;
        StartButton = GameObject.Find("StartButton");
        StartButton.SetActive(true);
    }

    private GameObject GetClickedObj()
    {
        if (!InputBroker.GetMouseButtonDown(0)) return null;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit hit))
        {
            return hit.transform.root.gameObject;
        }
        return null;
    }

    /**
     * This function calculate the ratio PC, N, PNC stims
     */
    private float[] getRatio(int[] arr)
    {
        float sum = 0;
        float[] result = new float [arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            sum += arr[i];
        }

        for (int i = 0; i < arr.Length; i++)
        {
            result[i] = arr[i] / sum;
            ;
        }

        return result;
    }

    /*
     * Calculate the number of PC, N, PNC stims in trial
     */
    private int[] getStimNum(float[] ratio)
    {
        int PC_num = (int) Math.Floor(ratio[0] * CurrentTrialDef.numTrialStims);
        int N_num = (int) Math.Floor(ratio[1] * CurrentTrialDef.numTrialStims);
        int PNC_num = (int) Math.Floor(ratio[2] * CurrentTrialDef.numTrialStims);
        if (PC_num == 0) PC_num = 1;
        if (N_num == 0) N_num = 1;
        if (PNC_num == 0) PNC_num = 1;
        int tmp = 0;
        while ((PC_num + N_num + PNC_num) < CurrentTrialDef.numTrialStims)
        {
            if (tmp % 3 == 0)
            {
                PC_num += 1;
            }
            else if (tmp % 3 == 1)
            {
                N_num += 1;
            }
            else
            {
                PNC_num += 1;
            }

            tmp++;
        }

        return new[] {PC_num, N_num, PNC_num};
    }

    /*
     * This function randomly changes the context color in each trial
     */
    private void changeContext(Color[] colors)
    {
        int num = Random.Range(0, colors.Length - 1);
        Camera.main.backgroundColor = colors[num];
    }

    /*
     * Helper function for debug log
     */
    private void getLog(List<int> list, string name)
    {
        string result = name + ": ";
        foreach (var item in list)
        {
            result += item.ToString() + ", ";
        }

        Debug.Log(result);
    }


    private void updateBlockDefs(GameObject chosen)
    {
        int curStimCount = currentTrialStims.stimDefs.Count;
        int chosenStimIndex = 0;

        // Loop through all trial stims in current trial
        for (int i = 0; i < curStimCount; i++)
        {
            GameObject curStim = currentTrialStims.stimDefs[i].StimGameObject;
            int code = currentTrialStims.stimDefs[i].StimCode - 1;

            // find the stim that was chosen in current trial
            if (chosen == curStim)
            {
                // Add it to previously chosen, remove it from unseen and PNC
                chosenStimIndex = code;
                CurrentTrialDef.PreviouslyChosenStimuli.Add(chosenStimIndex);
                CurrentTrialDef.UnseenStims.Remove(chosenStimIndex);
                CurrentTrialDef.PreviouslyNotChosenStimuli.Remove(chosenStimIndex);
            }
            else
            {
                // Add not chosen stims to PNC
                if (!CurrentTrialDef.PreviouslyNotChosenStimuli.Contains(code) &&
                    !CurrentTrialDef.PreviouslyChosenStimuli.Contains(code))
                {
                    CurrentTrialDef.PreviouslyNotChosenStimuli.Add(code);
                }
            }
        }
    }
}

//Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
//destroyed at TrialLevel_Finish
//ExternalStims in this call will be replaced with CurrentBlockDef.BlockStims once Marcus gets that working
//StimGroup currentTrialStims = new StimGroup("CurrentTrialStims", ExternalStims, CurrentTrialDef.TrialStimIndices);

//add all previously chosen stimuli to current trial 

//create new list consisting of PreviouslyChosenStimuli + one random non previously chosen stim from BlockStimIndices;
//List<int> trialStimIndices = CurrentTrialDef.PreviouslyChosenStimuli;
//if currentTrialDef.TrialCountInBlock == 0
//choose two random stims
//if currenTrialDef.TrialCountInBlock % 2 == 0
//trialStimIndices.Add(random non previously-chosen stim);
//else
//trialStimIndices.Add(previously non chosen object);