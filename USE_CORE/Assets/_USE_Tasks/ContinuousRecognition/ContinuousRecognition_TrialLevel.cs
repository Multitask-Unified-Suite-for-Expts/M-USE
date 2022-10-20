using UnityEngine;
using System.Collections.Generic;
using USE_States;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;
using System;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using USE_ExperimentTemplate_Block;
using System.IO;

public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef currentTrial => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();

    public bool CompletedAllTrials;
    public bool EndBlock;
    bool GotCorrect;
    bool stimIsChosen;

    private bool ContextActive;

    public GameObject GreenBorderPrefab;
    public GameObject RedBorderPrefab;
    public GameObject InsideGreyBorderPrefab;
    public GameObject Starfield;
    public List<GameObject> BorderPrefabList;
    [NonSerialized]
    public GameObject StartButton;

    public StimGroup trialStims;

    public List<int> ChosenStimIndices;

    public int NumCorrect_Block;
    public int NumTrials_Block;
    public int TokenBarCompletions_Block;

    public int TokenCount;

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
        State DisplayResults = new State("DisplayResults");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, DisplayStims, ChooseStim, TouchFeedback, TokenUpdate, DisplayResults, ITI });

        TokenFBController.enabled = false;
        Starfield.SetActive(false);

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + currentTrial.ContextName + ".png");
            ContextActive = true;
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            Starfield.SetActive(true);
            CreateStartButton();
            if (!variablesLoaded) LoadConfigUIVariables();
            TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) +
                                "\nNew_Stim: " + currentTrial.New_Stim.Count +
                                "\nPC_Stim: " + currentTrial.PC_Stim.Count +
                                "\nPNC_Stim: " + currentTrial.PNC_Stim.Count;
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial); //auto terminates after doing everything.

        SelectionHandler<ContinuousRecognition_StimDef> mouseHandler = new SelectionHandler<ContinuousRecognition_StimDef>();
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            currentTrial.IsNewStim = false;
            CompletedAllTrials = false;
            EndBlock = false;
            stimIsChosen = false;
            GotCorrect = false;
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
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            });

        //DISPLAY STIMs state -----------------------------------------------------------------------------------------------------
        //stim are turned on as soon as it enters DisplayStims state. no initialization method needed. 
        DisplayStims.AddTimer(() => currentTrial.DisplayStimsDuration, ChooseStim);

        //CHOOSE STIM state -------------------------------------------------------------------------------------------------------

        GameObject chosenStimObj = null;
        ContinuousRecognition_StimDef chosenStimDef = null;
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseStim);

        ChooseStim.AddUpdateMethod(() =>
        {
            chosenStimObj = mouseHandler.SelectedGameObject;
            chosenStimDef = mouseHandler.SelectedStimDef;

            if (chosenStimDef != null) //They Clicked a Stim
            {
                if (!ChosenStimIndices.Contains(chosenStimDef.StimCode - 1)) //THEY GUESSED RIGHT
                {
                    GotCorrect = true;
                    CorrectSelectionUpdateData();
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["CorrectResponse"]);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchTargetStart"]);

                    if (currentTrial.PNC_Stim.Contains(chosenStimDef.StimCode - 1)) //If they chose a PNC stim...
                    {
                        Debug.Log($"Right! Player chose a PNC_Stim with Index =  {chosenStimDef.StimCode - 1}");
                        currentTrial.PNC_Stim.Remove(chosenStimDef.StimCode - 1);
                    }
                    if (currentTrial.New_Stim.Contains(chosenStimDef.StimCode - 1)) //If they chose a New Stim...
                    {
                        Debug.Log($"Right! Player chose a NEW_Stim with Index =  {chosenStimDef.StimCode - 1}");
                        currentTrial.New_Stim.Remove(chosenStimDef.StimCode - 1);
                    }

                    chosenStimDef.PreviouslyChosen = true;
                    currentTrial.PC_Stim.Add(chosenStimDef.StimCode - 1);
                    ChosenStimIndices.Add(chosenStimDef.StimCode - 1); //also adding to chosenIndices so I can keep in order for display results. 

                    //TRYING TO REMOVE ALL NEW STIM THAT WEREN'T CHOSEN, FROM NEW STIM AND INTO PNC STIM. 
                    List<int> newStimToRemove = currentTrial.New_Stim.ToList();
                    foreach (var stim in newStimToRemove)
                    {
                        if (currentTrial.New_Stim.Contains(stim) && stim != chosenStimDef.StimCode - 1)
                        {
                            Debug.Log($"REMOVING STIM #{stim} FROM NEW AND ADDING TO PNC");
                            currentTrial.New_Stim.Remove(stim);
                            currentTrial.PNC_Stim.Add(stim);
                        }
                    }

                    //SINCE THEY GOT IT RIGHT, CHECK IF LAST TRIAL IN BLOCK. IF SO, MAKE THE GOTALLTRIALSCORRECT VARIABLE TRUE. 
                    if(TrialCount_InBlock + 1 == currentTrial.MaxNumTrials)
                    {
                        CompletedAllTrials = true;
                    }
                }
                else //THEY GUESSED WRONG
                {
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchDistractorStart"]);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["IncorrectResponse"]);
                    Debug.Log($"WRONG! CHOSE A PREVIOUSLY CHOSEN STIM WITH INDEX =  {chosenStimDef.StimCode - 1}");
                    currentTrial.WrongStimIndex = chosenStimDef.StimCode - 1; //identifies the stim they got wrong for Block FB purposes. 
                }
            }
         
            if (chosenStimObj != null) //if they chose a stimObj and it has a pointer to the actual stimDef.  
            {
                StimDefPointer pointer = chosenStimObj.GetComponent<StimDefPointer>();
                if (!pointer) return;
                else stimIsChosen = true;
            }
            currentTrial.IsNewStim = GotCorrect;
        });
        ChooseStim.SpecifyTermination(() => stimIsChosen, TouchFeedback);
        ChooseStim.AddTimer(() => selectObjectDuration.value, ITI, () =>     //if no choice, skip touchFB/tokenFB and go to display results so the event codes can send.
        {
            EndBlock = true;
            EventCodeManager.SendCodeImmediate(TaskEventCodes["NoChoice"]); 
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });

        //TOUCH FEEDBACK state -------------------------------------------------------------------------------------------------------
        TouchFeedback.AddInitializationMethod(() =>
        {
            if (!stimIsChosen) return;
            if (GotCorrect) HaloFBController.ShowPositive(chosenStimObj);
            else HaloFBController.ShowNegative(chosenStimObj);

            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
        });
        TouchFeedback.AddTimer(() => fbDuration.value, TokenUpdate, () => EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionVisualFbOff"]));

        //TOKEN UPDATE state -------------------------------------------------------------------------------------------------------
        TokenUpdate.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (GotCorrect)
            {
                TokenFBController.AddTokens(chosenStimObj, 1); //will put "currentTrial.StimTrialRewardMag" here !
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
                TokenCount++;
                HandleTokenUpdate();
            }
            else
            {
                TokenFBController.RemoveTokens(chosenStimObj, 1, Color.grey);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                TokenCount--;
                HandleTokenUpdate();
                EndBlock = true;
            }
        });
        TokenUpdate.AddTimer(() => currentTrial.TokenUpdateDuration + currentTrial.TokenRevealDuration , DisplayResults);


        DisplayResults.AddInitializationMethod(() =>
        {
            if (EndBlock || CompletedAllTrials) GenerateBlockFeedback(); //IF THEY LOST or CompletedAllTrial, DISPLAY RESULTS
        });

        DisplayResults.AddTimer(() => currentTrial.DisplayResultDuration, ITI, () =>
        {
            StartCoroutine(DestroyFeedbackBorders());
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });
            
        DisplayResults.SpecifyTermination(() => !EndBlock && !CompletedAllTrials, ITI, () =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });


        ITI.AddInitializationMethod(() => ContextActive = false);

        ITI.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            NumTrials_Block++;
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]); //next trial starts next frame
        });
        
        LogTrialData();
        LogFrameData();
    }



    private void HandleTokenUpdate()
    {
        if(TokenCount == currentTrial.TotalTokensNum)
        {
            TokenBarCompletions_Block++;
            TokenCount = 0;
        }
    }

    private void CorrectSelectionUpdateData()
    {
        NumCorrect_Block++;
    }

    private void GenerateBlockFeedback()
    {
        Starfield.SetActive(false);
        TokenFBController.enabled = false;

        StimGroup rightGroup = new StimGroup("Right");

        Vector3[] FeedbackLocations = new Vector3[ChosenStimIndices.Count + 1];

        int locCount = 0;
        for (int i = 0; i < ChosenStimIndices.Count; i++)
        {
            FeedbackLocations[i] = currentTrial.TrialFeedbackLocations[i];
            locCount++;
        }

        FeedbackLocations[locCount] = currentTrial.TrialFeedbackLocations[locCount];
        FeedbackLocations = CenterFeedbackLocations(FeedbackLocations);

        rightGroup = new StimGroup("Right", ExternalStims, ChosenStimIndices);
        GenerateFeedbackStim(rightGroup, FeedbackLocations.Take(FeedbackLocations.Length-1).ToArray());
        GenerateFeedbackBorders(rightGroup);

        StimGroup wrongGroup = new StimGroup("Wrong");
        StimDef wrongStim = ExternalStims.stimDefs[currentTrial.WrongStimIndex].CopyStimDef(wrongGroup);
        wrongStim.StimGameObject = null;

        GenerateFeedbackStim(wrongGroup, FeedbackLocations.Skip(FeedbackLocations.Length-1).Take(1).ToArray());
        GenerateFeedbackBorders(wrongGroup);

    }


    private Vector3[] CenterFeedbackLocations(Vector3[] locations)
    {
        //----- CENTER HORIZONTALLY--------
        float leftMargin = locations[0].x + 4f;
        float rightMargin;
        
        //if length less than 7, use first and last. 
        if (locations.Length < 7) rightMargin = 4f - locations[locations.Length - 1].x;
        //if greater than 6, use first(0) and firstRowLast(5)!!
        else rightMargin = 4f - locations[5].x;

        float leftMarginNeeded = (leftMargin + rightMargin) / 2;
        float leftShiftAmount = leftMarginNeeded - leftMargin;

        for (int i = 0; i < locations.Length; i++)  locations[i].x += leftShiftAmount;
       
        //----- CENTER VERTICALLY----------
        float topMargin = 2.25f - locations[0].y;
        float bottomMargin = locations[locations.Length - 1].y + 2.25f;

        float shiftDownNeeded = (topMargin + bottomMargin) / 2;
        float shiftDownAmount = shiftDownNeeded - topMargin;

        for(int i = 0; i < locations.Length; i++)   locations[i].y -= shiftDownAmount;
        
        return locations;
    }


    //Generate the correct number of New, PC, and PNC stim for each trial. 
    //Called when the trial is defined!
    //The TrialStims group are auto loaded in the SetupTrial StateInitialization, and destroyed in the FinishTrial StateTermination
    protected override void DefineTrialStims()
    {        
        if(TrialCount_InBlock == 0)
        {
            //clear stim lists in case it's NOT the first block!
            ClearCurrentTrialStimLists();

            //Add each block stim to unseen list.
            var numBlockStims = currentTrial.BlockStimIndices.Length;
            for (int i = 0; i < numBlockStims; i++)
            {
                currentTrial.Unseen_Stim.Add(currentTrial.BlockStimIndices[i]);
            }

            //Pick 2 random New stim and add to TrialStimIndices and NewStim. Also remove from UnseenStim.
            int[] tempArray = new int[currentTrial.NumObjectsMinMax[0]];
            for (int i = 0; i < currentTrial.NumObjectsMinMax[0]; i++) //Pick2 stim randomly from blockStimIndices. 
            {
                int ranNum = Random.Range(0, numBlockStims);
                while (Array.IndexOf(tempArray, ranNum) != -1)
                {
                    ranNum = Random.Range(0, numBlockStims);
                }
                tempArray[i] = ranNum;
                currentTrial.TrialStimIndices.Add(ranNum);
                currentTrial.Unseen_Stim.Remove(ranNum);
                currentTrial.New_Stim.Add(ranNum);
            }
            Debug.Log($"{currentTrial.New_Stim.Count} STIM WERE GENERATED FOR TRIAL #{TrialCount_InBlock}");

            trialStims = new StimGroup("TrialStims", ExternalStims, currentTrial.TrialStimIndices);
            foreach (ContinuousRecognition_StimDef stim in trialStims.stimDefs) stim.PreviouslyChosen = false;
            trialStims.SetLocations(currentTrial.TrialStimLocations);
            trialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenUpdate")); //Visible when start DisplayStims, invisible when finish TokenUpdate.
            TrialStims.Add(trialStims);

        }

        else 
        {
            currentTrial.TrialStimIndices.Clear();

            float[] stimPercentages = GetStimRatioPercentages(currentTrial.InitialStimRatio);
            int[] stimNumbers = GetStimNumbers(stimPercentages);

            int PC_Num = stimNumbers[0];
            int New_Num = stimNumbers[1];
            int PNC_Num = stimNumbers[2];

            Debug.Log($"CALCULATED FOR TRIAL {TrialCount_InBlock}: PC_Stim: {PC_Num}, New_Stim: {New_Num}, PNC_Stim: {PNC_Num}");

            List<int> NewStim_Copy = ShuffleList(currentTrial.Unseen_Stim).ToList();
            if (NewStim_Copy.Count > 1) NewStim_Copy = NewStim_Copy.GetRange(0, New_Num);
            for (int i = 0; i < NewStim_Copy.Count; i++)
            {
                int current = NewStim_Copy[i];
                currentTrial.TrialStimIndices.Add(current);
                currentTrial.Unseen_Stim.Remove(current);
                currentTrial.New_Stim.Add(current);
            }

            List<int> PC_Copy = ShuffleList(currentTrial.PC_Stim).ToList();
            if (PC_Copy.Count > 1) PC_Copy = PC_Copy.GetRange(0, PC_Num);
            for(int i = 0; i < PC_Copy.Count; i++)
            {
                currentTrial.TrialStimIndices.Add(PC_Copy[i]);
            }

            List<int> PNC_Copy = ShuffleList(currentTrial.PNC_Stim).ToList();
            if(PNC_Copy.Count > 1) PNC_Copy = PNC_Copy.GetRange(0, PNC_Num);
            for(int i = 0; i < PNC_Copy.Count; i++)
            {
                currentTrial.TrialStimIndices.Add(PNC_Copy[i]);
            }

            int numGenerated = NewStim_Copy.Count + PC_Copy.Count + PNC_Copy.Count;
            int numNeeded = PC_Num + New_Num + PNC_Num;
            if (numGenerated == numNeeded) Debug.Log($"THE CORRECT AMOUNT OF STIM ({numGenerated}) WERE GENERATED FOR TRIAL #{TrialCount_InBlock}");
            else Debug.Log($"NUM OF STIM GENERATED ({numGenerated}) DOES NOT EQUAL THE NUMBER NEEDED ({numNeeded}) FOR TRIAL #{TrialCount_InBlock}");

            trialStims = new StimGroup($"TrialStims", ExternalStims, currentTrial.TrialStimIndices);
            trialStims.SetLocations(currentTrial.TrialStimLocations);
            trialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenUpdate")); //Visible when start DisplayStims, invisible when finish TokenUpdate.
            TrialStims.Add(trialStims); 

        }
        getLog(currentTrial.Unseen_Stim, "Unseen_Stims");
        getLog(currentTrial.PC_Stim, "PC_Stims");
        getLog(currentTrial.New_Stim, "New_Stims");
        getLog(currentTrial.PNC_Stim, "PNC_Stims");
        getLog(currentTrial.TrialStimIndices, "TrialStimIndices");
    }

    private void GenerateFeedbackStim(StimGroup group, Vector3[] locations)
    {
        TrialStims.Add(group);
        group.SetLocations(locations);
        group.LoadStims();
        group.ToggleVisibility(true);
    }

    private void GenerateFeedbackBorders(StimGroup group)
    {
        if (BorderPrefabList.Count == 0) BorderPrefabList = new List<GameObject>();

        if (group.stimGroupName == "Wrong")
        {
            GameObject borderPrefab = Instantiate(RedBorderPrefab, group.stimDefs[0].StimGameObject.transform.position, Quaternion.identity);
            BorderPrefabList.Add(borderPrefab);
        }

        else
        {
            foreach (ContinuousRecognition_StimDef stim in group.stimDefs)
            {
                if (stim.StimCode - 1 == currentTrial.WrongStimIndex)
                {
                    GameObject borderPrefab = Instantiate(InsideGreyBorderPrefab, stim.StimGameObject.transform.position, Quaternion.identity);
                    BorderPrefabList.Add(borderPrefab); //add each to list so I can destroy them together
                }
                else
                {
                    GameObject borderPrefab = Instantiate(GreenBorderPrefab, stim.StimGameObject.transform.position, Quaternion.identity);
                    BorderPrefabList.Add(borderPrefab); //add to list so I can access and destroy them together. 
                }
            }

        }

    }

    private IEnumerator DestroyFeedbackBorders() //trying to match up the dissapearance of borders and stim.
    {
        yield return new WaitForSeconds(.5f);
        foreach (GameObject border in BorderPrefabList)
        {
            if (border != null) border.SetActive(false);
        }
        BorderPrefabList.Clear();
    }


    private List<int> ShuffleList(List<int> list)
    {
        if (list.Count == 1) return list;
        
        else
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                int temp = list[i];
                int rand = Random.Range(1, list.Count);
                list[i] = list[rand];
                list[rand] = temp;
            }
            return list;
        }
    }

    protected override bool CheckBlockEnd()
    {
        return EndBlock;
    }

    private void LogTrialData()
    {
        TrialData.AddDatum("Context", () => currentTrial.ContextName);
        TrialData.AddDatum("Unseen_Stim", () => currentTrial.Unseen_Stim);
        TrialData.AddDatum("PC_Stim", () => currentTrial.PC_Stim);
        TrialData.AddDatum("New_Stim", () => currentTrial.New_Stim);
        TrialData.AddDatum("PNC_Stim", () => currentTrial.PNC_Stim);
        TrialData.AddDatum("IsNewStim", () => currentTrial.IsNewStim);
        TrialData.AddDatum("CurrentTrialStims", () => currentTrial.TrialStimIndices);
        TrialData.AddDatum("Context", () => currentTrial.ContextName);
    }

    private void LogFrameData()
    {
        //LOT MORE TO ADD!!!!!
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("Context", () => currentTrial.ContextName);
        FrameData.AddDatum("StartButton", () => StartButton);
        FrameData.AddDatum("ContextActive", () => ContextActive);
    }

    private void ClearCurrentTrialStimLists()
    {
        currentTrial.New_Stim.Clear();
        currentTrial.PNC_Stim.Clear();
        currentTrial.PC_Stim.Clear();
        currentTrial.Unseen_Stim.Clear();
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
        int PC_Num = (int)Math.Floor(idealStimRatio[0] * currentTrial.NumTrialStims);
        int New_Num = (int)Math.Floor(idealStimRatio[1] * currentTrial.NumTrialStims);
        int PNC_Num = (int)Math.Floor(idealStimRatio[2] * currentTrial.NumTrialStims);
        if (PC_Num == 0) PC_Num = 1;
        if (New_Num == 0) New_Num = 1;
        if (PNC_Num == 0) PNC_Num = 1;

        int temp = 0;
        while ((PC_Num + New_Num + PNC_Num) < currentTrial.NumTrialStims)
        {
            if (temp % 3 == 0) PC_Num += 1;
            else if (temp % 3 == 1) New_Num += 1;
            else PNC_Num += 1;
            temp++;
        }
        return new[] { PC_Num, New_Num, PNC_Num };
    }

    private void CreateStartButton()
    {
        Texture2D tex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
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

    //public void CreateCanvasAndComponents()
    //{
    //    //put these up top if end up using this;
    //    //GameObject canvasGO;
    //    //Canvas canvas;
    //    //GameObject resultsTextGO;
    //    //Text resultsText;

    //    GameObject canvasGO = new GameObject();
    //    canvasGO.AddComponent<Canvas>();
    //    canvasGO.AddComponent<CanvasScaler>();
    //    canvasGO.AddComponent<GraphicRaycaster>();

    //    Canvas canvas = canvasGO.GetComponent<Canvas>();
    //    canvas.name = "Results_Canvas";
    //    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

    //    GameObject resultsTextGO = new GameObject();
    //    resultsTextGO.transform.parent = canvasGO.transform;
    //    resultsTextGO.AddComponent<Text>();

    //    Text resultsText = resultsTextGO.GetComponent<Text>();
    //    resultsText.name = "Results_Text";
    //    resultsText.text = "Results";
    //    resultsText.fontSize = 36;
    //    resultsText.color = Color.white;

    //    RectTransform rectTransform = resultsText.GetComponent<RectTransform>();
    //    rectTransform.localPosition = new Vector3(-825, -175, 0);
    //    rectTransform.sizeDelta = new Vector2(600, 200);
    //}


}
