using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using FruitRunner_Namespace;
using ConfigDynamicUI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


public class FruitRunner_TrialLevel : ControlLevel_Trial_Template
{
    public FruitRunner_TrialDef CurrentTrial => GetCurrentTrialDef<FruitRunner_TrialDef>();
    public FruitRunner_TaskLevel CurrentTaskLevel => GetTaskLevel<FruitRunner_TaskLevel>();
    public FruitRunner_TaskDef CurrentTask => GetTaskDef<FruitRunner_TaskDef>();

    //Set in Inspector:
    public GameObject FruitRunner_CanvasGO;
    public List<Material> SkyboxMaterials;

    private GameObject StartButton;

    private int SliderGainSteps;

    [HideInInspector] public ConfigNumber itiDuration, minObjectTouchDuration, maxObjectTouchDuration, sliderFlashingDuration, sliderUpdateDuration, sliderSize;

    GameObject PlayerGO;
    private PlayerMovement PlayerMovement;

    public GameObject MovementCirclesControllerGO;
    public MovementCirclesController MovementCirclesController;

    public GameObject FloorManagerGO;
    public FloorManager FloorManager;

    public GameObject ItemSpawnerGO;
    public ItemSpawner ItemSpawner;


    private StimGroup trialStims;



    public override void DefineControlLevel()
    {
        State InitTrial = new State("IniTrial");
        State Setup = new State("Setup");
        State Play = new State("Play");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, Setup, Play, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();

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
            //SetTrialSummaryString();

            //if (Handler.AllSelections.Count > 0)
            //    Handler.ClearSelections();
            //Handler.MinDuration = minObjectTouchDuration.value;
            //Handler.MaxDuration = maxObjectTouchDuration.value;

            TokenFBController.enabled = false;

        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), Setup, () =>
        {
            TokenFBController.AdjustTokenBarSizing(100);
            TokenFBController.SetRevealTime(.15f);
            TokenFBController.SetUpdateTime(.25f);

            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(25f, 1 * (1f / 4), new Vector3(0f, -10f, 0f));
            //////SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrial.SliderInitialValue * (1f / SliderGainSteps), new Vector3(0f, -43f, 0f));
            SliderFBController.SetSliderRectSize(new Vector2(400f, 25f));
            SliderFBController.SetUpdateDuration(sliderUpdateDuration.value);
            SliderFBController.SetFlashingDuration(sliderFlashingDuration.value);
            //SliderFBController.SliderGO.SetActive(true);


            //CurrentTaskLevel.TaskCam.GetComponent<Skybox>().material = SkyboxMaterials[Random.Range(0, SkyboxMaterials.Count - 1)];
            CurrentTaskLevel.TaskCam.GetComponent<Skybox>().material = Resources.Load<Material>("Materials/FS003_Night");
            CurrentTaskLevel.TaskCam.GetComponent<Skybox>().enabled = true;
            CurrentTaskLevel.TaskCam.fieldOfView = 60;
        });

        //Setup state ----------------------------------------------------------------------------------------------------------------------------------------------
        Setup.AddSpecificInitializationMethod(() =>
        {
            if (PlayerGO == null)
            {
                PlayerGO = Instantiate(Resources.Load<GameObject>("Prefabs/Player"));
                PlayerGO.name = "Player";
                PlayerMovement = PlayerGO.GetComponent<PlayerMovement>();
                PlayerMovement.TokenFbController = TokenFBController;
            }

            if (ItemSpawner == null)
            {
                ItemSpawnerGO = new GameObject("ItemSpawner");
                ItemSpawner = ItemSpawnerGO.AddComponent<ItemSpawner>();
            }

            if (FloorManager == null)
            {
                FloorManagerGO = new GameObject("FloorManager");
                FloorManager = FloorManagerGO.AddComponent<FloorManager>();
            }

            ItemSpawner.AddToQuaddleList(trialStims.stimDefs);

            ItemSpawner.gameObject.SetActive(true);
            FloorManager.gameObject.SetActive(true);

            PlayerMovement.StartAnimation("idle");
            PlayerMovement.DisableUserInput();
        });
        Setup.AddTimer(() => 1f, Play);

        //Play state ----------------------------------------------------------------------------------------------------------------------------------------------
        Play.AddSpecificInitializationMethod(() =>
        {
            if (MovementCirclesController == null)
            {
                MovementCirclesControllerGO = new GameObject("MovementCirclesController");
                MovementCirclesController = MovementCirclesControllerGO.AddComponent<MovementCirclesController>();
                MovementCirclesController.SetupMovementCircles(FruitRunner_CanvasGO.GetComponent<Canvas>(), PlayerGO);
            }

            PlayerMovement.StartAnimation("Run");
            PlayerMovement.AllowUserInput();

            FloorManager.ActivateMovement();

            AudioFBController.Play("EC_BalloonChosen");

            TokenFBController.enabled = true;
        });
        //Play.AddUpdateMethod(() =>
        //{

        //});
        Play.AddTimer(() => 5000, ITI, () =>
        {
            TokenFBController.enabled = false;
        });

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => .01f, FinishTrial);


        DefineTrialData();
        DefineFrameData();
    }


    protected override void DefineTrialStims()
    {
        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;
        trialStims = new StimGroup("TargetStim", group, CurrentTrial.TrialStimIndices);
        TrialStims.Add(trialStims);
    }

    private void CalculateSliderSteps()
    {
        foreach (int sliderGain in CurrentTrial.SliderGain)
        {
            SliderGainSteps += sliderGain;
        }
        SliderGainSteps += CurrentTrial.SliderInitialValue;
    }


    public override void FinishTrialCleanup()
    {
        SliderFBController.SliderGO.SetActive(false);
        SliderFBController.SliderHaloGO.SetActive(false);

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
        SliderGainSteps = 0;
        SliderFBController.ResetSliderBarFull();
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
        //what else to track?
    }

    private void LoadConfigUIVariables()
    {
        //minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        //maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        //itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        //sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        //sliderFlashingDuration = ConfigUiVariables.get<ConfigNumber>("sliderFlashingDuration");
        //sliderUpdateDuration = ConfigUiVariables.get<ConfigNumber>("sliderUpdateDuration");
    }

}
