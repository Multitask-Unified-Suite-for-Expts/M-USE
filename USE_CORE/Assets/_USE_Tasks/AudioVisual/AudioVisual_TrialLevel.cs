using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using AudioVisual_Namespace;
using ConfigDynamicUI;
using UnityEngine.UI;
using System.IO;
using TMPro;


public class AudioVisual_TrialLevel : ControlLevel_Trial_Template
{
    public AudioVisual_TrialDef CurrentTrial => GetCurrentTrialDef<AudioVisual_TrialDef>();
    public AudioVisual_TaskLevel CurrentTaskLevel => GetTaskLevel<AudioVisual_TaskLevel>();
    public AudioVisual_TaskDef CurrentTask => GetTaskDef<AudioVisual_TaskDef>();

    private GameObject StartButton;

    public GameObject AV_CanvasGO;

    [HideInInspector] public GameObject WaitCueGO;

    [HideInInspector] public GameObject LeftIconGO;
    [HideInInspector] public GameObject RightIconGO;

    private GameObject ChosenGO = null;
    private bool SelectionMade = false;
    private bool GotTrialCorrect;

    [HideInInspector] public AudioSource SoundAudioSource;
    [HideInInspector] public AudioClip SoundAudioClip;

    [HideInInspector] public AudioSource ClockAudioSource;
    [HideInInspector] public AudioClip ClockAudioClip;


    //Set In Inspector:
    public GameObject TimerGO;
    public TextMeshProUGUI TimerText;
    public Image TimerFill;

    //Config UI Variables:
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, touchFbDuration, tokenUpdateDuration, tokenRevealDuration, tokenFlashingDuration;


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
            {
                SoundAudioSource = gameObject.AddComponent<AudioSource>();
                SoundAudioSource.volume = 1f;
            }

            if(ClockAudioSource == null)
            {
                ClockAudioSource = gameObject.AddComponent<AudioSource>();
                ClockAudioSource.volume = 1f;
                ClockAudioSource.loop = true;
                ClockAudioClip = Resources.Load<AudioClip>("ClockTicking");
                if (ClockAudioClip == null)
                    Debug.LogError("NULL LOADING CLOCK AUDIO CLIP");
                else
                    ClockAudioSource.clip = ClockAudioClip;
            }

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
            CreateIcons();

            TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);
            TokenFBController.SetTokenBarValue(CurrentTrial.NumInitialTokens);
            TokenFBController.SetFlashingTime(tokenFlashingDuration.value);
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //------------------------------------------------------------------------------------------------------------------------
        var ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, PlayerChoice);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 15, AV_CanvasGO, true);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            LoadConfigUIVariables();

            SetTrialSummaryString();

            CurrentTaskLevel.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            TokenFBController.enabled = false;
            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
            TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);

            SetShadowType(CurrentTask.ShadowType, "AudioVisual_DirectionalLight");

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;

        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), Preparation);
        InitTrial.AddDefaultTerminationMethod(() =>
        {
            TokenFBController.enabled = true;
            Session.EventCodeManager.AddToFrameEventCodeBuffer("TokenBarVisible");
        });

        //Preparation state -------------------------------------------------------------------------------------------------------
        Preparation.AddSpecificInitializationMethod(() =>
        {
            WaitCueGO.SetActive(true);
        });
        Preparation.AddTimer(() => CurrentTrial.PreparationDuration, DisplayOptions);

        //DisplayOptions state -------------------------------------------------------------------------------------------------------
        DisplayOptions.AddSpecificInitializationMethod(() =>
        {
            LeftIconGO.SetActive(true);
            RightIconGO.SetActive(true);
        });
       DisplayOptions.AddTimer(() => CurrentTrial.DisplayOptionsDuration, PlayAudio);

        //PlayAudio state -------------------------------------------------------------------------------------------------------
        PlayAudio.AddSpecificInitializationMethod(() =>
        {
            if (SoundAudioClip == null)
                Debug.LogError("SOUND CLIP IS NULL");

            SoundAudioSource.Play();
        });
        PlayAudio.AddTimer(() => CurrentTrial.AudioClipLength, WaitPeriod);
        PlayAudio.AddDefaultTerminationMethod(() => SoundAudioSource.Stop());

        //WaitPeriod state -------------------------------------------------------------------------------------------------------
        WaitPeriod.AddSpecificInitializationMethod(() =>
        {
        });
        WaitPeriod.AddTimer(() => CurrentTrial.WaitPeriodDuration, PlayerChoice);

        //PlayerChoice state -------------------------------------------------------------------------------------------------------
        float timeRemaining = 0f;
        PlayerChoice.AddSpecificInitializationMethod(() =>
        {
            ChosenGO = null;
            SelectionMade = false;
            WaitCueGO.SetActive(false);

            if(Session.SessionDef.IsHuman)
            {
                TimerGO.SetActive(true);
                timeRemaining = CurrentTrial.ChoiceDuration;
                ClockAudioSource.Play();
                TimerText.text = timeRemaining.ToString();
                TimerFill.fillAmount = 1f;
            }


            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        PlayerChoice.AddUpdateMethod(() =>
        {
            if(timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                TimerFill.fillAmount = timeRemaining / CurrentTrial.ChoiceDuration;
            }

            TimerText.text = timeRemaining.ToString("0");

            ChosenGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
            if(ChosenGO != null)
            {
                if(ChosenGO == LeftIconGO)
                {
                    if (CurrentTrial.CorrectObject.ToLower().Contains("left"))
                    {
                        Debug.LogWarning("CORRECTLY CHOSE LEFT SIDE!");
                        GotTrialCorrect = true;
                    }
                    else
                        Debug.LogWarning("INCORRECTLY CHOSE LEFT SIDE");

                    SelectionMade = true;
                }
                else if(ChosenGO == RightIconGO)
                {
                    if (CurrentTrial.CorrectObject.ToLower().Contains("right"))
                    {
                        Debug.LogWarning("CORRECTLY CHOSE RIGHT SIDE!");
                        GotTrialCorrect = true;
                    }
                    else
                        Debug.LogWarning("INCORRECTLY CHOSE RIGHT SIDE");

                    SelectionMade = true;
                }
            }
        });
        PlayerChoice.AddTimer(() => CurrentTrial.ChoiceDuration, Feedback);
        PlayerChoice.SpecifyTermination(() => SelectionMade, Feedback);

        //Feedback state -------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.IsHuman)
            {
                TimerGO.SetActive(false);
                ClockAudioSource.Stop();
            }
            
            if (ChosenGO == null)
            {
                AudioFBController.Play("TimeRanOut");
                return;
            }

            if (GotTrialCorrect)
            {
                HaloFBController.ShowPositive(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, depth: 10f); //may need to adjust depth
                TokenFBController.AddTokens(ChosenGO, CurrentTrial.TokenGain, -3f);
            }
            else //Got wrong
            {
                HaloFBController.ShowNegative(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, depth: 10f); //may need to adjust depth
                TokenFBController.RemoveTokens(ChosenGO, CurrentTrial.TokenLoss, -3f);
            }
        });
        Feedback.AddTimer(() => CurrentTrial.FeedbackDuration, ITI);
        Feedback.SpecifyTermination(() => ChosenGO == null, ITI);
        Feedback.AddDefaultTerminationMethod(() =>
        {
            HaloFBController.DestroyAllHalos();
        });

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);

        //-----------------------------------------------------------------------------------------------------------------
        DefineTrialData();
        DefineFrameData();
    }

    public override void ResetTrialVariables()
    {
        GotTrialCorrect = false;
        SelectionMade = false;
    }

    private GameObject CreateIcon(string name, float size, Vector3 pos, string iconName, float[] color)
    {
        GameObject icon = new GameObject(name);
        icon.SetActive(false);
        icon.transform.parent = AV_CanvasGO.transform;
        icon.transform.localScale = Vector3.one;
        icon.transform.localPosition = pos;

        RectTransform rect = icon.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        Image image = icon.AddComponent<Image>();

        Sprite sprite = Resources.Load<Sprite>(iconName);

        if (sprite == null)
            Debug.LogError("SPRITE IS NULL FOR: " + iconName);
        else
            image.sprite = sprite;

        image.sprite = Resources.Load<Sprite>(iconName);
        image.color = ConvertFloatArrayToColor(color);

        return icon;
    }

    private void CreateIcons()
    {
        if (WaitCueGO != null)
            Destroy(WaitCueGO);

        WaitCueGO = CreateIcon("WaitCue", CurrentTrial.WaitCueSize, Vector3.zero, CurrentTrial.WaitCueIcon, CurrentTrial.WaitCueColor);


        if (LeftIconGO != null)
            Destroy(LeftIconGO);

        LeftIconGO = CreateIcon("LeftIcon", CurrentTrial.LeftObjectSize, CurrentTrial.LeftObjectPos, CurrentTrial.LeftObjectIcon, CurrentTrial.LeftObjectColor);


        if (RightIconGO != null)
            Destroy(RightIconGO);

        RightIconGO = CreateIcon("RightIcon", CurrentTrial.RightObjectSize, CurrentTrial.RightObjectPos, CurrentTrial.RightObjectIcon, CurrentTrial.RightObjectColor);

    }


    private void LoadAudioClip()
    {
        if (Session.UsingServerConfigs)
        {
            StartCoroutine(ServerManager.LoadAudioFromServer($"{CurrentTask.AudioClipsFolderPath}/{CurrentTrial.AudioClipName}", audioClipResult =>
            {
                if (audioClipResult != null)
                {
                    SoundAudioClip = audioClipResult;
                    SoundAudioSource.clip = SoundAudioClip;
                }
                else
                    Debug.LogError("NULL GETTING AUDIO CLIP FROM SERVER!");
            }));
        }
        else if (Session.UsingLocalConfigs)
        {
            SoundAudioClip = Session.SessionAudioController.LoadExternalWAV($"{CurrentTask.AudioClipsFolderPath}{Path.DirectorySeparatorChar}{CurrentTrial.AudioClipName}");
            if (SoundAudioClip == null)
                Debug.LogError("SOUND AUDIO CLIP IS NULL");
            SoundAudioSource.clip = SoundAudioClip;
        }
        else if(Session.UsingDefaultConfigs)
        {
            Debug.LogWarning("HAVENT IMPLMENTED LOADING AUDIO FOR DEFAULT CONFIGS YET!");
            //SoundAudioClip = Resources.Load<AudioClip>($"{Session.SessionDef.AudioClipsFolderPath}/{CurrentTrial.AudioClipName}");
            //SoundAudioSource.clip = SoundAudioClip;
        }

    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        touchFbDuration = ConfigUiVariables.get<ConfigNumber>("touchFbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Trial #" + (TrialCount_InBlock + 1) + " In Block" + "</b>" +
                             "\nCorrect Object: " + CurrentTrial.CorrectObject;
    }


    private void DefineTrialData()
    {
        TrialData.AddDatum("ChoseCorrectly", () => GotTrialCorrect);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        //FrameData.AddDatum("TrialStimShown", () => trialStims?.IsActive);
    }

}
