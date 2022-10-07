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
using USE_Settings;
using USE_ExperimentTemplate_Classes;

public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef currentTrial => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();

    public bool EndBlock = false;

    //Game object variables
    [NonSerialized]
    public GameObject StartButton;

    //Stim Group
    public StimGroup trialStims;

    //Config Variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration;

    //Misc Variables
    public string MaterialFilePath;
    private bool variablesLoaded;


    public override void DefineControlLevel()
    {        
        State InitTrial = new State("InitTrial");
        State DisplayStims = new State("DisplayStims");
        State ChooseStim = new State("ChooseStim");
        State TouchFeedback = new State("TouchFeedback");
        State TokenUpdate = new State("TokenUpdate");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, DisplayStims, ChooseStim, TouchFeedback, TokenUpdate, ITI });

        TokenFBController.enabled = false;

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            CreateStartButton();
            if (!variablesLoaded) LoadConfigUIVariables();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial); //auto terminates after doing everything.

        SelectionHandler<ContinuousRecognition_StimDef> mouseHandler = new SelectionHandler<ContinuousRecognition_StimDef>();
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox($"{MaterialFilePath}\\{currentTrial.ContextName}.png");
            TokenFBController.enabled = false;
            SetTokenFeedbackTimes();
            StartButton.SetActive(true);
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), 
            DisplayStims, () =>
            {
                StartButton.SetActive(false);
                TokenFBController.enabled = true;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            });

        //DISPLAY STIMs state -----------------------------------------------------------------------------------------------------
        //stim are turned on as soon as it enters DisplayStims state. no initialization method needed. 
        DisplayStims.AddTimer(() => currentTrial.DisplayStimsDuration, ChooseStim);

        //CHOOSE STIM state -------------------------------------------------------------------------------------------------------
        bool stimIsChosen = false;
        bool isNew = false;
        GameObject chosenStimObj = null;
        ContinuousRecognition_StimDef chosenStimDef = null;
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseStim);

        ChooseStim.AddUpdateMethod(() =>
        {
            stimIsChosen = false;
            isNew = false;
            chosenStimObj = mouseHandler.SelectedGameObject;
            chosenStimDef = mouseHandler.SelectedStimDef;

            if(chosenStimDef != null)
            {
                if(chosenStimDef.PreviouslyChosen == false) //THEY GUESSED RIGHT
                {
                    isNew = true;

                    if(currentTrial.PNC_Stim.Contains(chosenStimDef.StimCode - 1)) //If they chose a PNC stim...
                    {
                        Debug.Log($"Right! Player chose a PNC_Stim with Index =  {chosenStimDef.StimCode - 1}");
                        currentTrial.PNC_Stim.Remove(chosenStimDef.StimCode - 1);
                    }
                    if(currentTrial.New_Stim.Contains(chosenStimDef.StimCode -1))
                    {
                        Debug.Log($"Right! Player chose a NEW_Stim with Index =  {chosenStimDef.StimCode - 1}");
                        currentTrial.New_Stim.Remove(chosenStimDef.StimCode - 1);
                    }

                    chosenStimDef.PreviouslyChosen = true;
                    currentTrial.PC_Stim.Add(chosenStimDef.StimCode - 1);

                    List<int> newStimToRemove = currentTrial.New_Stim;

                    for (int i = 0; i < newStimToRemove.Count; i++)
                    {
                        var currentNum = newStimToRemove[i];
                        currentTrial.PNC_Stim.Add(currentNum);
                        currentTrial.New_Stim.Remove(currentNum);
                        newStimToRemove.Remove(currentNum);
                    }
                }
                else //THEY GUESSED WRONG
                {
                    Debug.Log($"WRONG! CHOSE A PREVIOUSLY CHOSEN STIM WITH INDEX =  {chosenStimDef.StimCode - 1}");
                    currentTrial.WrongStimIndex = chosenStimDef.StimCode-1; //identifies the stim they got wrong for Block FB purposes. 
                    //EndBlock = true;  //I'm going to end the block down below during ITI, so they have time for wrong feedback.
                }
            }
            if (chosenStimObj != null) //if they chose a stimObj and it has a pointer to the actual stimDef.  
            {
                StimDefPointer pointer = chosenStimObj.GetComponent<StimDefPointer>();
                if (!pointer) return;
                else stimIsChosen = true;
            }
            currentTrial.IsNewStim = isNew;
        });
        ChooseStim.SpecifyTermination(() => stimIsChosen, TouchFeedback, () => AddTrialData());
        ChooseStim.AddTimer(() => selectObjectDuration.value, TouchFeedback);

        //TOUCH FEEDBACK state -------------------------------------------------------------------------------------------------------
        TouchFeedback.AddInitializationMethod(() =>
        {
            if (!stimIsChosen) return;
            if (isNew) HaloFBController.ShowPositive(chosenStimObj);
            else HaloFBController.ShowNegative(chosenStimObj);

            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
        });
        TouchFeedback.AddTimer(() => fbDuration.value, TokenUpdate);

        //TOKEN UPDATE state -------------------------------------------------------------------------------------------------------
        TokenUpdate.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (isNew)
            {
                TokenFBController.AddTokens(chosenStimObj, 1); //will put "currentTrial.StimTrialRewardMag" here !
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else
            {
                AudioFBController.Play("Negative");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
            }
        });
        TokenUpdate.SpecifyTermination(() => !TokenFBController.IsAnimating(), ITI);

        ITI.AddInitializationMethod(() =>
        {
            if (!isNew) EndBlock = true; //telling block to end! doing here instead of when they get wrong so that there's time for the feedback. 
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        //Block will end if ENDBLOCK = True   (see CheckBlockEnd method below)
    }

    private void ShuffleList(List<int> list)
    {
        for(int i = 0; i < list.Count-1; i++)
        {
            int temp = list[i];
            int rand = Random.Range(1, list.Count);
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    protected override bool CheckBlockEnd()
    {
        return EndBlock;
    }

    private void AddTrialData()
    {
        TrialData.AddDatum("PC_Stim", () => currentTrial.PC_Stim);
        TrialData.AddDatum("Unseen_Stim", () => currentTrial.Unseen_Stim);
        TrialData.AddDatum("PNC_Stim", () => currentTrial.PNC_Stim);
        TrialData.AddDatum("IsNewStim", () => currentTrial.IsNewStim);
        TrialData.AddDatum("CurrentTrialStims", () => currentTrial.TrialStimIndices);
        //TrialData.AddDatum("PC_Count", () => currentTrial.PC_Count);
        //TrialData.AddDatum("PNC_Count", () => currentTrial.PNC_Count);
        //TrialData.AddDatum("Unseen_Count", () => currentTrial.New_Count);
    }
   

    protected override void DefineTrialStims()
    {
        //Generate the correct number of New, PC, and PNC stim for each trial. 
        //Called when the trial is defined!
        //The stim are auto loaded in the SetupTrial StateInitialization, and destroyed in the FinishTrial StateTermination

        currentTrial.TrialCount++;
        currentTrial.TrialStimIndices.Clear();

        if(currentTrial.TrialCount == 1)
        {
            var numBlockStims = currentTrial.BlockStimIndices.Length;
            for (int i = 0; i < numBlockStims; i++)
            {
                currentTrial.Unseen_Stim.Add(currentTrial.BlockStimIndices[i]); //Add each block stim to unseen list.
            }

            int[] tempArray = new int[currentTrial.NumObjectsMinMax[0]];
            for (int i = 0; i < currentTrial.NumObjectsMinMax[0]; i++) //Pick2 stim randomly from blockStimIndices. 
            {
                int ranNum = Random.Range(0, numBlockStims);
                while (Array.IndexOf(tempArray, ranNum) != -1)
                {
                    ranNum = Random.Range(0, numBlockStims);
                }
                tempArray[i] = ranNum;
                currentTrial.TrialStimIndices.Add(ranNum); //Add to TrialStimIndices
                currentTrial.Unseen_Stim.Remove(ranNum);   //Remove from Unseen stim list
                currentTrial.New_Stim.Add(ranNum); //Add it to list of new stim's indices.
            }
            Debug.Log($"{currentTrial.New_Stim.Count} STIM WERE GENERATED FOR TRIAL #{currentTrial.TrialCount}");
        }
        
        else 
        {
            float[] stimPercentages = GetStimRatioPercentages(currentTrial.InitialStimRatio);
            int[] stimNumbers = GetStimNumbers(stimPercentages);
            int PC_Num = stimNumbers[0];
            int New_Num = stimNumbers[1];
            int PNC_Num = stimNumbers[2];

            Debug.Log($"NUM OF PC_Stim CALCULATED FOR TRIAL {currentTrial.TrialCount} = {PC_Num}");
            Debug.Log($"NUM OF NEW_Stim CALCULATED FOR TRIAL {currentTrial.TrialCount} = {New_Num}");
            Debug.Log($"NUM OF PNC_Stim CALCULATED FOR TRIAL {currentTrial.TrialCount} = {PNC_Num}");


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

            List<int> NewStim_Sublist = currentTrial.Unseen_Stim.GetRange(0, New_Num);
            foreach (int stimIndex in NewStim_Sublist)
            {
                Debug.Log($"CURRENT TRIAL NEW_STIM: {stimIndex}");
                currentTrial.TrialStimIndices.Add(stimIndex);
                currentTrial.Unseen_Stim.Remove(stimIndex);
                currentTrial.New_Stim.Add(stimIndex);
            }

            //Generate PC_Stim needed for trial. Add to Indices.
            List<int> PC_Copy = currentTrial.PC_Stim;
            ShuffleList(PC_Copy);
            foreach (int stimIndex in PC_Copy)
            {
                Debug.Log($"CURRENT TRIAL PC:  {stimIndex}");
                currentTrial.TrialStimIndices.Add(stimIndex);
            }

            //Generate PNC_Stim needed for trial. Add to Indices.
            List<int> PNC_Copy = currentTrial.PNC_Stim;
            ShuffleList(PNC_Copy);
            foreach(int stimIndex in PNC_Copy)
            {
                Debug.Log($"CURRENT TRIAL PNC:  {stimIndex}");
                currentTrial.TrialStimIndices.Add(stimIndex);
            }

            int numGenerated = currentTrial.New_Stim.Count + currentTrial.PC_Stim.Count + currentTrial.PNC_Stim.Count;
            int numNeeded = PC_Num + New_Num + PNC_Num;
            if (numGenerated == numNeeded) Debug.Log($"THE CORRECT AMOUNT OF STIM ({numGenerated}) WERE GENERATED FOR TRIAL #{currentTrial.TrialCount}");
            else Debug.Log($"NUM OF STIM GENERATED ({numGenerated}) DOES NOT EQUAL THE NUMBER NEEDED ({numNeeded}) FOR TRIAL #{currentTrial.TrialCount}");
        }

        getLog(currentTrial.Unseen_Stim, "Unseen_Stims");
        getLog(currentTrial.PC_Stim, "PC_Stims");
        getLog(currentTrial.PNC_Stim, "PNC_Stims");
        getLog(currentTrial.TrialStimIndices, "TrialStimIndices");

        trialStims = new StimGroup($"TrialStims", ExternalStims, currentTrial.TrialStimIndices);
        trialStims.SetLocations(currentTrial.TrialStimLocations);
        trialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenUpdate")); //Visible when start DisplayStims, invisible when finish TokenUpdate.
        TrialStims.Add(trialStims);
    }

    private void getLog(List<int> list, string name)
    {
        string result = name + ": ";
        foreach (var item in list)
        {
            result += item.ToString() + ", ";
        }
        Debug.Log(result);
    }

    private float[] GetStimRatioPercentages(int[] ratioArray)
    {
        float sum = 0;
        float[] stimPercentages = new float[ratioArray.Length];

        foreach(var num in ratioArray)
        {
            sum += num;
        }
        for(int i = 0; i < ratioArray.Length; i++)
        {
            stimPercentages[i] = ratioArray[i] / sum;
        }
        return stimPercentages;
    }

    private int[] GetStimNumbers(float[] idealStimRatio)
    {
        int PC_num = (int)Math.Floor(idealStimRatio[0] * currentTrial.NumTrialStims);
        int New_num = (int)Math.Floor(idealStimRatio[1] * currentTrial.NumTrialStims);
        int PNC_num = (int)Math.Floor(idealStimRatio[2] * currentTrial.NumTrialStims);
        if (PC_num == 0) PC_num = 1;
        if (New_num == 0) New_num = 1;
        if (PNC_num == 0) PNC_num = 1;

        int temp = 0;
        while ((PC_num + New_num + PNC_num) < currentTrial.NumTrialStims)
        {
            if (temp % 3 == 0) PC_num += 1;
            else if (temp % 3 == 1) New_num += 1;
            else PNC_num += 1;
            temp++;
        }
        Debug.Log($"PC NUM = {PC_num}");
        Debug.Log($"NEW NUM = {New_num}");
        Debug.Log($"PNC NUM = {PNC_num}");

        return new[] { PC_num, New_num, PNC_num };


        //if (TrialCount_InBlock == 1) return new int[3] { 1, 1, 1 };
        
        //int PC_Num = (int) Math.Floor(idealStimRatio[0] * currentTrial.NumTrialStims);  
        //int New_Num = (int) Math.Floor(idealStimRatio[1] * currentTrial.NumTrialStims);
        //int PNC_Num = (int) Math.Floor(idealStimRatio[2] * currentTrial.NumTrialStims);

        //while ((PC_Num + New_Num + PNC_Num) < currentTrial.NumTrialStims)
        //{
        //    var numShort = currentTrial.NumTrialStims - (PC_Num + New_Num + PNC_Num);

        //    if (currentTrial.NumTrialStims % 2 == 0) New_Num++;
            
        //    else //odd amount of TotalTrialStim
        //    {
        //        if (numShort == 1) PNC_Num++;
        //        if (numShort == 2)
        //        {
        //            PNC_Num++;
        //            New_Num++;
        //        }
        //        else Debug.Log("CALCULATED STIM # ARE SHORT BY MORE THAN 2!");
        //    }  
        //}
        //Debug.Log($"PC NUM = {PC_Num}");
        //Debug.Log($"NEW NUM = {New_Num}");
        //Debug.Log($"PNC NUM = {PNC_Num}");

        //return new int[3] { PC_Num, New_Num, PNC_Num};
    }

    private void CreateStartButton()
    {
        Texture2D tex = LoadPNG($"{MaterialFilePath}\\StartButtonImage.png");
        Rect rect = new Rect(new Vector2(0, 0), new Vector2(1, 1));

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
        else Debug.Log("[ERROR] TaskDef is not in config folder");

        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer spriteRend = startButton.AddComponent<SpriteRenderer>();
        spriteRend.sprite = Sprite.Create(tex, new Rect(rect.x / 2, rect.y / 2, tex.width / 2, tex.height / 2 ), new Vector2(.5f, .5f), 100f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = buttonScale;
        startButton.transform.position = buttonPosition;
        StartButton = startButton;
    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        variablesLoaded = true;
    }

    private void SetTokenFeedbackTimes()
    {
        TokenFBController.SetRevealTime(currentTrial.TokenRevealDuration);
        TokenFBController.SetUpdateTime(currentTrial.TokenUpdateDuration);
    }

}
