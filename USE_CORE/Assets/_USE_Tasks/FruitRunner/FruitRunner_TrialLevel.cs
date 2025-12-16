using UnityEngine;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using FruitRunner_Namespace;
using ConfigDynamicUI;
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

    [HideInInspector] public ConfigNumber timeBeforeChoiceStarts, totalChoiceDuration, setupDuration, playDuration, celebrationDuration, itiDuration;

    GameObject PlayerGO;
    private FR_PlayerManager PlayerManager;

    public GameObject MovementCirclesControllerGO;
    public MovementCirclesController MovementCirclesController;

    public GameObject FloorManagerGO;
    public FR_FloorManager FloorManager;

    public GameObject ItemManagerGO;
    public FR_ItemManager ItemManager;

    public SpawnHalfCircle CircleSpawner;
    public GameObject CircleSpawnerGO;


    private StimGroup trialStims;

    private CameraIntroMovement CamMovement;

    private bool UsingBananas;

    //DATA:
    [HideInInspector] public int TargetsHit_Trial;
    [HideInInspector] public int TargetsMissed_Trial;
    [HideInInspector] public int DistractorsHit_Trial;
    [HideInInspector] public int DistractorsAvoided_Trial;
    [HideInInspector] public int BlockadesHit_Trial;
    [HideInInspector] public int BlockadesAvoided_Trial;

    [HideInInspector] public int TargetsHit_Block;
    [HideInInspector] public int TargetsMissed_Block;
    [HideInInspector] public int DistractorsHit_Block;
    [HideInInspector] public int DistractorsAvoided_Block;
    [HideInInspector] public int BlockadesHit_Block;
    [HideInInspector] public int BlockadesAvoided_Block;

    [HideInInspector] public int Score_Block;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State Setup = new State("Setup");
        State Play = new State("Play");
        State Celebration = new State("Celebration");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, Setup, Play, Celebration, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            SubscribeToFrEvents();

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

            if (CircleSpawnerGO != null)
                Destroy(CircleSpawnerGO);
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //----------------------------------------------------------------------------------------------------------------------------------------------
        if (Session.SessionDef.SelectionType.ToLower().Contains("gaze"))
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, InitTrial, Play);
        else
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", Session.SessionDef.SelectionType, Session.MouseTracker, InitTrial, Play);

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            LoadConfigUIVariables();

            SetTrialSummaryString();

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearChoices();

            SelectionHandler.TimeBeforeChoiceStarts = Session.SessionDef.StartButtonSelectionDuration;
            SelectionHandler.TotalChoiceDuration = Session.SessionDef.StartButtonSelectionDuration;

            TokenFBController.enabled = false;
            TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);
            TokenFBController.SetTokenBarValue(CurrentTrial.NumInitialTokens);

        });
        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), Setup, () =>
        {
            SelectionHandler.TimeBeforeChoiceStarts = timeBeforeChoiceStarts.value;
            SelectionHandler.TotalChoiceDuration = totalChoiceDuration.value;

            TokenFBController.AdjustTokenBarSizing(100);
            TokenFBController.SetRevealTime(.1f);
            TokenFBController.SetUpdateTime(.2f);

            Skybox skybox = CurrentTaskLevel.TaskCam.GetComponent<Skybox>();
            skybox.material = CurrentTrial.SkyboxName.ToLower() == "random" ? SkyboxMaterials[Random.Range(0, SkyboxMaterials.Count - 1)] : Resources.Load<Material>("Materials/" + CurrentTrial.SkyboxName);
            skybox.enabled = true;

            RenderSettings.fogColor = FogColors[SkyboxMaterials.IndexOf(skybox.material)];

            CurrentTaskLevel.TaskCam.fieldOfView = 60;

            HaloFBController.SetPositiveParticleHaloColor(Color.green);
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

            SetUsingBananas();

            ItemManagerGO = new GameObject("ItemManager");
            ItemManager = ItemManagerGO.AddComponent<FR_ItemManager>();
            ItemManager.SetupQuaddleList(trialStims.stimDefs);
            ItemManager.SetQuaddleGeneralPositions(CurrentTrial.TrialStimGeneralPositions);
            ItemManager.SetSpawnOrder(CurrentTrial.TrialGroup_InSpawnOrder);
            ItemManager.BananaTokenGain = CurrentTrial.BananaTokenGain;
            ItemManager.BlockadeTokenLoss = CurrentTrial.BlockadeTokenLoss;
            ItemManager.StimFaceCamera = CurrentTrial.StimFacingCamera;
            ItemManager.gameObject.SetActive(true);

            FloorManagerGO = new GameObject("FloorManager");
            FloorManager = FloorManagerGO.AddComponent<FR_FloorManager>();
            FloorManager.SetTotalTiles(CurrentTrial.TrialGroup_InSpawnOrder.Length, CurrentTrial.NumGroups);
            FloorManager.FloorMovementSpeed = CurrentTrial.FloorMovementSpeed;
            FloorManager.TileScale_Z = CurrentTrial.FloorTileLength;
            FloorManager.gameObject.SetActive(true);

            CamMovement = Camera.main.gameObject.AddComponent<CameraIntroMovement>();
            CamMovement.StartMovement(PlayerGO.transform, new Vector3(0f, 4f, -6f), new Vector3(0f, 2f, -3f));
        });
        Setup.SpecifyTermination(() => !CamMovement.Move, Play);
        //Setup.AddTimer(() => setupDuration.value, Play);

        //Play state ----------------------------------------------------------------------------------------------------------------------------------------------
        bool finishedPlaying = false;
        Play.AddSpecificInitializationMethod(() =>
        {
            //Determine next state depending on SkipCelebrationState boolean:
            StateAfterDelay = CurrentTrial.SkipCelebrationState ? ITI : Celebration;
            DelayDuration = 0;

            if (CurrentTrial.ShowUI)
            {
                SpeedSliderGO.SetActive(true);
                SpeedSlider.value = FloorManager.FloorMovementSpeed;

                ScoreManager.Score = 0;
                ScoreManager.ActivateScoreText();
            }

            PlayerManager.StartAnimation("Run");
            PlayerManager.AllowUserInput();

            FloorManager.ActivateMovement();

            AudioFBController.Play("EC_BalloonChosen");

            TokenFBController.enabled = true;

            finishedPlaying = false;

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearChoices();
        });
        Play.AddUpdateMethod(() =>
        {
            if (FloorManager.NumTilesSpawned > 1 && FloorManager.ActiveTiles.Count == 1)
                finishedPlaying = true;

            if (CurrentTrial.ShowUI && SpeedSlider != null)
                FloorManager.FloorMovementSpeed = SpeedSlider.value;

            if (InputBroker.GetKeyDown(KeyCode.A))
            {
                PlayerManager.AllowItemPickupAnimations = !PlayerManager.AllowItemPickupAnimations;
            }

            if (InputBroker.GetKeyDown(KeyCode.Q))
            {
                ScoreManager.ToggleScoreText();
                SpeedSliderGO.SetActive(!SpeedSliderGO.activeInHierarchy);
            }

            if (SelectionHandler.SuccessfulChoices.Count > 0)
            {
                GameObject lastSuccessful = SelectionHandler.SuccessfulChoices[0].SelectedGameObject;

                if (SelectedAMovementCircle(lastSuccessful))
                {
                    PlayerManager.MovementCirclesController.HandleCircleClicked(lastSuccessful);
                }
                SelectionHandler.ClearChoices();
            }

        });
        Play.SpecifyTermination(() => finishedPlaying, Delay);
        Play.AddTimer(() => playDuration.value, Delay);
        Play.AddDefaultTerminationMethod(() =>
        {
            SpeedSliderGO.SetActive(false);
            TokenFBController.enabled = false;
            ScoreManager.DeactivateScoreText();
        });

        //Celebration state ----------------------------------------------------------------------------------------------------------------------------------------------
        Celebration.AddSpecificInitializationMethod(() =>
        {
            PlayerManager.FinalCelebration();
            SpawnQuaddleCircle();
        });
        Celebration.AddTimer(() => celebrationDuration.value, ITI);
        Celebration.AddDefaultTerminationMethod(() =>
        {
            CircleSpawner.DestroySpawnedObjects();
            PlayerManager.DestroyFinalPlane();
        });

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);


        DefineTrialData();
        DefineFrameData();
    }

    private bool SelectedAMovementCircle(GameObject selectedGO)
    {
        if (selectedGO == PlayerManager.MovementCirclesController.LeftCircleGO
            ||
            selectedGO == PlayerManager.MovementCirclesController.MiddleCircleGO
            ||
            selectedGO == PlayerManager.MovementCirclesController.RightCircleGO)
        {
            return true;
        }

        return false;
    }

    private void SpawnQuaddleCircle()
    {
        //Circle Spawner:
        CircleSpawnerGO = new GameObject("CircleSpawner");
        CircleSpawnerGO.transform.position = Vector3.zero;
        CircleSpawnerGO.transform.localScale = Vector3.one;
        CircleSpawner = CircleSpawnerGO.AddComponent<SpawnHalfCircle>();
        List<GameObject> prefabList = new List<GameObject>() { };
        foreach (var stim in trialStims.stimDefs)
            prefabList.Add(stim.StimGameObject);
        CircleSpawner.SetPrefabs(prefabList);
        if (!UsingBananas)
            CircleSpawner.SpawnObjectsInArch();
    }

    public void SetUsingBananas()
    {
        //If using bananas, mark the banana boolean true for the PlayerManager so it wont instantiate quaddles/finalPlane at the celebration
        for (int i = 0; i < CurrentTrial.TrialGroup_InSpawnOrder.Length; i++)
        {
            for (int j = 0; j < CurrentTrial.TrialGroup_InSpawnOrder[i].Length; j++)
            {
                if (CurrentTrial.TrialGroup_InSpawnOrder[i][j] == -3)
                {
                    UsingBananas = true;
                    PlayerManager.UsingBananas = true;
                }
            }
        }
    }

    public override void OnTokenBarFull()
    {
        if(Session.SyncBoxController != null)
        {
            CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
            CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;

            StartCoroutine(Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize));
        }
    }

    private void SubscribeToFrEvents()
    {
        FR_EventManager.OnPlayerShift += PlayerShift;
        FR_EventManager.OnTargetHit += TargetHit;
        FR_EventManager.OnTargetMissed += TargetMissed;
        FR_EventManager.OnDistractorHit += DistractorHit;
        FR_EventManager.OnDistractorAvoided += DistractorAvoided;
        FR_EventManager.OnBlockadeAvoided += BlockadeAvoided;
        FR_EventManager.OnBlockadeHit += BlockadeHit;
    }


    public void PlayerShift(string from, string to)
    {
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["ShiftFrom" + from + "To" + to]);
    }

    public void TargetHit(string pos)
    {
        TargetsHit_Trial++;
        TargetsHit_Block++;
        CurrentTaskLevel.TargetsHit_Task++;
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetHit" + pos]);

    }
    public void TargetMissed(string pos)
    {
        TargetsMissed_Trial++;
        TargetsMissed_Block++;
        CurrentTaskLevel.TargetsMissed_Task++;
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetMissed" + pos]);

    }
    public void DistractorHit(string pos)
    {
        DistractorsHit_Trial++;
        DistractorsHit_Block++;
        CurrentTaskLevel.DistractorsHit_Task++;
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["DistractorHit" + pos]);

    }
    public void DistractorAvoided(string pos)
    {
        DistractorsAvoided_Trial++;
        DistractorsAvoided_Block++;
        CurrentTaskLevel.DistractorsAvoided_Task++;
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["DistractorAvoided" + pos]);
    }
    public void BlockadeHit(string pos)
    {
        BlockadesHit_Trial++;
        BlockadesHit_Block++;
        CurrentTaskLevel.BlockadesHit_Task++;
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["BlockadeHit" + pos]);

    }
    public void BlockadeAvoided(string pos)
    {
        BlockadesAvoided_Block++;
        BlockadesAvoided_Trial++;
        CurrentTaskLevel.BlockadesAvoided_Task++;
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["BlockadeAvoided" + pos]);
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
        }
    }


    public override void FinishTrialCleanup()
    {
        Score_Block += ScoreManager.Score;

        SpeedSliderGO.SetActive(false);
        ScoreManager.DeactivateScoreText();

        Destroy(PlayerGO);
        Destroy(FloorManagerGO);
        Destroy(ItemManagerGO);
        Destroy(MovementCirclesControllerGO);

        if (AbortCode == 0)
        {
            //TrialCompletions_Block++;
            //CurrentTaskLevel.TrialsCompleted_Task++;
            CurrentTaskLevel.SetBlockSummaryString();
        }
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }
    }

    public override void ResetTrialVariables()
    {
        TargetsHit_Trial = 0;
        TargetsMissed_Trial = 0;
        DistractorsHit_Trial = 0;
        DistractorsAvoided_Trial = 0;
        BlockadesHit_Trial = 0;
        BlockadesAvoided_Trial = 0;
    }

    public void ResetBlockVariables()
    {
        TargetsHit_Block = 0;
        TargetsMissed_Block = 0;
        DistractorsHit_Block = 0;
        DistractorsAvoided_Block = 0;
        BlockadesHit_Block = 0;
        BlockadesAvoided_Block = 0;
    }


    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) + " In Block" +
            "\nTargetsHit: " + TargetsHit_Trial +
            "\nTargetsMissed: " + TargetsMissed_Trial +
            "\nDistractorsHit: " + DistractorsHit_Trial +
            "\nDistractorsAvoided: " + DistractorsAvoided_Trial;
    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);

        //Data values:
        TrialData.AddDatum("TargetsHit", () => TargetsHit_Trial);
        TrialData.AddDatum("TargetsMissed", () => TargetsMissed_Trial);
        TrialData.AddDatum("DistractorsHit", () => DistractorsHit_Trial);
        TrialData.AddDatum("DistractorsAvoided", () => DistractorsAvoided_Trial);
        TrialData.AddDatum("BlockadesHit", () => BlockadesHit_Trial);
        TrialData.AddDatum("BlockadesAvoided", () => BlockadesAvoided_Trial);

    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("PlayerPosition", () => PlayerGO == null ? "-" : PlayerGO.transform.position.ToString());
        //what else to track?
    }

    private void LoadConfigUIVariables()
    {
        timeBeforeChoiceStarts = ConfigUiVariables.get<ConfigNumber>("timeBeforeChoiceStarts");
        totalChoiceDuration = ConfigUiVariables.get<ConfigNumber>("totalChoiceDuration");

        setupDuration = ConfigUiVariables.get<ConfigNumber>("setupDuration");
        playDuration = ConfigUiVariables.get<ConfigNumber>("playDuration");
        celebrationDuration = ConfigUiVariables.get<ConfigNumber>("celebrationDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
    }

    private void OnDestroy()
    {
        //UnSubscribe from Events:
        FR_EventManager.OnPlayerShift -= PlayerShift;
        FR_EventManager.OnTargetHit -= TargetHit;
        FR_EventManager.OnTargetMissed -= TargetMissed;
        FR_EventManager.OnDistractorHit -= DistractorHit;
        FR_EventManager.OnDistractorAvoided -= DistractorAvoided;
        FR_EventManager.OnBlockadeAvoided -= BlockadeAvoided;
        FR_EventManager.OnBlockadeHit -= BlockadeHit;
        
    }


}
