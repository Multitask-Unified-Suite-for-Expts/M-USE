using UnityEngine;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using ConReg_Namespace;
using System;
using System.Globalization;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using USE_Settings;

public class ConReg_TrialLevel : ControlLevel_Trial_Template
{
    public ConReg_TrialDef currentTrial => GetCurrentTrialDef<ConReg_TrialDef>();
    private StimGroup currentTrialStims, resultStims;

    //configUI variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration;

    // game object variables
    private GameObject startButton;
    private GameObject trialStim;

    //context variables
    public string MaterialFilePath;
    private int context;

    // misc variables
    private Ray mouseRay;
    private bool variablesLoaded;
    private int trialCount;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State DisplayStims = new State("DisplayStims");
        State ChooseStim = new State("ChooseStim");
        State TouchFeedback = new State("TouchFeedback");
        State TokenUpdate = new State("TokenUpdate");
        State trialResults = new State("TrialResults");
        State trialEnd = new State("TrialEnd");
        AddActiveStates(new List<State> { InitTrial, DisplayStims, ChooseStim, TouchFeedback, TokenUpdate, trialResults, trialEnd });

        SelectionHandler<ConReg_StimDef> mouseHandler = new SelectionHandler<ConReg_StimDef>();
        TokenFBController.enabled = false;

        SetupTrial.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\LG1.jpg");
            if (!variablesLoaded) loadVariables();
            trialCount++;
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial); //automatically terminate. lasts exactly one frame. 

        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial); // starts looking for input when InitTrial state starts?

        InitTrial.AddInitializationMethod(() =>
        {
            TokenFBController.enabled = false;
            TokenFBController
                .SetRevealTime(currentTrial.TokenRevealDuration)
                .SetUpdateTime(currentTrial.TokenUpdateDuration);

            startButton.SetActive(true);
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton), //init screen ends when blue square clicked. 
            DisplayStims, () =>
            {
                startButton.SetActive(false);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]); //CHECK THIS TIMING! MIGHT BE OFF
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
                TokenFBController.enabled = true;
            });

        //stims are automatically turned on as soon as it enters the DisplayStims state. No initialization method needed. 
        DisplayStims.AddTimer(() => currentTrial.DisplayStimsDuration, ChooseStim);

        // --------------ChooseStims State -----------------
        bool stimIsChosen = false;
        bool gotCorrect = false;
        GameObject chosenStim = null;
        ConReg_StimDef selectedStim = null;
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseStim);

        //I don't think ChooseStims needs an initialization method because the stim are already in place and the update method looking for user input. 

        ChooseStim.AddUpdateMethod(() =>
        {
            stimIsChosen = false;
            gotCorrect = false;

            chosenStim = mouseHandler.SelectedGameObject;
            updateBlockDefs(chosenStim);
            selectedStim = mouseHandler.SelectedStimDef;

            if (chosenStim != null)
            {
                StimDefPointer sdPointer = chosenStim.GetComponent<StimDefPointer>();
                if (!sdPointer) return;
                else stimIsChosen = true;
            }

            if (selectedStim != null && selectedStim.PreviouslyChosen == false)
            {
                selectedStim.PreviouslyChosen = true;
                gotCorrect = true;
                Debug.Log("[METRICS] Trial " + (currentTrial.trialCount + 1) + "; Correct when PNC Count = " + currentTrial.PNC_count + "; PC Count = " + currentTrial.PC_count + "; N Count = " + currentTrial.new_Count);
            }
            currentTrial.isNewStim = gotCorrect;
        });

        ChooseStim.SpecifyTermination(() => stimIsChosen, TouchFeedback);
        ChooseStim.AddTimer(() => selectObjectDuration.value, TouchFeedback); //maybe make a func with some red text that appears for a few seconds saying "Time's up!" 

        TrialData.AddDatum("PreviouslyChosen", () => currentTrial.PreviouslyChosenStim);
        TrialData.AddDatum("PreviouslyUnseen", () => currentTrial.UnseenStims);
        TrialData.AddDatum("PreviouslyNotChosenStims", () => currentTrial.PreviouslyNotChosenStimuli);
        TrialData.AddDatum("isNew", () => currentTrial.isNewStim);
        TrialData.AddDatum("CurrentTrialStims", () => currentTrial.TrialStimIndices);
        TrialData.AddDatum("PCStimsCount", () => currentTrial.PC_count);
        TrialData.AddDatum("PNCStimsCount", () => currentTrial.PNC_count);
        TrialData.AddDatum("UnseenStimsCount", () => currentTrial.new_Count);

        // --------------TouchFeedback State -----------------
        TouchFeedback.AddInitializationMethod(() =>
        {
            if (!stimIsChosen) return;
            if (gotCorrect)
            {
                HaloFBController.ShowPositive(chosenStim);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
            else
            {
                HaloFBController.ShowNegative(chosenStim);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
        });
        TouchFeedback.AddTimer(() => fbDuration.value, TokenUpdate);

        TokenUpdate.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (gotCorrect)
            {
                TokenFBController.AddTokens(chosenStim, 1); //will put "currentTrial.StimTrialRewardMag" here !
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else
            {
                AudioFBController.Play("Negative");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
            }
            Debug.Log("TRIAL COUNT IS " + trialCount + "; MAX TRIAL COUNT IS " + currentTrial.maxNumTrials);
        });


        //if it is NOT new OR it's the last trial, go to FinishTrial!
        TokenUpdate.SpecifyTermination(() => (!TokenFBController.IsAnimating() && (!gotCorrect || trialCount == currentTrial.maxNumTrials)), FinishTrial);
        //if it is new and it's NOT the last trial, go to TrialEnd!
        TokenUpdate.SpecifyTermination(() => !TokenFBController.IsAnimating() && (trialCount < currentTrial.maxNumTrials) && gotCorrect, trialEnd);

        trialEnd.AddTimer(() => itiDuration.value, FinishTrial);
    }


    //Function is called when the trial's defined!!!
    //The stim are auto loaded in the SetupTrial StateInitialization, and destroyed in the FinishTrial StateTermination
    protected override void DefineTrialStims()
    {
        if (TrialCount_InBlock == 0) //if first trial     
        {
            var blockNumStims = currentTrial.BlockStimIndices.Length;
            for (int i = 0; i < blockNumStims; i++)
            {
                currentTrial.UnseenStims.Add(currentTrial.BlockStimIndices[i]); //add each stim to unseenStims.
            }

            int[] temp = new int[currentTrial.nObjectsMinMax[0]]; //just using as temp placeholder to chck if num already in array. 
            for (int i = 0; i < currentTrial.nObjectsMinMax[0]; i++)
            {
                int num = Random.Range(0, blockNumStims);
                while (Array.IndexOf(temp, num) != -1) //keep generating random num if its not new.
                {
                    num = Random.Range(0, blockNumStims);
                }
                temp[i] = num; //add to array so next iteration can check if num in the array.
                currentTrial.TrialStimIndices.Add(num); //add the randomly selected stim's index to the trialStimIndices.
                currentTrial.UnseenStims.Remove(num); //and remove it from the unseen list. 
                currentTrial.new_Count += 1;
            }
        }
        else //if not first trial
        {
            float[] idealStimRatio = GetStimRatio(currentTrial.Ratio); //calc percentage of each stim type. 
            int[] ratio_array = GetStimNum(idealStimRatio); //plug percentages into method to get Num of stim for each type. 
            int PC_num = ratio_array[0];
            int N_num = ratio_array[1];
            int PNC_num = ratio_array[2];

            currentTrial.TrialStimIndices.Clear();

            int PC_length = currentTrial.PreviouslyChosenStim.Count;
            for (int i = 0; i < PC_num && PC_length > 0; i++)
            {
                int id = currentTrial.PreviouslyChosenStim[Random.Range(0, PC_length - 1)];
                while (currentTrial.TrialStimIndices.Contains(id) && PC_length > 0)
                {
                    id = currentTrial.PreviouslyChosenStim[Random.Range(0, PC_length - 1)];
                    PC_length--;
                }

                if (!currentTrial.TrialStimIndices.Contains(id))
                {
                    Debug.Log("added previously chosenStim: " + id);
                    currentTrial.TrialStimIndices.Add(id);
                    currentTrial.PC_count += 1;
                    currentTrial.UnseenStims.Remove(id);
                    PC_length--;
                }
            }

            //Generate New_Stim needed for trial. Add to Indices, remove from Unseen, add to NewList.
            //int numUnseen = currentTrial.Unseen_Stim.Count;

            //for (int i = 0; i < New_Num && numUnseen > 0; i++)
            //{
            //    int New_Id = currentTrial.Unseen_Stim[Random.Range(0, numUnseen)];
            //    while (currentTrial.TrialStimIndices.Contains(New_Id) && numUnseen > 0)
            //    {
            //        New_Id = currentTrial.Unseen_Stim[Random.Range(0, numUnseen)];
            //        numUnseen--;
            //    }
            //    Debug.Log($"CURRENT TRIAL NEW_STIM: {New_Id}");
            //    currentTrial.TrialStimIndices.Add(New_Id);
            //    currentTrial.Unseen_Stim.Remove(New_Id);
            //    currentTrial.New_Stim.Add(New_Id);
            //    numUnseen--;
            //}

            int N_length = currentTrial.UnseenStims.Count;
            for (int i = 0; i < N_num && N_length > 0; i++)
            {
                int id = currentTrial.UnseenStims[Random.Range(0, N_length - 1)];
                while (currentTrial.TrialStimIndices.Contains(id) && N_length > 0)
                {
                    id = currentTrial.UnseenStims[Random.Range(0, N_length - 1)];
                    N_length--;
                }

                if (!currentTrial.TrialStimIndices.Contains(id))
                {
                    Debug.Log("added new: " + id);
                    currentTrial.TrialStimIndices.Add(id);
                    currentTrial.new_Count += 1;
                    currentTrial.UnseenStims.Remove(id);
                    N_length--;
                }
            }

            int PNC_length = currentTrial.PreviouslyNotChosenStimuli.Count;
            for (int i = 0; i < PNC_num && PNC_length > 0; i++)
            {
                int id = currentTrial.PreviouslyNotChosenStimuli[
                    Random.Range(0, PNC_length - 1)];
                while (currentTrial.TrialStimIndices.Contains(id) && PNC_length > 0)
                {
                    id = currentTrial.PreviouslyNotChosenStimuli[
                        Random.Range(0, PNC_length - 1)];
                    PNC_length--;
                }

                if (!currentTrial.TrialStimIndices.Contains(id))
                {
                    currentTrial.TrialStimIndices.Add(id);
                    currentTrial.PNC_count += 1;
                    PNC_length--;
                }
            }

            while (currentTrial.TrialStimIndices.Count < currentTrial.numTrialStims)
            {
                if (N_length != 0)
                {
                    int id = currentTrial.UnseenStims[Random.Range(0, N_length - 1)];
                    while (currentTrial.TrialStimIndices.Contains(id) && N_length > 0)
                    {
                        id = currentTrial.UnseenStims[Random.Range(0, N_length - 1)];
                        N_length--;
                    }

                    if (!currentTrial.TrialStimIndices.Contains(id))
                    {
                        Debug.Log("added new: " + id);
                        currentTrial.TrialStimIndices.Add(id);
                        currentTrial.new_Count += 1;
                        currentTrial.UnseenStims.Remove(id);
                        N_length--;
                    }
                }
                else if (PNC_length != 0)
                {
                    int id = currentTrial.PreviouslyNotChosenStimuli[Random.Range(0, PNC_length - 1)];
                    while (currentTrial.TrialStimIndices.Contains(id) && PNC_length > 0)
                    {
                        id = currentTrial.PreviouslyNotChosenStimuli[Random.Range(0, PNC_length - 1)];
                        PNC_length--;
                    }

                    if (!currentTrial.TrialStimIndices.Contains(id))
                    {
                        Debug.Log("added previously not chosenStim: " + id);
                        currentTrial.TrialStimIndices.Add(id);
                        currentTrial.PNC_count += 1;
                        PNC_length--;
                    }
                }
                else if (currentTrial.PreviouslyChosenStim.Count != 0)
                {
                    int id = currentTrial.PreviouslyChosenStim[Random.Range(0, PC_length - 1)];
                    while (currentTrial.TrialStimIndices.Contains(id) && PNC_length > 0)
                    {
                        id = currentTrial.PreviouslyChosenStim[Random.Range(0, PC_length - 1)];
                        PC_length--;
                    }

                    if (!currentTrial.TrialStimIndices.Contains(id))
                    {
                        Debug.Log("added previously chosenStim: " + id);
                        currentTrial.TrialStimIndices.Add(id);
                        currentTrial.PC_count += 1;
                        currentTrial.UnseenStims.Remove(id);
                        PC_length--;
                    }
                }
                else Debug.Log("Not enough stims");
            }
        }
        // Log for debugging
        getLog(currentTrial.UnseenStims, "UnseenStims");
        getLog(currentTrial.PreviouslyChosenStim, "PreviouslyChosenStimuli");
        getLog(currentTrial.PreviouslyNotChosenStimuli, "PreviouslyNotChosenStimuli");
        getLog(currentTrial.TrialStimIndices, "TrialStimIndices");

        // set trial stims
        currentTrialStims = new StimGroup("TrialStims", ExternalStims, currentTrial.TrialStimIndices);
        currentTrialStims.SetLocations(currentTrial.TrialStimLocations);
        //makes the stims visible when it ENTERS the DisplayStims state, and makes them invisible when EXITS the TokenUpdate state.         !!!!!!!!!!!!!!!!!
        currentTrialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenUpdate"));
        TrialStims.Add(currentTrialStims);
    }


    private void updateBlockDefs(GameObject chosenStim)
    {
        int curStimCount = currentTrialStims.stimDefs.Count;
        int chosenStimIndex = 0;

        // Loop through all trial stims in current trial
        for (int i = 0; i < curStimCount; i++)
        {
            GameObject curStim = currentTrialStims.stimDefs[i].StimGameObject;
            int code = currentTrialStims.stimDefs[i].StimCode - 1;

            // find the stim that was chosenStim in current trial
            if (chosenStim == curStim)
            {
                chosenStimIndex = code;
                TrialData.AddDatum("ChosenStim", () => chosenStimIndex);
                currentTrial.PreviouslyChosenStim.Add(chosenStimIndex);
                currentTrial.UnseenStims.Remove(chosenStimIndex);
                currentTrial.PreviouslyNotChosenStimuli.Remove(chosenStimIndex);
            }
            else
            {
                if (!currentTrial.PreviouslyNotChosenStimuli.Contains(code) &&
                    !currentTrial.PreviouslyChosenStim.Contains(code))
                {
                    currentTrial.PreviouslyNotChosenStimuli.Add(code);
                }
            }
        }
    }

    private void loadVariables()
    {
        Texture2D buttonTex = LoadPNG(MaterialFilePath + "\\StartButtonImage.png");
        startButton = CreateStartButton(buttonTex, new Rect(new Vector2(0, 0), new Vector2(1, 1)));

        //config UI variables from the ConfigUIdetails.json Config file. 
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");

        variablesLoaded = true;
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
     * This function calculate the idealStimRatio PC, N, PNC stims
     */
    private float[] GetStimRatio(int[] arr)
    {
        float sum = 0;
        float[] result = new float[arr.Length];
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
        float[] idealStimRatio = GetStimRatio(currentTrial.Ratio);
        int sum = 0;
        int start = currentTrial.nObjectsMinMax[0];
        for (int i = 0; i < currentTrial.nObjectsMinMax[1]; i++)
        {
            sum += (int)Math.Ceiling((idealStimRatio[1] * (start + i)));
        }
        return sum;
    }

    /*
     * Calculate the number of PC, N, PNC stims in trial
     */
    private int[] GetStimNum(float[] idealStimRatio)
    {
        int PC_num = (int)Math.Floor(idealStimRatio[0] * currentTrial.numTrialStims);
        int N_num = (int)Math.Floor(idealStimRatio[1] * currentTrial.numTrialStims);
        int PNC_num = (int)Math.Floor(idealStimRatio[2] * currentTrial.numTrialStims);
        if (PC_num == 0) PC_num = 1;
        if (N_num == 0) N_num = 1;
        if (PNC_num == 0) PNC_num = 1;

        int temp = 0;
        while ((PC_num + N_num + PNC_num) < currentTrial.numTrialStims)
        {
            if (temp % 3 == 0) PC_num += 1;
            else if (temp % 3 == 1) N_num += 1;
            else PNC_num += 1;
            temp++;
        }

        return new[] { PC_num, N_num, PNC_num };
    }

    /*
     * This function randomly changes the context color in each trial
     */
    ///*private void changeContext(Color[] colors)
    //{
    //    int num = Random.Range(0, colors.Length - 1);
    //    Camera.main.backgroundColor = colors[num];
    //}*/

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
        // Debug.Log(result);
    }

    private void getLog2(int[] arr, string name)
    {
        string result = name + ": ";
        foreach (var item in arr)
        {
            result += item.ToString() + ", ";
        }
        // Debug.Log(result);
    }


    public void clickEvent()
    {
        Debug.Log("did it");
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
        Vector3 buttonPosition = Vector3.zero;
        Vector3 buttonScale = Vector3.zero;
        string TaskName = "ContinuousRecognition";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
        }
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
        }

        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sr = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sr.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);

        startButton.AddComponent<BoxCollider>();

        startButton.transform.localScale = buttonScale;
        startButton.transform.position = buttonPosition;

        return startButton;
    }

}