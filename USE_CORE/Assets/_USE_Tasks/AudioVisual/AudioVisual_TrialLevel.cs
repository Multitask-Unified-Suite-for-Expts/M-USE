using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using AudioVisual_Namespace;
using ConfigDynamicUI;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;


public class AudioVisual_TrialLevel : ControlLevel_Trial_Template
{
    public AudioVisual_TrialDef CurrentTrial => GetCurrentTrialDef<AudioVisual_TrialDef>();
    public AudioVisual_TaskLevel CurrentTaskLevel => GetTaskLevel<AudioVisual_TaskLevel>();
    public AudioVisual_TaskDef CurrentTask => GetTaskDef<AudioVisual_TaskDef>();

    private GameObject StartButton;

    public GameObject AV_CanvasGO;

    //Set In Inspector:
    public GameObject ScoreTextGO;
    public GameObject NumCorrectTextGO;

    [HideInInspector] public GameObject WaitCueGO;
    [HideInInspector] public GameObject LeftIconGO;
    [HideInInspector] public GameObject RightIconGO;

    [HideInInspector] public Sprite WaitCue_Sprite;
    [HideInInspector] public Sprite LeftIcon_Sprite;
    [HideInInspector] public Sprite RightIcon_Sprite;

    private GameObject ChosenGO = null;
    private bool SelectionMade = false;
    private bool GotTrialCorrect;

    [HideInInspector] public AudioSource NoiseAudioSource;
    [HideInInspector] public AudioClip SoundAudioClip;

    public GameObject FeedbackPanelGO;
    public TextMeshProUGUI FeedbackText;

    //Config UI Variables:
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, touchFbDuration, tokenUpdateDuration, tokenRevealDuration, tokenFlashingDuration;


    [HideInInspector] public int NumCorrect_Block, NumTbCompletions_Block; //DONE
    [HideInInspector] public float AvgTimeToChoice_Block, TimeToCompletion_Block, TimeToCompletion_StartTime;
    [HideInInspector] public List<float> TimeToChoice_Block;

    private int score;
    [HideInInspector]
    public int Score
    {
        get
        {
            return score;
        }
    }

    //internal data:
    private int Correct_Block;
    private int Completed_Block;



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
            if (NoiseAudioSource == null)
            {
                NoiseAudioSource = gameObject.AddComponent<AudioSource>();
                NoiseAudioSource.volume = 1f;
            }


            if(Session.SessionDef.IsHuman)
            {
                Session.TimerController.CreateTimer(AV_CanvasGO.transform);
                Session.TimerController.SetVisibilityOnOffStates(PlayerChoice, PlayerChoice);
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

            //Set timer duration for the trial:
            if(Session.SessionDef.IsHuman)
                Session.TimerController.SetDuration(CurrentTrial.ChoiceDuration);
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //------------------------------------------------------------------------------------------------------------------------
        var ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, PlayerChoice);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 20, AV_CanvasGO, false);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            ShotgunHandler.SelectablePeriod = true;

            LoadConfigUIVariables();

            SetTrialSummaryString();

            CurrentTaskLevel.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            TokenFBController.enabled = false;
            SetTokenValues();

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
            Session.EventCodeManager.SendCodeImmediate("TokenBarVisible");

            if(Session.SessionDef.IsHuman)
            {
                SetText();
                ScoreTextGO.SetActive(true);
                NumCorrectTextGO.SetActive(true);
            }
        });

        //Preparation state -------------------------------------------------------------------------------------------------------
        Preparation.AddSpecificInitializationMethod(() =>
        {
            ShotgunHandler.SelectablePeriod = false;

            WaitCueGO.SetActive(true);
            Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["WaitCueActive"]);

        });
        Preparation.AddTimer(() => CurrentTrial.PreparationDuration, DisplayOptions);

        //DisplayOptions state -------------------------------------------------------------------------------------------------------
        DisplayOptions.AddSpecificInitializationMethod(() =>
        {
            LeftIconGO.SetActive(true);
            RightIconGO.SetActive(true);
            Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["LeftObjectActive"]);
            Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["RightObjectActive"]);
        });
        DisplayOptions.AddTimer(() => CurrentTrial.DisplayOptionsDuration, PlayAudio);

        //PlayAudio state -------------------------------------------------------------------------------------------------------
        PlayAudio.AddSpecificInitializationMethod(() =>
        {
            if (SoundAudioClip == null)
                Debug.LogError("SOUND CLIP IS NULL");

            NoiseAudioSource.Play();
        });
        PlayAudio.AddTimer(() => CurrentTrial.AudioClipLength, WaitPeriod);
        PlayAudio.AddDefaultTerminationMethod(() => NoiseAudioSource.Stop());

        //WaitPeriod state -------------------------------------------------------------------------------------------------------
        WaitPeriod.AddTimer(() => CurrentTrial.WaitPeriodDuration, PlayerChoice);

        //PlayerChoice state -------------------------------------------------------------------------------------------------------
        PlayerChoice.AddSpecificInitializationMethod(() =>
        {
            ShotgunHandler.SelectablePeriod = true;

            ChosenGO = null;
            SelectionMade = false;
            WaitCueGO.SetActive(false);

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        PlayerChoice.AddUpdateMethod(() =>
        {
            ChosenGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
            if(ChosenGO != null)
            {
                if(ChosenGO == LeftIconGO)
                {
                    if (CurrentTrial.CorrectObject.ToLower().Contains("left"))
                    {
                        UpdateScore(true);
                        GotTrialCorrect = true;
                    }
                    else
                        UpdateScore(false);
                    

                    SelectionMade = true;
                }
                else if(ChosenGO == RightIconGO)
                {
                    if (CurrentTrial.CorrectObject.ToLower().Contains("right"))
                    {
                        UpdateScore(true);
                        GotTrialCorrect = true;
                    }
                    else
                        UpdateScore(false);

                    SelectionMade = true;
                }
            }
        });
        PlayerChoice.AddTimer(() => CurrentTrial.ChoiceDuration, Feedback);
        PlayerChoice.SpecifyTermination(() => SelectionMade, Feedback);

        //Feedback state -------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTrial.ShowTextFeedback)
                FeedbackPanelGO.SetActive(true);
            
            if (ChosenGO == null)
            {
                FeedbackText.text = "Time Ran Out!";
                AudioFBController.Play("NegativeShow");
                return;
            }

            if (GotTrialCorrect)
            {
                FeedbackText.text = "You Got It!";
                HaloFBController.ShowPositive(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, depth: 10f); //may need to adjust depth
                TokenFBController.AddTokens(ChosenGO, CurrentTrial.TokenGain, -3f);
            }
            else //Got wrong
            {
                FeedbackText.text = "Wrong Choice!";
                HaloFBController.ShowNegative(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, depth: 10f); //may need to adjust depth
                TokenFBController.RemoveTokens(ChosenGO, CurrentTrial.TokenLoss, -3f);
            }
        });
        Feedback.AddTimer(() => CurrentTrial.FeedbackDuration, ITI);
        //Feedback.SpecifyTermination(() => ChosenGO == null, ITI);
        Feedback.AddDefaultTerminationMethod(() =>
        {
            FeedbackPanelGO.SetActive(false);
            HaloFBController.DestroyAllHalos();

            if(Session.SessionDef.IsHuman)
            {
                ScoreTextGO.SetActive(false);
                NumCorrectTextGO.SetActive(false);
            }
        });

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);

        //-----------------------------------------------------------------------------------------------------------------
        DefineTrialData();
        DefineFrameData();
    }

    private void SetText()
    {
        ScoreTextGO.GetComponent<TextMeshProUGUI>().text = $"SCORE:  {score}";
        NumCorrectTextGO.GetComponent<TextMeshProUGUI>().text = $"CORRECT: {Correct_Block}/{Completed_Block}";
    }

    private void UpdateScore(bool increase)
    {
        //Score
        int change = (TrialCount_InBlock + 1) * 100;
        if (!increase)
            change = -change;
        score += change;

        //Correct
        if (increase)
            Correct_Block++;

        Completed_Block++;

        SetText();
    }

    public override void FinishTrialCleanup()
    {
        ScoreTextGO.SetActive(false);
        NumCorrectTextGO.SetActive(false);

        if (AbortCode == 0)
        {
            CurrentTaskLevel.TrialsCompleted_Task++;

            if (GotTrialCorrect)
            {
                NumCorrect_Block++;
                CurrentTaskLevel.TrialsCorrect_Task++;
            }

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
        GotTrialCorrect = false;
        SelectionMade = false;
    }

    public void ResetBlockVariables()
    {
        //internal:
        Completed_Block = 0;
        Correct_Block = 0;
        score = 0;

        NumCorrect_Block = 0;
        NumTbCompletions_Block = 0;
        TimeToChoice_Block.Clear();
        AvgTimeToChoice_Block = 0;
        TimeToCompletion_Block = 0;
    }

    private void SetTokenValues()
    {
        TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);
        TokenFBController.SetTokenBarValue(CurrentTrial.NumInitialTokens);
        TokenFBController.SetRevealTime(tokenRevealDuration.value);
        TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
        TokenFBController.SetFlashingTime(tokenFlashingDuration.value);
    }

    public override void OnTokenBarFull()
    {
        NumTbCompletions_Block++;
        CurrentTaskLevel.TokenBarCompletions_Task++;

        CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
        CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;

        Session.SyncBoxController?.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize);
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

        string iconFilePath = $"{Session.SessionDef.ContextExternalFilePath}/{iconName}";

        StartCoroutine(LoadIcon(iconFilePath, iconName, image, color));

        return icon;
    }

    private IEnumerator LoadIcon(string filePath, string iconName, Image image, float[] color)
    {
        Texture2D tex = null;

        if(Session.UsingDefaultConfigs) //For default configs, need to load sprite directly, not a texture
        {
            if (iconName.Contains(".")) //remove the .png if they included it in config
                iconName = iconName.Split('.')[0];

            Sprite sprite = Resources.Load<Sprite>(iconName);
            if (sprite == null)
                Debug.LogError("SPRITE IS NULL!");
            else
                image.sprite = sprite;
        }
        else
        {
            yield return LoadTexture(filePath, result =>
            {
                if (result != null)
                {
                    tex = result;
                    image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                else
                    Debug.LogWarning("TEX RESULT IS NULL!");
            });
        }

        image.color = ConvertFloatArrayToColor(color);
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
                    NoiseAudioSource.clip = SoundAudioClip;
                }
                else
                    Debug.LogError("NULL GETTING AUDIO CLIP FROM SERVER!");
            }));
        }
        else if (Session.UsingLocalConfigs)
        {
            SoundAudioClip = Session.SessionAudioController.LoadExternalWAV($"{CurrentTask.AudioClipsFolderPath}{Path.DirectorySeparatorChar}{CurrentTrial.AudioClipName}");
            if (SoundAudioClip == null)
                Debug.LogError("SOUND AUDIO CLIP IS NULL WHEN LOADING FROM LOCAL FOLDER");
            else
                NoiseAudioSource.clip = SoundAudioClip;
        }
        else if(Session.UsingDefaultConfigs)
        {
            //the .wav files inside unity resources folder dont contain .wav so remove it if its there:
            if(CurrentTrial.AudioClipName.Contains("."))
                CurrentTrial.AudioClipName = CurrentTrial.AudioClipName.Split('.')[0];
            
            SoundAudioClip = Resources.Load<AudioClip>($"{CurrentTask.AudioClipsFolderPath}/{CurrentTrial.AudioClipName}");
            if (SoundAudioClip == null)
                Debug.LogError("CLIP IS NULL WHEN LOADING FROM INTERNAL RESOURCES FOLDER");
            else
                NoiseAudioSource.clip = SoundAudioClip;
        }

    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        touchFbDuration = ConfigUiVariables.get<ConfigNumber>("touchFbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        tokenFlashingDuration = ConfigUiVariables.get<ConfigNumber>("tokenFlashingDuration");
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) + " In Block" +
                             "\nCorrect Object: " + CurrentTrial.CorrectObject +
                             "\nDifficulty Level: " + CurrentTrial.DifficultyLevel;
    }


    private void DefineTrialData()
    {
        TrialData.AddDatum("ChoseCorrectly", () => GotTrialCorrect);
        TrialData.AddDatum("DifficultyLevel", () => CurrentTrial.DifficultyLevel);

    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButtonActive", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("WaitCueActive", () => WaitCueGO != null && WaitCueGO.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("LeftIconActive", () => LeftIconGO != null && LeftIconGO.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("RightIconActive", () => RightIconGO != null && RightIconGO.activeInHierarchy ? "Active" : "NotActive");

    }

}
