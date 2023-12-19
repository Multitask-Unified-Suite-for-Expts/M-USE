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
    public GameObject FloorManagerGO;
    public GameObject ItemSpawnerGO;
    public List<Material> SkyboxMaterials;

    private GameObject StartButton;

    private int SliderGainSteps;

    [HideInInspector] public ConfigNumber itiDuration, minObjectTouchDuration, maxObjectTouchDuration, sliderFlashingDuration, sliderUpdateDuration, sliderSize;

    GameObject Player;

 

    public override void DefineControlLevel()
    {
        State InitTrial = new State("IniTrial");
        State Play = new State("Play");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, Play, ITI });

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

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, Play); //Setup Handler
        //TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 15, FruitRunner_CanvasGO, false); //Enable Touch Feedback

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            //SetTrialSummaryString();

            //if (Handler.AllSelections.Count > 0)
            //    Handler.ClearSelections();
            //Handler.MinDuration = minObjectTouchDuration.value;
            //Handler.MaxDuration = maxObjectTouchDuration.value;

        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), Play, () =>
        {
            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(20f, 1 * (1f / 4), new Vector3(0f, -2f, 0f));
            //SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrial.SliderInitialValue * (1f / SliderGainSteps), new Vector3(0f, -43f, 0f));
            SliderFBController.SetSliderRectSize(new Vector2(400f, 25f));
            SliderFBController.SetUpdateDuration(sliderUpdateDuration.value);
            SliderFBController.SetFlashingDuration(sliderFlashingDuration.value);
            SliderFBController.SliderGO.SetActive(true);


            CurrentTaskLevel.TaskCam.GetComponent<Skybox>().material = SkyboxMaterials[Random.Range(0, SkyboxMaterials.Count - 1)];
            //CurrentTaskLevel.TaskCam.GetComponent<Skybox>().material = Resources.Load<Material>("Materials/6sidedCosmicCoolCloud");
            CurrentTaskLevel.TaskCam.GetComponent<Skybox>().enabled = true;
            CurrentTaskLevel.TaskCam.fieldOfView = 60;
        });

        //Play state ----------------------------------------------------------------------------------------------------------------------------------------------
        float startTime = 0f;
        Play.AddSpecificInitializationMethod(() =>
        {
            if (Player != null)
                Destroy(Player);

            Player = Instantiate(Resources.Load<GameObject>("Prefabs/Player"));

            ItemSpawnerGO.SetActive(true);
            FloorManagerGO.SetActive(true);

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();

            startTime = Time.time;
        });
        Play.AddUpdateMethod(() =>
        {
            if(Time.time - startTime >= 10f)
            {
                CurrentTaskLevel.TaskCam.GetComponent<Skybox>().material = SkyboxMaterials[Random.Range(0, SkyboxMaterials.Count - 1)];
                startTime = Time.time;
            }


        });
        Play.AddTimer(() => 500f, ITI);

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => .01f, FinishTrial);


        DefineTrialData();
        DefineFrameData();
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

    protected override void DefineTrialStims()
    {

    }
    
}
