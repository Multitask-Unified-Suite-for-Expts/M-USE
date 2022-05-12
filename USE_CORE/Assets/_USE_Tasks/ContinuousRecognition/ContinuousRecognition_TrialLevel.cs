using UnityEngine;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;
using System;
using System.Globalization;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Random = UnityEngine.Random;

public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef CurrentTrialDef => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();
    private StimGroup currentTrialStims, resultStims;
    
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
        new Color(0.46f, 0.139f, 0.5471f),
        new Color(0.6f, 0.6f, 0f),
        new Color(0.9f, 0.6f, 0f)
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
        SelectionHandler<ContinuousRecognition_StimDef> mouseHandler = new SelectionHandler<ContinuousRecognition_StimDef>();
        AddActiveStates(new List<State> {initTrial, displayStims, chooseStim, touchFeedback, tokenFeedback, displayResult, trialEnd});

        // --------------SetupTrial-----------------
        bool started = false;
        bool end = false;
        SetupTrial.AddUpdateMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                //loadVariables();
            }

            int sum = checkStimNum();
            if (sum > CurrentTrialDef.BlockStimIndices.Length)
            {
                Debug.Log("[ERROR] Number of stims is not enough. " + sum + " stims are required.");
                end = true;
            }

        });
        SetupTrial.SpecifyTermination(() => true, initTrial);

        MouseTracker.AddSelectionHandler(mouseHandler, initTrial);
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
            if (mouseHandler.SelectionMatches(StartButton))
            {
                started = true;
            }
        });
        initTrial.SpecifyTermination(() => started, displayStims, () => StartButton.SetActive(false));

        // --------------Initialize displayStims State -----------------
        displayStims.AddTimer(() => CurrentTrialDef.DisplayStimsDuration, chooseStim);

        // --------------chooseStims State -----------------
        bool StimIsChosen = false;
        bool isNew = false;
        GameObject chosen = null;
        //GameObject selected = null;
        bool terminate = false;
        ContinuousRecognition_StimDef selectedSD = null;
        MouseTracker.AddSelectionHandler(mouseHandler, chooseStim);
        chooseStim.AddUpdateMethod(() =>
        {
            StimIsChosen = false;
            isNew = false;

            chosen = mouseHandler.SelectedGameObject;
            updateBlockDefs(chosen);
            selectedSD = mouseHandler.SelectedStimDef;

            // con't
            if (chosen != null)
            {
                StimDefPointer sdPointer = chosen.GetComponent<StimDefPointer>();
                if (!sdPointer)
                {
                    return;
                }
                else
                {
                    StimIsChosen = true;
                }
            }

            //ContinuousRecognition_StimDef sd = sdPointer.GetStimDef<ContinuousRecognition_StimDef>();
            if (selectedSD != null && selectedSD.PreviouslyChosen == false)
            {
                Debug.Log("NOT CHOSEN BEFORE");
                selectedSD.PreviouslyChosen = true;
                Debug.Log(selectedSD.PreviouslyChosen);
                isNew = true;
                Debug.Log("[METRICS] Trial " + (CurrentTrialDef.trialCount+1) +"; Correct when PNC Count = " + CurrentTrialDef.PNC_count + "; PC Count = " + CurrentTrialDef.PC_count + "; N Count = " + CurrentTrialDef.new_Count);

            }
            else
            {
                isNew = false;
                Debug.Log("CHOSEN BEFORE");
            }
            CurrentTrialDef.isNewStim = isNew;
        });
        chooseStim.SpecifyTermination(() => StimIsChosen, touchFeedback);
        chooseStim.AddTimer(() => CurrentTrialDef.ChooseStimDuration, FinishTrial);
        
        TrialData.AddDatum("PreviouslyChosen", () => CurrentTrialDef.PreviouslyChosenStimuli);
        TrialData.AddDatum("PreviouslyUnseen", ()=>CurrentTrialDef.UnseenStims);
        TrialData.AddDatum("PreviouslyNotChosenStims", ()=>CurrentTrialDef.PreviouslyNotChosenStimuli);
        TrialData.AddDatum("isNew", ()=>CurrentTrialDef.isNewStim);
        TrialData.AddDatum("CurrentTrialStims", () => CurrentTrialDef.TrialStimIndices);
        TrialData.AddDatum("PCStimsCount", ()=>CurrentTrialDef.PC_count);
        TrialData.AddDatum("PNCStimsCount", ()=>CurrentTrialDef.PNC_count);
        TrialData.AddDatum("UnseenStimsCount", ()=>CurrentTrialDef.new_Count);

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
            }
            Debug.Log("TRIAL COUNT IS " + trialCount + "; MAX TRIAL COUNT IS " +
                      CurrentTrialDef.maxNumTrials);
            
        });
        //tokenFeedback.SpecifyTermination(() => !isNew, ()=>displayResult);
        //tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), trialEnd);

        //FIXME: fix here
        tokenFeedback.SpecifyTermination(()=> (!TokenFBController.IsAnimating() && (!isNew || trialCount == CurrentTrialDef.maxNumTrials)), FinishTrial, ()=> end = true);
        tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating() && (trialCount < CurrentTrialDef.maxNumTrials) && isNew, trialEnd);
        trialEnd.AddTimer(() => CurrentTrialDef.TrialEndDuration, FinishTrial);
        this.AddTerminationSpecification(()=>end);
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
                    CurrentTrialDef.new_Count += 1;
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
                        CurrentTrialDef.PC_count += 1;
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
                        CurrentTrialDef.new_Count += 1;
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
                        CurrentTrialDef.PNC_count += 1;
                        PNC_length--;
                    }
                }

                while (CurrentTrialDef.TrialStimIndices.Count < CurrentTrialDef.numTrialStims)
                {
                    if (CurrentTrialDef.UnseenStims.Count != 0)
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
                            CurrentTrialDef.new_Count += 1;
                            CurrentTrialDef.UnseenStims.Remove(id);
                            N_length--;
                        }
                    } else if (CurrentTrialDef.PreviouslyNotChosenStimuli.Count != 0)
                    {
                        int id = CurrentTrialDef.PreviouslyNotChosenStimuli[Random.Range(0, CurrentTrialDef.PreviouslyNotChosenStimuli.Count - 1)];
                        while (CurrentTrialDef.TrialStimIndices.Contains(id) && PNC_length > 0)
                        {
                            id = CurrentTrialDef.PreviouslyNotChosenStimuli[Random.Range(0, CurrentTrialDef.PreviouslyNotChosenStimuli.Count - 1)];
                            PNC_length--;
                        }

                        if (!CurrentTrialDef.TrialStimIndices.Contains(id))
                        {
                            Debug.Log("added previously not chosen: " + id);
                            CurrentTrialDef.TrialStimIndices.Add(id);
                            CurrentTrialDef.PNC_count += 1;
                            PNC_length--;
                        }
                    } else if (CurrentTrialDef.PreviouslyChosenStimuli.Count != 0)
                    {
                        int id = CurrentTrialDef.PreviouslyChosenStimuli[Random.Range(0, CurrentTrialDef.PreviouslyChosenStimuli.Count - 1)];
                        while (CurrentTrialDef.TrialStimIndices.Contains(id) && PNC_length > 0)
                        {
                            id = CurrentTrialDef.PreviouslyChosenStimuli[Random.Range(0, CurrentTrialDef.PreviouslyChosenStimuli.Count - 1)];
                            PC_length--;
                        }

                        if (!CurrentTrialDef.TrialStimIndices.Contains(id))
                        {
                            Debug.Log("added previously chosen: " + id);
                            CurrentTrialDef.TrialStimIndices.Add(id);
                            CurrentTrialDef.PC_count += 1;
                            CurrentTrialDef.UnseenStims.Remove(id);
                            PC_length--;
                        }
                    }
                    else
                    {
                        Debug.Log("Not enough Stims");
                    }
                }
            }
            /*
            Debug.Log("xxxxxxxxxxxxx new: " + CurrentTrialDef.new_Count);
            Debug.Log("xxxxxxxxxxxxx PNC: " + CurrentTrialDef.PNC_count);
            Debug.Log("xxxxxxxxxxxxx PC: " + CurrentTrialDef.PC_count);*/

            // Log for debugging
            getLog(CurrentTrialDef.UnseenStims, "UnseenStims");
            getLog(CurrentTrialDef.PreviouslyChosenStimuli, "PreviouslyChosenStimuli");
            getLog(CurrentTrialDef.PreviouslyNotChosenStimuli, "PreviouslyNotChosenStimuli");
            getLog(CurrentTrialDef.TrialStimIndices, "TrialStimIndices");
            
            // set trial stims
            currentTrialStims = new StimGroup("TrialStims", ExternalStims, CurrentTrialDef.TrialStimIndices);
            currentTrialStims.SetLocations(CurrentTrialDef.TrialStimLocations);
            currentTrialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"),  GetStateFromName("TokenFeedback"));
            TrialStims.Add(currentTrialStims);
        }
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
    
    private int checkStimNum()
    {
        bool result;
        float[] ratio = getRatio(CurrentTrialDef.Ratio);
        int sum = 0;
        int start = CurrentTrialDef.nObjectsMinMax[0];
        for (int i = 0; i < CurrentTrialDef.nObjectsMinMax[1]; i++)
        {
            //Debug.Log("RRRRRRRRRRRRR plus" + (int) Math.Ceiling((ratio[1] * (start + i))));
            sum += (int)Math.Ceiling((ratio[1] * (start + i)));
        }
        //Debug.Log("RRRRRRRRRRRRRR ratio1 is: " + ratio[1]);
        //Debug.Log("RRRRRRRRRRRRRR: " + sum);
        return sum;
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
    
    private void getLog2(int[] arr, string name)
    {
        string result = name + ": ";
        foreach (var item in arr)
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
                TrialData.AddDatum("ChosenStim", ()=>chosenStimIndex);
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