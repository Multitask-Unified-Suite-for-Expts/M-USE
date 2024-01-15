/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConfigDynamicUI;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using USE_UI;

public class MazeGame_TrialLevel : ControlLevel_Trial_Template
{
    // Generic Task Variables
    public GameObject MG_CanvasGO;
    private GameObject StartButton;
    [HideInInspector] public string ContextName;
    // Block Ending Variable
    public List<float?> runningPercentError = new List<float?>();
    private float percentError;
    public int MinTrials;

    // Maze Object Variables
    public string mazeDefName;

    // Tile objects
    public StimGroup tiles; // top of trial level with other variable definitions
    public StimGroup landmarks;
    public Texture2D tileTex;
    public Texture2D mazeBgTex;
    
    private int totalErrors_InTrial;
    private int ruleAbidingErrors_InTrial;
    private int ruleBreakingErrors_InTrial;
    private int retouchCorrect_InTrial;
    private int retouchErroneous_InTrial;
    private int correctTouches_InTrial;
    private int backtrackErrors_InTrial;
    private int perseverativeErrors_InTrial;
    private bool choiceMade;
    public List<float> choiceDurationsList = new List<float>();
    private int flashingCounter;

    // Config UI Variables
    private bool configVariablesLoaded;
    [HideInInspector]
    public ConfigNumber mazeOnsetDelay;
    public ConfigNumber correctFbDuration;
    public ConfigNumber previousCorrectFbDuration;
    public ConfigNumber incorrectRuleAbidingFbDuration;
    public ConfigNumber incorrectRuleBreakingFbDuration;
    public ConfigNumber itiDuration;
    public ConfigNumber flashingFbDuration;
    public ConfigNumber sliderSize;
    public ConfigNumber tileBlinkingDuration;
    public ConfigNumber maxMazeDuration;
    public ConfigNumber minObjectTouchDuration;
    public ConfigNumber maxObjectTouchDuration;

    // Player View Variables
    private PlayerViewPanel PlayerViewPanelController;
    private GameObject PlayerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;

    // Touch Evaluation Variables
    private GameObject selectedGO;
    // private StimDef selectedSD;

    // Slider & Animation variables
    private float finishedFbDuration;
    public float tileFbDuration;

    [FormerlySerializedAs("mazeManager")] public MazeManager MazeManager;

    public MazeGame_TrialDef CurrentTrialDef => GetCurrentTrialDef<MazeGame_TrialDef>();
    public MazeGame_TaskLevel CurrentTaskLevel => GetTaskLevel<MazeGame_TaskLevel>();
   public MazeGame_TaskDef CurrentTaskDef => GetTaskDef<MazeGame_TaskDef>();


    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State InitTrial = new State("InitTrial");
        State ChooseTile = new State("ChooseTile");
        State SelectionFeedback = new State("SelectionFeedback");
        State TileFlashFeedback = new State("TileFlashFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State>
            { InitTrial, ChooseTile, SelectionFeedback, TileFlashFeedback, ITI  });
        string[] stateNames =
            { "InitTrial", "ChooseTile", "SelectionFeedback", "TileFlashFeedback", "ITI"};

        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            FileLoadingDelegate = LoadTileAndBgTextures; //Set file loading delegate

            MazeManager.Initialize(this, CurrentTrialDef, CurrentTaskDef);

            if (!Session.WebBuild) //player view variables
            {
                PlayerViewPanelController = gameObject.AddComponent<PlayerViewPanel>();
                PlayerViewParent = GameObject.Find("MainCameraCopy");
            }
        });


        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(MG_CanvasGO.GetComponent<Canvas>(), CurrentTaskDef.StartButtonPosition, CurrentTaskDef.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }
            
            CurrentTaskLevel.SetTaskSummaryString();
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        var SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, ITI);
        TouchFBController.EnableTouchFeedback(SelectionHandler, CurrentTaskDef.TouchFeedbackDuration, CurrentTaskDef.StartButtonScale * 15, MG_CanvasGO, false);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            InitializeSelectionHandler(SelectionHandler);
        });

        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), Delay, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeOn"]);

            if (CurrentTrialDef.GuidedMazeSelection)
                StateAfterDelay = TileFlashFeedback;
            else
                StateAfterDelay = ChooseTile;

            DelayDuration = mazeOnsetDelay.value;

            SliderFBController.ConfigureSlider(sliderSize.value);
            SliderFBController.SliderGO.SetActive(true);

            if (!Session.WebBuild && !MazeManager.freePlay)
                CreateTextOnExperimenterDisplay();

            CurrentTaskLevel.CalculateBlockSummaryString();
            SetTrialSummaryString();
        });

        ChooseTile.AddSpecificInitializationMethod(() =>
        {
            //TouchFBController.SetPrefabSizes(tileScale);
            MazeManager.ActivateMazeElements();
            MazeManager.choiceStartTime = Time.unscaledTime;
            if (MazeManager.mazeStartTime == 0)
                MazeManager.mazeStartTime = Time.unscaledTime;

            SelectionHandler.HandlerActive = true;
            if (SelectionHandler.AllSelections.Count > 0)
                SelectionHandler.ClearSelections();
            
        });
        ChooseTile.AddUpdateMethod(() =>
        {
            SetTrialSummaryString(); // called every frame to update duration info

            if (SelectionHandler.SuccessfulSelections.Count > 0)
            {
                if (SelectionHandler.LastSuccessfulSelection.SelectedGameObject.GetComponent<Tile>() != null)
                {
                    choiceMade = true;
                    AddChoiceDurationToDataTrackers();
                    selectedGO = SelectionHandler.LastSuccessfulSelection.SelectedGameObject;
                    MazeManager.selectedTilesGO.Add(selectedGO);
                    SelectionHandler.ClearSelections();
                }
            }
        });
        ChooseTile.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {
            SelectionHandler.HandlerActive = false;

            if (selectedGO == MazeManager.startTileGO)
            {
                //If the tile that is selected is the start tile
                MazeManager.startedMaze = true;
                if (Session.SessionDef.EventCodesActive)
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeStart"]);
            }

            if (selectedGO == MazeManager.finishTileGO && MazeManager.currentMaze.mNextStep == MazeManager.currentMaze.mFinish)
            {
                //if the tile that is selected is the end tile, stop the timer
                MazeManager.finishedMaze = true;
                AddMazeDurationToDataTrackers();
                if (Session.SessionDef.EventCodesActive)
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeFinish"]);
            }
        });
/*        ChooseTile.SpecifyTermination(() => (MazeManager.mazeDuration > CurrentTrialDef.MaxMazeDuration) || (MazeManager.choiceDuration > CurrentTrialDef.MaxChoiceDuration), () => FinishTrial, () =>
        {
            Timeout Termination
            Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            CurrentTaskLevel.MazeDurations_InBlock.Add(null);
            CurrentTaskLevel.MazeDurations_InTask.Add(null);

            CurrentTaskLevel.ChoiceDurations_InBlock.Add(null);
            CurrentTaskLevel.ChoiceDurations_InTask.Add(null);

            runningPercentError.Add(null);
        });*/

        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TileFbOn"]);
            choiceMade = false;

            // This is what actually determines the result of the tile choice
            selectedGO.GetComponent<Tile>().SelectionFeedback();
            if (!MazeManager.freePlay)
                percentError = (float)decimal.Divide(totalErrors_InTrial, MazeManager.currentMaze.mNumSquares);

            finishedFbDuration = (tileFbDuration + flashingFbDuration.value);
            SliderFBController.SetUpdateDuration(tileFbDuration);
            SliderFBController.SetFlashingDuration(finishedFbDuration);

            if (MazeManager.returnToLast)
            {
                AudioFBController.Play("Positive");
                if (CurrentTrialDef.ErrorPenalty)
                    SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
            }
            else if (MazeManager.erroneousReturnToLast)
            {
                AudioFBController.Play("Negative");
            }
            else if (MazeManager.correctSelection)
            {
              

                if (MazeManager.finishedMaze)
                    SliderFBController.UpdateSliderValue(1); // fill up the remainder of the slider
                else
                    SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
              
                if (!Session.WebBuild && !MazeManager.freePlay)
                    PlayerViewParent.transform.Find((MazeManager.currentPathIndex + 1).ToString()).GetComponent<Text>().color = new Color(0, 0.392f, 0);
            }
            else if (selectedGO != null && MazeManager.erroneousReturnToLast)
            {
                AudioFBController.Play("Negative");
                if (CurrentTrialDef.ErrorPenalty && MazeManager.consecutiveErrors == 1)
                    SliderFBController.UpdateSliderValue(-selectedGO.GetComponent<Tile>().sliderValueChange);
            }
            else
            {
                AudioFBController.Play("Negative");
            }

            selectedGO = null; //Reset selectedGO before the next touch evaluation
            MazeManager.ResetSelectionClassifications();
        });
        SelectionFeedback.AddUpdateMethod(() => { SetTrialSummaryString(); });// called every frame to update duration info

        SelectionFeedback.AddTimer(() => MazeManager.finishedMaze ? finishedFbDuration : tileFbDuration, Delay, () =>
        {
            if (CurrentTaskDef.UsingFixedRatioReward)
                HandleFixedRatioReward();
            if (MazeManager.outOfMoves || MazeManager.finishedMaze)
            {
                StateAfterDelay = ITI;
                DelayDuration = 0;

                if (MazeManager.finishedMaze)
                {
                    HandleMazeCompletion();
                }
            }
            else if (CheckTileFlash() || (CurrentTrialDef.GuidedMazeSelection && GameObject.Find(MazeManager.currentMaze.mNextStep).GetComponent<Tile>().assignedTileFlash))
                StateAfterDelay = TileFlashFeedback;
            else
                StateAfterDelay = ChooseTile; // could be incorrect or correct but it will still go back

            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TileFbOff"]);


            SetTrialSummaryString(); //Set the Trial Summary String to reflect the results of choice
            CurrentTaskLevel.CalculateBlockSummaryString();
            CurrentTaskLevel.SetTaskSummaryString();
        });
        TileFlashFeedback.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["FlashingTileFbOn"]);
            
            if (!tiles.IsActive)
                tiles.ToggleVisibility(true);
            
            if(MazeManager.currentMaze.loadingSquareMaze)
                MazeManager.mazeBackgroundGO.SetActive(true);
            
            if (!MazeManager.startedMaze)
                MazeManager.startTileGO.GetComponent<Tile>().FlashTile();
            else
                MazeManager.currentTilePositionGO.GetComponent<Tile>().FlashTile();
        });
        TileFlashFeedback.AddTimer(() => tileBlinkingDuration.value, ChooseTile, () =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["FlashingTileFbOff"]);
        });
        ITI.AddSpecificInitializationMethod(() =>
        {
            if (!Session.WebBuild)
                DestroyChildren(PlayerViewParent);

            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeOff"]);

            if (MazeManager.finishedMaze)
                Session.EventCodeManager.AddToFrameEventCodeBuffer("SliderFbController_SliderCompleteFbOff");

            if (CurrentTaskDef.NeutralITI)
            {
                ContextName = "NeutralITI";
                CurrentTaskLevel.SetSkyBox(GetContextNestedFilePath(!string.IsNullOrEmpty(CurrentTaskDef.ContextExternalFilePath) ? CurrentTaskDef.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath, "NeutralITI"));
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        DefineTrialData();
        DefineFrameData();
    }


    //This method is for EventCodes and gets called automatically at end of SetupTrial:
    public override void AddToStimLists()
    {
        foreach (StimDef stim in tiles.stimDefs)
        {
            if (MazeManager.freePlay)
                Session.TargetObjects.Add(stim.StimGameObject);
            else if (MazeManager.currentMaze.mPath.Contains(stim.StimGameObject.name))
                Session.TargetObjects.Add(stim.StimGameObject);
        }
    }

    private IEnumerator LoadTileAndBgTextures()
    {
        if (mazeBgTex != null) // only want it to load them the first time. 
        {
            TrialFilesLoaded = true; //Setting this to true triggers the LoadTrialTextures state to end
            yield break;
        }

        string contextPath = !string.IsNullOrEmpty(CurrentTaskDef.ContextExternalFilePath) ? CurrentTaskDef.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath;

        if (Session.UsingServerConfigs)
        {
            yield return StartCoroutine(LoadTexture(contextPath + "/" + CurrentTaskDef.TileTexture + ".png", textureResult =>
            {
                if (textureResult != null)
                    tileTex = textureResult;
                else
                    Debug.LogWarning("TILE TEX RESULT IS NULL!");
            }));

            yield return StartCoroutine(LoadTexture(contextPath + "/" + CurrentTaskDef.MazeBackgroundTexture + ".png", textureResult =>
            {
                if (textureResult != null)
                    mazeBgTex = textureResult;
                else
                    Debug.LogWarning("MAZE BACKGROUND TEXTURE RESULT IS NULL!");
            }));
        }
        else if (Session.UsingDefaultConfigs)
        {
            tileTex = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/{CurrentTaskDef.TileTexture}");
            mazeBgTex = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/{CurrentTaskDef.MazeBackgroundTexture}");
        }
        else if (Session.UsingLocalConfigs)
        {
            tileTex = LoadExternalPNG(GetContextNestedFilePath(contextPath, CurrentTaskDef.TileTexture));
            mazeBgTex = LoadExternalPNG(GetContextNestedFilePath(contextPath, CurrentTaskDef.MazeBackgroundTexture));
        }

        TrialFilesLoaded = true; //Setting this to true triggers the LoadTrialTextures state to end
    }



    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningPercentError,
            CurrentTrialDef.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
            CurrentTaskLevel.MaxTrials_InBlock);
    }
    protected override void DefineTrialStims()
    {
        LoadConfigVariables();
        tiles = MazeManager.CreateMaze(tileTex, mazeBgTex);
        TrialStims.Add(tiles);
    }
    
    private void InitializeSelectionHandler(SelectionTracking.SelectionTracker.SelectionHandler selectionHandler)
    {
        selectionHandler.HandlerActive = true;
        if (selectionHandler.AllSelections.Count > 0)
            selectionHandler.ClearSelections();
        selectionHandler.MinDuration = minObjectTouchDuration.value;
        selectionHandler.MaxDuration = maxObjectTouchDuration.value;
    }
    
    public bool CheckTileFlash()
    {
        if (MazeManager.consecutiveErrors >= 2)
        {
            // Should provide flashing feedback of the last correct tile
            Debug.Log("*Perseverative Error*");

            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["PerseverativeError"]);

            perseverativeErrors_InTrial++;
            CurrentTaskLevel.PerseverativeErrors_InBlock++;
            CurrentTaskLevel.PerseverativeErrors_InTask++;

            return true;
        }
        return false;
    }

    private void HandleFixedRatioReward()
    {
        if (MazeManager.correctSelection && correctTouches_InTrial % CurrentTrialDef.RewardRatio == 0)
        {
            if (Session.SyncBoxController != null)
            {
                Session.SyncBoxController.SendRewardPulses(1, CurrentTrialDef.PulseSize);
                CurrentTaskLevel.NumRewardPulses_InBlock++; ;
                CurrentTaskLevel.NumRewardPulses_InTask++;
            }
        }
    }

    private void HandleMazeCompletion()
    {
        if (!MazeManager.freePlay)
        {
            percentError = (float)decimal.Divide(totalErrors_InTrial, MazeManager.currentMaze.mNumSquares);
            runningPercentError.Add(percentError);
        }

        CurrentTaskLevel.NumSliderBarFull_InBlock++;
        CurrentTaskLevel.NumSliderBarFull_InTask++;
        Session.EventCodeManager.AddToFrameEventCodeBuffer("SliderFbController_SliderCompleteFbOn");

        if (Session.SyncBoxController != null)
        {
            Session.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
            CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
            CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
        }
        
        DisableSceneElements();

    }

    private void AddChoiceDurationToDataTrackers()
    {
        choiceDurationsList.Add(MazeManager.choiceDuration);
        CurrentTaskLevel.ChoiceDurations_InBlock.Add(MazeManager.choiceDuration);
        CurrentTaskLevel.ChoiceDurations_InTask.Add(MazeManager.choiceDuration);

        MazeManager.choiceDuration = 0;
    }

    private void AddMazeDurationToDataTrackers()
    {
        MazeManager.mazeStartTime = 0;
        CurrentTaskLevel.MazeDurations_InBlock.Add(MazeManager.mazeDuration);
        CurrentTaskLevel.MazeDurations_InTask.Add(MazeManager.mazeDuration);
    }
    private void LoadConfigVariables()
    {
        //config UI variables
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        flashingFbDuration = ConfigUiVariables.get<ConfigNumber>("flashingFbDuration");
        mazeOnsetDelay = ConfigUiVariables.get<ConfigNumber>("mazeOnsetDelay");
        correctFbDuration = ConfigUiVariables.get<ConfigNumber>("correctFbDuration");
        previousCorrectFbDuration = ConfigUiVariables.get<ConfigNumber>("previousCorrectFbDuration");
        incorrectRuleAbidingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleAbidingFbDuration");
        incorrectRuleBreakingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleBreakingFbDuration");
        tileBlinkingDuration = ConfigUiVariables.get<ConfigNumber>("tileBlinkingDuration");
        maxMazeDuration = ConfigUiVariables.get<ConfigNumber>("maxMazeDuration");
        maxMazeDuration = ConfigUiVariables.get<ConfigNumber>("maxMazeDuration");
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");

        configVariablesLoaded = true;
    }

    

    private void DefineTrialData()
    {
        TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("MazeDefName", () => mazeDefName);
        TrialData.AddDatum("SelectedTiles", () => string.Join(",", MazeManager.selectedTilesGO));
        TrialData.AddDatum("TotalErrors", () => $"[{string.Join(", ", totalErrors_InTrial)}]");
        // TrialData.AddDatum("CorrectTouches", () => correctTouches_InTrial); DOESN'T GIVE ANYTHING USEFUL, JUST PATH LENGTH
        TrialData.AddDatum("RetouchCorrect", () => $"[{string.Join(", ", retouchCorrect_InTrial)}]");
        TrialData.AddDatum("RetouchErroneous", () => $"[{string.Join(", ", retouchErroneous_InTrial)}]");
        TrialData.AddDatum("PerseverativeErrors", () => $"[{string.Join(", ", perseverativeErrors_InTrial)}]");
        TrialData.AddDatum("BacktrackingErrors", () => $"[{string.Join(", ", backtrackErrors_InTrial)}]");
        TrialData.AddDatum("Rule-AbidingErrors", () => $"[{string.Join(", ", ruleAbidingErrors_InTrial)}]");
        TrialData.AddDatum("Rule-BreakingErrors", () => $"[{string.Join(", ", ruleBreakingErrors_InTrial)}]");
        TrialData.AddDatum("MazeDuration", () => MazeManager.mazeDuration);
        //TrialData.AddDatum("TotalClicks", ()=>MouseTracker.GetClickCount().Length);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("ChoiceMade", () => choiceMade);
        FrameData.AddDatum("SelectedObject", () => selectedGO?.name);
        FrameData.AddDatum("StartedMaze", () => MazeManager.startedMaze);
    }
    private void DisableSceneElements()
    {
        DeactivateChildren(MazeManager.gameObject);
        if (GameObject.Find("SliderCanvas") != null)
            DeactivateChildren(GameObject.Find("SliderCanvas"));
    }

    private void CreateTextOnExperimenterDisplay()
    {
        // sets parent for any PlayerViewPanelController elements on experimenter display
        for (int i = 0; i < MazeManager.currentMaze.mPath.Count; i++)
        {
            foreach (StimDef sd in tiles.stimDefs)
            {
                Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
                Vector2 textSize = new Vector2(200, 200);

                if (tileComponent.mCoord.chessCoord == MazeManager.currentMaze.mPath[i])
                {
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(tileComponent.transform.position), PlayerViewParent.transform);
                    playerViewText = PlayerViewPanelController.CreateTextObject((i + 1).ToString(), (i + 1).ToString(),
                        Color.red, textLocation, textSize, PlayerViewParent.transform);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewText.SetActive(true);
                }
            }
        }
    }

    public override void FinishTrialCleanup()
    {
        DisableSceneElements();
        if (!Session.WebBuild)
            DestroyChildren(PlayerViewParent);

        DestroyChildren(MazeManager.tileConnectorsContainerGO);
        DestroyChildren(MazeManager.landmarksContainerGO);


        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;

        if (AbortCode == 0)
            CurrentTaskLevel.CalculateBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }

        // Reset the maze so that the correct next step is the start
        MazeManager.currentMaze.mNextStep = MazeManager.currentMaze.mStart;
    }

    public override void ResetTrialVariables()
    {
        SliderFBController.ResetSliderBarFull();
        selectedGO = null;
        choiceMade = false;
        configVariablesLoaded = false;
        Session.MouseTracker.ResetClicks();
        MazeManager.ResetMazeVariables();
        correctTouches_InTrial = 0;
        perseverativeErrors_InTrial = 0;
        backtrackErrors_InTrial  = 0;
        ruleAbidingErrors_InTrial = 0;
        ruleBreakingErrors_InTrial = 0;
        totalErrors_InTrial = 0;
        retouchCorrect_InTrial = 0;
        retouchErroneous_InTrial = 0;
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Maze Name: </b>" + mazeDefName +
                             "\n<b>Guided Selection: </b>" + CurrentTrialDef.GuidedMazeSelection +
                             "\n" +
                             "\n<b>Percent Error: </b>" + String.Format("{0:0.00}%", percentError * 100) +
                             "\n<b>Total Errors: </b>" + totalErrors_InTrial +
                             "\n" +
                             "\n<b>Rule-Abiding Errors: </b>" + ruleAbidingErrors_InTrial +
                             "\n<b>Rule-Breaking Errors: </b>" + ruleBreakingErrors_InTrial +
                             "\n<b>Perseverative Errors: </b>" + perseverativeErrors_InTrial +
                             "\n<b>Backtrack Errors: </b>" + backtrackErrors_InTrial +
                             "\n<b>Retouch Correct: </b>" + retouchCorrect_InTrial +
                             "\n<b>Retouch Erroneous: </b>" + retouchErroneous_InTrial +
                             "\n" +
                             "\n<b>Choice Duration: </b>" + String.Format("{0:0.0}", MazeManager.choiceDuration) +
                             "\n<b>Maze Duration: </b>" + String.Format("{0:0.0}", MazeManager.mazeDuration) +
                             "\n<b>Slider Value: </b>" + String.Format("{0:0.00}", SliderFBController.Slider.value);

    }


    public void HandleRuleBreakingError(int currentPathIndex)
    {

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["RuleBreakingError"]);

        ruleBreakingErrors_InTrial++;
        CurrentTaskLevel.RuleBreakingErrors_InBlock++;
        CurrentTaskLevel.RuleBreakingErrors_InTask++;

        totalErrors_InTrial++;
        CurrentTaskLevel.TotalErrors_InBlock++;
        CurrentTaskLevel.TotalErrors_InTask++;
        MazeManager.consecutiveErrors++;

        tileFbDuration = MazeManager.tileSettings.incorrectRuleBreakingSeconds;
    }

    public void HandleRuleAbidingError(int currentPathIndex)
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["RuleAbidingError"]);

        totalErrors_InTrial++;
        CurrentTaskLevel.TotalErrors_InBlock++;
        CurrentTaskLevel.TotalErrors_InTask++;

        ruleAbidingErrors_InTrial++;
        CurrentTaskLevel.RuleAbidingErrors_InBlock++;
        CurrentTaskLevel.RuleAbidingErrors_InTask++;

        MazeManager.consecutiveErrors++;
        tileFbDuration = MazeManager.tileSettings.incorrectRuleAbidingSeconds;
    }

    public void HandleBackTrackError(int currentPathIndex)
    {
        backtrackErrors_InTrial++;
        CurrentTaskLevel.BacktrackErrors_InBlock++;
        CurrentTaskLevel.BacktrackErrors_InTask++;
    }
    public void HandleRetouchErroneous(int currentPathIndex)
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["LastCorrectSelection"]);

        MazeManager.erroneousReturnToLast = true;
        retouchErroneous_InTrial++;
        CurrentTaskLevel.RetouchErroneous_InBlock++;
        CurrentTaskLevel.RetouchErroneous_InTask++;

        MazeManager.consecutiveErrors = 0;
        tileFbDuration = MazeManager.tileSettings.prevCorrectFeedbackSeconds;
    }

    public void HandleRetouchCorrect(int currentPathIndex)
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["LastCorrectSelection"]);

        MazeManager.returnToLast = true;

        retouchCorrect_InTrial++;
        CurrentTaskLevel.RetouchCorrect_InBlock++;
        CurrentTaskLevel.RetouchCorrect_InTask++;

        MazeManager.consecutiveErrors = 0;
        tileFbDuration = MazeManager.tileSettings.prevCorrectFeedbackSeconds;
    }

    public void HandleCorrectTouch()
    {
        Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");

        correctTouches_InTrial++;
        CurrentTaskLevel.CorrectTouches_InBlock++;
        CurrentTaskLevel.CorrectTouches_InTask++;

        MazeManager.correctSelection = true;
        MazeManager.consecutiveErrors = 0;

        tileFbDuration = MazeManager.tileSettings.correctFeedbackSeconds;

    }


}
