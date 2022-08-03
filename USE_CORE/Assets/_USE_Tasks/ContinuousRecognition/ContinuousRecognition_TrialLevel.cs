using UnityEngine;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;
using System;
using System.Globalization;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Random = UnityEngine.Random;
using ConfigDynamicUI;

public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef CurrentTrialDef => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();
    private StimGroup currentTrialStims, resultStims;

    //configui variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration;
    // game object variables
    private GameObject initButton;
    private GameObject trialStim;

    //context variables
    public string MaterialFilePath;
    private int context;


    // misc variables
    private Ray mouseRay;
    private bool variablesLoaded;
    private int trialCount;
    private bool tokenBarComplete;

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
        SelectionHandler<ContinuousRecognition_StimDef> mouseHandler =
            new SelectionHandler<ContinuousRecognition_StimDef>();
        AddActiveStates(new List<State>
            { initTrial, displayStims, chooseStim, touchFeedback, tokenFeedback, displayResult, trialEnd });

        // --------------SetupTrial-----------------
        // Show blue start button and wait for click
        MouseTracker.AddSelectionHandler(mouseHandler, initTrial);
        SetupTrial.AddInitializationMethod(() =>
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
            TokenFBController
                .SetRevealTime(CurrentTrialDef.TokenRevealDuration)
                .SetUpdateTime(CurrentTrialDef.TokenUpdateDuration);
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\Blank.png");
            initButton.SetActive(true);
            TokenFBController.enabled = false;
        });
        initTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(initButton),
            displayStims, () =>
            {
                initButton.SetActive(false);
                TokenFBController.enabled = true;
                RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\" + CurrentTrialDef.ContextName + ".png");
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]); //CHECK THIS TIMING MIGHT BE OFF
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
            });

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
                selectedSD.PreviouslyChosen = true;
                isNew = true;
                Debug.Log("[METRICS] Trial " + (CurrentTrialDef.trialCount+1) +"; Correct when PNC Count = " + CurrentTrialDef.PNC_count + "; PC Count = " + CurrentTrialDef.PC_count + "; N Count = " + CurrentTrialDef.new_Count);

            }
            else
            {
                isNew = false;
            }
            CurrentTrialDef.isNewStim = isNew;
        });
        chooseStim.SpecifyTermination(() => StimIsChosen, touchFeedback);
        chooseStim.AddTimer(() => selectObjectDuration.value, FinishTrial);
        
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
                
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
            else
            {
                HaloFBController.ShowNegative(chosen);
                
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
        });
        touchFeedback.AddTimer(() => fbDuration.value, tokenFeedback, ()=>{

            });
        //tokenFeedback.SpecifyTermination(() => !isNew, FinishTrial, ()=>Debug.Log("[tokenFeedback]: going to finishTrial"));
        
        tokenFeedback.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (isNew)
            {
                TokenFBController.AddTokens(chosen, 1);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else
            {
                AudioFBController.Play("Negative");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
            }
            // Debug.Log("TRIAL COUNT IS " + trialCount + "; MAX TRIAL COUNT IS " + CurrentTrialDef.maxNumTrials);
        });

        tokenFeedback.AddUpdateMethod(() =>
        {
            if (TokenFBController.IsAnimating())
            {
                tokenBarComplete = true;
            }
        });

        //tokenFeedback.SpecifyTermination(() => !isNew, ()=>displayResult);
        //tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), trialEnd);
        //FIXME: fix here
        tokenFeedback.SpecifyTermination(()=> (!TokenFBController.IsAnimating() && (!isNew || trialCount == CurrentTrialDef.maxNumTrials)), FinishTrial);
        tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating() && (trialCount < CurrentTrialDef.maxNumTrials) && isNew, trialEnd);
        trialEnd.AddTimer(() => itiDuration.value, FinishTrial);
        //this.AddTerminationSpecification(()=>end);
        FinishTrial.SpecifyTermination(() => !isNew, () => null, ()=>Debug.Log("[FinishTrial]: finishing trial"));
    }

    // Helper Functions
    protected override void DefineTrialStims()
    {
        if (TrialCount_InBlock == 0)
        {
            for (int i = 0; i < CurrentTrialDef.BlockStimIndices.Length; i++)
            {
                CurrentTrialDef.UnseenStims.Add(CurrentTrialDef.BlockStimIndices[i]);
            }

            int[] tmp = new int[CurrentTrialDef.nObjectsMinMax[0]];
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
            float[] ratio = getRatio(CurrentTrialDef.Ratio);
            int[] ratio_array = getStimNum(ratio);
            int PC_num = ratio_array[0];
            int N_num = ratio_array[1];
            int PNC_num = ratio_array[2];

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
                    // Debug.Log("added previously not chosen: " + id);
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
                        // Debug.Log("added new: " + id);
                        CurrentTrialDef.TrialStimIndices.Add(id);
                        CurrentTrialDef.new_Count += 1;
                        CurrentTrialDef.UnseenStims.Remove(id);
                        N_length--;
                    }
                }
                else if (CurrentTrialDef.PreviouslyNotChosenStimuli.Count != 0)
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
                }
                else if (CurrentTrialDef.PreviouslyChosenStimuli.Count != 0)
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

        // Log for debugging
        getLog(CurrentTrialDef.UnseenStims, "UnseenStims");
        getLog(CurrentTrialDef.PreviouslyChosenStimuli, "PreviouslyChosenStimuli");
        getLog(CurrentTrialDef.PreviouslyNotChosenStimuli, "PreviouslyNotChosenStimuli");
        getLog(CurrentTrialDef.TrialStimIndices, "TrialStimIndices");

        // set trial stims
        currentTrialStims = new StimGroup("TrialStims", ExternalStims, CurrentTrialDef.TrialStimIndices);
        currentTrialStims.SetLocations(CurrentTrialDef.TrialStimLocations);
        currentTrialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenFeedback"));
        TrialStims.Add(currentTrialStims);

    }
    private void loadVariables()
    {
        initButton = GameObject.Find("StartButton");
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        variablesLoaded = true;
    }
    public static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }
    public Material CreateSkybox(string filePath)
    {
        Texture2D tex = null;
        Material materialSkybox = new Material(Shader.Find("Skybox/6 Sided"));

        tex = LoadPNG(filePath); // load the texture from a PNG -> Texture2D

        //Set the textures of the skybox to that of the PNG
        materialSkybox.SetTexture("_FrontTex", tex);
        materialSkybox.SetTexture("_BackTex", tex);
        materialSkybox.SetTexture("_LeftTex", tex);
        materialSkybox.SetTexture("_RightTex", tex);
        materialSkybox.SetTexture("_UpTex", tex);
        materialSkybox.SetTexture("_DownTex", tex);

        return materialSkybox;
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
            sum += (int)Math.Ceiling((ratio[1] * (start + i)));
        }
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
    /*private void changeContext(Color[] colors)
    {
        int num = Random.Range(0, colors.Length - 1);
        Camera.main.backgroundColor = colors[num];
    }*/

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
    public void clickEvent()
    {
        Debug.Log("adid it");
    }
}