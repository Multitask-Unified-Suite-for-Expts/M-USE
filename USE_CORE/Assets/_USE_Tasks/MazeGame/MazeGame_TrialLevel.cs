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
using Unity.Collections.LowLevel.Unsafe;
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
/*    public Texture2D tileTex;
    public Texture2D mazeBgTex;*/
    
    private int totalErrors_InTrial;
    private int ruleAbidingErrors_InTrial;
    private int ruleBreakingErrors_InTrial;
    private int retouchCorrect_InTrial;
    private int retouchErroneous_InTrial;
    private int correctTouches_InTrial;
    private int backtrackErrors_InTrial;
    private int perseverativeRetouchErrors_InTrial;
    private int perseverativeBackTrackErrors_InTrial;
    private int perseverativeRuleAbidingErrors_InTrial;
    private int perseverativeRuleBreakingErrors_InTrial;

    private bool choiceMade;
    public List<float> choiceDurationsList = new List<float>();

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
    private GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewTextLoaded;

    // Slider & Animation variables
    private float finishedFbDuration;

    public MazeManager MazeManager;

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
            //FileLoadingDelegate = LoadTileAndBgTextures; //Set file loading delegate

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

            if (CurrentTrialDef.TileFlashingRatio != 0)
                StateAfterDelay = TileFlashFeedback;
            else
                StateAfterDelay = ChooseTile;

            DelayDuration = mazeOnsetDelay.value;

            SliderFBController.ConfigureSlider(sliderSize.value);
            SliderFBController.SliderGO.SetActive(true);


            CurrentTaskLevel.CalculateBlockSummaryString();
            SetTrialSummaryString();
        });
        ChooseTile.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down

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
            if (!Session.WebBuild && !MazeManager.IsFreePlay() && !playerViewTextLoaded)
                CreateTextOnExperimenterDisplay();
            
            SetTrialSummaryString(); // called every frame to update duration info
            
            if (SelectionHandler.SuccessfulSelections.Count > 0)
            {
                if (SelectionHandler.LastSuccessfulSelection.SelectedGameObject.GetComponent<Tile>() != null)
                {
                    choiceMade = true;
                    AddChoiceDurationToDataTrackers();
                    MazeManager.SetSelectedTileGO(SelectionHandler.LastSuccessfulSelection.SelectedGameObject);
                    SelectionHandler.ClearSelections();
                }
            }
        });
        ChooseTile.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {
            SelectionHandler.HandlerActive = false;

            if (MazeManager.GetSelectedTile() == MazeManager.GetStartTile())
            {
                //If the tile that is selected is the start tile
                MazeManager.SetMazeStarted(true);
                if (Session.SessionDef.EventCodesActive)
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeStart"]);
            }

            if (MazeManager.GetSelectedTile() == MazeManager.GetFinishTile() && MazeManager.GetCurrentMaze().mNextStep == MazeManager.GetCurrentMaze().mFinish)
            {
                //if the tile that is selected is the end tile, stop the timer
                MazeManager.SetMazeFinished(true);
                AddMazeDurationToDataTrackers();
                if (Session.SessionDef.EventCodesActive)
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeFinish"]);
            }
        });
        ChooseTile.SpecifyTermination(() => (MazeManager.mazeDuration > CurrentTrialDef.MaxMazeDuration) || (MazeManager.choiceDuration > CurrentTrialDef.MaxChoiceDuration), () => FinishTrial, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            CurrentTaskLevel.MazeDurations_InBlock.Add(null);
            CurrentTaskLevel.MazeDurations_InTask.Add(null);

            CurrentTaskLevel.ChoiceDurations_InBlock.Add(null);
            CurrentTaskLevel.ChoiceDurations_InTask.Add(null);

            runningPercentError.Add(null);
        });

        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TileFbOn"]);
            choiceMade = false;

            // This is what actually determines the result of the tile choice
            MazeManager.GetSelectedTile().GetComponent<Tile>().SelectionFeedback();
            ManageDataHandlers();
            
            if (!MazeManager.IsFreePlay())
                percentError = (float)decimal.Divide(totalErrors_InTrial, MazeManager.GetCurrentMaze().mNumSquares);

            SliderFBController.SetUpdateDuration(MazeManager.GetSelectedTile().GetComponent<Tile>().GetTileFbDuration());

            if (MazeManager.IsCorrectReturnToLast())
            {
                AudioFBController.Play("Positive");
                if (CurrentTrialDef.ErrorPenalty)
                    SliderFBController.UpdateSliderValue(MazeManager.GetSelectedTile().GetComponent<Tile>().GetSliderValueChange());
            }
            else if (MazeManager.IsErroneousReturnToLast())
            {
                AudioFBController.Play("Negative");
            }
            else if (MazeManager.IsCorrectSelection())
            {
                if (MazeManager.IsMazeFinished())
                {
                    SliderFBController.SetFlashingDuration(flashingFbDuration.value);
                    SliderFBController.UpdateSliderValue(1); // fill up the remainder of the slider
                }
                else
                    SliderFBController.UpdateSliderValue(MazeManager.GetSelectedTile().GetComponent<Tile>().GetSliderValueChange());
              
                if (!Session.WebBuild && !MazeManager.IsFreePlay())
                    PlayerViewParent.transform.Find((MazeManager.GetCurrentPathIndex() + 1).ToString()).GetComponent<Text>().color = new Color(0, 0.392f, 0);
            }
            else if (MazeManager.GetSelectedTile() != null && MazeManager.IsErroneousReturnToLast())
            {
                AudioFBController.Play("Negative");
                if (CurrentTrialDef.ErrorPenalty && MazeManager.GetConsecutiveErrorCount() == 1)
                    SliderFBController.UpdateSliderValue(-MazeManager.GetSelectedTile().GetComponent<Tile>().GetSliderValueChange());
            }
            else
            {
                AudioFBController.Play("Negative");
            }

        });
        SelectionFeedback.AddUpdateMethod(() => { SetTrialSummaryString(); });// called every frame to update duration info

        SelectionFeedback.AddTimer(() => MazeManager.IsMazeFinished() ? finishedFbDuration : MazeManager.GetSelectedTile().GetComponent<Tile>().GetTileFbDuration(), Delay, () =>
        {
            if (CurrentTaskDef.UsingFixedRatioReward)
                HandleFixedRatioReward();
            if (MazeManager.IsOutOfMoves() || MazeManager.IsMazeFinished())
            {
                StateAfterDelay = ITI;
                DelayDuration = 0;

                 HandleMazeCompletion();
                
            }
            else if (CheckTileFlash() || (CurrentTrialDef.TileFlashingRatio != 0 && GameObject.Find(MazeManager.GetCurrentMaze().mNextStep).GetComponent<Tile>().assignedTileFlash))
                StateAfterDelay = TileFlashFeedback;
            else
                StateAfterDelay = ChooseTile; // could be incorrect or correct but it will still go back

            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TileFbOff"]);


            SetTrialSummaryString(); //Set the Trial Summary String to reflect the results of choice
            CurrentTaskLevel.CalculateBlockSummaryString();
            CurrentTaskLevel.SetTaskSummaryString();
            
            MazeManager.ResetSelectionClassifications();
        });
        TileFlashFeedback.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["FlashingTileFbOn"]);
            
            if (!tiles.IsActive)
                tiles.ToggleVisibility(true);

            if (MazeManager.GetCurrentMaze().loadingSquareMaze)
                MazeManager.ActivateMazeBackground();

            if (!MazeManager.IsMazeStarted())
                MazeManager.FlashNextCorrectTile(MazeManager.GetStartTile()); 
            else
                MazeManager.FlashNextCorrectTile(GameObject.Find(MazeManager.GetCurrentMaze().mNextStep));
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

            if (MazeManager.IsMazeFinished())
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
            if (MazeManager.IsFreePlay())
                Session.TargetObjects.Add(stim.StimGameObject);
            else if (MazeManager.GetCurrentMaze().mPath.Contains(stim.StimGameObject.name))
                Session.TargetObjects.Add(stim.StimGameObject);
        }
    }

/*    private IEnumerator LoadTileAndBgTextures()
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
    }*/



    protected override bool CheckBlockEnd()
    {
        return CurrentTaskLevel.TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningPercentError,
            CurrentTrialDef.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
            CurrentTaskLevel.MaxTrials_InBlock);
    }
    protected override void DefineTrialStims()
    {
        LoadConfigVariables();

        if (!MazeManager.GetMazeManagerInitialized())
            MazeManager.Initialize(this, CurrentTrialDef, CurrentTaskDef);
        tiles = MazeManager.CreateMaze();
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
        if (MazeManager.GetConsecutiveErrorCount() >= 2)
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["PerseverativeError"]);

            return true;
        }
        return false;
    }

    private void HandleFixedRatioReward()
    {
        if (MazeManager.IsCorrectSelection() && correctTouches_InTrial % CurrentTrialDef.RewardRatio == 0)
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
        if (!MazeManager.IsFreePlay())
        {
            percentError = (float)decimal.Divide(totalErrors_InTrial, MazeManager.GetCurrentMaze().mNumSquares);
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
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");

        finishedFbDuration = flashingFbDuration.value + correctFbDuration.value;
        configVariablesLoaded = true;
    }

    

    private void DefineTrialData()
    {
        TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("MazeDefName", () => mazeDefName);
        TrialData.AddDatum("MazeDefName", () => CurrentTrialDef.MazeName);
        TrialData.AddDatum("SelectedTiles", () => string.Join(",", MazeManager.GetAllSelectedTiles().Select(go => go.name)));
        TrialData.AddDatum("TotalErrors", () => totalErrors_InTrial);
        TrialData.AddDatum("CorrectTouches", () => correctTouches_InTrial); 
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrect_InTrial);
        TrialData.AddDatum("RetouchErroneous", () => retouchErroneous_InTrial);
        
        TrialData.AddDatum("BacktrackingErrors", () => backtrackErrors_InTrial);
        TrialData.AddDatum("Rule-AbidingErrors", () => ruleAbidingErrors_InTrial);
        TrialData.AddDatum("Rule-BreakingErrors", () => ruleBreakingErrors_InTrial);
        
        TrialData.AddDatum("PerseverativeRetouchErrors", () => perseverativeRetouchErrors_InTrial);
        TrialData.AddDatum("PerseverativeBackTrackErrors", () => perseverativeBackTrackErrors_InTrial);
        TrialData.AddDatum("PerseverativeRuleAbidingErrors", () => perseverativeRuleAbidingErrors_InTrial);
        TrialData.AddDatum("PerseverativeRuleBreakingErrors", () => perseverativeRuleBreakingErrors_InTrial);
        TrialData.AddDatum("MazeDuration", () => MazeManager.mazeDuration);
        //TrialData.AddDatum("TotalClicks", ()=>MouseTracker.GetClickCount().Length);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("ChoiceMade", () => choiceMade);
        FrameData.AddDatum("SelectedObject", () => MazeManager?.GetSelectedTile()?.name);
        FrameData.AddDatum("StartedMaze", () => MazeManager?.IsMazeStarted());
    }
    private void DisableSceneElements()
    {
        MazeManager.DeactivateMazeElements();

        tiles.ToggleVisibility(false);
        if (GameObject.Find("SliderCanvas") != null)
            DeactivateChildren(GameObject.Find("SliderCanvas"));
    }

    private void CreateTextOnExperimenterDisplay()
    {
        // sets parent for any PlayerViewPanelController elements on experimenter display
        bool invalidLocalPositionEncountered = false;
        bool allDifferentPositions = tiles.stimDefs
            .Select(sd => sd.StimGameObject.GetComponent<Tile>().GetTilePosition())
            .Where(position => position != null)
            .Distinct()
            .Count() == tiles.stimDefs.Count;

        if (!allDifferentPositions)
        {
            invalidLocalPositionEncountered = true;
            return;
        }

        for (int i = 0; i < MazeManager.GetCurrentMaze().mPath.Count; i++)
        {
            foreach (StimDef sd in tiles.stimDefs)
            {
                Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
                Vector3? tilePosition = tileComponent.GetTilePosition();
                Vector2 textSize = new Vector2(200, 200);
                
                if (tileComponent.GetChessCoord() == MazeManager.GetCurrentMaze().mPath[i])
                {
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint((Vector3)tileComponent.GetTilePosition()), PlayerViewParent.transform);
                    playerViewText = PlayerViewPanelController.CreateTextObject((i + 1).ToString(), (i + 1).ToString(),
                        Color.red, textLocation, textSize, PlayerViewParent.transform);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewText.SetActive(true);
                }
            }
        }

        playerViewTextLoaded = !invalidLocalPositionEncountered;
    }

    public override void FinishTrialCleanup()
    {
        DisableSceneElements();
        if (!Session.WebBuild)
            DestroyChildren(PlayerViewParent);

        MazeManager.MazeCleanUp();


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
    }

    public override void ResetTrialVariables()
    {
        SliderFBController.ResetSliderBarFull();
        choiceMade = false;
        configVariablesLoaded = false;
        playerViewTextLoaded = false;
        MazeManager.ResetMazeVariables();
        correctTouches_InTrial = 0;
        perseverativeRetouchErrors_InTrial = 0;
        perseverativeBackTrackErrors_InTrial = 0;
        perseverativeRuleAbidingErrors_InTrial = 0;
        perseverativeRuleBreakingErrors_InTrial = 0;
        backtrackErrors_InTrial  = 0;
        ruleAbidingErrors_InTrial = 0;
        ruleBreakingErrors_InTrial = 0;
        totalErrors_InTrial = 0;
        retouchCorrect_InTrial = 0;
        retouchErroneous_InTrial = 0;
    }
    private void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Maze Name: </b>" + CurrentTrialDef.MazeName +
                             "\n<b>Tile Flashing ratio: </b>" + CurrentTrialDef.TileFlashingRatio +
                             "\n" +
                             "\n<b>Percent Error: </b>" + String.Format("{0:0.00}%", percentError * 100) +
                             "\n<b>Total Errors: </b>" + totalErrors_InTrial +
                             "\n" +
                             "\n<b>Rule-Abiding Errors: </b>" + ruleAbidingErrors_InTrial +
                             "\n<b>Rule-Breaking Errors: </b>" + ruleBreakingErrors_InTrial +
                             "\n<b>Backtrack Errors: </b>" + backtrackErrors_InTrial +
                             "\n<b>Retouch Correct: </b>" + retouchCorrect_InTrial +
                             "\n<b>Retouch Erroneous: </b>" + retouchErroneous_InTrial +
                             "\n" +
                             "\n<b>Choice Duration: </b>" + String.Format("{0:0.0}", MazeManager.choiceDuration) +
                             "\n<b>Maze Duration: </b>" + String.Format("{0:0.0}", MazeManager.mazeDuration) +
                             "\n<b>Slider Value: </b>" + String.Format("{0:0.00}", SliderFBController.Slider.value);

    }

    private void HandleRuleBreakingErrorData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["RuleBreakingError"]);

        ruleBreakingErrors_InTrial++;
        CurrentTaskLevel.RuleBreakingErrors_InBlock++;
        CurrentTaskLevel.RuleBreakingErrors_InTask++;

        totalErrors_InTrial++;
        CurrentTaskLevel.TotalErrors_InBlock++;
        CurrentTaskLevel.TotalErrors_InTask++;
    }

    private void HandleRuleAbidingErrorData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["RuleAbidingError"]);

        ruleAbidingErrors_InTrial++;
        CurrentTaskLevel.RuleAbidingErrors_InBlock++;
        CurrentTaskLevel.RuleAbidingErrors_InTask++;

        totalErrors_InTrial++;
        CurrentTaskLevel.TotalErrors_InBlock++;
        CurrentTaskLevel.TotalErrors_InTask++;
    }

    private void HandleBackTrackErrorData()
    {
        backtrackErrors_InTrial++;
        CurrentTaskLevel.BacktrackErrors_InBlock++;
        CurrentTaskLevel.BacktrackErrors_InTask++;
    }
    private void HandleRetouchErroneousData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["LastCorrectSelection"]);

        totalErrors_InTrial++;
        CurrentTaskLevel.TotalErrors_InBlock++;
        CurrentTaskLevel.TotalErrors_InTask++;
        
        retouchErroneous_InTrial++;
        CurrentTaskLevel.RetouchError_InBlock++;
        CurrentTaskLevel.RetouchError_InTask++;
    }

    private void HandleRetouchCorrectData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["LastCorrectSelection"]);

        retouchCorrect_InTrial++;
        CurrentTaskLevel.RetouchCorrect_InBlock++;
        CurrentTaskLevel.RetouchCorrect_InTask++;
    }

    private void HandleCorrectTouch()
    {
        Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");

        correctTouches_InTrial++;
        CurrentTaskLevel.CorrectTouches_InBlock++;
        CurrentTaskLevel.CorrectTouches_InTask++;
    }

    private void HandlePerseverativeRetouchError()
    {
        perseverativeRetouchErrors_InTrial++;
        CurrentTaskLevel.PerseverativeRetouchErrors_InBlock++;
        CurrentTaskLevel.PerseverativeRetouchErrors_InTask++;
    }
    private void HandlePerseverativeBackTrackError()
    {
        perseverativeBackTrackErrors_InTrial++;
        CurrentTaskLevel.PerseverativeBackTrackErrors_InBlock++;
        CurrentTaskLevel.PerseverativeBackTrackErrors_InTask++;
    }
    private void HandlePerseverativeRuleAbidingError()
    {
        perseverativeRuleAbidingErrors_InTrial++;
        CurrentTaskLevel.PerseverativeRuleAbidingErrors_InBlock++;
        CurrentTaskLevel.PerseverativeRuleAbidingErrors_InTask++;
    }
    private void HandlePerseverativeRuleBreakingError()
    {
        perseverativeRuleBreakingErrors_InTrial++;
        CurrentTaskLevel.PerseverativeRuleBreakingErrors_InBlock++;
        CurrentTaskLevel.PerseverativeRuleBreakingErrors_InTask++;
    }


    private void ManageDataHandlers()
    {
        string errorType = MazeManager.DetermineErrorType();
        switch (errorType)
        {
            case "retouchCurrentTilePositionCorrect":
                HandleRetouchCorrectData();
                HandleCorrectTouch();
                break;
            case "correctNextTileChoice":
                HandleCorrectTouch();
                break;
            case "backTrackError":
                HandleBackTrackErrorData();
                HandleRuleBreakingErrorData();
                break;
            case "retouchCurrentTilePositionError":
                HandleRetouchErroneousData();
                break;
            case "ruleAbidingError":
                HandleRuleAbidingErrorData();
                break;
            case "ruleBreakingError":
                HandleRuleBreakingErrorData();
                break;
            case "perseverativeBackTrackError":
                HandleBackTrackErrorData();
                HandlePerseverativeBackTrackError();
                HandleRuleBreakingErrorData();
                HandlePerseverativeRuleBreakingError();
                break;
            case "perseverativeRetouchCurrentTilePositionError":
                HandlePerseverativeRetouchError();
                HandleRetouchErroneousData();
                break;
            case "perseverativeRuleAbidingError":
                HandlePerseverativeRuleAbidingError();
                HandleRuleAbidingErrorData();
                break;
            case "perseverativeRuleBreakingError":
                HandlePerseverativeRuleBreakingError();
                HandleRuleBreakingErrorData();
                break;
            
        }
        
    }

}
