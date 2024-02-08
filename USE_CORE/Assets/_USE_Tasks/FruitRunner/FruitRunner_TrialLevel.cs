using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using FruitRunner_Namespace;
using ConfigDynamicUI;
using TMPro;
using UnityEngine.UI;

public class FruitRunner_TrialLevel : ControlLevel_Trial_Template
{
    public FruitRunner_TrialDef CurrentTrial => GetCurrentTrialDef<FruitRunner_TrialDef>();
    public FruitRunner_TaskLevel CurrentTaskLevel => GetTaskLevel<FruitRunner_TaskLevel>();
    public FruitRunner_TaskDef CurrentTask => GetTaskDef<FruitRunner_TaskDef>();

    //Set in Inspector:
    public GameObject FruitRunner_CanvasGO;
    public List<Material> SkyboxMaterials;
    public List<Color> FogColors;
    public FR_ScoreManager ScoreManager;
    public GameObject SpeedSliderGO;
    public Slider SpeedSlider;

    private GameObject StartButton;

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, setupDuration, playDuration, celebrationDuration, itiDuration;

    GameObject PlayerGO;
    private FR_PlayerManager PlayerManager;

    public GameObject MovementCirclesControllerGO;
    public MovementCirclesController MovementCirclesController;

    public GameObject FloorManagerGO;
    public FR_FloorManager FloorManager;

    public GameObject ItemSpawnerGO;
    public FR_ItemSpawner ItemSpawner;


    private StimGroup trialStims;

    private CameraIntroMovement CamMovement;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("IniTrial");
        State Setup = new State("Setup");
        State Play = new State("Play");
        State Celebration = new State("Celebration");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, Setup, Play, Celebration, ITI });

        Add_ControlLevel_InitializationMethod(() =>
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
                    StartButton = Session.USE_StartButton.CreateStartButton(FruitRunner_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            CurrentTaskLevel.TaskCam.fieldOfView = 50;
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, Setup); //Setup Handler

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            LoadConfigUIVariables();

            SetTrialSummaryString();

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();

            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;

            TokenFBController.enabled = false;
            TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);
            TokenFBController.SetTokenBarValue(CurrentTrial.NumInitialTokens);

        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), Setup, () =>
        {
            TokenFBController.AdjustTokenBarSizing(100);
            TokenFBController.SetRevealTime(.15f);
            TokenFBController.SetUpdateTime(.25f);

            Skybox skybox = CurrentTaskLevel.TaskCam.GetComponent<Skybox>();
            skybox.material = CurrentTrial.SkyboxName.ToLower() == "random" ? SkyboxMaterials[Random.Range(0, SkyboxMaterials.Count - 1)] : Resources.Load<Material>("Materials/" + CurrentTrial.SkyboxName);
            skybox.enabled = true;

            RenderSettings.fogColor = FogColors[SkyboxMaterials.IndexOf(skybox.material)];

            CurrentTaskLevel.TaskCam.fieldOfView = 60;
        });

        //Setup state ----------------------------------------------------------------------------------------------------------------------------------------------
        Setup.AddSpecificInitializationMethod(() =>
        {
            PlayerGO = Instantiate(Resources.Load<GameObject>("Prefabs/Player"));
            PlayerGO.name = "Player";
            PlayerGO.tag = "Player";
            PlayerManager = PlayerGO.GetComponent<FR_PlayerManager>();
            PlayerManager.TokenFbController = TokenFBController;
            PlayerManager.DisableUserInput();
            PlayerManager.AllowItemPickupAnimations = CurrentTrial.AllowItemPickupAnimations;
            PlayerManager.CanvasTransform = FruitRunner_CanvasGO.transform; //Pass in the canvas for the player's MovementCirclesController
           
            ItemSpawnerGO = new GameObject("ItemSpawner");
            ItemSpawner = ItemSpawnerGO.AddComponent<FR_ItemSpawner>();
            ItemSpawner.SetupQuaddleList(trialStims.stimDefs);
            ItemSpawner.SetQuaddleGeneralPositions(CurrentTrial.TrialStimGeneralPositions);
            ItemSpawner.SetSpawnOrder(CurrentTrial.TrialGroup_InSpawnOrder);
            ItemSpawner.gameObject.SetActive(true);
            
            FloorManagerGO = new GameObject("FloorManager");
            FloorManager = FloorManagerGO.AddComponent<FR_FloorManager>();
            FloorManager.SetTotalTiles(CurrentTrial.TrialGroup_InSpawnOrder.Length, CurrentTrial.NumGroups);
            FloorManager.FloorMovementSpeed = CurrentTrial.FloorMovementSpeed;
            FloorManager.TileScale_Z = CurrentTrial.FloorTileLength;
            FloorManager.gameObject.SetActive(true);

            if (CamMovement != null)
                Destroy(CamMovement);
            CamMovement = Camera.main.gameObject.AddComponent<CameraIntroMovement>();
            CamMovement.StartMovement(PlayerGO.transform, new Vector3(0f, 4f, -6f), new Vector3(0f, 2f, -3f));
        });
        Setup.SpecifyTermination(() => !CamMovement.Move, Play);
        //Setup.AddTimer(() => setupDuration.value, Play);

        //Play state ----------------------------------------------------------------------------------------------------------------------------------------------
        bool finishedPlaying = false;
        float startTime = 0f;
        Play.AddSpecificInitializationMethod(() =>
        {
            SpeedSliderGO.SetActive(true);
            SpeedSlider.value = FloorManager.FloorMovementSpeed;

            ScoreManager.Score = 0;
            ScoreManager.ActivateScoreText();

            PlayerManager.StartAnimation("Run");
            PlayerManager.AllowUserInput();

            FloorManager.ActivateMovement();

            AudioFBController.Play("EC_BalloonChosen");

            TokenFBController.enabled = true;

            finishedPlaying = false;

            startTime = Time.time;
        });
        Play.AddUpdateMethod(() =>
        {
            if(Time.time - startTime > 4f)
            {
                Skybox skybox = CurrentTaskLevel.TaskCam.GetComponent<Skybox>();
                skybox.material = SkyboxMaterials[Random.Range(0, SkyboxMaterials.Count - 1)];
                RenderSettings.fogColor = FogColors[SkyboxMaterials.IndexOf(skybox.material)];
                startTime = Time.time;
            }

            if (FloorManager.NumTilesSpawned > 1 && FloorManager.ActiveTiles.Count == 1)
                finishedPlaying = true;

            if (SpeedSlider != null)
                FloorManager.FloorMovementSpeed = SpeedSlider.value;

            if(InputBroker.GetKeyDown(KeyCode.A))
            {
                PlayerManager.AllowItemPickupAnimations = !PlayerManager.AllowItemPickupAnimations;
            }

            if(InputBroker.GetKeyDown(KeyCode.Q))
            {
                ScoreManager.ToggleScoreText();
                SpeedSliderGO.SetActive(!SpeedSliderGO.activeInHierarchy);
            }

        });
        Play.SpecifyTermination(() => finishedPlaying, Celebration);
        Play.AddTimer(() => playDuration.value, Celebration);

        //Celebration state ----------------------------------------------------------------------------------------------------------------------------------------------
        Celebration.AddSpecificInitializationMethod(() =>
        {
            SpeedSliderGO.SetActive(false);

            PlayerManager.FinalCelebration();
            TokenFBController.enabled = false;
            ScoreManager.DeactivateScoreText();
        });
        Celebration.AddTimer(() => celebrationDuration.value, ITI);

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);


        DefineTrialData();
        DefineFrameData();
    }


    protected override void DefineTrialStims()
    {
        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;
        trialStims = new StimGroup("TargetStim", group, CurrentTrial.TrialStimIndices);
        TrialStims.Add(trialStims);

        for (int i = 0; i < CurrentTrial.TrialStimIndices.Length; i++)
        {
            FruitRunner_StimDef stim = (FruitRunner_StimDef)trialStims.stimDefs[i];
            
            stim.StimTokenRewardMag = chooseReward(CurrentTrial.ProbabilisticTokenReward[i]);

            //Set quaddle feedback type (pos, neg, neutral) depending on token reward:
            if(stim.StimTokenRewardMag == 0)
                stim.QuaddleFeedbackType = "Neutral";
            else if(stim.StimTokenRewardMag > 0)
                stim.QuaddleFeedbackType = "Positive";
            else if (stim.StimTokenRewardMag < 0)
                stim.QuaddleFeedbackType = "Negative";
            else
                Debug.LogError("STIM TOKEN REWARD MAG IS SOMETHING OTHER THAN 1, 0, -1");

        }
    }


    public override void FinishTrialCleanup()
    {
        SpeedSliderGO.SetActive(false);
        ScoreManager.DeactivateScoreText();

        Destroy(ScoreManager);
        Destroy(PlayerGO);
        Destroy(FloorManagerGO);
        Destroy(ItemSpawnerGO);
        Destroy(MovementCirclesControllerGO);

        if (AbortCode == 0)
        {
            //TrialCompletions_Block++;
            //CurrentTaskLevel.TrialsCompleted_Task++;
            CurrentTaskLevel.CalculateBlockSummaryString();
        }
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }
    }

    public override void ResetTrialVariables()
    {

    }

    public void ResetBlockVariables()
    {

    }

    void GiveReward()
    {
        CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
        CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;
        Session.SyncBoxController?.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize);
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Trial #" + (TrialCount_InBlock + 1) + " In Block" + "</b>";
    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("PlayerPosition", () => PlayerGO == null ? "-" : PlayerGO.transform.position.ToString());
        //what else to track?
    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");

        setupDuration = ConfigUiVariables.get<ConfigNumber>("setupDuration");
        playDuration = ConfigUiVariables.get<ConfigNumber>("playDuration");
        celebrationDuration = ConfigUiVariables.get<ConfigNumber>("celebrationDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
    }



}
