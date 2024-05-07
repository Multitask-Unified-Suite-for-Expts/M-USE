using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using AudioVisual_Namespace;
using ContinuousRecognition_Namespace;
using ConfigDynamicUI;
using UnityEditor.UIElements;

public class AudioVisual_TrialLevel : ControlLevel_Trial_Template
{
    public AudioVisual_TrialDef CurrentTrial => GetCurrentTrialDef<AudioVisual_TrialDef>();
    public AudioVisual_TaskLevel CurrentTaskLevel => GetTaskLevel<AudioVisual_TaskLevel>();
    public AudioVisual_TaskDef CurrentTask => GetTaskDef<AudioVisual_TaskDef>();

    private GameObject StartButton;

    public GameObject AV_CanvasGO;

    [HideInInspector] public GameObject WaitCueGO;

    [HideInInspector] public bool VariablesLoaded;

    [HideInInspector] public AudioSource SoundAudioSource;
    [HideInInspector] public AudioClip SoundAudioClip;

    private PlayerViewPanel playerView;
    private GameObject playerViewParent, playerViewText;
    [HideInInspector] public List<GameObject> playerViewTextList;

    //Config UI Variables:
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State Preparation = new State("Preparation");
        State DisplayOptions = new State("DisplayOptions");
        State PlayAudio = new State("PlayAudio");
        State WaitPeriod = new State("WaitPeriod");
        State PlayerChoice = new State("PlayerChoice");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, Preparation, DisplayOptions, PlayAudio, WaitPeriod, PlayerChoice, Feedback, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            if (SoundAudioSource == null)
                SoundAudioSource = gameObject.AddComponent<AudioSource>();

            if (!Session.WebBuild)
            {
                playerView = gameObject.AddComponent<PlayerViewPanel>();
                playerViewParent = GameObject.Find("MainCameraCopy");
                playerViewTextList = new List<GameObject>();
            }

            SetControllerBlockValues();

            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(AV_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

        });

        //SETUP TRIAL state ------------------------------------------------------------------------------------------------------
        SetupTrial.AddDefaultInitializationMethod(() =>
        {
            LoadAudioClip();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //------------------------------------------------------------------------------------------------------------------------
        var ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, PlayerChoice);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 15, AV_CanvasGO, true);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (!VariablesLoaded)
                LoadConfigUIVariables();

            SetTrialSummaryString();

            CurrentTaskLevel.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            TokenFBController.enabled = false;

            SetShadowType(CurrentTask.ShadowType, "AudioVisual_DirectionalLight");

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;

        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), Preparation);
        InitTrial.AddDefaultTerminationMethod(() =>
        {
            TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);
            TokenFBController.enabled = true;
            Session.EventCodeManager.AddToFrameEventCodeBuffer("TokenBarVisible");
        });

        //Preparation state -------------------------------------------------------------------------------------------------------
        Preparation.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("PREP STATE");
        });
        Preparation.AddTimer(() => CurrentTrial.PreparationDuration, DisplayOptions);

        //DisplayOptions state -------------------------------------------------------------------------------------------------------
        DisplayOptions.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("DISPLAY OPTIONS STATE");
        });
        DisplayOptions.AddTimer(() => CurrentTrial.DisplayOptionsDuration, PlayAudio);

        //PlayAudio state -------------------------------------------------------------------------------------------------------
        PlayAudio.AddSpecificInitializationMethod(() =>
        {
            Debug.LogWarning("PLAY AUDIO STATE");
            SoundAudioSource.Play();
        });
        PlayAudio.AddTimer(() => CurrentTrial.AudioClipPlayDuration, WaitPeriod);
        PlayAudio.AddDefaultTerminationMethod(() => SoundAudioSource.Stop());

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);

        //-----------------------------------------------------------------------------------------------------------------
        DefineTrialData();
        DefineFrameData();
    }

    private void LoadAudioClip()
    {
        Debug.LogWarning("CLIP NAME: " + CurrentTrial.AudioClipName);

        if (Session.UsingServerConfigs)
        {

        }
        else if (Session.UsingLocalConfigs)
        {

        }
        else //using default configs:
        {

        }

        //set the clip!!!

    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        //displayStimDuration = ConfigUiVariables.get<ConfigNumber>("displayStimDuration");
        //chooseStimDuration = ConfigUiVariables.get<ConfigNumber>("chooseStimDuration");
        //touchFbDuration = ConfigUiVariables.get<ConfigNumber>("touchFbDuration");
        //tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        //tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        //displayResultsDuration = ConfigUiVariables.get<ConfigNumber>("displayResultsDuration");
        VariablesLoaded = true;
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    }

    void SetTrialSummaryString()
    {
        //TrialSummaryString = "<b>Trial #" + (TrialCount_InBlock + 1) + " In Block" + "</b>" +
        //                     "\nPC_Stim: " + NumPC_Trial +
        //                     "\nNew_Stim: " + NumNew_Trial +
        //                     "\nPNC_Stim: " + NumPNC_Trial;
    }

    public void SetControllerBlockValues()
    {
        //TokenFBController.SetFlashingTime(1f);
        //HaloFBController.SetPositiveHaloColor(Color.yellow);
        //HaloFBController.SetNegativeHaloColor(Color.gray);
        //HaloFBController.SetParticleHaloSize(.65f);
        //HaloFBController.SetCircleHaloIntensity(1.5f);
    }

    private void DefineTrialData()
    {
        //TrialData.AddDatum("Context", () => CurrentTrial.ContextName);
        //TrialData.AddDatum("Starfield", () => CurrentTrial.UseStarfield);
        //TrialData.AddDatum("Num_UnseenStim", () => Unseen_Stim.Count);
        //TrialData.AddDatum("PC_Stim", () => PC_String);
        //TrialData.AddDatum("New_Stim", () => New_String);
        //TrialData.AddDatum("PNC_Stim", () => PNC_String);
        //TrialData.AddDatum("StimLocations", () => Locations_String);
        //TrialData.AddDatum("ChoseCorrectly", () => GotTrialCorrect);
        //TrialData.AddDatum("CurrentTrialStims", () => TrialStimIndices);
        //TrialData.AddDatum("PC_Percentage", () => CalculatePercentagePC());
    }

    private void DefineFrameData()
    {
        //FrameData.AddDatum("ContextActive", () => ContextActive);
        //FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        //FrameData.AddDatum("TrialStimShown", () => trialStims?.IsActive);
        //FrameData.AddDatum("StarfieldActive", () => Starfield != null && Starfield.activeInHierarchy ? "Active" : "NotActive");
    }

}
